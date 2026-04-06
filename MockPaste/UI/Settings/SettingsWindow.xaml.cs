using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using MockPaste.Core.Models;
using MockPaste.Infrastructure;

namespace MockPaste.UI.Settings;

public partial class SettingsWindow : Window
{
    private readonly AppSettings _settings;
    private HotkeyConfig _pendingHotkey;
    private bool _isCapturing;

    // snapshot used to detect unsaved changes
    private string _snapshotHotkey = string.Empty;
    private bool _snapshotPreserveClipboard;
    private string _snapshotPasteDelay = string.Empty;
    private bool _snapshotLaunchAtStartup;
    private string _snapshotHistorySize = string.Empty;
    private string _snapshotTheme = string.Empty;

    public event Action<AppSettings>? SettingsSaved;

    public SettingsWindow(AppSettings settings)
    {
        _settings = settings;
        _pendingHotkey = settings.Hotkey.Clone();
        InitializeComponent();
        LoadFromSettings();
        TakeSnapshot();
        SubscribeChangeHandlers();
    }

    private void LoadFromSettings()
    {
        HotkeyDisplay.Text = _pendingHotkey.ToDisplayString();
        PreserveClipboardCheck.IsChecked = _settings.PreserveClipboard;
        PasteDelayBox.Text = _settings.PasteDelayMs.ToString();
        LaunchStartupCheck.IsChecked = _settings.LaunchAtStartup;
        HistorySizeBox.Text = _settings.HistorySize.ToString();

        (ThemeDark.IsChecked, ThemeLight.IsChecked, ThemeSystem.IsChecked) = _settings.Theme switch
        {
            "Light"  => (false, true,  false),
            "System" => (false, false, true),
            _        => (true,  false, false)
        };
    }

    private string CurrentTheme => ThemeSystem.IsChecked == true ? "System"
                                 : ThemeLight.IsChecked  == true ? "Light"
                                 : "Dark";

    private void TakeSnapshot()
    {
        _snapshotHotkey             = _pendingHotkey.ToDisplayString();
        _snapshotPreserveClipboard  = PreserveClipboardCheck.IsChecked == true;
        _snapshotPasteDelay         = PasteDelayBox.Text;
        _snapshotLaunchAtStartup    = LaunchStartupCheck.IsChecked == true;
        _snapshotHistorySize        = HistorySizeBox.Text;
        _snapshotTheme              = CurrentTheme;
    }

    private bool HasChanges() =>
        _pendingHotkey.ToDisplayString()        != _snapshotHotkey
        || (PreserveClipboardCheck.IsChecked == true) != _snapshotPreserveClipboard
        || PasteDelayBox.Text                   != _snapshotPasteDelay
        || (LaunchStartupCheck.IsChecked == true)     != _snapshotLaunchAtStartup
        || HistorySizeBox.Text                  != _snapshotHistorySize
        || CurrentTheme                         != _snapshotTheme;

    private void UpdateSaveButton() => SaveButton.IsEnabled = HasChanges();

    private void SubscribeChangeHandlers()
    {
        PasteDelayBox.TextChanged           += OnSettingChanged;
        HistorySizeBox.TextChanged          += OnSettingChanged;
        PreserveClipboardCheck.Checked      += OnSettingChanged;
        PreserveClipboardCheck.Unchecked    += OnSettingChanged;
        LaunchStartupCheck.Checked          += OnSettingChanged;
        LaunchStartupCheck.Unchecked        += OnSettingChanged;
        ThemeDark.Checked                   += OnSettingChanged;
        ThemeLight.Checked                  += OnSettingChanged;
        ThemeSystem.Checked                 += OnSettingChanged;
    }

    private void OnSettingChanged(object sender, RoutedEventArgs e) => UpdateSaveButton();

    // Shows a status message that fades out after a short hold.
    private void ShowStatus(string message)
    {
        StatusText.BeginAnimation(OpacityProperty, null);
        StatusText.Opacity = 1;
        StatusText.Text = message;
        var fade = new DoubleAnimation(1, 0, new Duration(TimeSpan.FromSeconds(1.5)))
        {
            BeginTime = TimeSpan.FromSeconds(2)
        };
        StatusText.BeginAnimation(OpacityProperty, fade);
    }

    private void ChangeHotkey_Click(object sender, RoutedEventArgs e)
    {
        if (_isCapturing)
        {
            StopCapture();
            return;
        }
        StartCapture();
    }

    private void StartCapture()
    {
        _isCapturing = true;
        HotkeyDisplay.Text = "Press a key combination...";
        ChangeHotkeyButton.Content = "Cancel";
        StatusText.BeginAnimation(OpacityProperty, null);
        StatusText.Text = "";
        PreviewKeyDown += CaptureKeyDown;
    }

    private void StopCapture()
    {
        _isCapturing = false;
        HotkeyDisplay.Text = _pendingHotkey.ToDisplayString();
        ChangeHotkeyButton.Content = "Change";
        PreviewKeyDown -= CaptureKeyDown;
        UpdateSaveButton();
    }

    private void CaptureKeyDown(object sender, KeyEventArgs e)
    {
        e.Handled = true;
        var key = e.Key == Key.System ? e.SystemKey : e.Key;

        if (key == Key.Escape)
        {
            StopCapture();
            return;
        }

        if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt
            or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin)
        {
            var modifiers = Keyboard.Modifiers;
            HotkeyDisplay.Text = FormatModifiers(modifiers) + "...";
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
            ShowStatus("At least one modifier key (Ctrl, Alt, Shift, Win) is required.");
            return;
        }

        _pendingHotkey = config;
        ShowStatus($"Hotkey set to: {config.ToDisplayString()}");
        StopCapture();
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

    private void ResetHotkey_Click(object sender, RoutedEventArgs e)
    {
        _pendingHotkey = HotkeyConfig.Default;
        HotkeyDisplay.Text = _pendingHotkey.ToDisplayString();
        ShowStatus("Hotkey reset to default.");
        UpdateSaveButton();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        _settings.Hotkey = _pendingHotkey;
        _settings.PreserveClipboard = PreserveClipboardCheck.IsChecked == true;
        _settings.LaunchAtStartup = LaunchStartupCheck.IsChecked == true;
        if (int.TryParse(PasteDelayBox.Text, out var delay) && delay is >= 0 and <= 500)
            _settings.PasteDelayMs = delay;
        if (int.TryParse(HistorySizeBox.Text, out var historySize) && historySize is >= 1 and <= 500)
            _settings.HistorySize = historySize;
        _settings.Theme = CurrentTheme;

        SettingsSaved?.Invoke(_settings);
        TakeSnapshot();
        UpdateSaveButton();
        ShowStatus("Settings saved.");
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void TitleBarClose_Click(object sender, RoutedEventArgs e) => Close();
}
