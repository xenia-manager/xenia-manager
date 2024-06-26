﻿using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Globalization;
using System.Net.Http;

// Imported
using Serilog;
using Newtonsoft.Json;
using Xenia_Manager.Classes;
using Xenia_Manager.Windows;
using Newtonsoft.Json.Linq;

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
        private async Task CheckForUpdates()
        {
            try
            {
                Log.Information("Checking for updates.");

                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "C# HttpClient");
                    client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");

                    HttpResponseMessage response = await client.GetAsync("https://api.github.com/repos/xenia-canary/xenia-canary/releases");

                    if (response.IsSuccessStatusCode)
                    {
                        string json = await response.Content.ReadAsStringAsync();
                        JArray releases = JArray.Parse(json);

                        if (releases.Count > 0)
                        {
                            JObject latestRelease = (JObject)releases[0];
                            int id = (int)latestRelease["id"];
                            DateTime releaseDate;
                            DateTime.TryParseExact(latestRelease["published_at"].Value<string>(), "MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out releaseDate);
                            if (id != appConfiguration.Version)
                            {
                                Log.Information("Found newer version of Xenia");
                                MessageBoxResult result = MessageBox.Show("Found a new version of Xenia. Do you want to update Xenia?", "Confirmation", MessageBoxButton.YesNo);

                                if (result == MessageBoxResult.Yes)
                                {
                                    Log.Information($"ID of the build: {id}");
                                    JArray assets = (JArray)latestRelease["assets"];

                                    if (assets.Count > 0)
                                    {
                                        JObject firstAsset = (JObject)assets[0];
                                        string downloadUrl = firstAsset["browser_download_url"].ToString();
                                        Log.Information($"Download link of the build: {downloadUrl}");

                                        // Perform download and extraction
                                        DownloadManager downloadManager = new DownloadManager(null, downloadUrl, AppDomain.CurrentDomain.BaseDirectory + @"\xenia.zip");
                                        Log.Information("Downloading the latest Xenia Canary build");
                                        await downloadManager.DownloadAndExtractAsync();
                                        Log.Information("Downloading and extraction of the latest Xenia Canary build done");

                                        // Update configuration
                                        appConfiguration.Version = id;
                                        appConfiguration.ReleaseDate = releaseDate;
                                        appConfiguration.LastUpdateCheckDate = DateTime.Now;
                                        await File.WriteAllTextAsync(AppDomain.CurrentDomain.BaseDirectory + "config.json", JsonConvert.SerializeObject(App.appConfiguration));
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
                            Log.Error("No releases found");
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
                appConfiguration.LastUpdateCheckDate = DateTime.Now;
                await File.WriteAllTextAsync(AppDomain.CurrentDomain.BaseDirectory + "config.json", JsonConvert.SerializeObject(App.appConfiguration));
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
                // If there is a configuration file, check if it already checked for an update
                if (appConfiguration.LastUpdateCheckDate == null || (DateTime.Now - appConfiguration.LastUpdateCheckDate.Value).TotalDays >= 1)
                {
                    await CheckForUpdates();
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