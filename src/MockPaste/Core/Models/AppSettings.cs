namespace MockPaste.Core.Models;

/// <summary>
/// Holds all user-configurable application settings. Acts as the single source of truth
/// shared across services and ViewModels. Persisted to disk by <c>SettingsService</c>.
/// </summary>
public sealed class AppSettings
{
    /// <summary>Minimum allowed paste delay in milliseconds.</summary>
    public const int PasteDelayMin = 0;

    /// <summary>Maximum allowed paste delay in milliseconds.</summary>
    public const int PasteDelayMax = 500;

    /// <summary>Default paste delay in milliseconds applied when no user value is set.</summary>
    public const int PasteDelayDefault = 50;

    /// <summary>Default delay in milliseconds to wait before restoring the original clipboard content after a paste.</summary>
    public const int ClipboardRestoreDelayDefault = 100;

    /// <summary>Minimum allowed clipboard history size (number of entries).</summary>
    public const int HistorySizeMin = 1;

    /// <summary>Maximum allowed clipboard history size (number of entries).</summary>
    public const int HistorySizeMax = 500;

    /// <summary>Default clipboard history size applied when no user value is set.</summary>
    public const int HistorySizeDefault = 10;

    /// <summary>Default value for <see cref="PreserveClipboard"/>.</summary>
    public const bool PreserveClipboardDefault = true;

    /// <summary>Default value for <see cref="LaunchAtStartup"/>.</summary>
    public const bool LaunchAtStartupDefault = false;

    /// <summary>Default value for <see cref="TrackClipboardHistory"/>.</summary>
    public const bool TrackClipboardHistoryDefault = false;

    /// <summary>Default application theme.</summary>
    public const AppTheme ThemeDefault = AppTheme.System;

    /// <summary>Schema version; incremented on breaking changes to enable forward-compatible migration.</summary>
    public int Version { get; set; } = 1;

    /// <summary>Global hotkey configuration used to trigger the paste popup.</summary>
    public HotkeyConfig Hotkey { get; set; } = HotkeyConfig.Default;

    /// <summary>When <c>true</c>, the original clipboard content is restored after each paste operation.</summary>
    public bool PreserveClipboard { get; set; } = PreserveClipboardDefault;

    /// <summary>Delay in milliseconds inserted between setting the clipboard and simulating the paste keystroke.</summary>
    public int PasteDelayMs { get; set; } = PasteDelayDefault;

    /// <summary>Delay in milliseconds to wait after pasting before restoring the original clipboard content.</summary>
    public int ClipboardRestoreDelayMs { get; set; } = ClipboardRestoreDelayDefault;

    /// <summary>When <c>true</c>, the application is registered in the Windows startup registry key.</summary>
    public bool LaunchAtStartup { get; set; } = LaunchAtStartupDefault;

    /// <summary>When <c>true</c>, clipboard entries produced by the app are stored in the history list.</summary>
    public bool TrackClipboardHistory { get; set; } = TrackClipboardHistoryDefault;

    /// <summary>The active UI theme (Dark, Light, or follow the OS setting).</summary>
    public AppTheme Theme { get; set; } = ThemeDefault;

    /// <summary>Maximum number of entries retained in the clipboard history list.</summary>
    public int HistorySize { get; set; } = HistorySizeDefault;

    /// <summary>
    /// Attempts to parse <paramref name="text"/> as a paste delay in milliseconds.
    /// Returns <c>true</c> and sets <paramref name="value"/> only when the input is a valid
    /// integer within [<see cref="PasteDelayMin"/>, <see cref="PasteDelayMax"/>].
    /// </summary>
    public static bool TryParsePasteDelay(string text, out int value) =>
        int.TryParse(text, out value)
        && value >= PasteDelayMin
        && value <= PasteDelayMax;

    /// <summary>
    /// Attempts to parse <paramref name="text"/> as a history size.
    /// Returns <c>true</c> and sets <paramref name="value"/> only when the input is a valid
    /// integer within [<see cref="HistorySizeMin"/>, <see cref="HistorySizeMax"/>].
    /// </summary>
    public static bool TryParseHistorySize(string text, out int value) =>
        int.TryParse(text, out value)
        && value >= HistorySizeMin
        && value <= HistorySizeMax;

    /// <summary>
    /// Copies all configurable properties from <paramref name="source"/> into this instance.
    /// Used to apply saved settings without replacing the shared reference held by other components.
    /// </summary>
    public void CopyFrom(AppSettings source)
    {
        ArgumentNullException.ThrowIfNull(source);
        Version = source.Version;
        Hotkey = source.Hotkey;
        PreserveClipboard = source.PreserveClipboard;
        PasteDelayMs = source.PasteDelayMs;
        ClipboardRestoreDelayMs = source.ClipboardRestoreDelayMs;
        LaunchAtStartup = source.LaunchAtStartup;
        TrackClipboardHistory = source.TrackClipboardHistory;
        Theme = source.Theme;
        HistorySize = source.HistorySize;
    }

    /// <summary>
    /// Clamps numeric fields to their valid ranges and resets any invalid values to defaults.
    /// Safe to call after deserialisation.
    /// </summary>
    public void Sanitize()
    {
        Hotkey ??= HotkeyConfig.Default;
        if (!Hotkey.IsValid())
        {
            Hotkey = HotkeyConfig.Default;
        }

        PasteDelayMs = Math.Clamp(PasteDelayMs, PasteDelayMin, PasteDelayMax);
        HistorySize = Math.Clamp(HistorySize, HistorySizeMin, HistorySizeMax);
        if (!Enum.IsDefined(Theme))
        {
            Theme = ThemeDefault;
        }
    }
}
