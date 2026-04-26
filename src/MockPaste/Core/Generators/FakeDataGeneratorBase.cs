using MockPaste.Core.Models;

namespace MockPaste.Core.Generators;

/// <summary>
/// Base class for fake-data generators. Handles format registration, caching of
/// <see cref="DataFormat"/> metadata, and dispatching <see cref="Generate"/> calls to
/// the correct <see cref="FormatDefinition"/> delegate.
/// </summary>
public abstract class FakeDataGeneratorBase : IFakeDataGenerator
{
    private readonly Dictionary<string, FormatDefinition> _formats;
    private readonly FormatDefinition _default;
    private readonly IReadOnlyList<DataFormat> _cachedFormats;

    /// <summary>
    /// Initializes the generator by indexing the supplied <paramref name="formats"/> by ID
    /// and pre-building the read-only <see cref="SupportedFormats"/> list.
    /// The first element of <paramref name="formats"/> is used as the default fallback.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="MnemonicKey"/> is not exactly one character.
    /// </exception>
    protected FakeDataGeneratorBase(IEnumerable<FormatDefinition> formats)
    {
        var list = formats.ToList();
        _formats = list.ToDictionary(f => f.FormatId, StringComparer.OrdinalIgnoreCase);
        _default = list[0];
        _cachedFormats = list
            .Select(f => new DataFormat { FormatId = f.FormatId, Name = f.Name, Description = f.Description })
            .ToList()
            .AsReadOnly();

        if (MnemonicKey.Length != 1)
            throw new InvalidOperationException(
                $"{GetType().Name}.MnemonicKey must be exactly one character, got '{MnemonicKey}'.");
    }

    /// <inheritdoc/>
    public abstract string CategoryName { get; }

    /// <inheritdoc/>
    public abstract string MnemonicKey { get; }

    /// <inheritdoc/>
    public virtual int Order => 100;

    /// <inheritdoc/>
    public IReadOnlyList<DataFormat> SupportedFormats => _cachedFormats;

    /// <inheritdoc/>
    public string Generate(FakeDataOptions options)
    {
        var fmt = _formats.GetValueOrDefault(options.FormatId) ?? _default;
        return fmt.Generate(options);
    }
}
