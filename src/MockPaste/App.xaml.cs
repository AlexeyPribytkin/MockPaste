using System.IO;
using System.Windows;
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

    private HotkeyManager? _hotkeyManager;
    private TrayIconManager? _trayIcon;
    private PopupWindow? _popup;
    private PasteOrchestrator? _orchestrator;
    private SettingsService? _settingsService;
    private AppSettings? _settings;
    private GeneratorRegistry? _generators;
    private HistoryService? _history;
    private IntPtr _lastForegroundWindow;
    private SettingsWindow? _settingsWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        _instanceMutex = new Mutex(true, "MockPaste_SingleInstance", out bool createdNew);
        if (!createdNew)
        {
            MessageBox.Show("MockPaste is already running.", "MockPaste",
                MessageBoxButton.OK, MessageBoxImage.Information);
            _instanceMutex.Dispose();
            Shutdown(0);
            return;
        }

        base.OnStartup(e);
        InitializeLogging();
        AppLogger.Information("MockPaste starting");

        try
        {
            _settingsService = new SettingsService();
            _settings = _settingsService.Load();

            ThemeService.Apply(_settings.Theme);
            StartupService.Apply(_settings.LaunchAtStartup);
            SystemEvents.UserPreferenceChanged += OnSystemThemeChanged;

            _generators = GeneratorRegistry.CreateDefault();

            _history = new HistoryService(_settings.HistorySize);

            _orchestrator = new PasteOrchestrator(
                _generators,
                new ClipboardService(),
                new InputSimulationService(),
                _settings,
                _history);

            InitializeTray();
            InitializeHotkey();
            InitializePopup();

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
        _trayIcon.EnabledChanged += enabled =>
        {
            if (enabled)
                _hotkeyManager?.Register(_settings!.Hotkey);
            else
                _hotkeyManager?.Unregister();
        };
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

    private void InitializePopup()
    {
        _popup = new PopupWindow(_generators!, _history!);
        _popup.FormatSelected += OnFormatSelected;
        _popup.HistoryItemSelected += OnHistoryItemSelected;
    }

    private void OnHotkeyPressed()
    {
        if (_trayIcon is { IsEnabled: false }) return;

        _lastForegroundWindow = NativeMethods.GetForegroundWindow();
        AppLogger.Debug($"Showing popup, target window: {_lastForegroundWindow}");
        _popup?.ShowAtCursor();
    }

    private async void OnFormatSelected(string categoryName, string formatId)
    {
        AppLogger.Information($"Format selected: {categoryName}/{formatId}");
        await (_orchestrator?.ExecuteAsync(categoryName, formatId, _lastForegroundWindow) ?? Task.CompletedTask);
    }

    private async void OnHistoryItemSelected(string value)
    {
        AppLogger.Information("History item selected for paste");
        await (_orchestrator?.ExecuteDirectAsync(value, _lastForegroundWindow) ?? Task.CompletedTask);
    }

    private void ShowSettings()
    {
        if (_settingsWindow != null)
        {
            _settingsWindow.Activate();
            return;
        }
        _settingsWindow = new SettingsWindow(_settings!);
        _settingsWindow.SettingsSaved = OnSettingsSaved;
        _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        _settingsWindow.ShowDialog();
    }

    private bool OnSettingsSaved(AppSettings settings)
    {
        _settings = settings;
        var saved = _settingsService?.Save(settings) ?? false;
        _history?.UpdateMaxSize(settings.HistorySize);

        _hotkeyManager?.Unregister();
        if (_trayIcon is { IsEnabled: true })
        {
            if (!_hotkeyManager!.Register(settings.Hotkey))
            {
                MessageBox.Show(
                    $"Could not register hotkey: {settings.Hotkey.ToDisplayString()}\nIt may be in use by another application.",
                    "MockPaste",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        ThemeService.Apply(settings.Theme);
        StartupService.Apply(settings.LaunchAtStartup);
        AppLogger.Information("Settings updated");
        return saved;
    }

    private void OnSystemThemeChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category == UserPreferenceCategory.General)
            ThemeService.Reapply();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        AppLogger.Information("MockPaste shutting down");
        SystemEvents.UserPreferenceChanged -= OnSystemThemeChanged;
        _hotkeyManager?.Dispose();
        _trayIcon?.Dispose();
        AppLogger.CloseAndFlush();
        _instanceMutex?.ReleaseMutex();
        _instanceMutex?.Dispose();
        base.OnExit(e);
    }
}
