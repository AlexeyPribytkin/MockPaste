using System.IO;
using System.Text;

namespace MockPaste.Infrastructure;

/// <summary>
/// File-based logger. Implements <see cref="IAppLogger"/> for consumers that receive it via constructor injection.
/// Static helpers on this class delegate to the singleton <see cref="Instance"/> so call-sites in
/// infrastructure code (e.g. <see cref="SettingsService"/>) don't need to carry a logger parameter.
/// </summary>
public sealed class AppLogger : IAppLogger
{
    // ── Singleton instance ────────────────────────────────────────────────────
    private static AppLogger _instance = new();
    private static readonly Lock _initLock = new();

    /// <summary>Current logger instance used by the static helper methods.</summary>
    public static AppLogger Instance => _instance;

    // ── Static helpers ────────────────────────────────────────────────────────
    public static void Debug(string message, Exception? ex = null) => _instance.Write("DBG", message, ex);
    public static void Information(string message, Exception? ex = null) => _instance.Write("INF", message, ex);
    public static void Warning(string message, Exception? ex = null) => _instance.Write("WRN", message, ex);
    public static void Error(string message, Exception? ex = null) => _instance.Write("ERR", message, ex);
    public static void Fatal(string message, Exception? ex = null) => _instance.Write("FTL", message, ex);
    public static void CloseAndFlush() => _instance.Flush();

    public static void Initialize(string logDir)
    {
        lock (_initLock)
        {
            _instance.Flush();
            _instance = new AppLogger(logDir);
        }
    }

    // ── IAppLogger (instance) ─────────────────────────────────────────────────
    void IAppLogger.Debug(string message, Exception? ex) => Write("DBG", message, ex);
    void IAppLogger.Information(string message, Exception? ex) => Write("INF", message, ex);
    void IAppLogger.Warning(string message, Exception? ex) => Write("WRN", message, ex);
    void IAppLogger.Error(string message, Exception? ex) => Write("ERR", message, ex);
    void IAppLogger.Fatal(string message, Exception? ex) => Write("FTL", message, ex);

    // ── Instance implementation ───────────────────────────────────────────────
    private StreamWriter? _writer;
    private string _logDir = string.Empty;
    private string _currentLogFile = string.Empty;
    private readonly Lock _lock = new();

    /// <summary>Creates a no-op logger (used before <see cref="Initialize"/> is called).</summary>
    private AppLogger() { }

    private AppLogger(string logDir)
    {
        _logDir = logDir;
        Directory.CreateDirectory(logDir);
        OpenLogFile();
    }

    private void OpenLogFile()
    {
        _writer?.Dispose();
        var now = DateTime.Now;
        var fileName = $"mockpaste-{now:yyyy-MM-dd}.log";
        _currentLogFile = Path.Combine(_logDir, fileName);
        _writer = new StreamWriter(_currentLogFile, append: true, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)) { AutoFlush = true };
        CleanupOldLogs();
    }

    private void CleanupOldLogs()
    {
        try
        {
            foreach (var file in Directory.GetFiles(_logDir, "mockpaste-*.log")
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.CreationTimeUtc)
                .Skip(7))
            {
                file.Delete();
            }
        }
        catch (Exception ex)
        {
            // Cleanup is best-effort; log to debug output to aid diagnosing permission or IO issues
            System.Diagnostics.Debug.WriteLine($"[AppLogger] Failed to clean up old logs: {ex.Message}");
        }
    }

    private void Write(string level, string message, Exception? ex)
    {
        lock (_lock)
        {
            if (_writer is null) return;

            var now = DateTime.Now;
            var expectedFile = Path.Combine(_logDir, $"mockpaste-{now:yyyy-MM-dd}.log");
            if (_currentLogFile != expectedFile)
            {
                OpenLogFile();
            }

            _writer.WriteLine($"{now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}");
            if (ex is not null)
            {
                _writer.WriteLine(ex.ToString());
            }
        }
    }

    /// <summary>
    /// Flushes and closes the current log file. After calling this, logging is
    /// disabled until <see cref="Initialize"/> is called again.
    /// </summary>
    public void Flush()
    {
        lock (_lock)
        {
            _writer?.Flush();
            _writer?.Dispose();
            _writer = null;
        }
    }
}
