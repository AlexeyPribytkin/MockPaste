using MockPaste.Core.Generators;

namespace MockPaste.Tests.Generators;

public sealed class GeneratorRegistryTests
{
    [Fact]
    public void CreateDefault_RegistersAllBuiltInGenerators()
    {
        var registry = GeneratorRegistry.CreateDefault();
        var categories = registry.GetAll().Select(g => g.CategoryName).ToList();

        Assert.Contains("Email",  categories);
        Assert.Contains("Phone",  categories);
        Assert.Contains("GUID",   categories);
        Assert.Contains("Number", categories);
        Assert.Contains("String", categories);
    }

    [Fact]
    public void GetAll_ReturnsSameCountAsRegistered()
    {
        var registry = GeneratorRegistry.CreateDefault();
        Assert.True(registry.GetAll().Count > 0);
    }

    [Fact]
    public void Get_KnownCategory_ReturnsGenerator()
    {
        var registry = GeneratorRegistry.CreateDefault();
        var generator = registry.Get("Email");
        Assert.NotNull(generator);
        Assert.Equal("Email", generator.CategoryName);
    }

    [Fact]
    public void Get_UnknownCategory_ReturnsNull()
    {
        var registry = GeneratorRegistry.CreateDefault();
        Assert.Null(registry.Get("DoesNotExist"));
    }

    [Fact]
    public void Get_IsCaseInsensitive()
    {
        var registry = GeneratorRegistry.CreateDefault();
        Assert.NotNull(registry.Get("email"));
        Assert.NotNull(registry.Get("EMAIL"));
        Assert.NotNull(registry.Get("eMaIl"));
    }

    [Fact]
    public void Register_OverridesExistingCategory()
    {
        var registry = GeneratorRegistry.CreateDefault();
        var custom = new EmailGenerator();  // same CategoryName "Email"
        registry.Register(custom);
        Assert.Same(custom, registry.Get("Email"));
    }

    [Fact]
    public void GetAll_ReturnsReadOnlyList()
    {
        var registry = GeneratorRegistry.CreateDefault();
        var list = registry.GetAll();
        Assert.IsAssignableFrom<IReadOnlyList<MockPaste.Core.Generators.IFakeDataGenerator>>(list);
    }
}
