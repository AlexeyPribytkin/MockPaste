using System.Globalization;
using MockPaste.Core.Models;

namespace MockPaste.Core.Generators;

public sealed class NumberGenerator : IFakeDataGenerator
{
    public string CategoryName => "Number";
    public string MnemonicKey => "N";

    public IReadOnlyList<DataFormat> SupportedFormats { get; } =
    [
        new() { FormatId = "number-integer",    Name = "Integer",    Description = "Random integer (0–99999)" },
        new() { FormatId = "number-decimal",    Name = "Decimal",    Description = "Random decimal (0.00–999.99)" },
        new() { FormatId = "number-percentage", Name = "Percentage", Description = "Random percentage (0–100)" },
        new() { FormatId = "number-byte",       Name = "Byte",       Description = "Random byte (0–255)" },
    ];

    public string Generate(FakeDataOptions options)
    {
        var rng = options.Seed.HasValue ? new Random(options.Seed.Value) : Random.Shared;

        return options.FormatId switch
        {
            "number-integer"    => rng.Next(0, 100_000).ToString(CultureInfo.InvariantCulture),
            "number-decimal"    => (rng.NextDouble() * 1000).ToString("F2", CultureInfo.InvariantCulture),
            "number-percentage" => rng.Next(0, 101).ToString(CultureInfo.InvariantCulture) + "%",
            "number-byte"       => rng.Next(0, 256).ToString(CultureInfo.InvariantCulture),
            _                   => rng.Next(0, 100_000).ToString(CultureInfo.InvariantCulture),
        };
    }
}
