using System.Reflection;
using MockPaste.Infrastructure;

namespace MockPaste.Core.Generators;

public sealed class GeneratorRegistry
{
    private readonly Dictionary<string, IFakeDataGenerator> _generators = new(StringComparer.OrdinalIgnoreCase);

    public void Register(IFakeDataGenerator generator)
    {
        ArgumentNullException.ThrowIfNull(generator);
        if (string.IsNullOrWhiteSpace(generator.CategoryName))
        {
            throw new ArgumentException("Generator category name cannot be empty.", nameof(generator));
        }

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
        var log = logger ?? AppLogger.Instance;
        var generatorType = typeof(IFakeDataGenerator);
        var generatorNamespace = typeof(GeneratorRegistry).Namespace;

        foreach (var type in Assembly.GetExecutingAssembly().GetTypes()
            .Where(t =>
                t.IsClass
                && !t.IsAbstract
                && t.Namespace == generatorNamespace
                && generatorType.IsAssignableFrom(t))
            .OrderBy(t => t.Name))
        {
            if (type.GetConstructor(Type.EmptyTypes) is null)
            {
                log.Warning($"Skipping generator '{type.FullName}' because it does not have a public parameterless constructor");
                continue;
            }

            try
            {
                if (Activator.CreateInstance(type) is IFakeDataGenerator g)
                {
                    registry.Register(g);
                }
                else
                {
                    log.Warning($"Generator type '{type.FullName}' was instantiated but does not implement IFakeDataGenerator correctly");
                }
            }
            catch (MemberAccessException ex)
            {
                LogInstantiationError(log, type, ex);
            }
            catch (TargetInvocationException ex)
            {
                LogInstantiationError(log, type, ex);
            }
        }

        return registry;
    }

    private static void LogInstantiationError(IAppLogger log, Type type, Exception exception)
    {
        log.Error($"Failed to instantiate generator '{type.FullName}'", exception);
    }
}
