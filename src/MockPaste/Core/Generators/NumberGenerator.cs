using System.Globalization;
using MockPaste.Core.Models;

namespace MockPaste.Core.Generators;

/// <summary>
/// Generates random numeric values: integer, decimal, percentage, and single byte.
/// </summary>
public sealed class NumberGenerator() : FakeDataGeneratorBase([
    new("number-integer",    "Integer",    "Random integer (0–99999)",    opt => Rng(opt).Next(0, 100_000).ToString(CultureInfo.InvariantCulture)),
    new("number-decimal",    "Decimal",    "Random decimal (0.00–999.99)", opt => (Rng(opt).NextDouble() * 1000).ToString("F2", CultureInfo.InvariantCulture)),
    new("number-percentage", "Percentage", "Random percentage (0–100)",   opt => Rng(opt).Next(0, 101).ToString(CultureInfo.InvariantCulture) + "%"),
    new("number-byte",       "Byte",       "Random byte (0–255)",         opt => Rng(opt).Next(0, 256).ToString(CultureInfo.InvariantCulture)),
])
{
    public override string CategoryName => "Number";
    public override int Order => 2;
    public override string MnemonicKey => "N";

    /// <summary>Returns a seeded <see cref="Random"/> when a seed is provided, otherwise <see cref="Random.Shared"/>.</summary>
    private static Random Rng(FakeDataOptions options) =>
        options.Seed.HasValue ? new Random(options.Seed.Value) : Random.Shared;
}
