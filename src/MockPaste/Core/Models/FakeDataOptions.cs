namespace MockPaste.Core.Models;

/// <summary>
/// Parameters passed to <see cref="IFakeDataGenerator"/>-derived generators when
/// producing a value. Allows callers to select a specific format and optionally
/// seed the RNG for reproducible output.
/// </summary>
public sealed class FakeDataOptions
{
    /// <summary>Identifies the format to generate (must match a <see cref="FormatDefinition.FormatId"/> registered by the target generator).</summary>
    public required string FormatId { get; init; }

    /// <summary>Optional fixed seed for the random-number generator. When <c>null</c>, <see cref="Random.Shared"/> is used.</summary>
    public int? Seed { get; init; }

    /// <summary>Additional format-specific parameters (e.g. <c>"length"</c> for string generators).</summary>
    public Dictionary<string, string> Parameters { get; init; } = [];
}
