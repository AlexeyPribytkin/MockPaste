using MockPaste.Core.Generators;
using MockPaste.Core.Models;

namespace MockPaste.Tests.Generators;

public sealed class StringGeneratorTests
{
    private readonly StringGenerator _sut = new();

    // ── Metadata ─────────────────────────────────────────────────────────────

    [Fact]
    public void CategoryName_IsString() =>
        Assert.Equal("String", _sut.CategoryName);

    [Fact]
    public void MnemonicKey_IsSingleChar() =>
        Assert.Equal(1, _sut.MnemonicKey.Length);

    [Fact]
    public void SupportedFormats_HasFourEntries() =>
        Assert.Equal(4, _sut.SupportedFormats.Count);

    // ── Alphanumeric ─────────────────────────────────────────────────────────

    [Fact]
    public void Generate_Alphanumeric_DefaultLength16()
    {
        var result = _sut.Generate(new FakeDataOptions { FormatId = "string-alphanumeric", Seed = 1 });
        Assert.Equal(16, result.Length);
    }

    [Fact]
    public void Generate_Alphanumeric_OnlyAlphanumericChars()
    {
        var result = _sut.Generate(new FakeDataOptions { FormatId = "string-alphanumeric", Seed = 1 });
        Assert.True(result.All(c => char.IsLetterOrDigit(c)));
    }

    [Fact]
    public void Generate_Alphanumeric_CustomLength()
    {
        var result = _sut.Generate(new FakeDataOptions
        {
            FormatId = "string-alphanumeric",
            Seed = 1,
            Parameters = { ["length"] = "8" }
        });
        Assert.Equal(8, result.Length);
    }

    // ── Alpha Only ───────────────────────────────────────────────────────────

    [Fact]
    public void Generate_Alpha_DefaultLength16()
    {
        var result = _sut.Generate(new FakeDataOptions { FormatId = "string-alpha", Seed = 1 });
        Assert.Equal(16, result.Length);
    }

    [Fact]
    public void Generate_Alpha_OnlyLetters()
    {
        var result = _sut.Generate(new FakeDataOptions { FormatId = "string-alpha", Seed = 1 });
        Assert.True(result.All(char.IsLetter));
    }

    // ── Hex ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Generate_Hex_DefaultLength32()
    {
        var result = _sut.Generate(new FakeDataOptions { FormatId = "string-hex", Seed = 1 });
        Assert.Equal(32, result.Length);
    }

    [Fact]
    public void Generate_Hex_OnlyHexChars()
    {
        var result = _sut.Generate(new FakeDataOptions { FormatId = "string-hex", Seed = 1 });
        Assert.True(result.All(c => "0123456789abcdef".Contains(c)));
    }

    // ── Lorem Ipsum ──────────────────────────────────────────────────────────

    [Fact]
    public void Generate_Lorem_IsEightWords()
    {
        var result = _sut.Generate(new FakeDataOptions { FormatId = "string-lorem", Seed = 1 });
        Assert.Equal(8, result.Split(' ').Length);
    }

    [Fact]
    public void Generate_Lorem_OnlyLowercaseWords()
    {
        var result = _sut.Generate(new FakeDataOptions { FormatId = "string-lorem", Seed = 1 });
        Assert.True(result.Replace(" ", "").All(c => char.IsLower(c)));
    }

    // ── Seeded determinism ───────────────────────────────────────────────────

    [Theory]
    [InlineData("string-alphanumeric")]
    [InlineData("string-alpha")]
    [InlineData("string-hex")]
    [InlineData("string-lorem")]
    public void Generate_SameSeed_ReturnsSameValue(string formatId)
    {
        var opts = new FakeDataOptions { FormatId = formatId, Seed = 55 };
        Assert.Equal(_sut.Generate(opts), _sut.Generate(opts));
    }
}
