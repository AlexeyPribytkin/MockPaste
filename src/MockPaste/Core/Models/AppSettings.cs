namespace MockPaste.Core.Models;

public sealed class AppSettings
{
    public const int PasteDelayMin = 0;
    public const int PasteDelayMax = 500;
    public const int PasteDelayDefault = 50;

    public const int ClipboardRestoreDelayDefault = 100;

    public const int HistorySizeMin = 1;
    public const int HistorySizeMax = 500;
    public const int HistorySizeDefault = 10;

    public const bool PreserveClipboardDefault = true;
    public const bool LaunchAtStartupDefault = false;
    public const bool TrackClipboardHistoryDefault = false;
    public const AppTheme ThemeDefault = AppTheme.Dark;

    public int Version { get; set; } = 1;
    public HotkeyConfig Hotkey { get; set; } = HotkeyConfig.Default;
    public bool PreserveClipboard { get; set; } = PreserveClipboardDefault;
    public int PasteDelayMs { get; set; } = PasteDelayDefault;
    public int ClipboardRestoreDelayMs { get; set; } = ClipboardRestoreDelayDefault;
    public bool LaunchAtStartup { get; set; } = LaunchAtStartupDefault;
    public bool TrackClipboardHistory { get; set; } = TrackClipboardHistoryDefault;
    public AppTheme Theme { get; set; } = ThemeDefault;
    public int HistorySize { get; set; } = HistorySizeDefault;

    public static bool TryParsePasteDelay(string text, out int value) =>
        int.TryParse(text, out value)
        && value >= PasteDelayMin
        && value <= PasteDelayMax;

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
