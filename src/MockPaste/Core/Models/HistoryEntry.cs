namespace MockPaste.Core.Models;

/// <summary>
/// Immutable record of a single clipboard entry produced by the app and stored in history.
/// </summary>
/// <param name="Value">The raw generated string that was placed on the clipboard.</param>
/// <param name="CategoryName">Name of the generator category that produced the value (e.g. "GUID").</param>
/// <param name="FormatName">Display name of the specific format used (e.g. "Standard").</param>
/// <param name="GeneratedAt">UTC timestamp of when the value was generated.</param>
public sealed record HistoryEntry(
    string Value,
    string CategoryName,
    string FormatName,
    DateTime GeneratedAt);
