using MockPaste.Core.Models;

namespace MockPaste.Core.Generators;

public interface IFakeDataGenerator
{
    string CategoryName { get; }
    string MnemonicKey { get; }
    int Order { get; }
    IReadOnlyList<DataFormat> SupportedFormats { get; }
    string Generate(FakeDataOptions options);
}
