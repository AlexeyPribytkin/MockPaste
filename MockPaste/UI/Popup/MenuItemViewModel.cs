using System.ComponentModel;

namespace MockPaste.UI.Popup;

public sealed class MenuItemViewModel : INotifyPropertyChanged
{
    public required string DisplayName { get; init; }
    public string MnemonicKey { get; init; } = string.Empty;
    public string CategoryName { get; init; } = string.Empty;
    public string FormatId { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public bool HasSubMenu { get; init; }

    public event PropertyChangedEventHandler? PropertyChanged;
}
