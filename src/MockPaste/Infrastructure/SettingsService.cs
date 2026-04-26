using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MockPaste.Core.Models;

namespace MockPaste.Infrastructure;

/// <summary>
/// Persists and loads <see cref="AppSettings"/> as a JSON file in the user's AppData folder.
/// Implements atomic writes (write-to-temp-then-rename), automatic backup, and
/// forward-compatible migration so older settings files can be safely upgraded.
/// </summary>
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

    /// <summary>
    /// Resolves the settings, backup, and temp file paths under the user's AppData folder.
    /// The settings directory is created if it does not already exist.
    /// </summary>
    public SettingsService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var dir = Path.Combine(appData, AppFolderName);
        Directory.CreateDirectory(dir);
        _settingsPath = Path.Combine(dir, SettingsFileName);
        _backupPath = _settingsPath + ".bak";
        _tempPath = _settingsPath + ".tmp";
    }

    /// <summary>
    /// Loads settings from disk, migrating older schemas and sanitising values.
    /// Falls back to the backup file if the primary is corrupted, or to defaults
    /// if both are unreadable.
    /// </summary>
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

    /// <summary>
    /// Serialises <paramref name="settings"/> to disk using an atomic temp-file write.
    /// The existing file is first copied to a <c>.bak</c> backup.
    /// </summary>
    /// <returns><c>true</c> on success; <c>false</c> if an <see cref="IOException"/> occurred.</returns>
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

    /// <summary>Creates a fresh <see cref="AppSettings"/> instance stamped with the current schema version.</summary>
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
