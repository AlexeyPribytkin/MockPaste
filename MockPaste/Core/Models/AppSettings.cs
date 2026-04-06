namespace MockPaste.Core.Models;

public sealed class AppSettings
{
    public int Version { get; set; } = 1;
    public HotkeyConfig Hotkey { get; set; } = HotkeyConfig.Default;
    public bool PreserveClipboard { get; set; } = true;
    public int PasteDelayMs { get; set; } = 50;
    public bool LaunchAtStartup { get; set; }
    public string Theme { get; set; } = "Dark";
    public int HistorySize { get; set; } = 10;
    public Dictionary<string, bool> EnabledGenerators { get; set; } = [];
}
