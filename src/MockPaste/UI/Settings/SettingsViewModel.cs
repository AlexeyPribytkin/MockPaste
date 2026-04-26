using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MockPaste.Core.Models;

namespace MockPaste.UI.Settings;

public sealed class SettingsViewModel : INotifyPropertyChanged
{
    private readonly AppSettings _settings;
    private HotkeyConfig _pendingHotkey;

    // snapshot used to detect unsaved changes
    private HotkeyConfig _snapshotHotkey = HotkeyConfig.Default;
    private bool _snapshotPreserveClipboard;
    private string _snapshotPasteDelay = string.Empty;
    private bool _snapshotLaunchAtStartup;
    private string _snapshotHistorySize = string.Empty;
    private bool _snapshotTrackClipboardHistory;
    private AppTheme _snapshotTheme;

    // backing fields
    private bool _isCapturing;
    private string _hotkeyDisplayText = string.Empty;
    private bool _preserveClipboard;
    private string _pasteDelayText = string.Empty;
    private bool _launchAtStartup;
    private string _historySizeText = string.Empty;
    private bool _trackClipboardHistory;
    private bool _isThemeDark;
    private bool _isThemeLight;
    private bool _isThemeSystem;
    private string _statusMessage = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;
    public Func<AppSettings, bool>? SettingsSaved;

    public Func<string, string> ResourceResolver { get; set; } =
        key => System.Windows.Application.Current?.Resources[key] as string ?? key;

    public SettingsViewModel(AppSettings settings)
    {
        _settings = settings;
        _pendingHotkey = settings.Hotkey.Clone();

        SaveCommand = new RelayCommand(Save, () => IsDirty && IsPasteDelayValid && IsHistorySizeValid);
        ChangeHotkeyCommand = new RelayCommand(ToggleCapture);
        ResetHotkeyCommand = new RelayCommand(ResetHotkey);

        LoadFromSettings();
        TakeSnapshot();
    }

    // ── Commands ─────────────────────────────────────────────────────────────

    public ICommand SaveCommand { get; }
    public ICommand ChangeHotkeyCommand { get; }
    public ICommand ResetHotkeyCommand { get; }

    // ── Properties ───────────────────────────────────────────────────────────

    public bool IsCapturing
    {
        get => _isCapturing;
        private set => SetField(ref _isCapturing, value);
    }

    public string HotkeyDisplayText
    {
        get => _hotkeyDisplayText;
        private set => SetField(ref _hotkeyDisplayText, value);
    }

    public bool PreserveClipboard
    {
        get => _preserveClipboard;
        set => SetDirtyField(ref _preserveClipboard, value);
    }

    public string PasteDelayText
    {
        get => _pasteDelayText;
        set
        {
            if (SetDirtyField(ref _pasteDelayText, value))
            {
                Notify(nameof(IsPasteDelayValid));
                Notify(nameof(PasteDelay));
                Notify(nameof(PasteDelayDisplay));
            }
        }
    }

    public int PasteDelay
    {
        get => AppSettings.TryParsePasteDelay(_pasteDelayText, out var v) ? v : AppSettings.PasteDelayDefault;
        set => PasteDelayText = value.ToString();
    }

    public string PasteDelayDisplay => $"{PasteDelay} {Res("StringUnitMilliseconds")}";

    public bool LaunchAtStartup
    {
        get => _launchAtStartup;
        set => SetDirtyField(ref _launchAtStartup, value);
    }

    public bool TrackClipboardHistory
    {
        get => _trackClipboardHistory;
        set => SetDirtyField(ref _trackClipboardHistory, value);
    }

    public string HistorySizeText
    {
        get => _historySizeText;
        set
        {
            if (SetDirtyField(ref _historySizeText, value))
            {
                Notify(nameof(IsHistorySizeValid));
                Notify(nameof(HistorySize));
                Notify(nameof(HistorySizeDisplay));
            }
        }
    }

    public int HistorySize
    {
        get => AppSettings.TryParseHistorySize(_historySizeText, out var v) ? v : AppSettings.HistorySizeDefault;
        set => HistorySizeText = value.ToString();
    }

    public string HistorySizeDisplay
    {
        get
        {
            var count = HistorySize;
            var unit = count == 1 ? Res("StringUnitItem") : Res("StringUnitItems");
            return $"{count} {unit}";
        }
    }

    public bool IsThemeDark
    {
        get => _isThemeDark;
        set => SetDirtyField(ref _isThemeDark, value);
    }

    public bool IsThemeLight
    {
        get => _isThemeLight;
        set => SetDirtyField(ref _isThemeLight, value);
    }

    public bool IsThemeSystem
    {
        get => _isThemeSystem;
        set => SetDirtyField(ref _isThemeSystem, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetField(ref _statusMessage, value);
    }

    public bool IsDirty => HasChanges();
    public bool IsPasteDelayValid => AppSettings.TryParsePasteDelay(_pasteDelayText, out _);
    public bool IsHistorySizeValid => AppSettings.TryParseHistorySize(_historySizeText, out _);

    // ── Capture API (called by code-behind) ──────────────────────────────────

    /// <summary>Updates the hotkey display with a partial modifier hint while keys are held.</summary>
    internal void ShowCaptureHint(string hint)
    {
        if (_hotkeyDisplayText != hint)
        {
            _hotkeyDisplayText = hint;
            Notify(nameof(HotkeyDisplayText));
        }
    }

    public void CancelCapture()
    {
        IsCapturing = false;
        HotkeyDisplayText = _pendingHotkey.ToDisplayString();
    }

    public void AcceptHotkey(HotkeyConfig config)
    {
        _pendingHotkey = config;
        HotkeyDisplayText = config.ToDisplayString();
        IsCapturing = false;
        NotifyDirty();
        SetStatus(string.Format(Res("StringStatusHotkeySet"), config.ToDisplayString()));
    }

    internal void SetStatus(string message) => StatusMessage = message;

    /// <summary>Formats active modifier keys into a human-readable prefix string like "Ctrl + Alt + ".</summary>
    public static string FormatModifiers(ModifierKeys modifiers)
    {
        var parts = new List<string>();
        if (modifiers.HasFlag(ModifierKeys.Control)) parts.Add("Ctrl");
        if (modifiers.HasFlag(ModifierKeys.Alt)) parts.Add("Alt");
        if (modifiers.HasFlag(ModifierKeys.Shift)) parts.Add("Shift");
        if (modifiers.HasFlag(ModifierKeys.Windows)) parts.Add("Win");
        return parts.Count > 0 ? string.Join(" + ", parts) + " + " : "";
    }

    // ── Private ──────────────────────────────────────────────────────────────

    private AppTheme CurrentTheme => _isThemeSystem ? AppTheme.System : _isThemeLight ? AppTheme.Light : AppTheme.Dark;

    private void LoadFromSettings()
    {
        _hotkeyDisplayText = _pendingHotkey.ToDisplayString();
        _preserveClipboard = _settings.PreserveClipboard;
        _pasteDelayText = _settings.PasteDelayMs.ToString();
        _launchAtStartup = _settings.LaunchAtStartup;
        _trackClipboardHistory = _settings.TrackClipboardHistory;
        _historySizeText = _settings.HistorySize.ToString();

        (_isThemeDark, _isThemeLight, _isThemeSystem) = _settings.Theme switch
        {
            AppTheme.Light => (false, true, false),
            AppTheme.System => (false, false, true),
            _ => (true, false, false)
        };
    }

    private void TakeSnapshot()
    {
        _snapshotHotkey = _pendingHotkey.Clone();
        _snapshotPreserveClipboard = _preserveClipboard;
        _snapshotPasteDelay = _pasteDelayText;
        _snapshotLaunchAtStartup = _launchAtStartup;
        _snapshotTrackClipboardHistory = _trackClipboardHistory;
        _snapshotHistorySize = _historySizeText;
        _snapshotTheme = CurrentTheme;
    }

    private bool HasChanges() =>
        !_pendingHotkey.Equals(_snapshotHotkey)
        || _preserveClipboard != _snapshotPreserveClipboard
        || _pasteDelayText != _snapshotPasteDelay
        || _launchAtStartup != _snapshotLaunchAtStartup
        || _trackClipboardHistory != _snapshotTrackClipboardHistory
        || _historySizeText != _snapshotHistorySize
        || CurrentTheme != _snapshotTheme;

    private void NotifyDirty()
    {
        Notify(nameof(IsDirty));
        ((RelayCommand)SaveCommand).NotifyCanExecuteChanged();
    }

    private void ToggleCapture()
    {
        if (_isCapturing)
        {
            CancelCapture();
        }
        else
        {
            StartCapture();
        }
    }

    private void StartCapture()
    {
        IsCapturing = true;
        HotkeyDisplayText = Res("StringStatusCapturePrompt");
        StatusMessage = string.Empty;
    }

    private void ResetHotkey()
    {
        if (_isCapturing)
        {
            CancelCapture();
        }

        _pendingHotkey = HotkeyConfig.Default;
        HotkeyDisplayText = _pendingHotkey.ToDisplayString();
        NotifyDirty();
        SetStatus(Res("StringStatusHotkeyReset"));
    }

    private void Save()
    {
        _settings.Hotkey = _pendingHotkey;
        _settings.PreserveClipboard = _preserveClipboard;
        _settings.LaunchAtStartup = _launchAtStartup;
        _settings.TrackClipboardHistory = _trackClipboardHistory;
        if (AppSettings.TryParsePasteDelay(_pasteDelayText, out var delay))
        {
            _settings.PasteDelayMs = delay;
        }
        if (AppSettings.TryParseHistorySize(_historySizeText, out var size))
        {
            _settings.HistorySize = size;
        }
        _settings.Theme = CurrentTheme;

        var saved = SettingsSaved?.Invoke(_settings) ?? true;
        TakeSnapshot();
        NotifyDirty();
        SetStatus(saved ? Res("StringStatusSaved") : Res("StringStatusSaveFailed"));
    }

    private string Res(string key) => ResourceResolver(key);

    private bool SetDirtyField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        Notify(name);
        NotifyDirty();
        return true;
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        Notify(name);
        return true;
    }

    private void Notify([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
