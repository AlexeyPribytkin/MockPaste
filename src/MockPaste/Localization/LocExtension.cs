using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace MockPaste.Localization;

[MarkupExtensionReturnType(typeof(object))]
public sealed class Loc : MarkupExtension
{
    public string Key { get; set; } = string.Empty;

    public Loc()
    {
    }

    public Loc(string key)
    {
        Key = key;
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var binding = new Binding($"[{Key}]")
        {
            Source = LocalizationManager.Instance,
            Mode = BindingMode.OneWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        };

        return binding.ProvideValue(serviceProvider);
    }
}
