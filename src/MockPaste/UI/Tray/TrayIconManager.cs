using H.NotifyIcon;
using MockPaste.Infrastructure;

namespace MockPaste.UI.Tray;

public sealed class TrayIconManager : IDisposable
{
    private readonly TaskbarIcon _taskbarIcon;
    private readonly TrayViewModel _viewModel;

    public event Action? OnSettingsClicked;
    public event Action? OnExitClicked;
    public event Action? OnTrayLeftClicked;
    public bool IsEnabled => _viewModel.IsEnabled;
    public event Action<bool>? EnabledChanged;

    public TrayIconManager()
    {
        _taskbarIcon = (TaskbarIcon)System.Windows.Application.Current.Resources["TrayIcon"];

        _viewModel = new TrayViewModel();
        _viewModel.SettingsRequested += () => OnSettingsClicked?.Invoke();
        _viewModel.ExitRequested += () => OnExitClicked?.Invoke();
        _viewModel.EnabledChanged += enabled =>
        {
            _taskbarIcon.ToolTipText = _viewModel.ToolTipText;
            AppLogger.Information($"MockPaste {(enabled ? "enabled" : "disabled")}");
            EnabledChanged?.Invoke(enabled);
        };

        (_taskbarIcon.ContextMenu ?? throw new InvalidOperationException("TrayIcon ContextMenu is not set."))
            .DataContext = _viewModel;
        _taskbarIcon.TrayLeftMouseDown += (_, _) => OnTrayLeftClicked?.Invoke();
        _taskbarIcon.ForceCreate(enablesEfficiencyMode: false);
    }

    public void Dispose()
    {
        _taskbarIcon.Dispose();
    }
}
