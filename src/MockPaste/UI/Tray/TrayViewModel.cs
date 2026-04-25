using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace MockPaste.UI.Tray;

internal sealed class TrayViewModel : INotifyPropertyChanged
{
    private bool _isEnabled = true;

    public event Action? SettingsRequested;
    public event Action? ExitRequested;
    public event Action<bool>? EnabledChanged;

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

    public string ToolTipText => IsEnabled ? "MockPaste" : "MockPaste (Disabled)";

    public ICommand ToggleCommand { get; }
    public ICommand SettingsCommand { get; }
    public ICommand ExitCommand { get; }

    public TrayViewModel()
    {
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

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
