namespace MockPaste.Core.Models;

/// <summary>Specifies the visual theme applied to the application's UI.</summary>
public enum AppTheme
{
    /// <summary>Forces the dark color scheme regardless of the OS setting.</summary>
    Dark,

    /// <summary>Forces the light color scheme regardless of the OS setting.</summary>
    Light,

    /// <summary>Follows the current Windows color mode (dark or light).</summary>
    System,
}
