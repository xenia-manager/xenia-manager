using System;
using System.Windows;

// Imported
using Serilog;
using XeniaManager.Logging;

namespace XeniaManager.DesktopApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Override of what happens on startup
        /// </summary>
        /// <param name="e"></param>
        protected override void OnStartup(StartupEventArgs e)
        {
            Logger.InitializeLogger(); // Initialize Logger
            base.OnStartup(e);
        }

        /// <summary>
        /// While the application is starting up this executes
        /// </summary>
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // Check if "-console" argument is present
            if (e.Args.Contains("-console"))
            {
                // Show Console if the argument is present
                Logger.AllocConsole();
            }
        }
    }
}