namespace MockPaste.Core.Models;

public sealed class FakeDataOptions
{
    public required string FormatId { get; init; }
    public int? Seed { get; init; }
    public Dictionary<string, string> Parameters { get; init; } = [];
}
