using System.Windows.Forms;
using MockPaste.Infrastructure;
using MockPaste.Infrastructure.Native;

namespace MockPaste.UI.Tray;

public sealed class TrayIconManager : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly NativeWindow _menuWindow = new();

    private const uint CmdToggle = 1;
    private const uint CmdSettings = 2;
    private const uint CmdExit = 3;

    public event Action? OnSettingsClicked;
    public event Action? OnExitClicked;
    public bool IsEnabled { get; private set; } = true;

    public event Action<bool>? EnabledChanged;

    public TrayIconManager()
    {
        _menuWindow.CreateHandle(new CreateParams());

        _notifyIcon = new NotifyIcon
        {
            Text = "MockPaste",
            Icon = CreateDefaultIcon(),
            Visible = true
        };
        _notifyIcon.MouseClick += OnNotifyIconMouseClick;
    }

    private void OnNotifyIconMouseClick(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Right)
            ShowNativeContextMenu();
    }

    private void ShowNativeContextMenu()
    {
        var hMenu = NativeMethods.CreatePopupMenu();
        if (hMenu == IntPtr.Zero) return;

        try
        {
            uint enabledFlags = NativeMethods.MF_STRING | (IsEnabled ? NativeMethods.MF_CHECKED : 0);
            NativeMethods.AppendMenu(hMenu, enabledFlags, CmdToggle, "Enabled");
            NativeMethods.AppendMenu(hMenu, NativeMethods.MF_SEPARATOR, 0, null);
            NativeMethods.AppendMenu(hMenu, NativeMethods.MF_STRING, CmdSettings, "Settings");
            NativeMethods.AppendMenu(hMenu, NativeMethods.MF_SEPARATOR, 0, null);
            NativeMethods.AppendMenu(hMenu, NativeMethods.MF_STRING, CmdExit, "Exit");

            NativeMethods.GetCursorPos(out var pt);
            NativeMethods.SetForegroundWindow(_menuWindow.Handle);

            uint cmd = NativeMethods.TrackPopupMenuEx(
                hMenu,
                NativeMethods.TPM_RETURNCMD | NativeMethods.TPM_NONOTIFY | NativeMethods.TPM_BOTTOMALIGN,
                pt.X, pt.Y,
                _menuWindow.Handle,
                IntPtr.Zero);

            switch (cmd)
            {
                case CmdToggle:
                    IsEnabled = !IsEnabled;
                    _notifyIcon.Text = IsEnabled ? "MockPaste" : "MockPaste (Disabled)";
                    EnabledChanged?.Invoke(IsEnabled);
                    AppLogger.Information($"MockPaste {(IsEnabled ? "enabled" : "disabled")}");
                    break;
                case CmdSettings:
                    OnSettingsClicked?.Invoke();
                    break;
                case CmdExit:
                    OnExitClicked?.Invoke();
                    break;
            }
        }
        finally
        {
            NativeMethods.DestroyMenu(hMenu);
        }
    }

    private static Icon CreateDefaultIcon()
    {
        try
        {
            var uri = new Uri("pack://application:,,,/Assets/app.ico", UriKind.Absolute);
            var info = System.Windows.Application.GetResourceStream(uri);
            if (info?.Stream != null)
                return new Icon(info.Stream);
        }
        catch
        {
            // ignore and fall back
        }

        var bitmap = new Bitmap(16, 16);
        using var g = Graphics.FromImage(bitmap);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var brush = new SolidBrush(Color.FromArgb(124, 58, 237));
        g.FillRectangle(brush, 0, 0, 16, 16);
        return Icon.FromHandle(bitmap.GetHicon());
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _menuWindow.DestroyHandle();
    }
}
