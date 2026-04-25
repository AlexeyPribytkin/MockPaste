using System.Text;

namespace MockPaste.Infrastructure.Native;

/// <summary>
/// Tracks the most recent foreground window that does not belong to this process
/// and is not a shell/taskbar window, so it is always available instantly without
/// querying at popup-show time (by which point focus may have already shifted to
/// the taskbar due to a tray icon click).
/// </summary>
internal sealed class ForegroundWindowTracker : IDisposable
{
    // Shell window classes that receive focus when clicking the tray area.
    private static readonly HashSet<string> _shellClasses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Shell_TrayWnd",       // main taskbar
        "Shell_SecondaryTrayWnd", // secondary monitor taskbar
        "NotifyIconOverflowWindow", // overflow tray popup
        "Button",              // Start button (older Windows)
        "Windows.UI.Core.CoreWindow", // Start menu / Action Center (Win10)
    };

    private readonly IntPtr _hook;
    // Keep a strong reference so the delegate is not GC'd while the hook is active.
    private readonly NativeMethods.WinEventProc _hookProc;
    private readonly int _ownPid = Environment.ProcessId;

    public IntPtr LastForegroundWindow { get; private set; }

    public ForegroundWindowTracker()
    {
        _hookProc = OnWinEvent;
        _hook = NativeMethods.SetWinEventHook(
            NativeMethods.EVENT_SYSTEM_FOREGROUND,
            NativeMethods.EVENT_SYSTEM_FOREGROUND,
            IntPtr.Zero,
            _hookProc,
            0, 0,
            NativeMethods.WINEVENT_OUTOFCONTEXT);
    }

    private void OnWinEvent(
        IntPtr hWinEventHook, uint eventType,
        IntPtr hwnd, int idObject, int idChild,
        uint dwEventThread, uint dwmsEventTime)
    {
        if (hwnd == IntPtr.Zero)
        {
            return;
        }

        NativeMethods.GetWindowThreadProcessId(hwnd, out uint pid);
        if (pid == (uint)_ownPid)
        {
            return;
        }

        var className = new StringBuilder(256);
        NativeMethods.GetClassName(hwnd, className, className.Capacity);
        if (_shellClasses.Contains(className.ToString()))
        {
            return;
        }

        LastForegroundWindow = hwnd;
    }

    public void Dispose()
    {
        if (_hook != IntPtr.Zero)
        {
            NativeMethods.UnhookWinEvent(_hook);
        }
    }
}