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
                    AppLogger.Debug($"Skipped clipboard format '{format}' during save", ex);
                }
            }
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
        if (_savedClipboard is null) return false;

        for (int i = 0; i < MaxRetries; i++)
        {
            try
            {
                Clipboard.SetDataObject(_savedClipboard, true);
                _savedClipboard = null;
                return true;
            }
            catch (COMException)
            {
                Thread.Sleep(RetryDelayMs);
            }
        }

        AppLogger.Warning($"Failed to restore clipboard after {MaxRetries} attempts");
        _savedClipboard = null;
        return false;
    }

    public bool TrySetText(string text)
    {
        for (int i = 0; i < MaxRetries; i++)
        {
            try
            {
                Clipboard.SetText(text);
                return true;
            }
            catch (COMException)
            {
                Thread.Sleep(RetryDelayMs);
            }
        }

        AppLogger.Error($"Failed to set clipboard text after {MaxRetries} attempts");
        return false;
    }
}
