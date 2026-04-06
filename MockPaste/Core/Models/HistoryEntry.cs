namespace MockPaste.Core.Models;

public sealed record HistoryEntry(
    string Value,
    string CategoryName,
    string FormatName,
    DateTime GeneratedAt);
