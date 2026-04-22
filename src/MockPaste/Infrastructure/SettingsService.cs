using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using MockPaste.Core.Models;

namespace MockPaste.Infrastructure;

public sealed class SettingsService
{
    /// <summary>Increment this when a breaking change is made to <see cref="AppSettings"/>.</summary>
    private const int CurrentVersion = 1;

    private const string AppFolderName = "MockPaste";
    private const string SettingsFileName = "settings.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _settingsPath;

    public SettingsService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var dir = Path.Combine(appData, AppFolderName);
        Directory.CreateDirectory(dir);
        _settingsPath = Path.Combine(dir, SettingsFileName);
    }

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(_settingsPath))
                return CreateDefault();

            var json = File.ReadAllText(_settingsPath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
            if (settings is null)
            {
                AppLogger.Warning("Invalid settings file, reverting to defaults");
                return CreateDefault();
            }

            settings = Migrate(settings);
            settings.Sanitize();
            return settings;
        }
        catch (Exception ex)
        {
            AppLogger.Warning("Failed to load settings, reverting to defaults", ex);
            return CreateDefault();
        }
    }

    public bool Save(AppSettings settings)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(_settingsPath, json);
            AppLogger.Information("Settings saved");
            return true;
        }
        catch (Exception ex)
        {
            AppLogger.Error("Failed to save settings", ex);
            return false;
        }
    }

    private static AppSettings CreateDefault() => new() { Version = CurrentVersion };

    /// <summary>
    /// Applies incremental migrations so that settings from older versions are upgraded
    /// to the current schema before use.
    /// </summary>
    private static AppSettings Migrate(AppSettings settings)
    {
        // Version 0 → 1: Version field was not present in very early builds; treat it as 0.
        // No structural changes required for v1 yet — just stamp the current version so
        // future migrations can detect the gap correctly.
        if (settings.Version < 1)
        {
            AppLogger.Information($"Migrating settings from version {settings.Version} to 1");
            settings.Version = 1;
        }

        // Future migrations go here:
        // if (settings.Version < 2) { ... settings.Version = 2; }

        return settings;
    }
}
