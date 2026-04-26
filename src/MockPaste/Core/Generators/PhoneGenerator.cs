using MockPaste.Core.Models;

namespace MockPaste.Core.Generators;

/// <summary>
/// Generates fictional phone numbers in four formats: US parenthetical, international
/// E.164-style, digits-only, and dotted.
/// </summary>
public sealed class PhoneGenerator() : FakeDataGeneratorBase([
    new("phone-us",            "US",            "(555) 123-4567",   opt => Generate(opt, "us")),
    new("phone-international", "International", "+1-555-123-4567",  opt => Generate(opt, "international")),
    new("phone-digits",        "Digits Only",   "5551234567",       opt => Generate(opt, "digits")),
    new("phone-dotted",        "Dotted",        "555.123.4567",     opt => Generate(opt, "dotted")),
])
{
    public override string CategoryName => "Phone";
    public override int Order => 4;
    public override string MnemonicKey => "P";

    /// <summary>Produces a single fake phone number for the given <paramref name="variant"/> style.</summary>
    private static string Generate(FakeDataOptions options, string variant)
    {
        var rng = options.Seed.HasValue ? new Random(options.Seed.Value) : Random.Shared;
        int area = rng.Next(200, 999);
        int prefix = rng.Next(200, 999);
        int line = rng.Next(1000, 9999);

        return variant switch
        {
            "international" => $"+1-{area}-{prefix}-{line}",
            "digits" => $"{area}{prefix}{line}",
            "dotted" => $"{area}.{prefix}.{line}",
            _ => $"({area}) {prefix}-{line}",
        };
    }
}
