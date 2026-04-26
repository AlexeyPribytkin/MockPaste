using System.Runtime.InteropServices;

namespace MockPaste.Infrastructure.Native;

/// <summary>
/// P/Invoke declarations for Win32 APIs used by MockPaste: hotkey registration,
/// keyboard input simulation, clipboard monitoring, and foreground-window tracking.
/// </summary>
internal static partial class NativeMethods
{
    /// <summary>Windows message posted when a registered hotkey is pressed.</summary>
    public const int WM_HOTKEY = 0x0312;
    /// <summary>Alt modifier flag for <c>RegisterHotKey</c>.</summary>
    public const uint MOD_ALT = 0x0001;

    /// <summary>Ctrl modifier flag for <c>RegisterHotKey</c>.</summary>
    public const uint MOD_CONTROL = 0x0002;

    /// <summary>Shift modifier flag for <c>RegisterHotKey</c>.</summary>
    public const uint MOD_SHIFT = 0x0004;

    /// <summary>Windows key modifier flag for <c>RegisterHotKey</c>.</summary>
    public const uint MOD_WIN = 0x0008;

    /// <summary>Suppresses repeated hotkey messages while the key is held down.</summary>
    public const uint MOD_NOREPEAT = 0x4000;

    /// <summary>Input type value for keyboard events in the <see cref="INPUT"/> structure.</summary>
    public const int INPUT_KEYBOARD = 1;

    /// <summary>Flag indicating a key-up (release) event in <see cref="KEYBDINPUT.dwFlags"/>.</summary>
    public const uint KEYEVENTF_KEYUP = 0x0002;

    /// <summary>Virtual-key code for the Ctrl key.</summary>
    public const ushort VK_CONTROL = 0x11;

    /// <summary>Virtual-key code for the V key (used in Ctrl+V paste simulation).</summary>
    public const ushort VK_V = 0x56;

    /// <summary>Registers a system-wide hotkey on the window identified by <paramref name="hWnd"/>.</summary>
    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    /// <summary>Unregisters the hotkey previously registered with <see cref="RegisterHotKey"/>.</summary>
    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool UnregisterHotKey(IntPtr hWnd, int id);

    /// <summary>Sends an array of synthesized keyboard or mouse input events to the input queue.</summary>
    [LibraryImport("user32.dll", SetLastError = true)]
    public static partial uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetCursorPos(out POINT lpPoint);

    [LibraryImport("user32.dll")]
    public static partial IntPtr GetForegroundWindow();

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetForegroundWindow(IntPtr hWnd);

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct INPUT
    {
        public int type;
        public INPUTUNION u;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct INPUTUNION
    {
        [FieldOffset(0)] public KEYBDINPUT ki;
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public HARDWAREINPUT hi;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HARDWAREINPUT
    {
        public uint uMsg;
        public ushort wParamL;
        public ushort wParamH;
    }

    // Foreground window tracking
    public const uint EVENT_SYSTEM_FOREGROUND = 0x0003;
    public const uint WINEVENT_OUTOFCONTEXT = 0x0000;

    public delegate void WinEventProc(
        IntPtr hWinEventHook, uint eventType,
        IntPtr hwnd, int idObject, int idChild,
        uint dwEventThread, uint dwmsEventTime);

    [LibraryImport("user32.dll", SetLastError = true)]
    public static partial IntPtr SetWinEventHook(
        uint eventMin, uint eventMax,
        IntPtr hmodWinEventProc, WinEventProc pfnWinEventProc,
        uint idProcess, uint idThread, uint dwFlags);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool UnhookWinEvent(IntPtr hWinEventHook);

    [LibraryImport("user32.dll", SetLastError = true)]
    public static partial uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool AttachThreadInput(uint idAttach, uint idAttachTo, [MarshalAs(UnmanagedType.Bool)] bool fAttach);

    [LibraryImport("kernel32.dll")]
    public static partial uint GetCurrentThreadId();

    [LibraryImport("user32.dll", EntryPoint = "GetClassNameW", SetLastError = true)]
    public static unsafe partial int GetClassName(IntPtr hWnd, char* lpClassName, int nMaxCount);

    /// <summary>Creates a key-down <see cref="INPUT"/> structure for the given virtual-key code.</summary>
    public static INPUT KeyDown(ushort vk) => new()
    {
        type = INPUT_KEYBOARD,
        u = new INPUTUNION { ki = new KEYBDINPUT { wVk = vk } }
    };

    /// <summary>Creates a key-up <see cref="INPUT"/> structure for the given virtual-key code.</summary>
    public static INPUT KeyUp(ushort vk) => new()
    {
        type = INPUT_KEYBOARD,
        u = new INPUTUNION { ki = new KEYBDINPUT { wVk = vk, dwFlags = KEYEVENTF_KEYUP } }
    };

    /// <summary>Windows message sent to a window registered via <see cref="AddClipboardFormatListener"/> when clipboard contents change.</summary>
    public const int WM_CLIPBOARDUPDATE = 0x031D;

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool AddClipboardFormatListener(IntPtr hwnd);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool RemoveClipboardFormatListener(IntPtr hwnd);
}
