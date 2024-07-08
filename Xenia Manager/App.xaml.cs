using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using Microsoft.Win32;


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
        private async Task CheckForXeniaCanaryUpdates()
        {
            try
            {
                Log.Information("Checking for Xenia Canary updates");

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
                        if (releaseDate != appConfiguration.XeniaCanary.ReleaseDate)
                        {
                            Log.Information("Found newer version of Xenia");
                            MessageBoxResult result = MessageBox.Show("Found a new version of Xenia Canary. Do you want to update it?", "Confirmation", MessageBoxButton.YesNo);

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
                                    await downloadManager.DownloadAndExtractAsync(appConfiguration.XeniaCanary.EmulatorLocation);
                                    Log.Information("Downloading and extraction of the latest Xenia Canary build done");

                                    // Update configuration
                                    appConfiguration.XeniaCanary.Version = (string)latestRelease["tag_name"];
                                    appConfiguration.XeniaCanary.ReleaseDate = releaseDate;
                                    appConfiguration.XeniaCanary.LastUpdateCheckDate = DateTime.Now;
                                    await appConfiguration.SaveAsync(AppDomain.CurrentDomain.BaseDirectory + "config.json");
                                    Log.Information("Xenia Canary has been updated to the latest build");
                                    MessageBox.Show("Xenia Canary has been updated to the latest build");
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
                appConfiguration.XeniaCanary.LastUpdateCheckDate = DateTime.Now;
                await appConfiguration.SaveAsync(AppDomain.CurrentDomain.BaseDirectory + "config.json");
            }
        }

        private async Task DownloadXeniaManagerUpdater()
        {
            try
            {
                if (!File.Exists(AppDomain.CurrentDomain.BaseDirectory + "Xenia Manager Updater.exe"))
                {
                    Log.Information("Downloading Xenia Manager Updater");
                    await downloadManager.DownloadFileAsync("https://github.com/xenia-manager/xenia-manager/releases/download/updater/Xenia.Manager.Updater.zip", AppDomain.CurrentDomain.BaseDirectory + @"\xenia manager updater.zip");
                    Log.Information("Extracting Xenia Manager Updater");
                    downloadManager.ExtractZipFile(AppDomain.CurrentDomain.BaseDirectory + @"\xenia manager updater.zip", AppDomain.CurrentDomain.BaseDirectory);
                    Log.Information("Cleaning up");
                    downloadManager.DeleteFile(AppDomain.CurrentDomain.BaseDirectory + @"\xenia manager updater.zip");
                }
                await Task.Delay(1);
            }
            catch (Exception ex)
            {
                Log.Error($"An error occurred: {ex.Message}");
                MessageBox.Show($"An error occurred: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads the selected theme into the UI
        /// </summary>
        public static async Task LoadTheme()
        {
            try
            {
                switch (appConfiguration.ThemeSelected)
                {
                    case "Light":
                        // Apply the Light.xaml theme
                        Log.Information("Applying light theme");
                        Application.Current.Resources.MergedDictionaries.Clear();
                        ((App)Application.Current).Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = new Uri("pack://application:,,,/Assets/Themes/Light.xaml", UriKind.Absolute) });
                        break;
                    case "Dark":
                        // Apply the Dark.xaml theme
                        Log.Information("Applying dark theme");
                        Application.Current.Resources.MergedDictionaries.Clear();
                        ((App)Application.Current).Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = new Uri("pack://application:,,,/Assets/Themes/Dark.xaml", UriKind.Absolute) });
                        break;
                    case "Default":
                        // Check system and then apply the correct one
                        Log.Information("Checking the selected theme in Windows");
                        using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                        {
                            if (key != null)
                            {
                                object value = key.GetValue("AppsUseLightTheme");
                                if (value != null && int.TryParse(value.ToString(), out int appsUseLightTheme))
                                {
                                    if (appsUseLightTheme == 0)
                                    {
                                        Log.Information("Dark theme detected in Windows");
                                        Application.Current.Resources.MergedDictionaries.Clear();
                                        ((App)Application.Current).Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = new Uri("pack://application:,,,/Assets/Themes/Dark.xaml", UriKind.Absolute) });
                                    }
                                    else if (appsUseLightTheme == 1)
                                    {
                                        Log.Information("Light theme detected in Windows");
                                        Application.Current.Resources.MergedDictionaries.Clear();
                                        ((App)Application.Current).Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = new Uri("pack://application:,,,/Assets/Themes/Light.xaml", UriKind.Absolute) });
                                    }
                                    else
                                    {
                                        Log.Information("Couldn't detect the selected theme in Windows");
                                        Log.Information("Applying Light theme");
                                        Application.Current.Resources.MergedDictionaries.Clear();
                                        ((App)Application.Current).Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = new Uri("pack://application:,,,/Assets/Themes/Light.xaml", UriKind.Absolute) });
                                    }
                                }
                            }
                            else
                            {
                                Log.Information("Couldn't detect the selected theme in Windows");
                                Log.Information("Applying Light theme");
                                Application.Current.Resources.MergedDictionaries.Clear();
                                ((App)Application.Current).Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = new Uri("pack://application:,,,/Assets/Themes/Light.xaml", UriKind.Absolute) });
                            }
                        }
                        break;
                    default:
                        Log.Information("No theme selected");
                        Log.Information("Default one will be loaded");
                        break;
                }
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
                // Load the correct theme
                await LoadTheme();

                // Check if Xenia Canary is installed
                if (appConfiguration.XeniaCanary != null)
                {
                    // Check if it already checked for Xenia Canary updates
                    if (appConfiguration.XeniaCanary.LastUpdateCheckDate == null || (DateTime.Now - appConfiguration.XeniaCanary.LastUpdateCheckDate.Value).TotalDays >= 1)
                    {
                        // If it didn't, check for a Xenia update
                        await CheckForXeniaCanaryUpdates();
                    }
                }
                else
                {
                    // If there isn't configuration file, download Xenia Manager Updater and load Welcome Window
                    WelcomeDialog welcome = new WelcomeDialog();
                    welcome.Show();
                }
            }
            else
            {
                // If there isn't Xenia installed, load Welcome Window
                await DownloadXeniaManagerUpdater();
                appConfiguration = new Configuration();
                appConfiguration.ThemeSelected = "Light";
                appConfiguration.Manager = new UpdateInfo
                {
                    Version = $"{Assembly.GetExecutingAssembly().GetName().Version.Major}.{Assembly.GetExecutingAssembly().GetName().Version.Minor}.{Assembly.GetExecutingAssembly().GetName().Version.Build}",
                    ReleaseDate = null,
                    LastUpdateCheckDate = DateTime.Now
                };
                await appConfiguration.SaveAsync(AppDomain.CurrentDomain.BaseDirectory + "config.json");
                WelcomeDialog welcome = new WelcomeDialog();
                welcome.Show();
            }
            Log.Information("Application is running");
        }
    }
}