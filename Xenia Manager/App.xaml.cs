using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;

// Imported
using Microsoft.Win32;
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

        // Base directory
        public static string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

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
                string configPath = Path.Combine(baseDirectory, "config.json");

                if (File.Exists(configPath))
                {
                    Log.Information("Configuration file found");
                    string json = await File.ReadAllTextAsync(configPath).ConfigureAwait(false);
                    appConfiguration = JsonConvert.DeserializeObject<Configuration>(json);
                    Log.Information("Configuration file loaded");
                }
                else
                {
                    Log.Warning("Configuration file not found (Possibly fresh install)");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occurred while loading the configuration file");
                MessageBox.Show($"{ex.Message}\nFull Error:\n{ex}");
            }
        }

        /// <summary>
        /// Function that checks for Xenia updates (Canary or Stable).
        /// If there is a newer version, asks the user if they want to update.
        /// If the user wants to update, updates Xenia to the latest version.
        /// </summary>
        /// <param name="isCanary">Boolean indicating whether to check for Canary updates (true) or Stable updates (false).</param>
        private async Task CheckForXeniaUpdates(bool isCanary)
        {
            try
            {
                // Determine the type of update (Canary or Stable)
                string updateType = isCanary ? "Canary" : "Stable";
                Log.Information($"Checking for Xenia {updateType} updates");

                // Construct the URL based on update type
                string url = isCanary
                    ? "https://api.github.com/repos/xenia-canary/xenia-canary/releases/latest"
                    : "https://api.github.com/repos/xenia-project/release-builds-windows/releases/latest";

                // Initialize HttpClient and set headers
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "Xenia Manager (https://github.com/xenia-manager/xenia-manager)");
                    client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");

                    // Send GET request to GitHub API
                    HttpResponseMessage response = await client.GetAsync(url);

                    // Check if response is successful
                    if (response.IsSuccessStatusCode)
                    {
                        string json = await response.Content.ReadAsStringAsync();
                        JObject latestRelease = JObject.Parse(json);

                        // Parse release date from response
                        DateTime releaseDate;
                        bool isDateParsed = DateTime.TryParseExact(
                            latestRelease["published_at"].Value<string>(),
                            "MM/dd/yyyy HH:mm:ss",
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.None,
                            out releaseDate
                        );

                        if (!isDateParsed)
                        {
                            Log.Warning($"Failed to parse release date from response: {latestRelease["published_at"].Value<string>()}");
                        }

                        // Retrieve the current configuration based on update type
                        EmulatorInfo currentConfig = isCanary ? appConfiguration.XeniaCanary : appConfiguration.XeniaStable;

                        // Check if the release date indicates a new version
                        if (releaseDate != currentConfig.ReleaseDate)
                        {
                            Log.Information($"Found a newer version of Xenia {updateType} available.");

                            // Prompt user for update confirmation
                            MessageBoxResult result = MessageBox.Show(
                                $"Found a new version of Xenia {updateType}. Do you want to update it?",
                                "Confirmation",
                                MessageBoxButton.YesNo
                            );

                            if (result == MessageBoxResult.Yes)
                            {
                                Log.Information($"User chose to update to the new version (Release date: {releaseDate})");

                                // Retrieve the download URL for the appropriate file
                                JArray assets = (JArray)latestRelease["assets"];
                                if (assets.Count > 0)
                                {
                                    string zipFileName = isCanary ? "xenia_canary.zip" : "xenia_master.zip";
                                    JObject xeniaRelease = (JObject)assets.FirstOrDefault(file => file["name"].ToString() == zipFileName);

                                    if (xeniaRelease != null)
                                    {
                                        string downloadUrl = xeniaRelease["browser_download_url"].ToString();
                                        Log.Information($"Download link for the new Xenia {updateType} build: {downloadUrl}");

                                        // Perform download and extraction
                                        downloadManager.progressBar = null;
                                        downloadManager.downloadUrl = downloadUrl;
                                        downloadManager.downloadPath = Path.Combine(baseDirectory, "xenia.zip");
                                        Log.Information($"Starting the download of the latest Xenia {updateType} build.");
                                        await downloadManager.DownloadAndExtractAsync(Path.Combine(baseDirectory, currentConfig.EmulatorLocation));
                                        Log.Information($"Download and extraction of the latest Xenia {updateType} build completed.");

                                        if (!isCanary)
                                        {
                                            Log.Information("Downloading Xenia VFS Dumper");
                                            await DownloadXeniaVFSDumper();
                                            Log.Information("Xenia VFS Dumper downloaded");
                                        }

                                        // Update configuration with the new version details
                                        currentConfig.Version = (string)latestRelease["tag_name"];
                                        currentConfig.ReleaseDate = releaseDate;
                                        currentConfig.LastUpdateCheckDate = DateTime.Now;
                                        await appConfiguration.SaveAsync(Path.Combine(baseDirectory, "config.json"));
                                        Log.Information($"Xenia {updateType} updated to version {currentConfig.Version}");
                                        MessageBox.Show($"Xenia {updateType} has been updated to the latest build.");
                                    }
                                    else
                                    {
                                        Log.Warning($"No matching asset found for {zipFileName} in the release");
                                    }
                                }
                                else
                                {
                                    Log.Warning("No assets found in the latest release");
                                }
                            }
                            else
                            {
                                Log.Information("User chose not to update");
                            }
                        }
                        else
                        {
                            Log.Information("Latest version is already installed");
                        }
                    }
                    else
                    {
                        Log.Error($"Failed to retrieve releases");
                        Log.Error($"Status code: {response.StatusCode}");
                        Log.Error($"Response content: {await response.Content.ReadAsStringAsync()}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"An error occurred while checking for updates: {ex.Message}");
                MessageBox.Show($"An error occurred: {ex.Message}");
            }
            finally
            {
                // Always update last update check date, regardless of the outcome
                EmulatorInfo currentConfig = isCanary ? appConfiguration.XeniaCanary : appConfiguration.XeniaStable;
                currentConfig.LastUpdateCheckDate = DateTime.Now;
                await appConfiguration.SaveAsync(Path.Combine(baseDirectory, "config.json"));
                Log.Information("Update check date updated");
            }
        }

        /// <summary>
        /// Downloads Xenia Manager Updater if it's not there
        /// </summary>
        public static async Task DownloadXeniaManagerUpdater()
        {
            try
            {
                if (!File.Exists(Path.Combine(baseDirectory, "Xenia Manager Updater.exe")))
                {
                    Log.Information("Downloading Xenia Manager Updater");
                    await downloadManager.DownloadFileAsync("https://github.com/xenia-manager/xenia-manager/releases/download/updater/Xenia.Manager.Updater.zip", Path.Combine(baseDirectory, @"xenia manager updater.zip"));
                    Log.Information("Extracting Xenia Manager Updater");
                    downloadManager.ExtractZipFile(Path.Combine(baseDirectory, @"xenia manager updater.zip"), baseDirectory);
                    Log.Information("Cleaning up");
                    downloadManager.DeleteFile(Path.Combine(baseDirectory, @"xenia manager updater.zip"));
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
        /// Downloads Xenia VFS Dump tool
        /// </summary>
        public static async Task DownloadXeniaVFSDumper()
        {
            try
            {
                if (!Directory.Exists(Path.Combine(baseDirectory, @"Xenia VFS Dump Tool\")))
                {
                    Directory.CreateDirectory(Path.Combine(baseDirectory, @"Xenia VFS Dump Tool\"));
                }
                Log.Information("Downloading Xenia VFS Dump Tool");
                await downloadManager.DownloadFileAsync("https://github.com/xenia-project/release-builds-windows/releases/latest/download/xenia-vfs-dump_master.zip", Path.Combine(baseDirectory, @"xenia-vfs-dump.zip"));
                Log.Information("Extracting Xenia VFS Dump Tool");
                downloadManager.ExtractZipFile(Path.Combine(baseDirectory, @"xenia-vfs-dump.zip"), Path.Combine(baseDirectory, @"Xenia VFS Dump Tool\"));
                Log.Information("Cleaning up");
                downloadManager.DeleteFile(Path.Combine(baseDirectory, @"xenia-vfs-dump.zip"));
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
                Log.Information("Checking which theme is currently selected");
                switch (appConfiguration.ThemeSelected)
                {
                    case "Light":
                        // Apply the Light.xaml theme
                        Log.Information("Applying Light theme");
                        Application.Current.Resources.MergedDictionaries.Clear();
                        ((App)Application.Current).Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = new Uri("pack://application:,,,/Assets/Themes/Light.xaml", UriKind.Absolute) });
                        break;
                    case "Dark":
                        // Apply the Dark.xaml theme
                        Log.Information("Applying Dark theme");
                        Application.Current.Resources.MergedDictionaries.Clear();
                        ((App)Application.Current).Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = new Uri("pack://application:,,,/Assets/Themes/Dark.xaml", UriKind.Absolute) });
                        break;
                    case "AMOLED":
                        // Apply the AMOLED.xaml theme
                        Log.Information("Applying Dark (AMOLED) theme");
                        Application.Current.Resources.MergedDictionaries.Clear();
                        ((App)Application.Current).Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = new Uri("pack://application:,,,/Assets/Themes/AMOLED.xaml", UriKind.Absolute) });
                        break;
                    case "Nord":
                        // Apply the Nord.xaml theme
                        Log.Information("Applying Nord theme");
                        Application.Current.Resources.MergedDictionaries.Clear();
                        ((App)Application.Current).Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = new Uri("pack://application:,,,/Assets/Themes/Nord.xaml", UriKind.Absolute) });
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
                                        Log.Information("Applying the Default (Light) theme");
                                        Application.Current.Resources.MergedDictionaries.Clear();
                                        ((App)Application.Current).Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = new Uri("pack://application:,,,/Assets/Themes/Light.xaml", UriKind.Absolute) });
                                    }
                                }
                            }
                            else
                            {
                                Log.Information("Couldn't detect the selected theme in Windows");
                                Log.Information("Applying the Default (Light) theme");
                                Application.Current.Resources.MergedDictionaries.Clear();
                                ((App)Application.Current).Resources.MergedDictionaries.Add(new ResourceDictionary() { Source = new Uri("pack://application:,,,/Assets/Themes/Light.xaml", UriKind.Absolute) });
                            }
                        }
                        break;
                    default:
                        Log.Information("No theme selected");
                        Log.Information("Applying the Default (Light) theme");
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
            if (!Directory.Exists(Path.Combine(baseDirectory, "Logs")))
            {
                Directory.CreateDirectory(Path.Combine(baseDirectory, "Logs"));
            }

            // Creating a folder where game icons will be stored
            if (!Directory.Exists(Path.Combine(baseDirectory, @"Icons\")))
            {
                Directory.CreateDirectory(Path.Combine(baseDirectory, @"Icons\"));
            }

            // Creating a folder where game icon cache will be
            if (!Directory.Exists(Path.Combine(baseDirectory, @"Icons\Cache")))
            {
                Directory.CreateDirectory(Path.Combine(baseDirectory, @"Icons\Cache"));
            }

            // Clearing icon cache
            foreach (string filePath in Directory.GetFiles(Path.Combine(baseDirectory, @"Icons\Cache"), "*", SearchOption.AllDirectories))
            {
                File.Delete(filePath);  
            }

            // Clean old logs (Older than 7 days)
            CleanUpOldLogFiles(Path.Combine(baseDirectory, "Logs"), TimeSpan.FromDays(7));

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

            Log.Information($"Xenia Manager Version {Assembly.GetExecutingAssembly().GetName().Version.Major}.{Assembly.GetExecutingAssembly().GetName().Version.Minor}.{Assembly.GetExecutingAssembly().GetName().Version.Build}");

            // Load the configuration file for Xenia Manager
            await LoadConfigurationFile();

            Mouse.OverrideCursor = Cursors.Wait;
            // Checking if there is a configuration file
            if (appConfiguration != null)
            {
                // Load the correct theme
                await LoadTheme();

                // This just a check if Xenia is installed (Stable or Canary)
                bool xeniaInstalled = false;

                // Check if Xenia Canary is installed
                if (appConfiguration.XeniaCanary != null)
                {
                    xeniaInstalled = true;
                    // Check if it already checked for Xenia Canary updates
                    if (appConfiguration.XeniaCanary.LastUpdateCheckDate == null || (DateTime.Now - appConfiguration.XeniaCanary.LastUpdateCheckDate.Value).TotalDays >= 1)
                    {
                        // If it didn't, check for a Xenia Canary update
                        await CheckForXeniaUpdates(true);
                    }
                }
                if (appConfiguration.XeniaStable != null)
                {
                    xeniaInstalled = true;
                    // Check if it already checked for Xenia Stable updates
                    if (appConfiguration.XeniaStable.LastUpdateCheckDate == null || (DateTime.Now - appConfiguration.XeniaStable.LastUpdateCheckDate.Value).TotalDays >= 1)
                    {
                        // If it didn't, check for a Xenia Stable update
                        await CheckForXeniaUpdates(false);
                    }
                }
                if (!xeniaInstalled)
                {
                    // If Xenia isn't installed, launch WelcomeDialog
                    WelcomeDialog welcome = new WelcomeDialog();
                    welcome.Show();
                }
                if (appConfiguration.VFSDumpToolLocation == null)
                {
                    // If Xenia XFS Dump tool isn't installed, install it
                    await DownloadXeniaVFSDumper();
                    appConfiguration.VFSDumpToolLocation = @"Xenia VFS Dump Tool\xenia-vfs-dump.exe";
                    await appConfiguration.SaveAsync(Path.Combine(baseDirectory, "config.json"));
                }
            }
            else
            {
                // If there is no configuration file, launch the first time setup process
                await DownloadXeniaManagerUpdater();
                await DownloadXeniaVFSDumper();
                appConfiguration = new Configuration();
                appConfiguration.ThemeSelected = "Light";
                appConfiguration.Manager = new UpdateInfo
                {
                    Version = $"{Assembly.GetExecutingAssembly().GetName().Version.Major}.{Assembly.GetExecutingAssembly().GetName().Version.Minor}.{Assembly.GetExecutingAssembly().GetName().Version.Build}",
                    ReleaseDate = null,
                    LastUpdateCheckDate = DateTime.Now
                };
                appConfiguration.VFSDumpToolLocation = @"Xenia VFS Dump Tool\xenia-vfs-dump.exe";
                await appConfiguration.SaveAsync(Path.Combine(baseDirectory, "config.json"));
                WelcomeDialog welcome = new WelcomeDialog();
                welcome.Show();
            }
            Mouse.OverrideCursor = null;
            Log.Information("Application is running");
        }
    }
}