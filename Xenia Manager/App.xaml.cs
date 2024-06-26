using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;

// Imported
using Serilog;

namespace Xenia_Manager
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        // This is needed for Console to show up when using argument -console
        [DllImport("Kernel32")]
        public static extern void AllocConsole();

        /// <summary>
        /// This function is used to delete old log files (older than a week)
        /// </summary>
        /// <param name="logDirectory"></param>
        /// <param name="retentionPeriod"></param>
        private void CleanUpOldLogFiles(string logDirectory, TimeSpan retentionPeriod)
        {
            string[] logFiles = Directory.GetFiles(logDirectory, "Log-*.txt");
            DateTime currentTime = DateTime.UtcNow;

            foreach (string logFile in logFiles)
            {
                FileInfo fileInfo = new FileInfo(logFile);
                if (fileInfo.CreationTimeUtc < currentTime - retentionPeriod)
                {
                    fileInfo.Delete();
                }
            }
        }

        /// <summary>
        /// This function holds everything that happens when the application is launching
        /// </summary>
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            CleanUpOldLogFiles("Logs", TimeSpan.FromDays(7));
            // Initializing Logger
            Serilog.Log.Logger = Log.Logger;
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console(
                    outputTemplate: "{Timestamp:dd-MM-yyyy HH:mm:ss}|{Level}|{Message}{Exception}")
                //.WriteTo.File("Logs/Log-.txt", rollingInterval: RollingInterval.Day) - Uncomment this line to save logs into a file
                .CreateLogger();

            // Checks for all of the Launch Arguments
            if (e.Args.Contains("-console"))
            {
                AllocConsole();
            }

            Log.Information("Application is running");
        }
    }
}