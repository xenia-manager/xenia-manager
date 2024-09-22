using Serilog;
using System;
using System.Windows;

// Imported
using XeniaManager.Logging;

namespace XeniaManager.DesktopApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            Logger.InitializeLogger();
            base.OnStartup(e);
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            if (e.Args.Contains("-console"))
            {
                Logger.AllocConsole();
            }

            Log.Information("Application Startup");
        }
    }
}