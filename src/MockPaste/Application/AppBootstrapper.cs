using System.IO;
using System.Windows;
using Microsoft.Win32;
using MockPaste.Core.Generators;
using MockPaste.Core.Models;
using MockPaste.Infrastructure;
using MockPaste.Infrastructure.Native;
using MockPaste.UI.Popup;
using MockPaste.UI.Settings;
using MockPaste.UI.Tray;

namespace MockPaste.Application;

/// <summary>
/// Owns all application services and wires their interactions.
/// Keeps <see cref="App"/> as a thin WPF entry point.
/// </summary>
internal sealed class AppBootstrapper : IDisposable
{
    private readonly Action _shutdownApp;

    private HotkeyManager? _hotkeyManager;
    private TrayIconManager? _trayIcon;
    private PopupWindow? _popup;
    private PasteOrchestrator? _orchestrator;
    private SettingsService? _settingsService;
    private AppSettings? _settings;
    private HistoryService? _history;
    private ForegroundWindowTracker? _foregroundTracker;
    private IntPtr _lastForegroundWindow;
    private SettingsWindow? _settingsWindow;

    public AppBootstrapper(Action shutdownApp)
    {
        _shutdownApp = shutdownApp;
    }

    public void Startup()
    {
        InitializeLogging();
        AppLogger.Information("MockPaste starting");

        try
        {
            _settingsService = new SettingsService();
            _settings = _settingsService.Load();
            _history = new HistoryService(_settings.HistorySize);

            var generators = GeneratorRegistry.CreateDefault(AppLogger.Instance);
            var clipboard = new ClipboardService();
            var inputSimulation = new InputSimulationService();

            _orchestrator = new PasteOrchestrator(
                generators,
                clipboard,
                inputSimulation,
                _settings,
                _history,
                AppLogger.Instance);

            ThemeService.Apply(_settings.Theme);
            StartupService.Apply(_settings.LaunchAtStartup);

            InitializeTray();
            InitializeHotkey();
            InitializePopup(generators);
            _foregroundTracker = new ForegroundWindowTracker();

            // Subscribe only after successful init so we don't leak if startup fails.
            SystemEvents.UserPreferenceChanged += OnSystemThemeChanged;

            AppLogger.Information("MockPaste started successfully");
        }
        catch (Exception ex)
        {
            AppLogger.Fatal("Failed to start MockPaste", ex);
            MessageBox.Show($"Failed to start MockPaste:\n{ex.Message}", "MockPaste Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            _shutdownApp();
        }
    }

    public void Shutdown()
    {
        AppLogger.Information("MockPaste shutting down");
        SystemEvents.UserPreferenceChanged -= OnSystemThemeChanged;
    }

    public void Dispose()
    {
        _hotkeyManager?.Dispose();
        _trayIcon?.Dispose();
        _foregroundTracker?.Dispose();
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
        _trayIcon.OnExitClicked += _shutdownApp;
        _trayIcon.OnTrayLeftClicked += OnHotkeyPressed;
        _trayIcon.EnabledChanged += _ => ApplyHotkey(_settings!);
    }

    private void InitializeHotkey()
    {
        _hotkeyManager = new HotkeyManager();
        _hotkeyManager.Initialize();
        _hotkeyManager.HotkeyPressed += OnHotkeyPressed;

        if (!_hotkeyManager.Register(_settings!.Hotkey))
        {
            AppLogger.Warning("Default hotkey could not be registered");
        }
    }

    private void InitializePopup(GeneratorRegistry generators)
    {
        _popup = new PopupWindow(generators, _history!);
        _popup.FormatSelected += OnFormatSelected;
        _popup.HistoryItemSelected += OnHistoryItemSelected;
    }

    private void OnHotkeyPressed()
    {
        if (_trayIcon is { IsEnabled: false })
        {
            return;
        }

        _lastForegroundWindow = _foregroundTracker?.LastForegroundWindow ?? NativeMethods.GetForegroundWindow();
        AppLogger.Debug($"Showing popup, target window: {_lastForegroundWindow}");
        System.Windows.Application.Current.Dispatcher.Invoke(() => _popup?.ShowAtCursor());
    }

    private async void OnFormatSelected(string categoryName, string formatId)
    {
        // Snapshot the target window before any await so it is not overwritten by a subsequent hotkey press.
        var targetWindow = _lastForegroundWindow;
        await ExecuteFormatAsync(categoryName, formatId, targetWindow);
    }

    private async Task ExecuteFormatAsync(string categoryName, string formatId, IntPtr targetWindow)
    {
        AppLogger.Information($"Format selected: {categoryName}/{formatId}");
        try
        {
            await (_orchestrator?.ExecuteAsync(categoryName, formatId, targetWindow) ?? Task.CompletedTask);
        }
        catch (Exception ex)
        {
            AppLogger.Error("Paste operation failed", ex);
        }
    }

    private async void OnHistoryItemSelected(string value)
    {
        // Snapshot the target window before any await so it is not overwritten by a subsequent hotkey press.
        var targetWindow = _lastForegroundWindow;
        await ExecuteHistoryPasteAsync(value, targetWindow);
    }

    private async Task ExecuteHistoryPasteAsync(string value, IntPtr targetWindow)
    {
        AppLogger.Information("History item selected for paste");
        try
        {
            await (_orchestrator?.ExecuteDirectAsync(value, targetWindow) ?? Task.CompletedTask);
        }
        catch (Exception ex)
        {
            AppLogger.Error("History paste operation failed", ex);
        }
    }

    private void ShowSettings()
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            if (_settingsWindow != null)
            {
                _settingsWindow.Activate();
                return;
            }

            _settingsWindow = new SettingsWindow(
                _settings!,
                OnSettingsSaved,
                () => _hotkeyManager?.Unregister(),
                () =>
                {
                    if (_trayIcon is { IsEnabled: true })
                        _hotkeyManager?.Register(_settings!.Hotkey);
                });
            _settingsWindow.Closed += (_, _) => _settingsWindow = null;
            _settingsWindow.ShowDialog();
        });
    }

    private bool OnSettingsSaved(AppSettings updated)
    {
        _settings!.CopyFrom(updated);

        var saved = _settingsService?.Save(_settings) ?? false;
        _history!.UpdateMaxSize(_settings.HistorySize);

        ApplyHotkey(_settings);

        ThemeService.Apply(_settings.Theme);
        StartupService.Apply(_settings.LaunchAtStartup);
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
}
