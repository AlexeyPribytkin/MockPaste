using MockPaste.Core.Generators;
using MockPaste.Core.Models;

namespace MockPaste.Tests.Generators;

public sealed class PhoneGeneratorTests
{
    private readonly PhoneGenerator _sut = new();

    // ── Metadata ─────────────────────────────────────────────────────────────

    [Fact]
    public void CategoryName_IsPhone() =>
        Assert.Equal("Phone", _sut.CategoryName);

    [Fact]
    public void MnemonicKey_IsSingleChar() =>
        Assert.Equal(1, _sut.MnemonicKey.Length);

    [Fact]
    public void SupportedFormats_HasFourEntries() =>
        Assert.Equal(4, _sut.SupportedFormats.Count);

    // ── Output shape ─────────────────────────────────────────────────────────

    [Fact]
    public void Generate_US_MatchesParenthesisFormat()
    {
        var result = _sut.Generate(new FakeDataOptions { FormatId = "phone-us", Seed = 1 });
        // (NXX) NXX-XXXX
        Assert.Matches(@"^\(\d{3}\) \d{3}-\d{4}$", result);
    }

    [Fact]
    public void Generate_International_StartsWithPlusOne()
    {
        var result = _sut.Generate(new FakeDataOptions { FormatId = "phone-international", Seed = 1 });
        Assert.StartsWith("+1-", result);
        Assert.Matches(@"^\+1-\d{3}-\d{3}-\d{4}$", result);
    }

    [Fact]
    public void Generate_DigitsOnly_ContainsOnlyDigits()
    {
        var result = _sut.Generate(new FakeDataOptions { FormatId = "phone-digits", Seed = 1 });
        Assert.True(result.All(char.IsDigit));
        Assert.Equal(10, result.Length);
    }

    [Fact]
    public void Generate_Dotted_UsesDotSeparators()
    {
        var result = _sut.Generate(new FakeDataOptions { FormatId = "phone-dotted", Seed = 1 });
        Assert.Matches(@"^\d{3}\.\d{3}\.\d{4}$", result);
    }

    // ── Seeded determinism ───────────────────────────────────────────────────

    [Theory]
    [InlineData("phone-us")]
    [InlineData("phone-international")]
    [InlineData("phone-digits")]
    [InlineData("phone-dotted")]
    public void Generate_SameSeed_ReturnsSameValue(string formatId)
    {
        var opts = new FakeDataOptions { FormatId = formatId, Seed = 7 };
        Assert.Equal(_sut.Generate(opts), _sut.Generate(opts));
    }
}
