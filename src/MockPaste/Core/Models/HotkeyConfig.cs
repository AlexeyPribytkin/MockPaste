using System.Text;
using System.Text.Json.Serialization;
using System.Windows.Input;

namespace MockPaste.Core.Models;

public sealed class HotkeyConfig
{
    public bool Ctrl { get; set; } = true;
    public bool Alt { get; set; } = true;
    public bool Shift { get; set; }
    public bool Win { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Key Key { get; set; } = Key.Space;

    public string ToDisplayString()
    {
        var sb = new StringBuilder();
        if (Ctrl) sb.Append("Ctrl + ");
        if (Alt) sb.Append("Alt + ");
        if (Shift) sb.Append("Shift + ");
        if (Win) sb.Append("Win + ");
        sb.Append(Key);
        return sb.ToString();
    }

    public bool IsValid()
    {
        bool hasModifier = Ctrl || Alt || Shift || Win;
        bool hasKey = Key is not (Key.None
            or Key.LeftCtrl or Key.RightCtrl
            or Key.LeftAlt or Key.RightAlt
            or Key.LeftShift or Key.RightShift
            or Key.LWin or Key.RWin);
        return hasModifier && hasKey;
    }

    public HotkeyConfig Clone() => new()
    {
        Ctrl = Ctrl,
        Alt = Alt,
        Shift = Shift,
        Win = Win,
        Key = Key
    };

    public static HotkeyConfig Default => new() { Ctrl = true, Alt = true, Key = Key.Space };
}
