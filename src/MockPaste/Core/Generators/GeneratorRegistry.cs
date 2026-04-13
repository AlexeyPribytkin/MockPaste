namespace MockPaste.Core.Generators;

public sealed class GeneratorRegistry
{
    private readonly Dictionary<string, IFakeDataGenerator> _generators = new(StringComparer.OrdinalIgnoreCase);

    public void Register(IFakeDataGenerator generator)
    {
        _generators[generator.CategoryName] = generator;
    }

    public IFakeDataGenerator? Get(string categoryName) =>
        _generators.GetValueOrDefault(categoryName);

    public IReadOnlyList<IFakeDataGenerator> GetAll() =>
        _generators.Values.ToList().AsReadOnly();

    public static GeneratorRegistry CreateDefault()
    {
        var registry = new GeneratorRegistry();
        registry.Register(new GuidGenerator());
        registry.Register(new EmailGenerator());
        registry.Register(new PhoneGenerator());
        registry.Register(new StringGenerator());
        registry.Register(new NumberGenerator());
        return registry;
    }
}
