using MockPaste.Core.Models;

namespace MockPaste.Core.Generators;

public sealed class GuidGenerator : IFakeDataGenerator
{
    public string CategoryName => "GUID";
    public string MnemonicKey => "G";

    public IReadOnlyList<DataFormat> SupportedFormats { get; } =
    [
        new() { FormatId = "guid-standard",  Name = "Standard",  Description = "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx" },
        new() { FormatId = "guid-nodashes",  Name = "No Dashes", Description = "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx" },
        new() { FormatId = "guid-uppercase", Name = "Uppercase", Description = "XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX" },
        new() { FormatId = "guid-braced",    Name = "Braced",    Description = "{xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx}" },
    ];

    public string Generate(FakeDataOptions options) => options.FormatId switch
    {
        "guid-standard"  => Guid.NewGuid().ToString("D"),
        "guid-nodashes"  => Guid.NewGuid().ToString("N"),
        "guid-uppercase" => Guid.NewGuid().ToString("D").ToUpperInvariant(),
        "guid-braced"    => Guid.NewGuid().ToString("B"),
        _                => Guid.NewGuid().ToString("D"),
    };
}
