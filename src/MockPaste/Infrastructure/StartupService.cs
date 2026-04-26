using System.Diagnostics;
using Microsoft.Win32;

namespace MockPaste.Infrastructure;

/// <summary>
/// Manages the Windows startup registry entry that controls whether the application
/// launches automatically when the user logs in.
/// </summary>
internal static class StartupService
{
    private const string AppName = "MockPaste";
    private const string RunRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

    /// <summary>
    /// Adds or removes the app from the Windows startup registry key to match <paramref name="enable"/>.
    /// Settings file is the authoritative source; call this on startup and on every save.
    /// </summary>
    public static void Apply(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, writable: true);
            if (key is null)
            {
                AppLogger.Warning("Startup registry key not found");
                return;
            }

            if (enable)
            {
                var exe = Environment.ProcessPath
                    ?? Process.GetCurrentProcess().MainModule?.FileName;

                if (string.IsNullOrWhiteSpace(exe))
                {
                    AppLogger.Warning("Unable to determine executable path for startup registration");
                    return;
                }

                var value = $"\"{exe}\"";
                var current = key.GetValue(AppName) as string;

                if (current == value)
                {
                    return;
                }

                key.SetValue(AppName, value);
                AppLogger.Debug("Startup enabled");
            }
            else
            {
                if (key.GetValue(AppName) is not string current)
                {
                    return;
                }

                key.DeleteValue(AppName, throwOnMissingValue: false);
                AppLogger.Debug("Startup disabled");
            }
        }
        catch (Exception ex)
        {
            AppLogger.Warning($"Failed to apply startup setting (enable={enable})", ex);
        }
    }

    /// <summary>
    /// Returns whether the app is currently registered to run at Windows startup.
    /// </summary>
    public static bool IsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, writable: false);
            return key?.GetValue(AppName) is not null;
        }
        catch (Exception ex)
        {
            AppLogger.Warning("Failed to read startup registry setting", ex);
            return false;
        }
    }
}
