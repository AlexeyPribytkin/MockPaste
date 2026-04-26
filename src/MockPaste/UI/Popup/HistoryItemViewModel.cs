using System.Windows.Input;

namespace MockPaste.UI.Popup;

/// <summary>
/// View-model for a history entry displayed in the popup's history list.
/// Provides display-friendly truncation and a delete command.
/// </summary>
public sealed class HistoryItemViewModel : IPopupItem
{
    /// <summary>The full raw value that was pasted.</summary>
    public required string Value { get; init; }

    /// <summary>Name of the generator category that produced the value.</summary>
    public required string CategoryName { get; init; }

    /// <summary>Display name of the specific format that was used.</summary>
    public required string FormatName { get; init; }

    /// <summary>Command that removes this entry from the history list.</summary>
    public required ICommand DeleteCommand { get; init; }

    /// <summary>Truncated version of <see cref="Value"/> (max 65 chars) for compact display in the list.</summary>
    public string DisplayValue => Value.Length > 65 ? Value[..65] + "…" : Value;

    /// <summary>Secondary label showing the category and format name, e.g. "GUID  ·  Standard".</summary>
    public string SubLabel => $"{CategoryName}  ·  {FormatName}";
}
