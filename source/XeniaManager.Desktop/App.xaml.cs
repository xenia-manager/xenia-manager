using System.Windows;

using XeniaManager.Core;
using XeniaManager.Desktop.Utilities;

namespace XeniaManager.Desktop;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        // Initialize Logger
        Logger.Initialize(e.Args.HasConsoleArgument());

        // Load language
        LocalizationHelper.LoadDefaultLanguage();

        // Continue with startup
        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // Shutdown logger
        Logger.Shutdown();

        // Continue with shutdown
        base.OnExit(e);
    }

    private void Application_Startup(object sender, StartupEventArgs e)
    {

    }
}