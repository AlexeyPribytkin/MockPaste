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

    /// <summary>
    /// Discovers and registers all <see cref="IFakeDataGenerator"/> implementations in this assembly
    /// that have a public parameterless constructor.
    /// <para>
    /// Generators that require constructor arguments are silently skipped; add them explicitly via
    /// <see cref="Register"/> after calling this method if needed.
    /// </para>
    /// </summary>
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
                {
                    registry.Register(g);
                }
                else
                {
                    (logger ?? AppLogger.Instance).Warning($"Generator type '{type.FullName}' was instantiated but does not implement IFakeDataGenerator correctly");
                }
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
