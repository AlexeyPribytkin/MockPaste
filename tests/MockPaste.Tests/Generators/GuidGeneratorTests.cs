using MockPaste.Core.Generators;
using MockPaste.Core.Models;

namespace MockPaste.Tests.Generators;

public sealed class GuidGeneratorTests
{
    private readonly GuidGenerator _sut = new();

    // ── Metadata ─────────────────────────────────────────────────────────────

    [Fact]
    public void CategoryName_IsGuid() =>
        Assert.Equal("GUID", _sut.CategoryName);

    [Fact]
    public void MnemonicKey_IsSingleChar() =>
        Assert.Equal(1, _sut.MnemonicKey.Length);

    [Fact]
    public void SupportedFormats_HasFourEntries() =>
        Assert.Equal(4, _sut.SupportedFormats.Count);

    // ── Output shape ─────────────────────────────────────────────────────────

    [Fact]
    public void Generate_Standard_IsValidGuid()
    {
        var result = _sut.Generate(new FakeDataOptions { FormatId = "guid-standard" });
        Assert.True(Guid.TryParseExact(result, "D", out _));
    }

    [Fact]
    public void Generate_NoDashes_Is32HexChars()
    {
        var result = _sut.Generate(new FakeDataOptions { FormatId = "guid-nodashes" });
        Assert.True(Guid.TryParseExact(result, "N", out _));
        Assert.Equal(32, result.Length);
        Assert.DoesNotContain("-", result);
    }

    [Fact]
    public void Generate_Uppercase_IsAllUppercase()
    {
        var result = _sut.Generate(new FakeDataOptions { FormatId = "guid-uppercase" });
        Assert.Equal(result.ToUpperInvariant(), result);
        Assert.True(Guid.TryParseExact(result, "D", out _));
    }

    [Fact]
    public void Generate_Braced_IsWrappedInBraces()
    {
        var result = _sut.Generate(new FakeDataOptions { FormatId = "guid-braced" });
        Assert.StartsWith("{", result);
        Assert.EndsWith("}", result);
        Assert.True(Guid.TryParseExact(result, "B", out _));
    }

    // ── Uniqueness ────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("guid-standard")]
    [InlineData("guid-nodashes")]
    [InlineData("guid-uppercase")]
    [InlineData("guid-braced")]
    public void Generate_TwoCallsWithoutSeed_ReturnDifferentValues(string formatId)
    {
        var opts = new FakeDataOptions { FormatId = formatId };
        // GUIDs are random — collisions are astronomically unlikely
        Assert.NotEqual(_sut.Generate(opts), _sut.Generate(opts));
    }
}
