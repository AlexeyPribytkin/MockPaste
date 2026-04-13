using System.Windows.Input;
using System.Windows.Interop;
using MockPaste.Core.Models;
using MockPaste.Infrastructure;
using MockPaste.Infrastructure.Native;

namespace MockPaste.Application;

public sealed class HotkeyManager : IDisposable
{
    private const int HotkeyId = 9001;

    private HwndSource? _hwndSource;
    private bool _isRegistered;

    public event Action? HotkeyPressed;

    public void Initialize()
    {
        var parameters = new HwndSourceParameters("MockPasteHotkey")
        {
            Width = 0,
            Height = 0,
            ParentWindow = new IntPtr(-3) // HWND_MESSAGE – invisible message-only window
        };
        _hwndSource = new HwndSource(parameters);
        _hwndSource.AddHook(WndProc);
    }

    public bool Register(HotkeyConfig config)
    {
        if (_hwndSource is null)
            throw new InvalidOperationException("HotkeyManager is not initialized.");

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

        if (NativeMethods.RegisterHotKey(_hwndSource.Handle, HotkeyId, modifiers, vk))
        {
            _isRegistered = true;
            AppLogger.Information($"Hotkey registered: {config.ToDisplayString()}");
            return true;
        }

        AppLogger.Warning($"Failed to register hotkey: {config.ToDisplayString()} (possibly in use)");
        return false;
    }

    public void Unregister()
    {
        if (_isRegistered && _hwndSource is not null)
        {
            NativeMethods.UnregisterHotKey(_hwndSource.Handle, HotkeyId);
            _isRegistered = false;
        }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_HOTKEY && wParam.ToInt32() == HotkeyId)
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
        _hwndSource?.Dispose();
        _hwndSource = null;
    }
}
