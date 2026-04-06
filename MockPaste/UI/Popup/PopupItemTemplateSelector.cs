using System.Windows;
using System.Windows.Controls;

namespace MockPaste.UI.Popup;

public sealed class PopupItemTemplateSelector : DataTemplateSelector
{
    public DataTemplate? MenuTemplate { get; set; }
    public DataTemplate? HistoryTemplate { get; set; }

    public override DataTemplate? SelectTemplate(object item, DependencyObject container) =>
        item is HistoryItemViewModel ? HistoryTemplate : MenuTemplate;
}
