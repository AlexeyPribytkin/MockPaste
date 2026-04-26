namespace MockPaste.Core.Models;

/// <summary>
/// Full internal definition of a single data format, combining display metadata with
/// the delegate that actually generates a value. Used by <c>FakeDataGeneratorBase</c>
/// to build the format registry; not exposed directly in the UI.
/// </summary>
/// <param name="FormatId">Unique identifier for this format within its generator.</param>
/// <param name="Name">Short display name shown in the popup menu.</param>
/// <param name="Description">Optional example or hint text shown below the name.</param>
/// <param name="Generate">Factory function that produces a generated value given <see cref="FakeDataOptions"/>.</param>
public sealed record FormatDefinition(
    string FormatId,
    string Name,
    string Description,
    Func<FakeDataOptions, string> Generate
);
