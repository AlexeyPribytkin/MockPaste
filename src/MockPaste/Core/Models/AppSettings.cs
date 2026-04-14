namespace MockPaste.Core.Models;

public sealed class AppSettings
{
    public const int PasteDelayMin =   0;
    public const int PasteDelayMax = 500;
    public const int HistorySizeMin =   1;
    public const int HistorySizeMax = 500;

    public int Version { get; set; } = 1;
    public HotkeyConfig Hotkey { get; set; } = HotkeyConfig.Default;
    public bool PreserveClipboard { get; set; } = true;
    public int PasteDelayMs { get; set; } = 50;
    public bool LaunchAtStartup { get; set; }
    public string Theme { get; set; } = "Dark";
    public int HistorySize { get; set; } = 10;
    // TODO: Surface per-generator toggles in the Settings UI
    public Dictionary<string, bool> EnabledGenerators { get; set; } = [];

    /// <summary>
    /// Clamps numeric fields to their valid ranges and resets any invalid values to defaults.
    /// Safe to call after deserialisation.
    /// </summary>
    public void Sanitize()
    {
        if (!Hotkey.IsValid())
            Hotkey = HotkeyConfig.Default;

        PasteDelayMs = Math.Clamp(PasteDelayMs, PasteDelayMin, PasteDelayMax);
        HistorySize  = Math.Clamp(HistorySize,  HistorySizeMin, HistorySizeMax);

        if (Theme is not "Dark" and not "Light" and not "System")
            Theme = "Dark";
    }
}
