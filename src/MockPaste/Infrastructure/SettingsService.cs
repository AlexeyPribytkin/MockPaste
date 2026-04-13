using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using MockPaste.Core.Models;

namespace MockPaste.Infrastructure;

public sealed class SettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _settingsPath;

    public SettingsService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var dir = Path.Combine(appData, "MockPaste");
        Directory.CreateDirectory(dir);
        _settingsPath = Path.Combine(dir, "settings.json");
    }

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(_settingsPath))
                return CreateDefault();

            var json = File.ReadAllText(_settingsPath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
            if (settings is null || !settings.Hotkey.IsValid())
            {
                AppLogger.Warning("Invalid settings file, reverting to defaults");
                return CreateDefault();
            }
            return settings;
        }
        catch (Exception ex)
        {
            AppLogger.Warning("Failed to load settings, reverting to defaults", ex);
            return CreateDefault();
        }
    }

    public void Save(AppSettings settings)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(_settingsPath, json);
            AppLogger.Information("Settings saved");
        }
        catch (Exception ex)
        {
            AppLogger.Error("Failed to save settings", ex);
        }
    }

    private AppSettings CreateDefault()
    {
        var settings = new AppSettings();
        Save(settings);
        return settings;
    }
}
