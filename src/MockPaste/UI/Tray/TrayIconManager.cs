using H.NotifyIcon;
using MockPaste.Infrastructure;

namespace MockPaste.UI.Tray;

/// <summary>
/// Creates and manages the H.NotifyIcon <c>TaskbarIcon</c> declared in application
/// resources. Wires the tray context menu commands to application-level events and
/// propagates the enabled/disabled toggle state.
/// </summary>
public sealed class TrayIconManager : IDisposable
{
    private readonly TaskbarIcon _taskbarIcon;
    private readonly TrayViewModel _viewModel;
    private bool _isEnabled = true;

    /// <summary>Raised when the user clicks "Settings" via the tray context menu.</summary>
    public event Action? OnSettingsClicked;

    /// <summary>Raised when the user clicks "Exit" via the tray context menu.</summary>
    public event Action? OnExitClicked;

    /// <summary>Raised when the user clicks the tray icon with the left mouse button.</summary>
    public event Action? OnTrayLeftClicked;

    /// <summary><c>true</c> when the app is accepting hotkey presses; mirrors the toggle state.</summary>
    public bool IsEnabled => _isEnabled;

    /// <summary>Raised when the user toggles the enabled state from the tray menu, carrying the new value.</summary>
    public event Action<bool>? EnabledChanged;

    public TrayIconManager()
    {
        _taskbarIcon = (TaskbarIcon)System.Windows.Application.Current.Resources["TrayIcon"];

        _viewModel = new TrayViewModel();
        _viewModel.SettingsRequested += () => OnSettingsClicked?.Invoke();
        _viewModel.ExitRequested += () => OnExitClicked?.Invoke();
        _viewModel.EnabledChanged += enabled =>
        {
            _isEnabled = enabled;
            _taskbarIcon.ToolTipText = _viewModel.ToolTipText;
            AppLogger.Information($"MockPaste {(enabled ? "enabled" : "disabled")}");
            EnabledChanged?.Invoke(enabled);
        };

        ApplyContextMenuDataContext(_taskbarIcon.ContextMenu ?? throw new InvalidOperationException("TrayIcon ContextMenu is not set."));
        _taskbarIcon.TrayLeftMouseDown += (_, _) => OnTrayLeftClicked?.Invoke();
        _taskbarIcon.ForceCreate(enablesEfficiencyMode: false);
    }

    /// <summary>Recreates and reapplies the tray context menu so it picks up current theme resources.</summary>
    public void RefreshTheme()
    {
        if (System.Windows.Application.Current.Resources["TrayContextMenu"] is not System.Windows.Controls.ContextMenu menu)
        {
            throw new InvalidOperationException("TrayContextMenu resource is not set.");
        }

        ApplyContextMenuDataContext(menu);
        _taskbarIcon.ContextMenu = menu;
    }

    public void Dispose()
    {
        _taskbarIcon.Dispose();
    }

    private void ApplyContextMenuDataContext(System.Windows.Controls.ContextMenu contextMenu)
    {
        contextMenu.DataContext = _viewModel;
    }
}
