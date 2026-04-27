using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MockPaste.Localization;
using MockPaste.Resources;

namespace MockPaste.UI.Tray;

/// <summary>
/// ViewModel for the system-tray context menu. Exposes toggle, settings, and exit
/// commands, and tracks whether the app is currently enabled (accepting hotkey presses).
/// </summary>
internal sealed class TrayViewModel : INotifyPropertyChanged
{
    private bool _isEnabled = true;
    /// <summary>Raised when the user clicks "Settings" in the tray context menu.</summary>
    public event Action? SettingsRequested;

    /// <summary>Raised when the user clicks "Exit" in the tray context menu.</summary>
    public event Action? ExitRequested;

    /// <summary>Raised when the user toggles the enabled state, carrying the new value.</summary>
    public event Action<bool>? EnabledChanged;

    /// <summary>When <c>true</c>, MockPaste is active and will respond to the global hotkey.</summary>
    public bool IsEnabled
    {
        get => _isEnabled;
        private set
        {
            if (_isEnabled == value) return;
            _isEnabled = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ToolTipText));
        }
    }

    /// <summary>Tooltip text shown when hovering the tray icon, indicating the enabled/disabled state.</summary>
    public string ToolTipText => IsEnabled
        ? Strings.StringTrayTooltipEnabled
        : Strings.StringTrayTooltipDisabled;

    /// <summary>Toggles the application between enabled and disabled states.</summary>
    public ICommand ToggleCommand { get; }

    /// <summary>Opens the Settings window.</summary>
    public ICommand SettingsCommand { get; }

    /// <summary>Exits the application.</summary>
    public ICommand ExitCommand { get; }

    public TrayViewModel()
    {
        LocalizationManager.Instance.CultureChanged += OnCultureChanged;
        ToggleCommand = new RelayCommand(OnToggle);
        SettingsCommand = new RelayCommand(() => SettingsRequested?.Invoke());
        ExitCommand = new RelayCommand(() => ExitRequested?.Invoke());
    }

    private void OnToggle()
    {
        IsEnabled = !IsEnabled;
        EnabledChanged?.Invoke(IsEnabled);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnCultureChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(ToolTipText));
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
