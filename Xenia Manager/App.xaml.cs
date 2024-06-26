using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;

// Imported
using Serilog;
using Newtonsoft.Json;
using Xenia_Manager.Classes;
using Xenia_Manager.Windows;

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

        public static Configuration? appConfiguration;

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
        /// Function that loads the configuration file for Xenia Manager
        /// </summary>
        private async Task LoadConfigurationFile()
        {
            try
            {
                Log.Information("Trying to load configuration file");
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");

                if (File.Exists(configPath))
                {
                    string json = await File.ReadAllTextAsync(configPath).ConfigureAwait(false);
                    appConfiguration = JsonConvert.DeserializeObject<Configuration>(json);
                    Log.Information("Configuration file loaded.");
                }
                else
                {
                    Log.Warning("Configuration file not found. (Could be fresh install)");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred while loading the configuration file");
                MessageBox.Show($"{ex.Message}\nFull Error:\n{ex}");
            }
        }

        /// <summary>
        /// This function holds everything that happens when the application is launching
        /// </summary>
        private async void Application_Startup(object sender, StartupEventArgs e)
        {
            if (!Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + "Logs"))
            {
                Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + "Logs");
            }

            // Clean old logs (Older than 7 days)
            CleanUpOldLogFiles(AppDomain.CurrentDomain.BaseDirectory + "Logs", TimeSpan.FromDays(7));

            // Initializing Logger
            Serilog.Log.Logger = Log.Logger;
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console(
                    outputTemplate: "{Timestamp:dd-MM-yyyy HH:mm:ss}|{Level}|{Message}{NewLine}{Exception}")
                //.WriteTo.File("Logs/Log-.txt", rollingInterval: RollingInterval.Day) - Uncomment this line to save logs into a file
                .CreateLogger();

            // Checks for all of the Launch Arguments
            if (e.Args.Contains("-console"))
            {
                AllocConsole();
            }

            // Load the configuration file for Xenia Manager
            await LoadConfigurationFile();

            // Checking if there is a configuration file
            if (appConfiguration != null)
            {
                // If there is a configuration file, check if it already checked for an update
                if (appConfiguration.LastUpdateCheckDate == null || (DateTime.Now - appConfiguration.LastUpdateCheckDate.Value).TotalDays >= 1)
                {
                    // Here goes check for updates
                }
            }
            else
            {
                // If there isn't configuration file, load Welcome Window
                WelcomeDialog welcome = new WelcomeDialog();
                welcome.Show();
            }
            Log.Information("Application is running");
        }
    }
}