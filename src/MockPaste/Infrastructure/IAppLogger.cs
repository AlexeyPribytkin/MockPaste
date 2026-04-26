namespace MockPaste.Infrastructure;

/// <summary>
/// Minimal structured-logging abstraction used by services that need to be testable
/// without a real file system. Implemented by <see cref="AppLogger"/>.
/// </summary>
public interface IAppLogger
{
    /// <summary>Logs a verbose debug message, optionally with an associated exception.</summary>
    void Debug(string message, Exception? ex = null);

    /// <summary>Logs an informational message about normal application flow.</summary>
    void Information(string message, Exception? ex = null);

    /// <summary>Logs a non-fatal warning that may indicate a recoverable problem.</summary>
    void Warning(string message, Exception? ex = null);

    /// <summary>Logs an error that indicates a failure in the current operation.</summary>
    void Error(string message, Exception? ex = null);

    /// <summary>Logs a fatal error that requires the application to terminate.</summary>
    void Fatal(string message, Exception? ex = null);
}
