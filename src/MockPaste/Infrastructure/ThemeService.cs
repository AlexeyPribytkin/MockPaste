using System.Reflection;
using System.Windows;
using Microsoft.Win32;
using MockPaste.Core.Models;

namespace MockPaste.Infrastructure;

public static class ThemeService
{
    private static AppTheme _currentTheme = AppTheme.Dark;

    public static void Apply(AppTheme theme)
    {
        _currentTheme = theme;
        SwapTheme(Resolve(theme));
    }

    public static void Reapply() => SwapTheme(Resolve(_currentTheme));

    public static string Resolve(AppTheme theme) =>
        theme == AppTheme.System ? GetSystemTheme() : theme.ToString();

    private static void SwapTheme(string resolved)
    {
        var assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
        var newUri = new Uri($"/{assemblyName};component/Themes/{resolved}.xaml", UriKind.Relative);
        var merged = System.Windows.Application.Current.Resources.MergedDictionaries;

        var existing = merged.FirstOrDefault(d =>
            d.Source?.ToString().Contains("Dark.xaml") == true ||
            d.Source?.ToString().Contains("Light.xaml") == true);

        var newDict = new ResourceDictionary { Source = newUri };

        if (existing is not null)
        {
            int idx = merged.IndexOf(existing);
            merged.Remove(existing);
            merged.Insert(idx, newDict);
        }
        else
        {
            merged.Insert(0, newDict);
        }
    }

    private static string GetSystemTheme()
    {
        var value = Registry.GetValue(
            @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
            "AppsUseLightTheme", 1);
        return value is int v && v == 0 ? "Dark" : "Light";
    }
}
