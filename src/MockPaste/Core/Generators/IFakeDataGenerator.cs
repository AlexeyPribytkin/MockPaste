using MockPaste.Core.Models;

namespace MockPaste.Core.Generators;

/// <summary>
/// Contract for all fake-data generators. Each implementation represents one top-level
/// category (e.g. GUID, Email, Phone) and exposes one or more named output formats.
/// </summary>
public interface IFakeDataGenerator
{
    /// <summary>Display name shown as the category header in the popup menu (e.g. "GUID").</summary>
    string CategoryName { get; }

    /// <summary>Single-character keyboard shortcut that activates this category in the popup (e.g. "G").</summary>
    string MnemonicKey { get; }

    /// <summary>Relative sort position in the popup menu; lower values appear first.</summary>
    int Order { get; }

    /// <summary>Metadata for all output formats this generator supports, used to build the format sub-menu.</summary>
    IReadOnlyList<DataFormat> SupportedFormats { get; }

    /// <summary>
    /// Generates and returns a fake value according to the format and options specified
    /// in <paramref name="options"/>. Falls back to the first registered format when
    /// <see cref="FakeDataOptions.FormatId"/> is unrecognised.
    /// </summary>
    /// <param name="options">Generation parameters including the target format ID and optional RNG seed.</param>
    string Generate(FakeDataOptions options);
}
