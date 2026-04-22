using MockPaste.Core.Models;

namespace MockPaste.Core.Generators;

public abstract class FakeDataGeneratorBase : IFakeDataGenerator
{
    private readonly Dictionary<string, FormatDefinition> _formats;
    private readonly FormatDefinition _default;
    private readonly IReadOnlyList<DataFormat> _cachedFormats;

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

    public abstract string CategoryName { get; }
    public abstract string MnemonicKey { get; }
    public virtual int Order => 100;

    public IReadOnlyList<DataFormat> SupportedFormats => _cachedFormats;

    public string Generate(FakeDataOptions options)
    {
        var fmt = _formats.GetValueOrDefault(options.FormatId) ?? _default;
        return fmt.Generate(options);
    }
}
