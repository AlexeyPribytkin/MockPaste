using MockPaste.Core.Models;

namespace MockPaste.Core.Generators;

public sealed class PhoneGenerator : IFakeDataGenerator
{
    public string CategoryName => "Phone";
    public string MnemonicKey => "P";

    public IReadOnlyList<DataFormat> SupportedFormats { get; } =
    [
        new() { FormatId = "phone-us",            Name = "US",            Description = "(555) 123-4567" },
        new() { FormatId = "phone-international",  Name = "International", Description = "+1-555-123-4567" },
        new() { FormatId = "phone-digits",         Name = "Digits Only",   Description = "5551234567" },
        new() { FormatId = "phone-dotted",         Name = "Dotted",        Description = "555.123.4567" },
    ];

    public string Generate(FakeDataOptions options)
    {
        var rng = options.Seed.HasValue ? new Random(options.Seed.Value) : Random.Shared;
        int area = rng.Next(200, 999);
        int prefix = rng.Next(200, 999);
        int line = rng.Next(1000, 9999);

        return options.FormatId switch
        {
            "phone-us"           => $"({area}) {prefix}-{line}",
            "phone-international" => $"+1-{area}-{prefix}-{line}",
            "phone-digits"       => $"{area}{prefix}{line}",
            "phone-dotted"       => $"{area}.{prefix}.{line}",
            _                    => $"({area}) {prefix}-{line}",
        };
    }
}
