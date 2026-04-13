namespace MockPaste.Core.Models;

public sealed class DataFormat
{
    public required string FormatId { get; init; }
    public required string Name { get; init; }
    public string Description { get; init; } = string.Empty;
}
