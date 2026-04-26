using MockPaste.Core.Models;

namespace MockPaste.Core.Generators;

/// <summary>
/// Generates random strings in four formats: alphanumeric, letters-only, hexadecimal,
/// and lorem-ipsum words. Length can be controlled via the <c>"length"</c> parameter
/// in <see cref="FakeDataOptions.Parameters"/>.
/// </summary>
public sealed class StringGenerator() : FakeDataGeneratorBase([
    new("string-alphanumeric", "Alphanumeric", "Random letters and digits (16 chars)", opt => RandomString(opt, AlphanumericChars, defaultLength: 16)),
    new("string-alpha",        "Alpha Only",   "Random letters (16 chars)",            opt => RandomString(opt, AlphaChars, defaultLength: 16)),
    new("string-hex",          "Hex String",   "Random hex string (32 chars)",         opt => RandomString(opt, HexChars, defaultLength: 32)),
    new("string-lorem",        "Lorem Ipsum",  "Random lorem ipsum words",             opt => LoremIpsum(opt, 8)),
])
{
    public override string CategoryName => "String";
    public override int Order => 5;
    public override string MnemonicKey => "S";

    private const string AlphaChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string AlphanumericChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    private const string HexChars = "0123456789abcdef";

    private static readonly string[] LoremWords =
        ["lorem", "ipsum", "dolor", "sit", "amet", "consectetur", "adipiscing", "elit",
         "sed", "do", "eiusmod", "tempor", "incididunt", "ut", "labore", "et", "dolore",
         "magna", "aliqua", "enim", "ad", "minim", "veniam", "quis", "nostrud"];

    /// <summary>
    /// Builds a random string of the requested length by drawing characters from <paramref name="chars"/>.
    /// The length can be overridden via the <c>"length"</c> key in <see cref="FakeDataOptions.Parameters"/>.
    /// </summary>
    private static string RandomString(FakeDataOptions options, string chars, int defaultLength)
    {
        var rng = options.Seed.HasValue ? new Random(options.Seed.Value) : Random.Shared;
        int length = defaultLength;
        if (options.Parameters.TryGetValue("length", out var lenStr) && int.TryParse(lenStr, out var l))
            length = l;
        return new(Enumerable.Range(0, length).Select(_ => chars[rng.Next(chars.Length)]).ToArray());
    }

    /// <summary>Picks <paramref name="wordCount"/> random words from the lorem-ipsum word list and joins them with spaces.</summary>
    private static string LoremIpsum(FakeDataOptions options, int wordCount)
    {
        var rng = options.Seed.HasValue ? new Random(options.Seed.Value) : Random.Shared;
        return string.Join(' ', Enumerable.Range(0, wordCount).Select(_ => LoremWords[rng.Next(LoremWords.Length)]));
    }
}
