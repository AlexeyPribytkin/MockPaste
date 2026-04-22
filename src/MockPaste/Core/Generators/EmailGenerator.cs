using MockPaste.Core.Models;

namespace MockPaste.Core.Generators;

public sealed class EmailGenerator() : FakeDataGeneratorBase([
    new("email-standard", "Standard",   "firstname.lastname@domain.com", opt => Generate(opt, "standard")),
    new("email-numbered", "Numbered",   "user1234@domain.com",           opt => Generate(opt, "numbered")),
    new("email-simple",   "Simple",     "user@domain.com",               opt => Generate(opt, "simple")),
    new("email-plus",     "Plus alias", "user+tag@domain.com",           opt => Generate(opt, "plus")),
])
{
    public override string CategoryName => "Email";
    public override int Order => 3;
    public override string MnemonicKey => "E";

    private static readonly string[] FirstNames =
        ["james", "mary", "john", "linda", "robert", "susan", "michael", "jessica", "david", "sarah",
         "alex", "taylor", "jordan", "casey", "morgan", "riley", "quinn", "avery", "blake", "cameron"];

    private static readonly string[] LastNames =
        ["smith", "johnson", "brown", "williams", "jones", "garcia", "miller", "davis", "martinez", "wilson",
         "anderson", "thomas", "jackson", "white", "harris", "clark", "lewis", "lee", "walker", "hall"];

    private static readonly string[] Domains =
        ["example.com", "test.org", "demo.net", "sample.io", "mock.dev",
         "fakecorp.com", "testmail.org", "devbox.net"];

    private static string Generate(FakeDataOptions options, string variant)
    {
        var rng = options.Seed.HasValue ? new Random(options.Seed.Value) : Random.Shared;
        var first = FirstNames[rng.Next(FirstNames.Length)];
        var last = LastNames[rng.Next(LastNames.Length)];
        var domain = Domains[rng.Next(Domains.Length)];
        var num = rng.Next(100, 9999);

        return variant switch
        {
            "numbered" => $"{first}{num}@{domain}",
            "simple" => $"{first[0]}{last}@{domain}",
            "plus" => $"{first}.{last}+test{num}@{domain}",
            _ => $"{first}.{last}@{domain}",
        };
    }
}
