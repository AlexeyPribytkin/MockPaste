using System.IO;
using System.Text;

namespace MockPaste.Infrastructure;

/// <summary>
/// File-based logger. Implements <see cref="IAppLogger"/> for DI consumers.
/// The static members delegate to <see cref="Instance"/> so legacy call-sites
/// continue to work without change.
/// </summary>
public sealed class AppLogger : IAppLogger
{
    // ── Singleton instance ────────────────────────────────────────────────────
    private static AppLogger _instance = new();

    /// <summary>Current logger instance used by the static helper methods.</summary>
    public static AppLogger Instance => _instance;

    // ── Static helpers (preserve all existing call-sites) ────────────────────
    public static void Debug(string message, Exception? ex = null)       => _instance.WriteDebug(message, ex);
    public static void Information(string message, Exception? ex = null) => _instance.WriteInformation(message, ex);
    public static void Warning(string message, Exception? ex = null)     => _instance.WriteWarning(message, ex);
    public static void Error(string message, Exception? ex = null)       => _instance.WriteError(message, ex);
    public static void Fatal(string message, Exception? ex = null)       => _instance.WriteFatal(message, ex);
    public static void CloseAndFlush()                                   => _instance.Flush();

    public static void Initialize(string logDir)
    {
        _instance = new AppLogger(logDir);
    }

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
        var fileName = $"mockpaste-{DateTime.Now:yyyy-MM-dd}.log";
        _currentLogFile = Path.Combine(_logDir, fileName);
        _writer = new StreamWriter(_currentLogFile, append: true, Encoding.UTF8) { AutoFlush = true };
        CleanupOldLogs();
    }

    private void CleanupOldLogs()
    {
        try
        {
            foreach (var file in Directory.GetFiles(_logDir, "mockpaste-*.log")
                                          .OrderByDescending(f => f)
                                          .Skip(7))
                File.Delete(file);
        }
        catch { }
    }

    void IAppLogger.Debug(string message, Exception? ex)       => WriteDebug(message, ex);
    void IAppLogger.Information(string message, Exception? ex) => WriteInformation(message, ex);
    void IAppLogger.Warning(string message, Exception? ex)     => WriteWarning(message, ex);
    void IAppLogger.Error(string message, Exception? ex)       => WriteError(message, ex);
    void IAppLogger.Fatal(string message, Exception? ex)       => WriteFatal(message, ex);

    private void WriteDebug(string message, Exception? ex)       => Write("DBG", message, ex);
    private void WriteInformation(string message, Exception? ex) => Write("INF", message, ex);
    private void WriteWarning(string message, Exception? ex)     => Write("WRN", message, ex);
    private void WriteError(string message, Exception? ex)       => Write("ERR", message, ex);
    private void WriteFatal(string message, Exception? ex)       => Write("FTL", message, ex);

    private void Write(string level, string message, Exception? ex)
    {
        if (_writer is null) return;
        lock (_lock)
        {
            var expectedFile = Path.Combine(_logDir, $"mockpaste-{DateTime.Now:yyyy-MM-dd}.log");
            if (_currentLogFile != expectedFile)
                OpenLogFile();

            _writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}");
            if (ex is not null)
                _writer.WriteLine(ex.ToString());
        }
    }

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
