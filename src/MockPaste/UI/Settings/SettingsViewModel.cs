using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using MockPaste.Core.Models;

namespace MockPaste.UI.Settings;

/// <summary>
/// ViewModel for the Settings window. Exposes all configurable application options
/// (hotkey, paste delay, clipboard history, theme, startup behavior) as bindable
/// properties, tracks unsaved changes via a snapshot, and provides commands for
/// saving, resetting, and capturing a new hotkey.
/// </summary>
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

    /// <summary>Raised whenever a bindable property value changes.</summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Callback invoked when the user saves settings. Receives the updated <see cref="AppSettings"/>
    /// and should return <c>true</c> on success or <c>false</c> if persisting failed.
    /// </summary>
    public Func<AppSettings, bool>? SettingsSaved;

    /// <summary>
    /// Resolves a resource key to its localized string value. Defaults to looking up
    /// keys in <see cref="System.Windows.Application.Current"/> resources.
    /// Can be replaced in tests to avoid a live WPF application.
    /// </summary>
    public Func<string, string> ResourceResolver { get; set; } =
        key => System.Windows.Application.Current?.Resources[key] as string ?? key;

    /// <summary>
    /// Initializes the ViewModel from the supplied <paramref name="settings"/>, wires up
    /// commands, loads current values into bindable properties, and takes an initial
    /// snapshot so that <see cref="IsDirty"/> starts as <c>false</c>.
    /// </summary>
    /// <param name="settings">The application settings model to read from and write to.</param>
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

    /// <summary>Persists all pending changes. Enabled only when <see cref="IsDirty"/>, <see cref="IsPasteDelayValid"/>, and <see cref="IsHistorySizeValid"/> are all <c>true</c>.</summary>
    public ICommand SaveCommand { get; }

    /// <summary>Toggles hotkey capture mode on or off.</summary>
    public ICommand ChangeHotkeyCommand { get; }

    /// <summary>Resets the hotkey to its default value and cancels any active capture.</summary>
    public ICommand ResetHotkeyCommand { get; }

    // ── Properties ───────────────────────────────────────────────────────────

    /// <summary>Indicates whether the UI is currently waiting for the user to press a hotkey combination.</summary>
    public bool IsCapturing
    {
        get => _isCapturing;
        private set => SetField(ref _isCapturing, value);
    }

    /// <summary>Human-readable representation of the current (pending) hotkey, or a capture prompt / modifier hint during capture mode.</summary>
    public string HotkeyDisplayText
    {
        get => _hotkeyDisplayText;
        private set => SetField(ref _hotkeyDisplayText, value);
    }

    /// <summary>When <c>true</c>, the original clipboard content is restored after a paste operation.</summary>
    public bool PreserveClipboard
    {
        get => _preserveClipboard;
        set => SetDirtyField(ref _preserveClipboard, value);
    }

    /// <summary>Raw text input for the paste delay. Validated via <see cref="IsPasteDelayValid"/>.</summary>
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

    /// <summary>Parsed paste delay in milliseconds, or the default value when the current text is invalid.</summary>
    public int PasteDelay
    {
        get => AppSettings.TryParsePasteDelay(_pasteDelayText, out var v) ? v : AppSettings.PasteDelayDefault;
        set => PasteDelayText = value.ToString();
    }

    /// <summary>Localized display string for the paste delay, e.g. "150 ms".</summary>
    public string PasteDelayDisplay => $"{PasteDelay} {Res("StringUnitMilliseconds")}";

    /// <summary>When <c>true</c>, the application is registered to start automatically with Windows.</summary>
    public bool LaunchAtStartup
    {
        get => _launchAtStartup;
        set => SetDirtyField(ref _launchAtStartup, value);
    }

    /// <summary>When <c>true</c>, clipboard entries are recorded to the history list.</summary>
    public bool TrackClipboardHistory
    {
        get => _trackClipboardHistory;
        set => SetDirtyField(ref _trackClipboardHistory, value);
    }

    /// <summary>Raw text input for the maximum clipboard history size. Validated via <see cref="IsHistorySizeValid"/>.</summary>
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

    /// <summary>Parsed maximum number of clipboard history entries, or the default value when the current text is invalid.</summary>
    public int HistorySize
    {
        get => AppSettings.TryParseHistorySize(_historySizeText, out var v) ? v : AppSettings.HistorySizeDefault;
        set => HistorySizeText = value.ToString();
    }

    /// <summary>Localized display string for the history size, e.g. "50 items".</summary>
    public string HistorySizeDisplay
    {
        get
        {
            var count = HistorySize;
            var unit = count == 1 ? Res("StringUnitItem") : Res("StringUnitItems");
            return $"{count} {unit}";
        }
    }

    /// <summary>When <c>true</c>, the Dark theme radio button is selected.</summary>
    public bool IsThemeDark
    {
        get => _isThemeDark;
        set => SetDirtyField(ref _isThemeDark, value);
    }

    /// <summary>When <c>true</c>, the Light theme radio button is selected.</summary>
    public bool IsThemeLight
    {
        get => _isThemeLight;
        set => SetDirtyField(ref _isThemeLight, value);
    }

    /// <summary>When <c>true</c>, the System (follow OS) theme radio button is selected.</summary>
    public bool IsThemeSystem
    {
        get => _isThemeSystem;
        set => SetDirtyField(ref _isThemeSystem, value);
    }

    /// <summary>Feedback message shown in the settings UI, e.g. "Saved" or "Hotkey reset".</summary>
    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetField(ref _statusMessage, value);
    }

    /// <summary><c>true</c> when any property differs from the last saved snapshot, enabling the Save command.</summary>
    public bool IsDirty => HasChanges();

    /// <summary><c>true</c> when <see cref="PasteDelayText"/> can be parsed as a valid paste delay.</summary>
    public bool IsPasteDelayValid => AppSettings.TryParsePasteDelay(_pasteDelayText, out _);

    /// <summary><c>true</c> when <see cref="HistorySizeText"/> can be parsed as a valid history size.</summary>
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

    /// <summary>
    /// Cancels an active hotkey capture session and restores the display text
    /// to the previously pending hotkey without modifying it.
    /// </summary>
    public void CancelCapture()
    {
        IsCapturing = false;
        HotkeyDisplayText = _pendingHotkey.ToDisplayString();
    }

    /// <summary>
    /// Accepts a newly captured hotkey combination, updates the pending hotkey and
    /// display text, ends capture mode, and marks the form as dirty.
    /// </summary>
    /// <param name="config">The hotkey combination that was recorded during capture.</param>
    public void AcceptHotkey(HotkeyConfig config)
    {
        _pendingHotkey = config;
        HotkeyDisplayText = config.ToDisplayString();
        IsCapturing = false;
        NotifyDirty();
        SetStatus(string.Format(Res("StringStatusHotkeySet"), config.ToDisplayString()));
    }

    /// <summary>Directly sets the <see cref="StatusMessage"/> shown in the UI. Intended for use by the code-behind.</summary>
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

    /// <summary>Derives the selected <see cref="AppTheme"/> from the three mutually exclusive theme toggle properties.</summary>
    private AppTheme CurrentTheme => _isThemeSystem ? AppTheme.System : _isThemeLight ? AppTheme.Light : AppTheme.Dark;

    /// <summary>
    /// Copies all values from the underlying <see cref="AppSettings"/> model into the
    /// backing fields that drive the bindable properties. Called once during construction.
    /// </summary>
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

    /// <summary>
    /// Records the current state of all editable fields as the "last saved" baseline.
    /// Called after construction and after a successful save so that <see cref="IsDirty"/>
    /// correctly reflects whether unsaved changes exist.
    /// </summary>
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

    /// <summary>Compares the current field values against the snapshot to determine whether unsaved changes exist.</summary>
    private bool HasChanges() =>
        !_pendingHotkey.Equals(_snapshotHotkey)
        || _preserveClipboard != _snapshotPreserveClipboard
        || _pasteDelayText != _snapshotPasteDelay
        || _launchAtStartup != _snapshotLaunchAtStartup
        || _trackClipboardHistory != _snapshotTrackClipboardHistory
        || _historySizeText != _snapshotHistorySize
        || CurrentTheme != _snapshotTheme;

    /// <summary>Fires change notifications for <see cref="IsDirty"/> and refreshes the Save command's can-execute state.</summary>
    private void NotifyDirty()
    {
        Notify(nameof(IsDirty));
        ((RelayCommand)SaveCommand).NotifyCanExecuteChanged();
    }

    /// <summary>Switches between active capture mode and idle mode; cancels capture if already active.</summary>
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

    /// <summary>Enters hotkey capture mode, showing a prompt in <see cref="HotkeyDisplayText"/> and clearing the status message.</summary>
    private void StartCapture()
    {
        IsCapturing = true;
        HotkeyDisplayText = Res("StringStatusCapturePrompt");
        StatusMessage = string.Empty;
    }

    /// <summary>
    /// Resets the pending hotkey to <see cref="HotkeyConfig.Default"/>, cancels any
    /// active capture, and marks the form as dirty.
    /// </summary>
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

    /// <summary>
    /// Writes all pending property values back to the <see cref="AppSettings"/> model,
    /// invokes <see cref="SettingsSaved"/> to persist them, takes a new snapshot, and
    /// updates the status message to reflect success or failure.
    /// </summary>
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

    /// <summary>Shorthand for resolving a resource string key via <see cref="ResourceResolver"/>.</summary>
    private string Res(string key) => ResourceResolver(key);

    /// <summary>
    /// Sets <paramref name="field"/> to <paramref name="value"/> and, if the value changed,
    /// fires a property-changed notification and refreshes dirty state.
    /// </summary>
    /// <returns><c>true</c> if the value changed; otherwise <c>false</c>.</returns>
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

    /// <summary>
    /// Sets <paramref name="field"/> to <paramref name="value"/> and, if the value changed,
    /// fires a property-changed notification without affecting dirty state.
    /// </summary>
    /// <returns><c>true</c> if the value changed; otherwise <c>false</c>.</returns>
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

    /// <summary>Raises <see cref="PropertyChanged"/> for the given property name.</summary>
    private void Notify([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
