using Microsoft.Win32;

namespace MockPaste.Infrastructure;

internal static class StartupService
{
    private const string AppName        = "MockPaste";
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
            if (key is null) return;

            if (enable)
            {
                var exe = Environment.ProcessPath;
                if (exe is not null)
                    key.SetValue(AppName, $"\"{exe}\"");
            }
            else
            {
                key.DeleteValue(AppName, throwOnMissingValue: false);
            }
        }
        catch (Exception ex)
        {
            AppLogger.Warning("Failed to apply startup setting", ex);
        }
    }
}
