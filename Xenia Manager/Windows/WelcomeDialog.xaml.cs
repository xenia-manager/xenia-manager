using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Http;
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
            // Checking if Xenia Stable is installed
            Log.Information("Checking if Xenia Stable is installed");
            if (App.appConfiguration.XeniaStable != null)
            {
                // If it's installed, show uninstall button and hide install button
                Log.Information("Xenia Stable is installed");
                Log.Information("Showing 'Uninstall Xenia Stable' button");
                InstallXeniaStable.Visibility = Visibility.Collapsed;
                UninstallXeniaStable.Visibility = Visibility.Visible;
            }
            else
            {
                // If it's not installed, show install button and hide uninstall button
                Log.Information("Xenia Stable is not installed");
                Log.Information("Showing 'Install Xenia Stable' button");
                InstallXeniaStable.Visibility = Visibility.Visible;
                UninstallXeniaStable.Visibility = Visibility.Collapsed;
            }

            // Checking if Xenia Canary is installed
            Log.Information("Checking if Xenia Canary is installed");
            if (App.appConfiguration.XeniaCanary != null)
            {
                // If it's installed, show uninstall button and hide install button
                Log.Information("Xenia Canary is installed");
                Log.Information("Showing 'Uninstall Xenia Canary' button");
                InstallXeniaCanary.Visibility = Visibility.Collapsed;
                UninstallXeniaCanary.Visibility = Visibility.Visible;
            }
            else
            {
                // If it's not installed, show install button and hide uninstall button
                Log.Information("Xenia Canary is not installed");
                Log.Information("Showing 'Install Xenia Canary' button");
                InstallXeniaCanary.Visibility = Visibility.Visible;
                UninstallXeniaCanary.Visibility = Visibility.Collapsed;
            }

            // Run animation and show the window
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
                client.DefaultRequestHeaders.Add("User-Agent", "Xenia Manager (https://github.com/xenia-manager/xenia-manager)");
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

                        // Parse release date from response
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
        /// Removes all games that use specified Xenia version
        /// </summary>
        /// <param name="XeniaVersion">Version that is getting removed</param>
        private async Task RemoveGames(string XeniaVersion)
        {
            try
            {
                await Task.Delay(1);
                List<InstalledGame> ListofGames = new List<InstalledGame>();
                // Reading all games from JSON file
                Log.Information("Retrieving a list of all games installed in Xenia Manager from installedGames.json");
                if (System.IO.File.Exists(AppDomain.CurrentDomain.BaseDirectory + @"installedGames.json"))
                {
                    ListofGames = JsonConvert.DeserializeObject<List<InstalledGame>>((System.IO.File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + @"installedGames.json")));
                }

                List<InstalledGame> gamesToRemove = new List<InstalledGame>();
                // Checking every game
                foreach (InstalledGame game in ListofGames)
                {
                    // Checking if the game emulator version matches the one we're looking for
                    if (game.EmulatorVersion == XeniaVersion)
                    {
                        Log.Information($"Removing '{game.Title}' because it's using Xenia {XeniaVersion}");

                        // Removing game icon
                        if (File.Exists(game.IconFilePath))
                        {
                            File.Delete(game.IconFilePath);
                        }

                        // Removing the game
                        gamesToRemove.Add(game);
                    }
                }

                // Removing the games from the main list
                foreach (InstalledGame game in gamesToRemove)
                {
                    ListofGames.Remove(game);
                }

                // Saving changes
                Log.Information("Saving changes made to the list of games that are installed in Xenia Manager");
                string JSON = JsonConvert.SerializeObject(ListofGames, Formatting.Indented);
                System.IO.File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + @"installedGames.json", JSON);

            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
                Mouse.OverrideCursor = null;
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
                    Mouse.OverrideCursor = Cursors.Wait;

                    // Downloading the build
                    Log.Information("Downloading the latest Xenia Stable build.");
                    App.downloadManager.progressBar = Progress;
                    App.downloadManager.downloadUrl = url;
                    App.downloadManager.downloadPath = Path.Combine(App.baseDirectory, "xenia.zip");
                    await App.downloadManager.DownloadAndExtractAsync(Path.Combine(App.baseDirectory, @"Xenia Stable\"));
                    Log.Information("Download and extraction process of the latest Xenia Stable build completed");

                    // Saving Configuration File as a JSON
                    Log.Information("Creating a configuration file for usage of Xenia Stable");
                    App.appConfiguration.XeniaStable = new EmulatorInfo
                    {
                        EmulatorLocation = @"Xenia Stable\",
                        ExecutableLocation = @"Xenia Stable\xenia.exe",
                        ConfigurationFileLocation = @"Xenia Stable\xenia.config.toml",
                        Version = tagName,
                        ReleaseDate = releaseDate,
                        LastUpdateCheckDate = DateTime.Now
                    };

                    App.appConfiguration.EmulatorLocation = App.appConfiguration.XeniaStable.EmulatorLocation;
                    App.appConfiguration.EmulatorVersion = "Stable";
                    App.appConfiguration.ExecutableLocation = App.appConfiguration.XeniaStable.ExecutableLocation;
                    App.appConfiguration.ConfigurationFileLocation = App.appConfiguration.XeniaStable.ConfigurationFileLocation;

                    Log.Information("Saving the configuration for Xenia Stable");
                    // Saving the configuration file
                    await App.appConfiguration.SaveAsync(Path.Combine(App.baseDirectory, "config.json"));

                    // Add portable.txt so the Xenia Emulator is in portable mode
                    if (!File.Exists(Path.Combine(App.baseDirectory, @"Xenia Stable\portable.txt")))
                    {
                        File.Create(Path.Combine(App.baseDirectory, @"Xenia Stable\portable.txt"));
                    }

                    // Add "config" directory for storing game specific configuration files
                    if (!Directory.Exists(Path.Combine(App.baseDirectory, @"Xenia Stable\config")))
                    {
                        Directory.CreateDirectory(Path.Combine(App.baseDirectory, @"Xenia Stable\config"));
                    }

                    // Download "gamecontrollerdb.txt" for SDL Input System
                    Log.Information("Downloading gamecontrollerdb.txt for SDL Input System");
                    await App.downloadManager.DownloadFileAsync("https://raw.githubusercontent.com/mdqinc/SDL_GameControllerDB/master/gamecontrollerdb.txt", Path.Combine(App.baseDirectory, @"Xenia Stable\gamecontrollerdb.txt"));
                }
                else
                {
                    Log.Error("Url is empty. Check connection.");
                    MessageBox.Show("Couldn't grab URL. Check your internet connection and try again");
                }

                // Generating Xenia configuration file
                Log.Information("Generating Xenia Stable configuration by running the emulator");
                await GenerateConfigFile(Path.Combine(App.baseDirectory, App.appConfiguration.XeniaStable.ExecutableLocation), Path.Combine(App.baseDirectory, App.appConfiguration.XeniaStable.ConfigurationFileLocation));
                Log.Information("Xenia Stable installed");
                Mouse.OverrideCursor = null;
                MessageBox.Show("Xenia Stable installed.\nPlease close Xenia if it's still open. (Happens when it shows the warning)");
                await ClosingAnimation();
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
                Mouse.OverrideCursor = null;
                return;
            }
        }

        /// <summary>
        /// Uninstalls Xenia Stable
        /// </summary>
        private async void UninstallXeniaStable_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await Task.Delay(1);
                MessageBoxResult result = MessageBox.Show("Do you want to uninstall Xenia Stable?\nThis will remove all save files and updates alongside the emulator.", "Confirmation", MessageBoxButton.YesNo);
                // Delete the folder containing Xenia Stable
                if (result == MessageBoxResult.Yes)
                {
                    Log.Information("Deleting Xenia Stable folder");
                    if (Directory.Exists(App.appConfiguration.XeniaStable.EmulatorLocation))
                    {
                        Directory.Delete(App.appConfiguration.XeniaStable.EmulatorLocation, true);
                    }

                    Log.Information("Removing all games that use Xenia Stable");
                    await RemoveGames("Stable");

                    // Update the configuration file of Xenia Manager
                    App.appConfiguration.XeniaStable = null;

                    if (App.appConfiguration.XeniaCanary != null)
                    {
                        App.appConfiguration.EmulatorVersion = "Canary";
                        App.appConfiguration.EmulatorLocation = App.appConfiguration.XeniaCanary.EmulatorLocation;
                        App.appConfiguration.ExecutableLocation = App.appConfiguration.XeniaCanary.ExecutableLocation;
                        App.appConfiguration.ConfigurationFileLocation = App.appConfiguration.XeniaCanary.ConfigurationFileLocation;
                    }
                    else
                    {
                        App.appConfiguration.EmulatorVersion = null;
                        App.appConfiguration.EmulatorLocation = null;
                        App.appConfiguration.ExecutableLocation = null;
                        App.appConfiguration.ConfigurationFileLocation = null;
                    }
                    await App.appConfiguration.SaveAsync(Path.Combine(App.baseDirectory, "config.json"));

                    // Hiding the uninstall button and showing install button again
                    InstallXeniaStable.Visibility = Visibility.Visible;
                    UninstallXeniaStable.Visibility = Visibility.Collapsed;

                    MessageBox.Show("Xenia Stable has been uninstalled.");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
                Mouse.OverrideCursor = null;
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
                    Mouse.OverrideCursor = Cursors.Wait;
                    // Downloading the build
                    Log.Information("Downloading the latest Xenia Canary build");
                    App.downloadManager.progressBar = Progress;
                    App.downloadManager.downloadUrl = url;
                    App.downloadManager.downloadPath = Path.Combine(App.baseDirectory, "xenia.zip");
                    await App.downloadManager.DownloadAndExtractAsync(Path.Combine(App.baseDirectory, @"Xenia Canary\"));
                    Log.Information("Download and extraction process of the latest Xenia Canary build done");

                    // Saving Configuration File as a JSON
                    Log.Information("Creating a configuration file for usage of Xenia Canary");
                    App.appConfiguration.XeniaCanary = new EmulatorInfo
                    {
                        EmulatorLocation = @"Xenia Canary\",
                        ExecutableLocation = @"Xenia Canary\xenia_canary.exe",
                        ConfigurationFileLocation = @"Xenia Canary\xenia-canary.config.toml",
                        Version = tagName,
                        ReleaseDate = releaseDate,
                        LastUpdateCheckDate = DateTime.Now
                    };

                    App.appConfiguration.EmulatorLocation = App.appConfiguration.XeniaCanary.EmulatorLocation;
                    App.appConfiguration.EmulatorVersion = "Canary";
                    App.appConfiguration.ExecutableLocation = App.appConfiguration.XeniaCanary.ExecutableLocation;
                    App.appConfiguration.ConfigurationFileLocation = App.appConfiguration.XeniaCanary.ConfigurationFileLocation;

                    Log.Information("Saving the configuration for Xenia Stable");
                    // Saving the configuration file
                    await App.appConfiguration.SaveAsync(Path.Combine(App.baseDirectory, "config.json"));

                    // Add portable.txt so the Xenia Emulator is in portable mode
                    if (!File.Exists(Path.Combine(App.baseDirectory, @"Xenia Canary\portable.txt")))
                    {
                        File.Create(Path.Combine(App.baseDirectory, @"Xenia Canary\portable.txt"));
                    }

                    // Add "config" directory for storing game specific configuration files
                    if (!Directory.Exists(Path.Combine(App.baseDirectory, @"Xenia Canary\config")))
                    {
                        Directory.CreateDirectory(Path.Combine(App.baseDirectory, @"Xenia Canary\config"));
                    }

                    // Download "gamecontrollerdb.txt" for SDL Input System
                    Log.Information("Downloading gamecontrollerdb.txt for SDL Input System");
                    await App.downloadManager.DownloadFileAsync("https://raw.githubusercontent.com/mdqinc/SDL_GameControllerDB/master/gamecontrollerdb.txt", Path.Combine(App.baseDirectory, @"Xenia Canary\gamecontrollerdb.txt"));
                }
                else
                {
                    Log.Error("Url is empty. Check connection.");
                    MessageBox.Show("Couldn't grab URL. Check your internet connection and try again");
                }

                // Generating Xenia configuration file
                Log.Information("Generating Xenia Canary configuration by running the emulator");
                await GenerateConfigFile(Path.Combine(App.baseDirectory, App.appConfiguration.XeniaCanary.ExecutableLocation), Path.Combine(App.baseDirectory, App.appConfiguration.XeniaCanary.ConfigurationFileLocation));
                Log.Information("Xenia Canary installed");
                Mouse.OverrideCursor = null;
                MessageBox.Show("Xenia Canary installed.\nPlease close Xenia if it's still open. (Happens when it shows the warning)");
                await ClosingAnimation();
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
                Mouse.OverrideCursor = null;
                return;
            }
        }

        /// <summary>
        /// Uninstalls Xenia Stable
        /// </summary>
        private async void UninstallXeniaCanary_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await Task.Delay(1);
                MessageBoxResult result = MessageBox.Show("Do you want to uninstall Xenia Canary?\nThis will remove all save files and updates alongside the emulator.", "Confirmation", MessageBoxButton.YesNo);
                // Delete the folder containing Xenia Canary
                if (result == MessageBoxResult.Yes)
                {
                    Log.Information("Deleting Xenia Canary folder");
                    if (Directory.Exists(App.appConfiguration.XeniaCanary.EmulatorLocation))
                    {
                        Directory.Delete(App.appConfiguration.XeniaCanary.EmulatorLocation, true);
                    }

                    // Remove all games using the emulator
                    Log.Information("Removing all games that use Xenia Canary");
                    await RemoveGames("Canary");

                    // Update the configuration file of Xenia Manager
                    App.appConfiguration.XeniaCanary = null;

                    if (App.appConfiguration.XeniaStable != null)
                    {
                        App.appConfiguration.EmulatorVersion = "Stable";
                        App.appConfiguration.EmulatorLocation = App.appConfiguration.XeniaStable.EmulatorLocation;
                        App.appConfiguration.ExecutableLocation = App.appConfiguration.XeniaStable.ExecutableLocation;
                        App.appConfiguration.ConfigurationFileLocation = App.appConfiguration.XeniaStable.ConfigurationFileLocation;
                    }
                    else
                    {
                        App.appConfiguration.EmulatorVersion = null;
                        App.appConfiguration.EmulatorLocation = null;
                        App.appConfiguration.ExecutableLocation = null;
                        App.appConfiguration.ConfigurationFileLocation = null;
                    }
                    await App.appConfiguration.SaveAsync(Path.Combine(App.baseDirectory, "config.json"));

                    // Hiding the uninstall button and showing install button again
                    InstallXeniaCanary.Visibility = Visibility.Visible;
                    UninstallXeniaCanary.Visibility = Visibility.Collapsed;

                    MessageBox.Show("Xenia Canary has been uninstalled.");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
                Mouse.OverrideCursor = null;
                return;
            }
        }
    }
}
