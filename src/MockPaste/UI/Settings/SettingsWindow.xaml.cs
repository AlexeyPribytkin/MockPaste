using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using MockPaste.Core.Models;
using MockPaste.UI.Dialogs;

namespace MockPaste.UI.Settings;

public partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _vm;
    private readonly Action? _unregisterHotkey;
    private readonly Action? _reregisterHotkey;

    public SettingsWindow(AppSettings settings, Func<AppSettings, bool> settingsSaved, Action? unregisterHotkey, Action? reregisterHotkey)
    {
        _unregisterHotkey = unregisterHotkey;
        _reregisterHotkey = reregisterHotkey;

        _vm = new SettingsViewModel(settings);
        InitializeComponent();
        DataContext = _vm;

        _vm.SettingsSaved = settingsSaved;
        _vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SettingsViewModel.StatusMessage))
            {
                AnimateStatus();
            }
            else if (e.PropertyName == nameof(SettingsViewModel.IsCapturing))
            {
                // Subscribes or unsubscribes the raw key capture handler based on VM state.
                // Registers or unregisters the hotkey handler.
                if (_vm.IsCapturing)
                {
                    _unregisterHotkey?.Invoke();
                    PreviewKeyDown += CaptureKeyDown;
                }
                else
                {
                    PreviewKeyDown -= CaptureKeyDown;
                    _reregisterHotkey?.Invoke();
                }
            }
        };


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
            _vm.ShowCaptureHint(SettingsViewModel.FormatModifiers(Keyboard.Modifiers) + "...");
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

    private bool ConfirmDiscard()
    {
        if (!_vm.IsDirty)
        {
            return true;
        }
        var msg = System.Windows.Application.Current.Resources["StringDiscardChangesMessage"] as string ?? "";
        return MessageDialog.Confirm(msg, "MockPaste", this);
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

    private void About_Click(object sender, RoutedEventArgs e)
    {
        var aboutWindow = new AboutWindow
        {
            Owner = this
        };
        aboutWindow.ShowDialog();
    }

    }

