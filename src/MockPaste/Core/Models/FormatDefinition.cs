namespace MockPaste.Core.Models;

public sealed record FormatDefinition(
    string FormatId,
    string Name,
    string Description,
    Func<FakeDataOptions, string> Generate
);
