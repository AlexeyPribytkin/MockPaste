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
    private HotkeyConfig _snapshotHotkey      = HotkeyConfig.Default;
    private bool   _snapshotPreserveClipboard;
    private string _snapshotPasteDelay        = string.Empty;
    private bool   _snapshotLaunchAtStartup;
    private string _snapshotHistorySize       = string.Empty;
    private AppTheme _snapshotTheme;

    // backing fields
    private bool   _isCapturing;
    private string _hotkeyDisplayText         = string.Empty;
    private bool   _preserveClipboard;
    private string _pasteDelayText            = string.Empty;
    private bool   _launchAtStartup;
    private string _historySizeText           = string.Empty;
    private bool   _isThemeDark;
    private bool   _isThemeLight;
    private bool   _isThemeSystem;
    private string _statusMessage             = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;
    public Func<AppSettings, bool>?           SettingsSaved;

    public SettingsViewModel(AppSettings settings)
    {
        _settings      = settings;
        _pendingHotkey = settings.Hotkey.Clone();

        SaveCommand        = new RelayCommand(Save, () => IsDirty && IsPasteDelayValid && IsHistorySizeValid);
        ChangeHotkeyCommand = new RelayCommand(ToggleCapture);
        ResetHotkeyCommand  = new RelayCommand(ResetHotkey);

        LoadFromSettings();
        TakeSnapshot();
    }

    // ── Commands ─────────────────────────────────────────────────────────────

    public ICommand SaveCommand         { get; }
    public ICommand ChangeHotkeyCommand { get; }
    public ICommand ResetHotkeyCommand  { get; }

    // ── Properties ───────────────────────────────────────────────────────────

    public bool IsCapturing
    {
        get => _isCapturing;
        private set
        {
            if (_isCapturing == value) return;
            _isCapturing = value;
            Notify();
        }
    }

    public string HotkeyDisplayText
    {
        get => _hotkeyDisplayText;
        private set { if (_hotkeyDisplayText != value) { _hotkeyDisplayText = value; Notify(); } }
    }

    public bool PreserveClipboard
    {
        get => _preserveClipboard;
        set { if (_preserveClipboard != value) { _preserveClipboard = value; Notify(); NotifyDirty(); } }
    }

    public string PasteDelayText
    {
        get => _pasteDelayText;
        set { if (_pasteDelayText != value) { _pasteDelayText = value; Notify(); Notify(nameof(IsPasteDelayValid)); NotifyDirty(); } }
    }

    public bool LaunchAtStartup
    {
        get => _launchAtStartup;
        set { if (_launchAtStartup != value) { _launchAtStartup = value; Notify(); NotifyDirty(); } }
    }

    public string HistorySizeText
    {
        get => _historySizeText;
        set { if (_historySizeText != value) { _historySizeText = value; Notify(); Notify(nameof(IsHistorySizeValid)); NotifyDirty(); } }
    }

    public bool IsThemeDark
    {
        get => _isThemeDark;
        set { if (_isThemeDark != value) { _isThemeDark = value; Notify(); NotifyDirty(); } }
    }

    public bool IsThemeLight
    {
        get => _isThemeLight;
        set { if (_isThemeLight != value) { _isThemeLight = value; Notify(); NotifyDirty(); } }
    }

    public bool IsThemeSystem
    {
        get => _isThemeSystem;
        set { if (_isThemeSystem != value) { _isThemeSystem = value; Notify(); NotifyDirty(); } }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set { if (_statusMessage != value) { _statusMessage = value; Notify(); } }
    }

    public bool IsDirty => HasChanges();
    public bool IsPasteDelayValid  => AppSettings.TryParsePasteDelay(_pasteDelayText,  out _);
    public bool IsHistorySizeValid => AppSettings.TryParseHistorySize(_historySizeText, out _);

    // ── Capture API (called by code-behind) ──────────────────────────────────

    /// <summary>Updates the hotkey display with a partial modifier hint while keys are held.</summary>
    internal void ShowCaptureHint(string hint)
    {
        if (_hotkeyDisplayText == hint) return;
        _hotkeyDisplayText = hint;
        Notify(nameof(HotkeyDisplayText));
    }

    public void CancelCapture()
    {
        IsCapturing = false;
        HotkeyDisplayText = _pendingHotkey.ToDisplayString();
    }

    public void AcceptHotkey(HotkeyConfig config)
    {
        _pendingHotkey    = config;
        HotkeyDisplayText = config.ToDisplayString();
        IsCapturing       = false;
        NotifyDirty();
        SetStatus(string.Format(Res("StringStatusHotkeySet"), config.ToDisplayString()));
    }

    internal void SetStatus(string message) => StatusMessage = message;

    // ── Private ──────────────────────────────────────────────────────────────

    private AppTheme CurrentTheme => _isThemeSystem ? AppTheme.System : _isThemeLight ? AppTheme.Light : AppTheme.Dark;

    private void LoadFromSettings()
    {
        _hotkeyDisplayText = _pendingHotkey.ToDisplayString();
        _preserveClipboard = _settings.PreserveClipboard;
        _pasteDelayText    = _settings.PasteDelayMs.ToString();
        _launchAtStartup   = _settings.LaunchAtStartup;
        _historySizeText   = _settings.HistorySize.ToString();

        (_isThemeDark, _isThemeLight, _isThemeSystem) = _settings.Theme switch
        {
            AppTheme.Light  => (false, true,  false),
            AppTheme.System => (false, false, true),
            _               => (true,  false, false)
        };
    }

    private void TakeSnapshot()
    {
        _snapshotHotkey            = _pendingHotkey.Clone();
        _snapshotPreserveClipboard = _preserveClipboard;
        _snapshotPasteDelay        = _pasteDelayText;
        _snapshotLaunchAtStartup   = _launchAtStartup;
        _snapshotHistorySize       = _historySizeText;
        _snapshotTheme             = CurrentTheme;
    }

    private bool HasChanges() =>
        !_pendingHotkey.Equals(_snapshotHotkey)
        || _preserveClipboard   != _snapshotPreserveClipboard
        || _pasteDelayText               != _snapshotPasteDelay
        || _launchAtStartup              != _snapshotLaunchAtStartup
        || _historySizeText              != _snapshotHistorySize
        || CurrentTheme                  != _snapshotTheme;

    private void NotifyDirty()
    {
        Notify(nameof(IsDirty));
        ((RelayCommand)SaveCommand).NotifyCanExecuteChanged();
    }

    private void ToggleCapture()
    {
        if (_isCapturing) CancelCapture();
        else StartCapture();
    }

    private void StartCapture()
    {
        IsCapturing       = true;
        HotkeyDisplayText = Res("StringStatusCapturePrompt");
        StatusMessage     = string.Empty;
    }

    private void ResetHotkey()
    {
        _pendingHotkey    = HotkeyConfig.Default;
        HotkeyDisplayText = _pendingHotkey.ToDisplayString();
        NotifyDirty();
        SetStatus(Res("StringStatusHotkeyReset"));
    }

    private void Save()
    {
        _settings.Hotkey            = _pendingHotkey;
        _settings.PreserveClipboard = _preserveClipboard;
        _settings.LaunchAtStartup   = _launchAtStartup;
        if (AppSettings.TryParsePasteDelay(_pasteDelayText,  out var delay)) _settings.PasteDelayMs = delay;
        if (AppSettings.TryParseHistorySize(_historySizeText, out var size))  _settings.HistorySize  = size;
        _settings.Theme             = CurrentTheme;

        var saved = SettingsSaved?.Invoke(_settings) ?? true;
        TakeSnapshot();
        NotifyDirty();
        SetStatus(saved ? Res("StringStatusSaved") : Res("StringStatusSaveFailed"));
    }

    private static string Res(string key) =>
        System.Windows.Application.Current.Resources[key] as string ?? key;

    private void Notify([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
