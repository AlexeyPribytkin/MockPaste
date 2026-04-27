using System.Reflection;
using System.Windows;
using Microsoft.Win32;
using MockPaste.Core.Models;

namespace MockPaste.Infrastructure;

/// <summary>
/// Applies WPF theme resource dictionaries (Dark / Light / System) at runtime by
/// swapping the appropriate <c>Themes/*.xaml</c> merged dictionary in the application
/// resources. Remembers the last applied theme so it can be reapplied after window
/// recreation.
/// </summary>
public static class ThemeService
{
    // Cached once to avoid repeated reflection calls.
    private static readonly string AssemblyName =
        Assembly.GetExecutingAssembly().GetName().Name!;

    private static AppTheme _currentTheme = AppTheme.Dark;

    // Tracks the currently applied dictionary so we avoid fragile string matching.
    private static ResourceDictionary? _currentDictionary;

    /// <summary>Applies <paramref name="theme"/> to the running WPF application. System theme is resolved to Dark or Light based on the OS setting.</summary>
    public static void Apply(AppTheme theme)
    {
        _currentTheme = theme;
        SwapTheme(Resolve(theme));
    }

    /// <summary>
    /// Reapplies the last theme set via <see cref="Apply"/>. Useful after dynamic
    /// resource dictionaries are reloaded (e.g. after a window is recreated).
    /// </summary>
    public static void Reapply() => SwapTheme(Resolve(_currentTheme));

    /// <summary>
    /// Resolves the effective theme name for the given <paramref name="theme"/>.
    /// <see cref="AppTheme.System"/> is resolved to the current OS preference.
    /// </summary>
    public static string Resolve(AppTheme theme) => theme switch
    {
        AppTheme.Dark => "Dark",
        AppTheme.Light => "Light",
        AppTheme.System => GetSystemTheme(),
        _ => "Light"
    };

    /// <summary>
    /// Replaces the currently active theme dictionary with the one matching
    /// <paramref name="resolved"/> ("Dark" or "Light"). Inserts at the same index
    /// to preserve resource merge order; no-op if the same URI is already active.
    /// </summary>
    private static void SwapTheme(string resolved)
    {
        var app = System.Windows.Application.Current;
        if (app is null)
        {
            return;
        }

        var newUri = new Uri($"/{AssemblyName};component/Themes/{resolved}.xaml", UriKind.Relative);
        var merged = app.Resources.MergedDictionaries;

        _currentDictionary ??= FindExistingThemeDictionary(merged);

        // Short-circuit if the same theme dictionary is already applied.
        if (_currentDictionary?.Source == newUri)
        {
            return;
        }

        var newDict = new ResourceDictionary { Source = newUri };

        if (_currentDictionary is not null && merged.Contains(_currentDictionary))
        {
            int idx = merged.IndexOf(_currentDictionary);
            merged.Remove(_currentDictionary);
            // Theme dictionary is inserted at the same position to preserve merge order.
            merged.Insert(idx, newDict);
        }
        else
        {
            // Theme dictionary is inserted last so it has the highest resource priority.
            merged.Add(newDict);
        }

        _currentDictionary = newDict;
    }

    private static ResourceDictionary? FindExistingThemeDictionary(IList<ResourceDictionary> dictionaries)
    {
        for (int i = 0; i < dictionaries.Count; i++)
        {
            var source = dictionaries[i].Source?.OriginalString;
            if (source is null)
            {
                continue;
            }

            if (source.EndsWith("Themes/Dark.xaml", StringComparison.OrdinalIgnoreCase)
                || source.EndsWith("Themes/Light.xaml", StringComparison.OrdinalIgnoreCase))
            {
                return dictionaries[i];
            }
        }

        return null;
    }

    private static string GetSystemTheme()
    {
        try
        {
            var value = Registry.GetValue(
                @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                "AppsUseLightTheme", 1);
            return value is int v && v == 0 ? "Dark" : "Light";
        }
        catch
        {
            return "Light";
        }
    }
}
