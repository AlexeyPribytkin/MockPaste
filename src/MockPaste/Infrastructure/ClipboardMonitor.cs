using System.Windows.Interop;
using MockPaste.Infrastructure.Native;

namespace MockPaste.Infrastructure;

/// <summary>
/// Listens for <c>WM_CLIPBOARDUPDATE</c> and raises <see cref="ClipboardChanged"/>
/// whenever the clipboard contents change. Must be created on the UI (STA) thread.
/// </summary>
internal sealed class ClipboardMonitor : IDisposable
{
    private HwndSource? _source;
    private bool _disposed;

    /// <summary>Raised on the UI thread whenever the clipboard content changes.</summary>
    public event Action? ClipboardChanged;

    public void Initialize()
    {
        var parameters = new HwndSourceParameters("MockPaste.ClipboardMonitor")
        {
            Width = 0,
            Height = 0,
            WindowStyle = 0
        };

        _source = new HwndSource(parameters);
        _source.AddHook(WndProc);
        NativeMethods.AddClipboardFormatListener(_source.Handle);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_CLIPBOARDUPDATE)
        {
            ClipboardChanged?.Invoke();
        }

        return IntPtr.Zero;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_source is not null)
        {
            NativeMethods.RemoveClipboardFormatListener(_source.Handle);
            _source.RemoveHook(WndProc);
            _source.Dispose();
            _source = null;
        }
    }
}
