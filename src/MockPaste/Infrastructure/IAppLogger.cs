namespace MockPaste.Infrastructure;

public interface IAppLogger
{
    void Debug(string message, Exception? ex = null);
    void Information(string message, Exception? ex = null);
    void Warning(string message, Exception? ex = null);
    void Error(string message, Exception? ex = null);
    void Fatal(string message, Exception? ex = null);
}
