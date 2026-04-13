using MockPaste.Core.Models;

namespace MockPaste.Core.Generators;

public sealed class EmailGenerator : IFakeDataGenerator
{
    private static readonly string[] FirstNames =
        ["james", "mary", "john", "linda", "robert", "susan", "michael", "jessica", "david", "sarah",
         "alex", "taylor", "jordan", "casey", "morgan", "riley", "quinn", "avery", "blake", "cameron"];

    private static readonly string[] LastNames =
        ["smith", "johnson", "brown", "williams", "jones", "garcia", "miller", "davis", "martinez", "wilson",
         "anderson", "thomas", "jackson", "white", "harris", "clark", "lewis", "lee", "walker", "hall"];

    private static readonly string[] Domains =
        ["example.com", "test.org", "demo.net", "sample.io", "mock.dev",
         "fakecorp.com", "testmail.org", "devbox.net"];

    public string CategoryName => "Email";
    public string MnemonicKey => "E";

    public IReadOnlyList<DataFormat> SupportedFormats { get; } =
    [
        new() { FormatId = "email-standard",  Name = "Standard",   Description = "firstname.lastname@domain.com" },
        new() { FormatId = "email-numbered",  Name = "Numbered",   Description = "user1234@domain.com" },
        new() { FormatId = "email-simple",    Name = "Simple",     Description = "user@domain.com" },
        new() { FormatId = "email-plus",      Name = "Plus alias", Description = "user+tag@domain.com" },
    ];

    public string Generate(FakeDataOptions options)
    {
        var rng = options.Seed.HasValue ? new Random(options.Seed.Value) : Random.Shared;
        var first = FirstNames[rng.Next(FirstNames.Length)];
        var last = LastNames[rng.Next(LastNames.Length)];
        var domain = Domains[rng.Next(Domains.Length)];
        var num = rng.Next(100, 9999);

        return options.FormatId switch
        {
            "email-standard" => $"{first}.{last}@{domain}",
            "email-numbered" => $"{first}{num}@{domain}",
            "email-simple"   => $"{first[0]}{last}@{domain}",
            "email-plus"     => $"{first}.{last}+test{num}@{domain}",
            _                => $"{first}.{last}@{domain}",
        };
    }
}
