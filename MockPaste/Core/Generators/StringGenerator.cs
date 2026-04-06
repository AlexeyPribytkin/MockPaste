using MockPaste.Core.Models;

namespace MockPaste.Core.Generators;

public sealed class StringGenerator : IFakeDataGenerator
{
    private const string AlphaChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string AlphanumericChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    private const string HexChars = "0123456789abcdef";

    private static readonly string[] LoremWords =
        ["lorem", "ipsum", "dolor", "sit", "amet", "consectetur", "adipiscing", "elit",
         "sed", "do", "eiusmod", "tempor", "incididunt", "ut", "labore", "et", "dolore",
         "magna", "aliqua", "enim", "ad", "minim", "veniam", "quis", "nostrud"];

    public string CategoryName => "String";
    public string MnemonicKey => "S";

    public IReadOnlyList<DataFormat> SupportedFormats { get; } =
    [
        new() { FormatId = "string-alphanumeric", Name = "Alphanumeric", Description = "Random letters and digits (16 chars)" },
        new() { FormatId = "string-alpha",        Name = "Alpha Only",   Description = "Random letters (16 chars)" },
        new() { FormatId = "string-hex",          Name = "Hex String",   Description = "Random hex string (32 chars)" },
        new() { FormatId = "string-lorem",        Name = "Lorem Ipsum",  Description = "Random lorem ipsum words" },
    ];

    public string Generate(FakeDataOptions options)
    {
        var rng = options.Seed.HasValue ? new Random(options.Seed.Value) : Random.Shared;
        int length = 16;
        if (options.Parameters.TryGetValue("length", out var lenStr) && int.TryParse(lenStr, out var l))
            length = l;

        return options.FormatId switch
        {
            "string-alphanumeric" => RandomString(rng, AlphanumericChars, length),
            "string-alpha"        => RandomString(rng, AlphaChars, length),
            "string-hex"          => RandomString(rng, HexChars, 32),
            "string-lorem"        => LoremIpsum(rng, 8),
            _                     => RandomString(rng, AlphanumericChars, length),
        };
    }

    private static string RandomString(Random rng, string chars, int length) =>
        new(Enumerable.Range(0, length).Select(_ => chars[rng.Next(chars.Length)]).ToArray());

    private static string LoremIpsum(Random rng, int wordCount) =>
        string.Join(' ', Enumerable.Range(0, wordCount).Select(_ => LoremWords[rng.Next(LoremWords.Length)]));
}
