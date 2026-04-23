using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using MockPaste.Core.Generators;
using MockPaste.Infrastructure;

namespace MockPaste.UI.Popup;

public partial class PopupWindow : Window
{
    private readonly GeneratorRegistry _generators;
    private readonly HistoryService _history;
    private IFakeDataGenerator? _currentGenerator;
    private bool _isFormatLevel;
    private bool _isHistoryLevel;
    private bool _suppressDeactivate;

    public IntPtr TargetWindow { get; set; }

    public event Action<string, string>? FormatSelected;
    public event Action<string>? HistoryItemSelected;

    public PopupWindow(GeneratorRegistry generators, HistoryService history)
    {
        _generators = generators;
        _history = history;
        InitializeComponent();
        PreviewKeyDown += PopupWindow_PreviewKeyDown;

        // Force native window (HWND) creation so the first ShowAtCursor()
        // behaves the same as subsequent calls. Without this, the first
        // Show() triggers HWND creation which can race with
        // activation/deactivation and cause the popup to silently fail.
        new WindowInteropHelper(this).EnsureHandle();
    }

    public void ShowAtCursor()
    {
        _isFormatLevel = false;
        _currentGenerator = null;
        ShowCategories();

        const double shadowPadding = 14;
        var dpi = VisualTreeHelper.GetDpi(this);
        Infrastructure.Native.NativeMethods.GetCursorPos(out var pt);
        double x = pt.X / dpi.DpiScaleX - shadowPadding;
        double y = pt.Y / dpi.DpiScaleY - shadowPadding;

        var screen = SystemParameters.WorkArea;
        UpdateLayout();
        if (x + ActualWidth > screen.Right) x = screen.Right - ActualWidth;
        if (y + ActualHeight > screen.Bottom) y = screen.Bottom - ActualHeight;
        if (x < screen.Left) x = screen.Left;
        if (y < screen.Top) y = screen.Top;

        Left = x;
        Top = y;

        _suppressDeactivate = true;
        Show();
        Activate();
        FocusSelectedItem();
        _suppressDeactivate = false;
    }

    public void HidePopup()
    {
        Hide();
    }

    private void ShowCategories()
    {
        _isFormatLevel = false;
        _isHistoryLevel = false;
        _currentGenerator = null;
        HeaderText.Text = "MockPaste";
        HeaderText.Cursor = Cursors.Arrow;
        HistoryButton.Visibility = Visibility.Visible;
        EmptyHistoryText.Visibility = Visibility.Collapsed;

        var items = _generators.GetAll().Select(g => new MenuItemViewModel
        {
            DisplayName = g.CategoryName,
            MnemonicKey = g.MnemonicKey,
            CategoryName = g.CategoryName,
            HasSubMenu = true
        }).ToList();

        MenuList.ItemsSource = items;
        if (items.Count > 0) MenuList.SelectedIndex = 0;
        FocusSelectedItem();
    }

    private void ShowFormats(IFakeDataGenerator generator)
    {
        _isFormatLevel = true;
        _isHistoryLevel = false;
        _currentGenerator = generator;
        HeaderText.Text = $"← {generator.CategoryName}";
        HeaderText.Cursor = Cursors.Hand;
        HistoryButton.Visibility = Visibility.Collapsed;
        EmptyHistoryText.Visibility = Visibility.Collapsed;

        var items = generator.SupportedFormats.Select(f => new MenuItemViewModel
        {
            DisplayName = f.Name,
            Description = f.Description,
            CategoryName = generator.CategoryName,
            FormatId = f.FormatId,
            HasSubMenu = false
        }).ToList();

        MenuList.ItemsSource = items;
        if (items.Count > 0) MenuList.SelectedIndex = 0;
        FocusSelectedItem();
    }

    private void ShowHistory()
    {
        _isHistoryLevel = true;
        _isFormatLevel = false;
        _currentGenerator = null;
        HeaderText.Text = "← History";
        HeaderText.Cursor = Cursors.Hand;
        HistoryButton.Visibility = Visibility.Collapsed;

        var entries = _history.GetAll();
        if (entries.Count == 0)
        {
            MenuList.ItemsSource = null;
            EmptyHistoryText.Visibility = Visibility.Visible;
        }
        else
        {
            EmptyHistoryText.Visibility = Visibility.Collapsed;
            MenuList.ItemsSource = entries.Select(e => new HistoryItemViewModel
            {
                Value = e.Value,
                CategoryName = e.CategoryName,
                FormatName = e.FormatName
            }).ToList();
        }

        if (MenuList.Items.Count > 0) MenuList.SelectedIndex = 0;
        FocusSelectedItem();
    }

    private void FocusSelectedItem()
    {
        if (MenuList.SelectedIndex < 0) return;
        MenuList.UpdateLayout();
        if (MenuList.ItemContainerGenerator.ContainerFromIndex(MenuList.SelectedIndex) is ListBoxItem container)
            container.Focus();
        else
            MenuList.Focus();
    }

    private void HeaderText_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_isFormatLevel || _isHistoryLevel)
            ShowCategories();
    }

    private void SelectCurrentItem()
    {
        if (MenuList.SelectedItem is HistoryItemViewModel histItem)
        {
            _suppressDeactivate = true;
            HidePopup();
            HistoryItemSelected?.Invoke(histItem.Value);
            _suppressDeactivate = false;
            return;
        }

        if (MenuList.SelectedItem is not MenuItemViewModel item) return;

        if (!_isFormatLevel)
        {
            var gen = _generators.Get(item.CategoryName);
            if (gen is not null) ShowFormats(gen);
        }
        else
        {
            _suppressDeactivate = true;
            HidePopup();
            FormatSelected?.Invoke(item.CategoryName, item.FormatId);
            _suppressDeactivate = false;
        }
    }

    private void PopupWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Escape:
                if (_isFormatLevel || _isHistoryLevel)
                    ShowCategories();
                else
                    HidePopup();
                e.Handled = true;
                break;

            case Key.Enter:
                SelectCurrentItem();
                e.Handled = true;
                break;

            case Key.Right:
                if (_isHistoryLevel) ShowCategories();
                else if (!_isFormatLevel) SelectCurrentItem();
                e.Handled = true;
                break;

            case Key.Left:
                if (_isFormatLevel) ShowCategories();
                else if (!_isHistoryLevel) ShowHistory();
                e.Handled = true;
                break;

            default:
                if (HandleMnemonic(e.Key))
                    e.Handled = true;
                break;
        }
    }

    private bool HandleMnemonic(Key key)
    {
        var keyChar = key.ToString();
        if (keyChar.Length != 1) return false;

        if (MenuList.ItemsSource is IEnumerable<MenuItemViewModel> items)
        {
            var match = items.FirstOrDefault(i =>
                i.MnemonicKey.Equals(keyChar, StringComparison.OrdinalIgnoreCase));
            if (match is not null)
            {
                MenuList.SelectedItem = match;
                SelectCurrentItem();
                return true;
            }
        }
        return false;
    }

    private void MenuList_KeyDown(object sender, KeyEventArgs e)
    {
        // Handled by PreviewKeyDown
    }

    private void HistoryButton_Click(object sender, RoutedEventArgs e)
    {
        ShowHistory();
    }

    private void MenuList_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is FrameworkElement { DataContext: MenuItemViewModel or HistoryItemViewModel })
            SelectCurrentItem();
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState != MouseButtonState.Pressed) return;
        // When HeaderText is a back button, let its MouseLeftButtonUp fire instead.
        bool isBackButton = (_isFormatLevel || _isHistoryLevel) &&
                            IsDescendantOf(e.OriginalSource as DependencyObject, HeaderText);
        if (!isBackButton)
            DragMove();
    }

    private static bool IsDescendantOf(DependencyObject? element, DependencyObject? ancestor)
    {
        while (element is not null)
        {
            if (element == ancestor) return true;
            element = VisualTreeHelper.GetParent(element);
        }
        return false;
    }

    private void Window_Deactivated(object? sender, EventArgs e)
    {
        if (!_suppressDeactivate)
            HidePopup();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        e.Cancel = true;
        HidePopup();
    }
}
