using MockPaste.Core.Generators;
using MockPaste.Core.Models;

namespace MockPaste.Tests.Generators;

public sealed class EmailGeneratorTests
{
    private readonly EmailGenerator _sut = new();

    // ── Metadata ─────────────────────────────────────────────────────────────

    [Fact]
    public void CategoryName_IsEmail() =>
        Assert.Equal("Email", _sut.CategoryName);

    [Fact]
    public void MnemonicKey_IsSingleChar() =>
        Assert.Equal(1, _sut.MnemonicKey.Length);

    [Fact]
    public void SupportedFormats_HasFourEntries() =>
        Assert.Equal(4, _sut.SupportedFormats.Count);

    [Fact]
    public void SupportedFormats_ReturnsSameInstance()
    {
        var first  = _sut.SupportedFormats;
        var second = _sut.SupportedFormats;
        Assert.Same(first, second);
    }

    // ── Output shape ─────────────────────────────────────────────────────────

    [Theory]
    [InlineData("email-standard")]
    [InlineData("email-numbered")]
    [InlineData("email-simple")]
    [InlineData("email-plus")]
    public void Generate_ContainsAtSign(string formatId)
    {
        var result = _sut.Generate(new FakeDataOptions { FormatId = formatId });
        Assert.Contains("@", result);
    }

    [Fact]
    public void Generate_Standard_MatchesFirstDotLastAtDomain()
    {
        var result = _sut.Generate(new FakeDataOptions { FormatId = "email-standard", Seed = 42 });
        // firstname.lastname@domain — two parts separated by dot before @
        var local = result.Split('@')[0];
        Assert.Contains(".", local);
    }

    [Fact]
    public void Generate_Numbered_ContainsDigits()
    {
        var result = _sut.Generate(new FakeDataOptions { FormatId = "email-numbered", Seed = 42 });
        var local = result.Split('@')[0];
        Assert.Contains(local, char.IsDigit);
    }

    [Fact]
    public void Generate_Plus_ContainsPlusSign()
    {
        var result = _sut.Generate(new FakeDataOptions { FormatId = "email-plus", Seed = 42 });
        Assert.Contains("+", result);
    }

    // ── Seeded determinism ───────────────────────────────────────────────────

    [Theory]
    [InlineData("email-standard")]
    [InlineData("email-numbered")]
    [InlineData("email-simple")]
    [InlineData("email-plus")]
    public void Generate_SameSeed_ReturnsSameValue(string formatId)
    {
        var opts = new FakeDataOptions { FormatId = formatId, Seed = 99 };
        Assert.Equal(_sut.Generate(opts), _sut.Generate(opts));
    }

    // ── Unknown format falls back to default ─────────────────────────────────

    [Fact]
    public void Generate_UnknownFormatId_FallsBackToDefault()
    {
        var result = _sut.Generate(new FakeDataOptions { FormatId = "does-not-exist", Seed = 1 });
        Assert.Contains("@", result);
    }
}
