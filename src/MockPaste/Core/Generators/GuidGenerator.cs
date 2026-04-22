namespace MockPaste.Core.Generators;

public sealed class GuidGenerator() : FakeDataGeneratorBase([
    new("guid-standard",  "Standard",  "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx", _ => Guid.NewGuid().ToString("D")),
    new("guid-nodashes",  "No Dashes", "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",      _ => Guid.NewGuid().ToString("N")),
    new("guid-uppercase", "Uppercase", "XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX", _ => Guid.NewGuid().ToString("D").ToUpperInvariant()),
    new("guid-braced",    "Braced",    "{xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx}", _ => Guid.NewGuid().ToString("B")),
])
{
    public override string CategoryName => "GUID";
    public override int Order => 1;
    public override string MnemonicKey => "G";
}
