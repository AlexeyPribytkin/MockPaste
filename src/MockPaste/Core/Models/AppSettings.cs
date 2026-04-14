namespace MockPaste.Core.Models;

public sealed class AppSettings
{
    public const int PasteDelayMin = 0;
    public const int PasteDelayMax = 500;
    public const int PasteDelayDefault = 50;

    public const int HistorySizeMin = 1;
    public const int HistorySizeMax = 500;
    public const int HistorySizeDefault = 10;

    public const bool PreserveClipboardDefault = true;
    public const bool LaunchAtStartupDefault = false;
    public const AppTheme ThemeDefault = AppTheme.Dark;

    public int Version { get; set; } = 1;
    public HotkeyConfig Hotkey { get; set; } = HotkeyConfig.Default;
    public bool PreserveClipboard { get; set; } = PreserveClipboardDefault;
    public int PasteDelayMs { get; set; } = PasteDelayDefault;
    public bool LaunchAtStartup { get; set; } = LaunchAtStartupDefault;
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
