using System.Globalization;
using MockPaste.Core.Generators;
using MockPaste.Core.Models;

namespace MockPaste.Tests.Generators;

public sealed class NumberGeneratorTests
{
    private readonly NumberGenerator _sut = new();

    // ── Metadata ─────────────────────────────────────────────────────────────

    [Fact]
    public void CategoryName_IsNumber() =>
        Assert.Equal("Number", _sut.CategoryName);

    [Fact]
    public void MnemonicKey_IsSingleChar() =>
        Assert.Equal(1, _sut.MnemonicKey.Length);

    [Fact]
    public void SupportedFormats_HasFourEntries() =>
        Assert.Equal(4, _sut.SupportedFormats.Count);

    // ── Integer ──────────────────────────────────────────────────────────────

    [Fact]
    public void Generate_Integer_IsInRange()
    {
        for (int seed = 0; seed < 20; seed++)
        {
            var result = _sut.Generate(new FakeDataOptions { FormatId = "number-integer", Seed = seed });
            var value = int.Parse(result, CultureInfo.InvariantCulture);
            Assert.InRange(value, 0, 99_999);
        }
    }

    // ── Decimal ──────────────────────────────────────────────────────────────

    [Fact]
    public void Generate_Decimal_IsTwoDecimalPlaces()
    {
        var result = _sut.Generate(new FakeDataOptions { FormatId = "number-decimal", Seed = 1 });
        Assert.Matches(@"^\d+\.\d{2}$", result);
    }

    [Fact]
    public void Generate_Decimal_IsInRange()
    {
        for (int seed = 0; seed < 20; seed++)
        {
            var result = _sut.Generate(new FakeDataOptions { FormatId = "number-decimal", Seed = seed });
            var value = double.Parse(result, CultureInfo.InvariantCulture);
            Assert.InRange(value, 0.0, 1000.0);
        }
    }

    // ── Percentage ───────────────────────────────────────────────────────────

    [Fact]
    public void Generate_Percentage_EndsWithPercent()
    {
        var result = _sut.Generate(new FakeDataOptions { FormatId = "number-percentage", Seed = 1 });
        Assert.EndsWith("%", result);
    }

    [Fact]
    public void Generate_Percentage_IsInRange()
    {
        for (int seed = 0; seed < 20; seed++)
        {
            var result = _sut.Generate(new FakeDataOptions { FormatId = "number-percentage", Seed = seed });
            var value = int.Parse(result.TrimEnd('%'), CultureInfo.InvariantCulture);
            Assert.InRange(value, 0, 100);
        }
    }

    // ── Byte ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Generate_Byte_IsInRange()
    {
        for (int seed = 0; seed < 20; seed++)
        {
            var result = _sut.Generate(new FakeDataOptions { FormatId = "number-byte", Seed = seed });
            var value = int.Parse(result, CultureInfo.InvariantCulture);
            Assert.InRange(value, 0, 255);
        }
    }

    // ── Seeded determinism ───────────────────────────────────────────────────

    [Theory]
    [InlineData("number-integer")]
    [InlineData("number-decimal")]
    [InlineData("number-percentage")]
    [InlineData("number-byte")]
    public void Generate_SameSeed_ReturnsSameValue(string formatId)
    {
        var opts = new FakeDataOptions { FormatId = formatId, Seed = 42 };
        Assert.Equal(_sut.Generate(opts), _sut.Generate(opts));
    }
}
