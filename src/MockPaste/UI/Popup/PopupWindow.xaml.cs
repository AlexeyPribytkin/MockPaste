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
    private readonly PopupViewModel _vm;
    private bool _suppressDeactivate;

    public IntPtr TargetWindow { get; set; }

    // Forwarded from VM so callers don't need to know about the VM type.
    public event Action<string, string>? FormatSelected;
    public event Action<string>? HistoryItemSelected;

    public PopupWindow(GeneratorRegistry generators, HistoryService history)
    {
        _vm = new PopupViewModel(generators, history);
        InitializeComponent();
        DataContext = _vm;
        PreviewKeyDown += PopupWindow_PreviewKeyDown;

        _vm.CloseRequested += () =>
        {
            try
            {
                _suppressDeactivate = true;
                HidePopup();
            }
            finally
            {
                _suppressDeactivate = false;
            }
        };
        _vm.FormatSelected += (cat, fmt) => FormatSelected?.Invoke(cat, fmt);
        _vm.HistoryItemSelected += val => HistoryItemSelected?.Invoke(val);
        _vm.PropertyChanged += OnVmPropertyChanged;

        // Force native window (HWND) creation so the first ShowAtCursor()
        // behaves the same as subsequent calls. Without this, the first
        // Show() triggers HWND creation which can race with
        // activation/deactivation and cause the popup to silently fail.
        new WindowInteropHelper(this).EnsureHandle();
    }

    public void ShowAtCursor()
    {
        _vm.ShowCategories();

        const double shadowPadding = 14;
        var dpi = VisualTreeHelper.GetDpi(this);
        Infrastructure.Native.NativeMethods.GetCursorPos(out var pt);
        double x = pt.X / dpi.DpiScaleX - shadowPadding;
        double y = pt.Y / dpi.DpiScaleY - shadowPadding;

        var screen = SystemParameters.WorkArea;
        UpdateLayout();
        if (x + ActualWidth > screen.Right) { x = screen.Right - ActualWidth; }
        if (y + ActualHeight > screen.Bottom) { y = screen.Bottom - ActualHeight; }
        if (x < screen.Left) { x = screen.Left; }
        if (y < screen.Top) { y = screen.Top; }

        Left = x;
        Top = y;

        try
        {
            _suppressDeactivate = true;
            Show();
            Activate();
            FocusSelectedItem();
        }
        finally
        {
            _suppressDeactivate = false;
        }
    }

    public void HidePopup()
    {
        Hide();
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(PopupViewModel.IsBackButton):
                HeaderText.Cursor = _vm.IsBackButton ? Cursors.Hand : Cursors.Arrow;
                break;

            case nameof(PopupViewModel.Items):
                Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, FocusSelectedItem);
                break;
        }
    }

    private void FocusSelectedItem()
    {
        if (MenuList.SelectedIndex < 0)
        {
            return;
        }

        MenuList.ScrollIntoView(MenuList.SelectedItem);
        if (MenuList.ItemContainerGenerator.ContainerFromIndex(MenuList.SelectedIndex) is ListBoxItem container)
        {
            container.Focus();
        }
        else
        {
            MenuList.Focus();
        }
    }

    private void HeaderText_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_vm.IsBackButton)
        {
            _vm.ShowCategories();
        }
    }

    private void PopupWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Escape:
                if (_vm.IsFormatLevel || _vm.IsHistoryLevel)
                {
                    _vm.ShowCategories();
                }
                else
                {
                    HidePopup();
                }
                e.Handled = true;
                break;

            case Key.Enter:
                _vm.SelectCurrentItem();
                e.Handled = true;
                break;

            case Key.Right:
                if (_vm.IsHistoryLevel)
                {
                    _vm.ShowCategories();
                }
                else if (!_vm.IsFormatLevel)
                {
                    _vm.SelectCurrentItem();
                }
                e.Handled = true;
                break;

            case Key.Left:
                if (_vm.IsFormatLevel)
                {
                    _vm.ShowCategories();
                }
                else if (!_vm.IsHistoryLevel)
                {
                    _vm.ShowHistory();
                }
                e.Handled = true;
                break;

            default:
                if (_vm.HandleMnemonic(new KeyConverter().ConvertToString(e.Key) ?? string.Empty))
                {
                    e.Handled = true;
                }
                break;
        }
    }

    private void MenuList_KeyDown(object sender, KeyEventArgs e)
    {
        // Handled by PreviewKeyDown
    }

    private void HistoryButton_Click(object sender, RoutedEventArgs e)
    {
        _vm.ShowHistory();
    }

    private void MenuList_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is FrameworkElement { DataContext: MenuItemViewModel or HistoryItemViewModel })
        {
            _vm.SelectCurrentItem();
        }
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState != MouseButtonState.Pressed)
        {
            return;
        }

        bool isBackButton =
            _vm.IsBackButton && IsDescendantOf(e.OriginalSource as DependencyObject, HeaderText);
        if (!isBackButton)
        {
            DragMove();
        }
    }

    private static bool IsDescendantOf(DependencyObject? element, DependencyObject? ancestor)
    {
        while (element is not null)
        {
            if (element == ancestor)
            {
                return true;
            }
            element = VisualTreeHelper.GetParent(element);
        }
        return false;
    }

    private void Window_Deactivated(object? sender, EventArgs e)
    {
        if (!_suppressDeactivate)
        {
            HidePopup();
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        _vm.PropertyChanged -= OnVmPropertyChanged;
        base.OnClosed(e);
    }

    // The window is intentionally never closed; OnClosing cancels all close attempts
    // to keep the native HWND alive for the lifetime of the application.
    protected override void OnClosing(CancelEventArgs e)
    {
        e.Cancel = true;
        HidePopup();
    }
}
