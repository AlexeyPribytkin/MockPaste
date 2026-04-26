using System.Windows;
using System.Windows.Controls;

namespace MockPaste.UI.Popup;

/// <summary>
/// Selects the correct <see cref="DataTemplate"/> for each popup list item:
/// <see cref="HistoryTemplate"/> for history entries and <see cref="MenuTemplate"/>
/// for all other items (<see cref="MenuItemViewModel"/>).
/// </summary>
public sealed class PopupItemTemplateSelector : DataTemplateSelector
{
    /// <summary>Template used for <see cref="MenuItemViewModel"/> items (categories and formats).</summary>
    public DataTemplate? MenuTemplate { get; set; }

    /// <summary>Template used for <see cref="HistoryItemViewModel"/> items.</summary>
    public DataTemplate? HistoryTemplate { get; set; }

    /// <inheritdoc/>
    public override DataTemplate? SelectTemplate(object item, DependencyObject container) =>
        item is HistoryItemViewModel ? HistoryTemplate : MenuTemplate;
}
