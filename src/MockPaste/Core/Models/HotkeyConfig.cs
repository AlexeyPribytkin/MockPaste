using System.Text;
using System.Text.Json.Serialization;
using System.Windows.Input;

namespace MockPaste.Core.Models;

/// <summary>
/// Describes a global hotkey combination consisting of one or more modifier keys
/// (Ctrl, Alt, Shift, Win) and a primary <see cref="Key"/>.
/// </summary>
public sealed class HotkeyConfig : IEquatable<HotkeyConfig>
{
    /// <summary>When <c>true</c>, the Ctrl modifier is part of the hotkey.</summary>
    public bool Ctrl { get; set; } = true;

    /// <summary>When <c>true</c>, the Alt modifier is part of the hotkey.</summary>
    public bool Alt { get; set; } = true;

    /// <summary>When <c>true</c>, the Shift modifier is part of the hotkey.</summary>
    public bool Shift { get; set; }

    /// <summary>When <c>true</c>, the Windows key modifier is part of the hotkey.</summary>
    public bool Win { get; set; }

    /// <summary>The primary key that, combined with the selected modifiers, forms the hotkey.</summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Key Key { get; set; } = Key.Space;

    /// <summary>Returns a human-readable string like "Ctrl + Alt + Space" for display in the UI.</summary>
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

    /// <summary>
    /// Returns <c>true</c> when the configuration has at least one modifier and a non-modifier,
    /// non-None primary key. Invalid configurations are not registered as hotkeys.
    /// </summary>
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

    /// <summary>Creates a deep copy of this instance.</summary>
    public HotkeyConfig Clone() => new()
    {
        Ctrl = Ctrl,
        Alt = Alt,
        Shift = Shift,
        Win = Win,
        Key = Key
    };

    /// <summary>Returns the factory-default hotkey: Ctrl + Alt + Space.</summary>
    public static HotkeyConfig Default => new() { Ctrl = true, Alt = true, Key = Key.Space };

    /// <summary>Returns <c>true</c> when all modifier flags and the primary key match <paramref name="other"/>.</summary>
    public bool Equals(HotkeyConfig? other) =>
        other is not null &&
        Ctrl  == other.Ctrl  &&
        Alt   == other.Alt   &&
        Shift == other.Shift &&
        Win   == other.Win   &&
        Key   == other.Key;

    public override bool Equals(object? obj) => Equals(obj as HotkeyConfig);
    public override int  GetHashCode()        => HashCode.Combine(Ctrl, Alt, Shift, Win, Key);
}
