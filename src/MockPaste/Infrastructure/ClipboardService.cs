using System.Runtime.InteropServices;
using System.Windows;

namespace MockPaste.Infrastructure;

public sealed class ClipboardService
{
    private const int MaxRetries = 5;
    private const int RetryDelayMs = 20;

    private IDataObject? _savedClipboard;

    public bool TrySaveClipboard()
    {
        EnforceStaThread();

        try
        {
            var source = Clipboard.GetDataObject();
            if (source is null)
            {
                _savedClipboard = null;
                return true;
            }

            // Copy all data out into a local DataObject before we overwrite the clipboard.
            // GetDataObject() returns a live COM proxy that becomes invalid once the clipboard
            // is replaced, so we must snapshot the actual bytes now.
            var snapshot = new DataObject();
            bool hadFormatErrors = false;

            foreach (string format in source.GetFormats(autoConvert: false))
            {
                try
                {
                    var data = source.GetData(format, autoConvert: false);
                    if (data is not null)
                        snapshot.SetData(format, data);
                }
                catch (Exception ex)
                {
                    hadFormatErrors = true;
                    AppLogger.Debug($"Skipped clipboard format '{format}' during save", ex);
                }
            }

            if (hadFormatErrors)
                AppLogger.Warning("Clipboard snapshot is incomplete: one or more formats could not be read");

            _savedClipboard = snapshot;
            return true;
        }
        catch (COMException ex)
        {
            AppLogger.Warning("Failed to save clipboard content", ex);
            return false;
        }
    }

    public bool TryRestoreClipboard()
    {
        EnforceStaThread();

        if (_savedClipboard is null)
        {
            return false;
        }

        var saved = _savedClipboard;
        bool success = Retry(i => Clipboard.SetDataObject(saved, true));

        if (success)
        {
            _savedClipboard = null;
        }
        else
        {
            AppLogger.Warning($"Failed to restore clipboard after {MaxRetries} attempts");
        }

        return success;
    }

    /// <summary>Instance wrapper — delegates to the static implementation so callers can use the injected service consistently.</summary>
    public bool TrySetTextInstance(string text) => TrySetText(text);

    public static bool TrySetText(string text)
    {
        EnforceStaThread();

        if (text is null)
        {
            return false;
        }

        bool success = Retry(i => Clipboard.SetText(text));

        if (!success)
            AppLogger.Error($"Failed to set clipboard text after {MaxRetries} attempts");

        return success;
    }

    /// <summary>Retries <paramref name="action"/> up to <see cref="MaxRetries"/> times with exponential backoff on <see cref="COMException"/>.</summary>
    private static bool Retry(Action<int> action)
    {
        for (int i = 0; i < MaxRetries; i++)
        {
            try
            {
                action(i);
                return true;
            }
            catch (COMException)
            {
                Thread.Sleep(RetryDelayMs << i);
            }
        }

        return false;
    }

    /// <summary>Throws if the calling thread is not STA. All WPF Clipboard operations require STA.</summary>
    private static void EnforceStaThread()
    {
        if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
        {
            throw new InvalidOperationException("Clipboard access requires an STA thread.");
        }
    }
}
