using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using MockPaste.Core.Models;

namespace MockPaste.UI.Settings;

public partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _vm;

    public Func<AppSettings, bool>? SettingsSaved;

    public SettingsWindow(AppSettings settings)
    {
        _vm = new SettingsViewModel(settings);
        InitializeComponent();
        DataContext = _vm;

        _vm.SettingsSaved = s => SettingsSaved?.Invoke(s) ?? true;
        _vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SettingsViewModel.StatusMessage))
            {
                AnimateStatus();
            }
            else if (e.PropertyName == nameof(SettingsViewModel.IsCapturing))
            {
                SyncCaptureSubscription();
            }
        };

        DataObject.AddPastingHandler(PasteDelayBox, OnDigitsOnlyPaste);
        DataObject.AddPastingHandler(HistorySizeBox, OnDigitsOnlyPaste);
    }

    // Subscribes or unsubscribes the raw key capture handler based on VM state.
    private void SyncCaptureSubscription()
    {
        if (_vm.IsCapturing)
        {
            PreviewKeyDown += CaptureKeyDown;
        }
        else
        {
            PreviewKeyDown -= CaptureKeyDown;
        }
    }

    // Fades the status text in, holds, then fades it out.
    private void AnimateStatus()
    {
        StatusText.BeginAnimation(OpacityProperty, null);

        if (string.IsNullOrEmpty(_vm.StatusMessage))
        {
            StatusText.Opacity = 0;
            return;
        }

        StatusText.Opacity = 1;
        var fade = new DoubleAnimation(1, 0, new Duration(TimeSpan.FromSeconds(1.5)))
        {
            BeginTime = TimeSpan.FromSeconds(2)
        };
        StatusText.BeginAnimation(OpacityProperty, fade);
    }

    private void CaptureKeyDown(object sender, KeyEventArgs e)
    {
        e.Handled = true;
        var key = e.Key == Key.System ? e.SystemKey : e.Key;

        if (key == Key.Escape)
        {
            _vm.CancelCapture();
            return;
        }

        if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt
            or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin)
        {
            _vm.ShowCaptureHint(FormatModifiers(Keyboard.Modifiers) + "...");
            return;
        }

        var config = new HotkeyConfig
        {
            Ctrl = Keyboard.Modifiers.HasFlag(ModifierKeys.Control),
            Alt = Keyboard.Modifiers.HasFlag(ModifierKeys.Alt),
            Shift = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift),
            Win = Keyboard.Modifiers.HasFlag(ModifierKeys.Windows),
            Key = key
        };

        if (!config.IsValid())
        {
            _vm.SetStatus(System.Windows.Application.Current.Resources["StringStatusHotkeyModifierRequired"] as string ?? "");
            return;
        }

        _vm.AcceptHotkey(config);
    }

    private static string FormatModifiers(ModifierKeys modifiers)
    {
        var parts = new List<string>();
        if (modifiers.HasFlag(ModifierKeys.Control)) parts.Add("Ctrl");
        if (modifiers.HasFlag(ModifierKeys.Alt)) parts.Add("Alt");
        if (modifiers.HasFlag(ModifierKeys.Shift)) parts.Add("Shift");
        if (modifiers.HasFlag(ModifierKeys.Windows)) parts.Add("Win");
        return parts.Count > 0 ? string.Join(" + ", parts) + " + " : "";
    }

    private bool ConfirmDiscard()
    {
        if (!_vm.IsDirty) return true;
        var msg = System.Windows.Application.Current.Resources["StringDiscardChangesMessage"] as string ?? "";
        return MessageBox.Show(msg, "MockPaste", MessageBoxButton.YesNo, MessageBoxImage.Question)
               == MessageBoxResult.Yes;
    }


    private void TitleBarClose_Click(object sender, RoutedEventArgs e)
    {
        if (ConfirmDiscard())
        {
            Close();
        }
    }
    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        if (ConfirmDiscard())
        {
            Close();
        }
    }

    private void DigitsOnly_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !e.Text.All(char.IsDigit);
    }

    private static void OnDigitsOnlyPaste(object sender, DataObjectPastingEventArgs e)
    {
        if (e.DataObject.GetDataPresent(DataFormats.Text))
        {
            var text = e.DataObject.GetData(DataFormats.Text) as string;
            if (text?.All(char.IsDigit) != true)
                e.CancelCommand();
        }
        else
        {
            e.CancelCommand();
        }
    }
}
