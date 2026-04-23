using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using MockPaste.Application;
using MockPaste.Core.Generators;
using MockPaste.Core.Models;
using MockPaste.Infrastructure;
using MockPaste.Infrastructure.Native;
using MockPaste.UI.Popup;
using MockPaste.UI.Settings;
using MockPaste.UI.Tray;

namespace MockPaste;

public partial class App : System.Windows.Application
{
    private static Mutex? _instanceMutex;

    private IServiceProvider? _services;
    private HotkeyManager? _hotkeyManager;
    private TrayIconManager? _trayIcon;
    private PopupWindow? _popup;
    private PasteOrchestrator? _orchestrator;
    private SettingsService? _settingsService;
    private IntPtr _lastForegroundWindow;
    private SettingsWindow? _settingsWindow;

    // Resolved from DI — kept as properties for convenience in event handlers.
    private AppSettings Settings => _services!.GetRequiredService<AppSettings>();
    private HistoryService History => _services!.GetRequiredService<HistoryService>();

    protected override void OnStartup(StartupEventArgs e)
    {
        bool createdNew;
        try
        {
            _instanceMutex = new Mutex(true, @"Global\MockPaste_SingleInstance", out createdNew);
        }
        catch (AbandonedMutexException)
        {
            createdNew = true;
        }

        if (!createdNew)
        {
            MessageBox.Show("MockPaste is already running.", "MockPaste",
                MessageBoxButton.OK, MessageBoxImage.Information);
            _instanceMutex?.Dispose();
            Shutdown(0);
            return;
        }

        base.OnStartup(e);
        InitializeLogging();
        AppLogger.Information("MockPaste starting");

        try
        {
            // Load settings first so they can be registered as a singleton.
            _settingsService = new SettingsService();
            var settings = _settingsService.Load();

            _services = BuildServices(settings);

            ThemeService.Apply(settings.Theme);
            StartupService.Apply(settings.LaunchAtStartup);

            _orchestrator = _services.GetRequiredService<PasteOrchestrator>();

            InitializeTray();
            InitializeHotkey();
            InitializePopup();

            // Subscribe only after successful init so we don't leak if startup fails.
            SystemEvents.UserPreferenceChanged += OnSystemThemeChanged;

            AppLogger.Information("MockPaste started successfully");
        }
        catch (Exception ex)
        {
            AppLogger.Fatal("Failed to start MockPaste", ex);
            MessageBox.Show($"Failed to start MockPaste:\n{ex.Message}", "MockPaste Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    private static ServiceProvider BuildServices(AppSettings settings)
    {
        var services = new ServiceCollection();

        // Infrastructure
        services.AddSingleton<IAppLogger>(AppLogger.Instance);
        services.AddSingleton<ClipboardService>();
        services.AddSingleton<InputSimulationService>();

        // Core
        services.AddSingleton(settings);
        services.AddSingleton(sp => GeneratorRegistry.CreateDefault(sp.GetRequiredService<IAppLogger>()));
        services.AddSingleton(sp => new HistoryService(sp.GetRequiredService<AppSettings>().HistorySize));

        // Application
        services.AddSingleton<PasteOrchestrator>();

        return services.BuildServiceProvider();
    }

    private static void InitializeLogging()
    {
        var logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MockPaste", "logs");
        AppLogger.Initialize(logDir);
    }

    private void InitializeTray()
    {
        _trayIcon = new TrayIconManager();
        _trayIcon.OnSettingsClicked += ShowSettings;
        _trayIcon.OnExitClicked += () => Shutdown();
        _trayIcon.EnabledChanged += enabled => ApplyHotkey(Settings);
    }

    private void InitializeHotkey()
    {
        _hotkeyManager = new HotkeyManager();
        _hotkeyManager.Initialize();
        _hotkeyManager.HotkeyPressed += OnHotkeyPressed;

        if (!_hotkeyManager.Register(Settings.Hotkey))
        {
            AppLogger.Warning("Default hotkey could not be registered");
        }
    }

    private void InitializePopup()
    {
        _popup = new PopupWindow(
            _services!.GetRequiredService<GeneratorRegistry>(),
            _services!.GetRequiredService<HistoryService>());
        _popup.FormatSelected += OnFormatSelected;
        _popup.HistoryItemSelected += OnHistoryItemSelected;
    }

    private void OnHotkeyPressed()
    {
        if (_trayIcon is { IsEnabled: false })
        {
            return;
        }

        _lastForegroundWindow = NativeMethods.GetForegroundWindow();
        AppLogger.Debug($"Showing popup, target window: {_lastForegroundWindow}");
        Dispatcher.Invoke(() => _popup?.ShowAtCursor());
    }

    private async void OnFormatSelected(string categoryName, string formatId)
    {
        AppLogger.Information($"Format selected: {categoryName}/{formatId}");
        try
        {
            await (_orchestrator?.ExecuteAsync(categoryName, formatId, _lastForegroundWindow) ?? Task.CompletedTask);
        }
        catch (Exception ex)
        {
            AppLogger.Error("Paste operation failed", ex);
        }
    }

    private async void OnHistoryItemSelected(string value)
    {
        AppLogger.Information("History item selected for paste");
        try
        {
            await (_orchestrator?.ExecuteDirectAsync(value, _lastForegroundWindow) ?? Task.CompletedTask);
        }
        catch (Exception ex)
        {
            AppLogger.Error("History paste operation failed", ex);
        }
    }

    private void ShowSettings()
    {
        Dispatcher.Invoke(() =>
        {
            if (_settingsWindow != null)
            {
                _settingsWindow.Activate();
                return;
            }

            _settingsWindow = new SettingsWindow(Settings)
            {
                SettingsSaved = OnSettingsSaved,
                UnregisterHotkey = () => _hotkeyManager?.Unregister(),
                ReregisterHotkey = () =>
                {
                    if (_trayIcon is { IsEnabled: true })
                        _hotkeyManager?.Register(Settings.Hotkey);
                }
            };
            _settingsWindow.Closed += (_, _) => _settingsWindow = null;
            _settingsWindow.ShowDialog();
        });
    }

    private bool OnSettingsSaved(AppSettings updated)
    {
        // Copy values into the registered singleton so all DI consumers see the change.
        var current = Settings;
        current.Hotkey = updated.Hotkey;
        current.PreserveClipboard = updated.PreserveClipboard;
        current.PasteDelayMs = updated.PasteDelayMs;
        current.LaunchAtStartup = updated.LaunchAtStartup;
        current.Theme = updated.Theme;
        current.HistorySize = updated.HistorySize;
        current.Version = updated.Version;

        var saved = _settingsService?.Save(current) ?? false;
        History.UpdateMaxSize(current.HistorySize);

        ApplyHotkey(current);

        ThemeService.Apply(current.Theme);
        StartupService.Apply(current.LaunchAtStartup);
        AppLogger.Information("Settings updated");
        return saved;
    }

    /// <summary>Unregisters the current hotkey and re-registers it if the tray icon is enabled.</summary>
    private void ApplyHotkey(AppSettings current)
    {
        _hotkeyManager?.Unregister();
        if (_trayIcon is { IsEnabled: true })
        {
            if (!_hotkeyManager!.Register(current.Hotkey))
            {
                MessageBox.Show(
                    $"Could not register hotkey: {current.Hotkey.ToDisplayString()}\nIt may be in use by another application.",
                    "MockPaste",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }
    }

    private void OnSystemThemeChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category == UserPreferenceCategory.General)
        {
            ThemeService.Reapply();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        AppLogger.Information("MockPaste shutting down");
        SystemEvents.UserPreferenceChanged -= OnSystemThemeChanged;
        _hotkeyManager?.Dispose();
        _trayIcon?.Dispose();
        (_services as IDisposable)?.Dispose();
        AppLogger.CloseAndFlush();
        _instanceMutex?.ReleaseMutex();
        _instanceMutex?.Dispose();
        base.OnExit(e);
    }
}

