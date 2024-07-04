using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Windows;

// Imported
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
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

        // Holds the configuration for the Xenia Manager
        public static Configuration? appConfiguration;

        // This is the instance of the downloadManager used throughout the whole app
        public static DownloadManager downloadManager = new DownloadManager(null, null, null);

        /// <summary>
        /// This function is used to delete old log files (older than a week)
        /// </summary>
        /// <param name="logDirectory"></param>
        /// <param name="retentionPeriod"></param>
        private void CleanUpOldLogFiles(string logDirectory, TimeSpan retentionPeriod)
        {
            string[] logFiles = Directory.GetFiles(logDirectory, "Log-*.txt");
            DateTime currentTime = DateTime.UtcNow;
            Log.Information("Looking for old log files to clean");
            foreach (string logFile in logFiles)
            {
                FileInfo fileInfo = new FileInfo(logFile);
                if (fileInfo.CreationTimeUtc < currentTime - retentionPeriod)
                {
                    Log.Information($"Deleting {fileInfo.Name}");
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
                    Log.Information("Configuration file found");
                    string json = await File.ReadAllTextAsync(configPath).ConfigureAwait(false);
                    appConfiguration = JsonConvert.DeserializeObject<Configuration>(json);
                    Log.Information("Configuration file loaded");
                }
                else
                {
                    Log.Warning("Configuration file not found (Could be fresh install)");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred while loading the configuration file");
                MessageBox.Show($"{ex.Message}\nFull Error:\n{ex}");
            }
        }

        /// <summary>
        /// Function that checks for an update
        /// If there is a newer version, ask user if he wants to update
        /// If user wants to update, update Xenia to the latest version
        /// </summary>
        private async Task CheckForXeniaUpdates()
        {
            try
            {
                Log.Information("Checking for updates.");

                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "C# HttpClient");
                    client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");

                    HttpResponseMessage response = await client.GetAsync("https://api.github.com/repos/xenia-canary/xenia-canary/releases/latest");

                    if (response.IsSuccessStatusCode)
                    {
                        string json = await response.Content.ReadAsStringAsync();
                        JObject latestRelease = JObject.Parse(json);
                        DateTime releaseDate;
                        DateTime.TryParseExact(latestRelease["published_at"].Value<string>(), "MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out releaseDate);
                        if (releaseDate != appConfiguration.Xenia.ReleaseDate)
                        {
                            Log.Information("Found newer version of Xenia");
                            MessageBoxResult result = MessageBox.Show("Found a new version of Xenia. Do you want to update Xenia?", "Confirmation", MessageBoxButton.YesNo);

                            if (result == MessageBoxResult.Yes)
                            {
                                Log.Information($"Release date of the build: {releaseDate}");
                                JArray assets = (JArray)latestRelease["assets"];

                                if (assets.Count > 0)
                                {
                                    JObject firstAsset = (JObject)assets[0];
                                    string downloadUrl = firstAsset["browser_download_url"].ToString();
                                    Log.Information($"Download link of the build: {downloadUrl}");

                                    // Perform download and extraction
                                    downloadManager.progressBar = null;
                                    downloadManager.downloadUrl = downloadUrl;
                                    downloadManager.downloadPath = AppDomain.CurrentDomain.BaseDirectory + @"\xenia.zip";
                                    Log.Information("Downloading the latest Xenia Canary build");
                                    await downloadManager.DownloadAndExtractAsync();
                                    Log.Information("Downloading and extraction of the latest Xenia Canary build done");

                                    // Update configuration
                                    appConfiguration.Xenia.Version = (string)latestRelease["tag_name"];
                                    appConfiguration.Xenia.ReleaseDate = releaseDate;
                                    appConfiguration.Xenia.LastUpdateCheckDate = DateTime.Now;
                                    await File.WriteAllTextAsync(AppDomain.CurrentDomain.BaseDirectory + "config.json", JsonConvert.SerializeObject(App.appConfiguration, Formatting.Indented));
                                    Log.Information("Xenia has been updated to the latest build");
                                    MessageBox.Show("Xenia has been updated to the latest build");
                                }
                            }
                        }
                        else
                        {
                            Log.Information("Latest version is already installed");
                        }
                    }
                    else
                    {
                        Log.Error($"Failed to retrieve releases\nStatus code: {response.StatusCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"An error occurred: {ex.Message}");
                MessageBox.Show($"An error occurred: {ex.Message}");
            }
            finally
            {
                // Always update last update check date
                appConfiguration.Xenia.LastUpdateCheckDate = DateTime.Now;
                await File.WriteAllTextAsync(AppDomain.CurrentDomain.BaseDirectory + "config.json", JsonConvert.SerializeObject(App.appConfiguration, Formatting.Indented));
            }
        }


        private async Task DownloadXeniaManagerUpdater()
        {
            try
            {
                Log.Information("Downloading Xenia Manager Updater");
                await downloadManager.DownloadFileAsync("https://github.com/xenia-manager/xenia-manager/releases/download/updater/Xenia.Manager.Updater.zip", AppDomain.CurrentDomain.BaseDirectory + @"\xenia manager updater.zip");
                Log.Information("Extracting Xenia Manager Updater");
                downloadManager.ExtractZipFile(AppDomain.CurrentDomain.BaseDirectory + @"\xenia manager updater.zip", AppDomain.CurrentDomain.BaseDirectory);
                Log.Information("Cleaning up");
                downloadManager.DeleteFile(AppDomain.CurrentDomain.BaseDirectory + @"\xenia manager updater.zip");
                await Task.Delay(1);
            }
            catch (Exception ex)
            {
                Log.Error($"An error occurred: {ex.Message}");
                MessageBox.Show($"An error occurred: {ex.Message}");
            }
        }

        /// <summary>
        /// This function holds everything that happens when the application is launching
        /// </summary>
        private async void Application_Startup(object sender, StartupEventArgs e)
        {
            // Creating Logs folder where all logs will be stored
            if (!Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + "Logs"))
            {
                Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + "Logs");
            }

            // Creating a folder where game icons will be stored
            if (!Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + @"Icons\"))
            {
                Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + @"Icons\");
            }

            // Creating a folder where game icon cache will be
            if (!Directory.Exists($@"{AppDomain.CurrentDomain.BaseDirectory}Icons\Cache"))
            {
                Directory.CreateDirectory($@"{AppDomain.CurrentDomain.BaseDirectory}Icons\Cache");
            }

            // Clearing icon cache
            foreach (string filePath in Directory.GetFiles($@"{AppDomain.CurrentDomain.BaseDirectory}Icons\Cache", "*", SearchOption.AllDirectories))
            {
                File.Delete(filePath);  
            }

            // Clean old logs (Older than 7 days)
            CleanUpOldLogFiles(AppDomain.CurrentDomain.BaseDirectory + "Logs", TimeSpan.FromDays(7));

            // Initializing Logger
            Serilog.Log.Logger = Log.Logger;
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console(
                    outputTemplate: "{Timestamp:dd-MM-yyyy HH:mm:ss}|{Level}|{Message}{NewLine}{Exception}")
                .WriteTo.File("Logs/Log-.txt", rollingInterval: RollingInterval.Day)
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
                // If there is a configuration file, check if it already checked for Xenia updates
                if (appConfiguration.Xenia.LastUpdateCheckDate == null || (DateTime.Now - appConfiguration.Xenia.LastUpdateCheckDate.Value).TotalDays >= 1)
                {
                    // If it didn't, check for a Xenia update
                    await CheckForXeniaUpdates();
                }
            }
            else
            {
                // If there isn't configuration file, download Xenia Manager Updater and load Welcome Window
                await DownloadXeniaManagerUpdater();
                WelcomeDialog welcome = new WelcomeDialog();
                welcome.Show();
            }
            Log.Information("Application is running");
        }
    }
}