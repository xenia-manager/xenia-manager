﻿using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;


// Imported
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using Xenia_Manager.Classes;

namespace Xenia_Manager.Windows
{
    /// <summary>
    /// Interaction logic for WelcomeDialog.xaml
    /// </summary>
    public partial class WelcomeDialog : Window
    {
        /// <summary>
        /// Stores the unique identifier for Xenia builds
        /// </summary>
        private string tagName;

        /// <summary>
        /// Stores release date of the Xenia Build
        /// </summary>
        private DateTime releaseDate;

        public WelcomeDialog()
        {
            InitializeComponent();
        }

        /// <summary>
        /// This function
        /// </summary>
        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Storyboard fadeInStoryboard = ((Storyboard)Application.Current.FindResource("FadeInAnimation")).Clone();
            fadeInStoryboard.Begin(this);
            await Task.Delay(1000);
            this.Topmost = false;
        }

        /// <summary>
        /// This is for dragging the window around with mouse
        /// </summary>
        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        /// <summary>
        /// Does fade out animation before closing the window
        /// </summary>
        private async Task ClosingAnimation()
        {
            Storyboard FadeOutClosingAnimation = ((Storyboard)Application.Current.FindResource("FadeOutAnimation")).Clone();

            FadeOutClosingAnimation.Completed += (sender, e) =>
            {
                Log.Information("Closing WelcomeDialog window");
                this.Close();
            };

            FadeOutClosingAnimation.Begin(this);
            await Task.Delay(1);
        }

        /// <summary>
        /// When Exit button is clicked, close the window
        /// </summary>
        private async void Exit_Click(object sender, RoutedEventArgs e)
        {
            await ClosingAnimation();
        }

        /// <summary>
        /// Function that grabs the download link of the selected build.
        /// </summary>
        /// <param name="url">URL of the builds releases page API</param>
        /// <returns>Download URL of the latest release</returns>
        private async Task<string> GrabbingDownloadLink(string url, int assetNumber = 0)
        {
            try
            {
                using HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "C# HttpClient");
                client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");

                HttpResponseMessage response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    Log.Information("Got the response from the Github API");
                    string json = await response.Content.ReadAsStringAsync();
                    JObject latestRelease = JObject.Parse(json);
                    if (latestRelease == null)
                    {
                        Log.Error("Couldn't find latest release");
                        return "";
                    }
                    else
                    {
                        JArray? assets = latestRelease["assets"] as JArray;
                        tagName = (string)latestRelease["tag_name"];
                        DateTime.TryParseExact(latestRelease["published_at"].Value<string>(), "MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out releaseDate);

                        Log.Information($"Release date of the build: {releaseDate.ToString()}");

                        if (assets != null && assets.Count > 0)
                        {
                            JObject? firstAsset = assets[assetNumber] as JObject;
                            string? downloadUrl = firstAsset?["browser_download_url"]?.ToString();

                            if (!string.IsNullOrEmpty(downloadUrl))
                            {
                                Log.Information($"Download link of the build: {downloadUrl}");
                                return downloadUrl;
                            }
                            else
                            {
                                Log.Error("No download URL found");
                                return "";
                            }
                        }
                        else
                        {
                            Log.Error("No assets found for the first release");
                            return "";
                        }
                    }
                }
                else
                {
                    Log.Error($"Failed to retrieve releases. Status code: {response.StatusCode}");
                    return "";
                }
            }
            catch (Exception ex)
            {
                Log.Error($"{ex.Message}\nFull Error:\n{ex}");
                MessageBox.Show(ex.Message);
                return "";
            }
        }

        /// <summary>
        /// Generates Xenia's configuration file
        /// </summary>
        /// <param name="executablePath">Path to Xenia executable</param>
        /// <param name="configPath">Path to Xenia configuration file</param>
        /// <returns></returns>
        private async Task GenerateConfigFile(string executablePath, string configPath)
        {
            try
            {
                Log.Information("Generating configuration file by launching the emulator");
                Process xenia = new Process();
                xenia.StartInfo.FileName = executablePath;
                xenia.Start();
                Log.Information("Emulator Launched");
                Log.Information("Waiting for configuration file to be generated");
                while (!File.Exists(configPath))
                {
                    await Task.Delay(100);
                }
                Log.Information("Configuration file found");
                Log.Information("Closing the emulator");
                xenia.Kill();
                Log.Information("Emulator closed");
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
                return;
            }
        }

        /// <summary>
        /// This downloads and installs the Xenia Stable
        /// </summary>
        private async void InstallXeniaStable_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Grabbing the download link for the Xenia Emulator
                Log.Information("Grabbing the link to the latest Xenia Stable build");
                string url = await GrabbingDownloadLink("https://api.github.com/repos/xenia-project/release-builds-windows/releases/latest", 2);

                // Checking if URL isn't an empty string
                if (url != "")
                {
                    // Downloading the build
                    App.downloadManager.progressBar = Progress;
                    App.downloadManager.downloadUrl = url;
                    App.downloadManager.downloadPath = AppDomain.CurrentDomain.BaseDirectory + @"\xenia.zip";
                    Log.Information("Downloading the latest Xenia Stable build.");
                    await App.downloadManager.DownloadAndExtractAsync(AppDomain.CurrentDomain.BaseDirectory + @"\Xenia Stable\");
                    Log.Information("Downloading and extraction of the latest Xenia Stable build done");

                    // Saving Configuration File as a JSON
                    App.appConfiguration.XeniaStable = new EmulatorInfo
                    {
                        EmulatorLocation = AppDomain.CurrentDomain.BaseDirectory + @"\Xenia Stable\",
                        Version = tagName,
                        ReleaseDate = releaseDate,
                        LastUpdateCheckDate = DateTime.Now
                    };

                    App.appConfiguration.EmulatorLocation = App.appConfiguration.XeniaStable.EmulatorLocation;
                    App.appConfiguration.EmulatorVersion = "Stable";
                    App.appConfiguration.ExecutableLocation = App.appConfiguration.XeniaStable.EmulatorLocation + @"xenia.exe";
                    App.appConfiguration.ConfigurationFileLocation = App.appConfiguration.XeniaStable.EmulatorLocation + @"\xenia.config.toml";

                    Log.Information("Saving the configuration as a JSON file");
                    // Saving the configuration file
                    await App.appConfiguration.SaveAsync(AppDomain.CurrentDomain.BaseDirectory + "config.json");

                    // Add portable.txt so the Xenia Emulator is in portable mode
                    if (!File.Exists(AppDomain.CurrentDomain.BaseDirectory + @"Xenia Stable\portable.txt"))
                    {
                        File.Create(AppDomain.CurrentDomain.BaseDirectory + @"Xenia Stable\portable.txt");
                    }

                    // Add "config" directory for storing game specific configuration files
                    if (!Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + @"Xenia Stable\config"))
                    {
                        Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + @"Xenia Stable\config");
                    }

                    // Download "gamecontrollerdb.txt" for SDL Input System
                    Log.Information("Downloading gamecontrollerdb.txt for SDL Input System");
                    await App.downloadManager.DownloadFileAsync("https://raw.githubusercontent.com/mdqinc/SDL_GameControllerDB/master/gamecontrollerdb.txt", AppDomain.CurrentDomain.BaseDirectory + @"Xenia Stable\gamecontrollerdb.txt");
                }
                else
                {
                    Log.Error("Url is empty. Check connection.");
                    MessageBox.Show("Couldn't grab URL. Check your internet connection and try again");
                }

                // Generating Xenia configuration file
                Log.Information("Generating Xenia configuration by running it");
                await GenerateConfigFile(App.appConfiguration.XeniaStable.EmulatorLocation + @"xenia.exe", App.appConfiguration.XeniaStable.EmulatorLocation + @"\xenia.config.toml");
                Log.Information("Xenia Stable installed.");
                MessageBox.Show("Xenia Stable installed.\nPlease close Xenia if it's still open. (Happens when it shows the warning)");
                await ClosingAnimation();
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
                return;
            }
        }

        /// <summary>
        /// This downloads and installs the Xenia Canary
        /// </summary>
        private async void InstallXeniaCanary_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Grabbing the download link for the Xenia Emulator
                Log.Information("Grabbing the link to the latest Xenia Canary build");
                string url = await GrabbingDownloadLink("https://api.github.com/repos/xenia-canary/xenia-canary/releases/latest");

                // Checking if URL isn't an empty string
                if (url != "")
                {
                    // Downloading the build
                    App.downloadManager.progressBar = Progress;
                    App.downloadManager.downloadUrl = url;
                    App.downloadManager.downloadPath = AppDomain.CurrentDomain.BaseDirectory + @"\xenia.zip";
                    Log.Information("Downloading the latest Xenia Canary build.");
                    await App.downloadManager.DownloadAndExtractAsync(AppDomain.CurrentDomain.BaseDirectory + @"\Xenia Canary\");
                    Log.Information("Downloading and extraction of the latest Xenia Canary build done");

                    // Saving Configuration File as a JSON
                    Log.Information("Creating a JSON configuration file for the Xenia Manager");
                    App.appConfiguration.Manager = new UpdateInfo
                    {
                        Version = $"{Assembly.GetExecutingAssembly().GetName().Version.Major}.{Assembly.GetExecutingAssembly().GetName().Version.Minor}.{Assembly.GetExecutingAssembly().GetName().Version.Build}",
                        ReleaseDate = releaseDate,
                        LastUpdateCheckDate = DateTime.Now
                    };
                    App.appConfiguration.XeniaCanary = new EmulatorInfo
                    {
                        EmulatorLocation = AppDomain.CurrentDomain.BaseDirectory + @"\Xenia Canary\",
                        Version = tagName,
                        ReleaseDate = releaseDate,
                        LastUpdateCheckDate = DateTime.Now
                    };

                    App.appConfiguration.EmulatorLocation = App.appConfiguration.XeniaCanary.EmulatorLocation;
                    App.appConfiguration.EmulatorVersion = "Canary";
                    App.appConfiguration.ExecutableLocation = App.appConfiguration.XeniaCanary.EmulatorLocation + @"xenia_canary.exe";
                    App.appConfiguration.ConfigurationFileLocation = App.appConfiguration.XeniaCanary.EmulatorLocation + @"\xenia-canary.config.toml";

                    Log.Information("Saving the configuration as a JSON file");
                    // Saving the configuration file
                    await App.appConfiguration.SaveAsync(AppDomain.CurrentDomain.BaseDirectory + "config.json");

                    // Add portable.txt so the Xenia Emulator is in portable mode
                    if (!File.Exists(AppDomain.CurrentDomain.BaseDirectory + @"Xenia Canary\portable.txt"))
                    {
                        File.Create(AppDomain.CurrentDomain.BaseDirectory + @"Xenia Canary\portable.txt");
                    }

                    // Add "config" directory for storing game specific configuration files
                    if (!Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + @"Xenia Canary\config"))
                    {
                        Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + @"Xenia Canary\config");
                    }

                    // Download "gamecontrollerdb.txt" for SDL Input System
                    Log.Information("Downloading gamecontrollerdb.txt for SDL Input System");
                    await App.downloadManager.DownloadFileAsync("https://raw.githubusercontent.com/mdqinc/SDL_GameControllerDB/master/gamecontrollerdb.txt", AppDomain.CurrentDomain.BaseDirectory + @"Xenia Canary\gamecontrollerdb.txt");
                }
                else
                {
                    Log.Error("Url is empty. Check connection.");
                    MessageBox.Show("Couldn't grab URL. Check your internet connection and try again");
                }

                // Generating Xenia configuration file
                Log.Information("Generating Xenia configuration by running it");
                await GenerateConfigFile(App.appConfiguration.XeniaCanary.EmulatorLocation + @"xenia_canary.exe", App.appConfiguration.XeniaCanary.EmulatorLocation + @"\xenia-canary.config.toml");
                Log.Information("Xenia Canary installed.");
                MessageBox.Show("Xenia Canary installed.\nPlease close Xenia if it's still open. (Happens when it shows the warning)");
                await ClosingAnimation();
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
                return;
            }
        }
    }
}
