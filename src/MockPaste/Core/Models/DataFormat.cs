namespace MockPaste.Core.Models;

/// <summary>
/// Represents a named output format offered by a fake-data generator category,
/// used to populate format sub-menus in the popup UI.
/// </summary>
public sealed class DataFormat
{
    /// <summary>Unique identifier for this format within its generator (e.g. "guid-standard").</summary>
    public required string FormatId { get; init; }

    /// <summary>Short display name shown in the popup menu (e.g. "Standard").</summary>
    public required string Name { get; init; }

    /// <summary>Optional longer description shown as a hint below the name (e.g. an example value).</summary>
    public string Description { get; init; } = string.Empty;
}
