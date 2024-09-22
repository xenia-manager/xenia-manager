using System;
using System.Windows;

// Imported
using Serilog;
using XeniaManager;
using XeniaManager.DesktopApp.Windows;
using XeniaManager.Logging;

namespace XeniaManager.DesktopApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Before startup, check if console should be enabled and initialize logger and cleanup of old log files
        /// <para>Afterwards, continue with startup</para>
        /// </summary>
        /// <param name="e"></param>
        protected override void OnStartup(StartupEventArgs e)
        {
            // Check if "-console" argument is present
            if (e.Args.Contains("-console"))
            {
                // Show Console if the argument is present
                Logger.AllocConsole();
            }
            Logger.InitializeLogger(); // Initialize Logger
            Logger.Cleanup(); // Check if there are any log files that should be deleted (Older than 7 days)

            ConfigurationManager.LoadConfigurationFile(); // Loading configuration file
            // Continue doing base startup function
            base.OnStartup(e);
        }

        /// <summary>
        /// While the application is starting up this executes
        /// </summary>
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            
        }
    }
}