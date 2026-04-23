using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;
using MockPaste.Core.Models;
using MockPaste.Infrastructure;
using MockPaste.Infrastructure.Native;

namespace MockPaste.Application;

/// <summary>
/// Manages a system-wide global hotkey using a Win32 message-only window.
/// Must be initialized and used on the UI thread.
/// </summary>
public sealed class HotkeyManager : IDisposable
{
    private const int HotkeyId = 9001;

    private HwndSource? _hwndSource;
    private bool _isRegistered;

    /// <summary>
    /// Raised on the UI thread when the registered hotkey is pressed.
    /// </summary>
    public event Action? HotkeyPressed;

    /// <summary>
    /// Creates the underlying message-only window used to receive hotkey notifications.
    /// Must be called on the UI thread before calling <see cref="Register"/>.
    /// Calling this more than once is a no-op.
    /// </summary>
    public void Initialize()
    {
        if (!System.Windows.Application.Current.Dispatcher.CheckAccess())
        {
            throw new InvalidOperationException("HotkeyManager must be initialized on the UI thread.");
        }

        if (_hwndSource is not null)
        {
            return;
        }

        var parameters = new HwndSourceParameters("MockPasteHotkey")
        {
            Width = 0,
            Height = 0,
            ParentWindow = new IntPtr(-3) // HWND_MESSAGE – invisible message-only window
        };

        _hwndSource = new HwndSource(parameters);
        _hwndSource.AddHook(WndProc);
    }

    /// <summary>
    /// Registers the global hotkey described by <paramref name="config"/>.
    /// Any previously registered hotkey is unregistered first.
    /// </summary>
    /// <returns><c>true</c> if registration succeeded; otherwise <c>false</c>.</returns>
    public bool Register(HotkeyConfig config)
    {
        if (_hwndSource is null)
        {
            throw new InvalidOperationException("HotkeyManager is not initialized.");
        }

        Unregister();

        if (!config.IsValid())
        {
            AppLogger.Warning($"Invalid hotkey config: {config.ToDisplayString()}");
            return false;
        }

        uint modifiers = NativeMethods.MOD_NOREPEAT;
        if (config.Ctrl) modifiers |= NativeMethods.MOD_CONTROL;
        if (config.Alt) modifiers |= NativeMethods.MOD_ALT;
        if (config.Shift) modifiers |= NativeMethods.MOD_SHIFT;
        if (config.Win) modifiers |= NativeMethods.MOD_WIN;

        uint vk = (uint)KeyInterop.VirtualKeyFromKey(config.Key);

        if (vk == 0)
        {
            AppLogger.Warning($"Key '{config.Key}' could not be mapped to a virtual key code.");
            return false;
        }

        if (NativeMethods.RegisterHotKey(_hwndSource.Handle, HotkeyId, modifiers, vk))
        {
            _isRegistered = true;
            AppLogger.Information($"Hotkey registered: {config.ToDisplayString()}");
            return true;
        }

        int error = Marshal.GetLastWin32Error();
        AppLogger.Warning($"Failed to register hotkey ({error}): {config.ToDisplayString()} (possibly in use)");
        return false;
    }

    /// <summary>
    /// Unregisters the currently active hotkey, if any.
    /// </summary>
    public void Unregister()
    {
        if (_isRegistered && _hwndSource is not null)
        {
            if (!NativeMethods.UnregisterHotKey(_hwndSource.Handle, HotkeyId))
                AppLogger.Debug("UnregisterHotKey failed (may have already been released).");

            _isRegistered = false;
        }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_HOTKEY && wParam == (IntPtr)HotkeyId)
        {
            AppLogger.Debug("Hotkey triggered");
            HotkeyPressed?.Invoke();
            handled = true;
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        Unregister();
        if (_hwndSource is not null)
        {
            _hwndSource.RemoveHook(WndProc);
            _hwndSource.Dispose();
            _hwndSource = null;
        }
    }
}
