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
    private ClipboardMonitor? _clipboardMonitor;

    /// <summary>Creates the bootstrapper with a callback that shuts down the WPF application.</summary>
    public AppBootstrapper(Action shutdownApp)
    {
        _shutdownApp = shutdownApp;
    }

    /// <summary>
    /// Initializes logging, loads settings, creates all services and UI components,
    /// and registers system event hooks. On failure, performs cleanup and exits the application.
    /// </summary>
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

            SeedHistoryFromClipboard(clipboard);
            InitializeTray();
            InitializeHotkey();
            InitializePopup(generators);
            InitializeClipboardMonitor(clipboard);
            _foregroundTracker = new ForegroundWindowTracker();

            ThemeService.Apply(_settings.Theme);
            StartupService.Apply(_settings.LaunchAtStartup);

            // Subscribe only after successful init so we don't leak if startup fails.
            SystemEvents.UserPreferenceChanged += OnSystemThemeChanged;

            AppLogger.Information("MockPaste started successfully");
        }
        catch (Exception ex)
        {
            try
            {
                CleanupFailedStartup();
            }
            catch (Exception cleanupEx)
            {
                AppLogger.Warning("Cleanup after failed startup encountered an error", cleanupEx);
            }

            AppLogger.Fatal("Failed to start MockPaste", ex);
            MessageBox.Show($"Failed to start MockPaste:\n{ex.Message}", "MockPaste Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            _shutdownApp();
        }
    }

    /// <summary>Logs the shutdown event and unregisters the system theme change handler.</summary>
    public void Shutdown()
    {
        AppLogger.Information("MockPaste shutting down");
        SystemEvents.UserPreferenceChanged -= OnSystemThemeChanged;
    }

    /// <summary>Disposes all managed resources (hotkey window, tray icon, event hooks, clipboard monitor).</summary>
    public void Dispose()
    {
        _hotkeyManager?.Dispose();
        _trayIcon?.Dispose();
        _foregroundTracker?.Dispose();
        _clipboardMonitor?.Dispose();
    }

    private void CleanupFailedStartup()
    {
        SystemEvents.UserPreferenceChanged -= OnSystemThemeChanged;

        if (_popup is not null)
        {
            _popup.FormatSelected -= OnFormatSelected;
            _popup.HistoryItemSelected -= OnHistoryItemSelected;
            _popup.HidePopup();
            _popup = null;
        }

        _clipboardMonitor?.Dispose();
        _clipboardMonitor = null;

        _hotkeyManager?.Dispose();
        _hotkeyManager = null;

        _trayIcon?.Dispose();
        _trayIcon = null;

        _foregroundTracker?.Dispose();
        _foregroundTracker = null;
    }

    /// <summary>Configures the <see cref="AppLogger"/> singleton to write to the user's AppData logs directory.</summary>
    private static void InitializeLogging()
    {
        var logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MockPaste", "logs");
        AppLogger.Initialize(logDir);
    }

    /// <summary>Creates and configures the system-tray icon and wires its commands to application actions.</summary>
    private void InitializeTray()
    {
        _trayIcon = new TrayIconManager();
        _trayIcon.OnSettingsClicked += ShowSettings;
        _trayIcon.OnExitClicked += _shutdownApp;
        _trayIcon.OnTrayLeftClicked += OnHotkeyPressed;
        _trayIcon.EnabledChanged += _ => ApplyHotkey(_settings!);
    }

    /// <summary>Creates the <see cref="HotkeyManager"/>, registers the configured hotkey, and subscribes to press events.</summary>
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

    /// <summary>Creates the popup window and subscribes to its format and history selection events.</summary>
    private void InitializePopup(GeneratorRegistry generators)
    {
        _popup = new PopupWindow(generators, _history!);
        _popup.FormatSelected += OnFormatSelected;
        _popup.HistoryItemSelected += OnHistoryItemSelected;
    }

    /// <summary>
    /// Handles the global hotkey (or tray icon click). Captures the foreground window
    /// handle before the popup steals focus, then shows the popup at the cursor position.
    /// </summary>
    private void OnHotkeyPressed()
    {
        if (_trayIcon is { IsEnabled: false })
        {
            return;
        }

        var trackedWindow = _foregroundTracker?.LastForegroundWindow ?? IntPtr.Zero;
        _lastForegroundWindow = trackedWindow != IntPtr.Zero
            ? trackedWindow
            : NativeMethods.GetForegroundWindow();

        AppLogger.Debug($"Showing popup, target window: {_lastForegroundWindow}");
        System.Windows.Application.Current.Dispatcher.Invoke(() => _popup?.ShowAtCursor());
    }

    /// <summary>Fires-and-forgets the paste pipeline for a generator format chosen in the popup.</summary>
    private void OnFormatSelected(string categoryName, string formatId)
    {
        // Snapshot the target window before any await so it is not overwritten by a subsequent hotkey press.
        var targetWindow = _lastForegroundWindow;
        _ = _orchestrator?.ExecuteAsync(categoryName, formatId, targetWindow) ?? Task.CompletedTask;
    }

    /// <summary>Fires-and-forgets the direct paste pipeline for a history entry chosen in the popup.</summary>
    private void OnHistoryItemSelected(string value)
    {
        // Snapshot the target window before any await so it is not overwritten by a subsequent hotkey press.
        var targetWindow = _lastForegroundWindow;
        _ = _orchestrator?.ExecuteDirectAsync(value, targetWindow) ?? Task.CompletedTask;
    }

    /// <summary>
    /// Opens the Settings window on the UI thread. If the window is already open, it is
    /// activated instead of opening a second instance.
    /// </summary>
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

    /// <summary>
    /// Applies and persists updated settings: propagates changes to history size, hotkey,
    /// theme, and startup registration.
    /// </summary>
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

    /// <summary>Re-applies the current theme when the Windows color scheme changes.</summary>
    private void OnSystemThemeChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category == UserPreferenceCategory.General)
        {
            ThemeService.Reapply();
        }
    }

    /// <summary>
    /// Reads the current clipboard text on startup and adds it to history if
    /// <see cref="AppSettings.TrackClipboardHistory"/> is enabled.
    /// </summary>
    private void SeedHistoryFromClipboard(ClipboardService clipboard)
    {
        if (!_settings!.TrackClipboardHistory)
        {
            return;
        }

        var text = clipboard.TryGetText();
        if (!string.IsNullOrWhiteSpace(text))
        {
            _history!.Add(new HistoryEntry(text, "Clipboard", "Clipboard", DateTime.Now));
            AppLogger.Debug("Added clipboard text to history");
        }
    }

    /// <summary>Creates and starts the clipboard change monitor that tracks new clipboard entries into history.</summary>
    private void InitializeClipboardMonitor(ClipboardService clipboard)
    {
        _clipboardMonitor = new ClipboardMonitor();
        _clipboardMonitor.ClipboardChanged += () => SeedHistoryFromClipboard(clipboard);
        _clipboardMonitor.Initialize();
    }
}
