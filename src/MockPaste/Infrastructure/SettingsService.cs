using System.IO;
using System.Text;
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
    private readonly string _backupPath;
    private readonly string _tempPath;

    public SettingsService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var dir = Path.Combine(appData, AppFolderName);
        Directory.CreateDirectory(dir);
        _settingsPath = Path.Combine(dir, SettingsFileName);
        _backupPath = _settingsPath + ".bak";
        _tempPath = _settingsPath + ".tmp";
    }

    public AppSettings Load()
    {
        if (!File.Exists(_settingsPath))
        {
            return CreateDefault();
        }

        try
        {
            var json = File.ReadAllText(_settingsPath, Encoding.UTF8);
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
        catch (JsonException ex)
        {
            AppLogger.Warning("Settings file is corrupted, attempting backup restore", ex);
            return TryRestoreFromBackup() ?? CreateDefault();
        }
        catch (IOException ex)
        {
            AppLogger.Warning("Failed to read settings file, reverting to defaults", ex);
            return CreateDefault();
        }
    }

    public bool Save(AppSettings settings)
    {
        try
        {
            // Stamp the current schema version into a temporary copy so the on-disk
            // representation is always up-to-date without mutating the caller's object.
            var toSerialize = new AppSettings();
            toSerialize.CopyFrom(settings);
            toSerialize.Version = CurrentVersion;

            var json = JsonSerializer.Serialize(toSerialize, JsonOptions);

            // Back up the existing file before overwriting.
            if (File.Exists(_settingsPath))
            {
                File.Copy(_settingsPath, _backupPath, overwrite: true);
            }

            // Atomic write: write to a temp file then replace, so a crash mid-write
            // never leaves the settings file in a corrupted state.
            File.WriteAllText(_tempPath, json, Encoding.UTF8);
            File.Move(_tempPath, _settingsPath, overwrite: true);

            AppLogger.Information("Settings saved");
            return true;
        }
        catch (IOException ex)
        {
            AppLogger.Error("Failed to save settings (IO error)", ex);
            return false;
        }
    }

    private static AppSettings CreateDefault() => new() { Version = CurrentVersion };

    /// <summary>
    /// Attempts to load settings from the backup file after the primary file is found corrupted.
    /// Returns <see langword="null"/> if no valid backup exists.
    /// </summary>
    private AppSettings? TryRestoreFromBackup()
    {
        if (!File.Exists(_backupPath))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(_backupPath, Encoding.UTF8);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
            if (settings is null)
            {
                return null;
            }

            AppLogger.Information("Settings restored from backup");
            settings = Migrate(settings);
            settings.Sanitize();
            return settings;
        }
        catch (JsonException ex)
        {
            AppLogger.Warning("Failed to restore settings from backup", ex);
            return null;
        }
        catch (IOException ex)
        {
            AppLogger.Warning("Failed to restore settings from backup", ex);
            return null;
        }
    }

    /// <summary>
    /// Applies incremental migrations so that settings from older versions are upgraded
    /// to the current schema before use.
    /// </summary>
    private static AppSettings Migrate(AppSettings settings)
    {
        if (settings.Version >= CurrentVersion)
        {
            return settings;
        }

        AppLogger.Information($"Migrating settings from version {settings.Version} to {CurrentVersion}");
        settings.Version = CurrentVersion;

        return settings;
    }
}
