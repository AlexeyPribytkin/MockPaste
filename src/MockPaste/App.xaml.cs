using System.Windows;
using MockPaste.Application;
using MockPaste.Infrastructure;
using MockPaste.UI.Dialogs;

namespace MockPaste;

public partial class App : System.Windows.Application
{
    private static Mutex? _instanceMutex;
    private AppBootstrapper? _bootstrapper;

    protected override void OnStartup(StartupEventArgs e)
    {
        bool createdNew;
        try
        {
            _instanceMutex = new Mutex(true, @"Global\MockPaste_SingleInstance", out createdNew);
        }
        catch (AbandonedMutexException)
        {
            createdNew = true;
        }

        if (!createdNew)
        {
            MessageDialog.Show("MockPaste is already running.", "MockPaste", DialogKind.Information);
            _instanceMutex?.Dispose();
            Shutdown(0);
            return;
        }

        base.OnStartup(e);

        _bootstrapper = new AppBootstrapper(() => Shutdown());
        _bootstrapper.Startup();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _bootstrapper?.Shutdown();
        _bootstrapper?.Dispose();
        AppLogger.CloseAndFlush();
        try
        {
            _instanceMutex?.ReleaseMutex();
        }
        catch (ApplicationException)
        {
            // Mutex was already released or was never owned (e.g. startup failed before base.OnStartup).
        }
        _instanceMutex?.Dispose();
        base.OnExit(e);
    }
}

