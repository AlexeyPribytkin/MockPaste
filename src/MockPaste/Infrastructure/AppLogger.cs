using System.IO;
using System.Text;

namespace MockPaste.Infrastructure;

internal static class AppLogger
{
    private static StreamWriter? _writer;
    private static string _logDir = string.Empty;
    private static string _currentLogFile = string.Empty;
    private static readonly Lock _lock = new();

    internal static void Initialize(string logDir)
    {
        _logDir = logDir;
        Directory.CreateDirectory(logDir);
        OpenLogFile();
    }

    private static void OpenLogFile()
    {
        _writer?.Dispose();
        var fileName = $"mockpaste-{DateTime.Now:yyyy-MM-dd}.log";
        _currentLogFile = Path.Combine(_logDir, fileName);
        _writer = new StreamWriter(_currentLogFile, append: true, Encoding.UTF8) { AutoFlush = true };
        CleanupOldLogs();
    }

    private static void CleanupOldLogs()
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

    public static void Debug(string message, Exception? ex = null) => Write("DBG", message, ex);
    public static void Information(string message, Exception? ex = null) => Write("INF", message, ex);
    public static void Warning(string message, Exception? ex = null) => Write("WRN", message, ex);
    public static void Error(string message, Exception? ex = null) => Write("ERR", message, ex);
    public static void Fatal(string message, Exception? ex = null) => Write("FTL", message, ex);

    private static void Write(string level, string message, Exception? ex)
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

    public static void CloseAndFlush()
    {
        lock (_lock)
        {
            _writer?.Flush();
            _writer?.Dispose();
            _writer = null;
        }
    }
}
