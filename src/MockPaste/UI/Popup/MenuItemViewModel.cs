namespace MockPaste.UI.Popup;

/// <summary>
/// View-model for a single entry in the popup menu, representing either a generator
/// category (top level) or a specific output format (sub-menu level).
/// </summary>
public sealed class MenuItemViewModel : IPopupItem
{
    /// <summary>Text shown in the popup list row.</summary>
    public required string DisplayName { get; init; }

    /// <summary>Single-character keyboard shortcut for this item (category level only).</summary>
    public string MnemonicKey { get; init; } = string.Empty;

    /// <summary>Name of the generator category this item belongs to.</summary>
    public string CategoryName { get; init; } = string.Empty;

    /// <summary>Format identifier passed to the generator when this item is selected.</summary>
    public string FormatId { get; init; } = string.Empty;

    /// <summary>Optional hint text displayed below the display name (e.g. an example value).</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>When <c>true</c>, selecting this item navigates to a format sub-menu instead of executing a paste.</summary>
    public bool HasSubMenu { get; init; }
}
