using System.Reflection;
using MockPaste.Infrastructure;

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
        _generators.Values.OrderBy(g => g.Order).ToList().AsReadOnly();

    public static GeneratorRegistry CreateDefault(IAppLogger? logger = null)
    {
        var registry = new GeneratorRegistry();
        var generatorType = typeof(IFakeDataGenerator);

        foreach (var type in Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && generatorType.IsAssignableFrom(t))
            .OrderBy(t => t.Name))
        {
            try
            {
                if (Activator.CreateInstance(type) is IFakeDataGenerator g)
                    registry.Register(g);
            }
            catch (Exception ex)
            {
                var log = logger ?? AppLogger.Instance;
                log.Error($"Failed to instantiate generator '{type.FullName}'", ex);
            }
        }

        return registry;
    }
}
