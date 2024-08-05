using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Controls;
using System.Windows.Navigation;

// Imported
using Newtonsoft.Json.Linq;
using Serilog;
using Xenia_Manager.Classes;
using Xenia_Manager.Pages;

namespace Xenia_Manager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Information on the latest version of Xenia Manager
        /// </summary>
        private UpdateInfo latestXeniaManagerRelease;

        /// <summary>
        /// Holds all of the WPF Pages that were opened at some point during the time Xenia Manager was open
        /// </summary>
        private Dictionary<string, Page> pageCache = new Dictionary<string, Page>();

        public MainWindow()
        {
            InitializeComponent();
            PageViewer.Navigated += PageViewer_Navigated;
        }

        /// <summary>
        /// Fade In animation
        /// </summary>
        private void PageViewer_Navigated(object sender, NavigationEventArgs e)
        {
            Page newPage = e.Content as Page;
            if (newPage != null)
            {
                DoubleAnimation fadeInAnimation = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.15));
                newPage.BeginAnimation(Page.OpacityProperty, fadeInAnimation);
            }
        }

        /// <summary>
        /// Check if the Page is already opened and cached and load that, otherwise load new page
        /// </summary>
        /// <param name="pageName">Name of the page user wants to navigate to</param>
        private async Task CheckForCachedPage(string pageName)
        {
            try
            {
                Log.Information($"Trying to navigate to {pageName}");
                switch (pageName)
                {
                    case "XeniaSettings":
                        // Xenia Settings Page
                        Log.Information("Checking if the Xenia Settings Page is already cached");
                        if (!pageCache.ContainsKey(pageName))
                        {
                            Log.Information("Xenia Settings Page is not cached");
                            Log.Information("Loading new Xenia Settings Page and caching it for future use");
                            XeniaSettings xeniaSettings = new XeniaSettings();
                            pageCache[pageName] = xeniaSettings;
                            PageViewer.Navigate(pageCache[pageName]);
                        }
                        else
                        {
                            Log.Information("Xenia Settings Page is already cached");
                            Log.Information("Loading cached Xenia Settings Page");
                            ((XeniaSettings)pageCache[pageName]).InitializeAsync();
                            PageViewer.Navigate(pageCache[pageName]);
                        }
                        break;
                    case "Settings":
                        // Xenia Manager Settings Page
                        Log.Information("Checking if the Settings Page is already cached");
                        if (!pageCache.ContainsKey(pageName))
                        {
                            Log.Information("Settings Page is not cached");
                            Log.Information("Loading new Settings Page and caching it for future use");
                            Settings settings = new Settings();
                            pageCache[pageName] = settings;
                            PageViewer.Navigate(pageCache[pageName]);
                        }
                        else
                        {
                            Log.Information("Settings Page is already cached");
                            Log.Information("Loading cached Settings Page");
                            ((Settings)pageCache[pageName]).InitializeAsync();
                            PageViewer.Navigate(pageCache[pageName]);
                        }
                        break;
                    default:
                        // Home/Library Page
                        Log.Information("Checking if the Library Page is already cached");
                        if (!pageCache.ContainsKey(pageName))
                        {
                            Log.Information("Library Page is not cached");
                            Log.Information("Loading new Library Page and caching it for future use");
                            Library library = new Library();
                            pageCache[pageName] = library;
                            PageViewer.Navigate(pageCache[pageName]);
                        }
                        else
                        {
                            Log.Information("Library Page is already cached");
                            Log.Information("Loading cached Library Page");
                            ((Library)pageCache[pageName]).LoadGamesStartup();
                            PageViewer.Navigate(pageCache[pageName]);
                        }
                        break;
                }
                await Task.Delay(1);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// Crossfade navigation to different WPF Pages
        /// </summary>
        /// <param name="pageName">Name of the page user wants to navigate to</param>
        public async Task NavigateToPage(string pageName)
        {
            if (PageViewer.Content != null)
            {
                Page currentPage = PageViewer.Content as Page;
                if (currentPage != null)
                {
                    DoubleAnimation fadeOutAnimation = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.15));
                    fadeOutAnimation.Completed += async (s, a) =>
                    {
                        await CheckForCachedPage(pageName);
                    };
                    currentPage.BeginAnimation(Page.OpacityProperty, fadeOutAnimation);
                }
            }
            else
            {
                await CheckForCachedPage(pageName);
            }
        }

        /// <summary>
        /// Used for dragging the window around
        /// </summary>
        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        /// <summary>
        /// Checks for updates and if there is a new update, grabs the information about the latest Xenia Manager release
        /// </summary>
        private async Task<bool> GetXeniaManagerUpdateInfo()
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "Xenia Manager (https://github.com/xenia-manager/xenia-manager)");
                    client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");

                    HttpResponseMessage response = await client.GetAsync("https://api.github.com/repos/xenia-manager/xenia-manager/releases/latest");

                    if (response.IsSuccessStatusCode)
                    {
                        string json = await response.Content.ReadAsStringAsync();
                        JObject latestRelease = JObject.Parse(json);
                        string version = (string)latestRelease["tag_name"];

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
                        if (version != App.appConfiguration.Manager.Version)
                        {
                            latestXeniaManagerRelease = new UpdateInfo();
                            latestXeniaManagerRelease.Version = version;
                            latestXeniaManagerRelease.ReleaseDate = releaseDate;
                            latestXeniaManagerRelease.UpdateAvailable = false;
                            latestXeniaManagerRelease.LastUpdateCheckDate = DateTime.Now;
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        Log.Error($"Failed to retrieve releases (Status code: {response.StatusCode})");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"An error occurred: {ex.Message}");
                MessageBox.Show($"An error occurred: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// When window loads, check for updates
        /// </summary>
        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Storyboard fadeInStoryboard = ((Storyboard)Application.Current.FindResource("FadeInAnimation")).Clone();
            fadeInStoryboard.Begin(this);
            try
            {
                if (App.appConfiguration != null)
                {
                    if (App.appConfiguration.Manager.UpdateAvailable != null && App.appConfiguration.Manager.UpdateAvailable == false)
                    {
                        if (App.appConfiguration.Manager.LastUpdateCheckDate == null || (DateTime.Now - App.appConfiguration.Manager.LastUpdateCheckDate.Value).TotalDays >= 1)
                        {
                            Log.Information("Checking for Xenia Manager updates");
                            bool newUpdate = await GetXeniaManagerUpdateInfo();
                            if (newUpdate)
                            {
                                Log.Information("Found newer version of Xenia Manager");
                                Update.Visibility = Visibility.Visible;
                                MessageBox.Show("Found newer version of Xenia Manager.\nClick on the Update button to update the Xenia Manager.");
                                App.appConfiguration.Manager.UpdateAvailable = true;
                                await App.appConfiguration.SaveAsync(Path.Combine(App.baseDirectory, "config.json"));
                            }
                            else
                            {
                                Log.Information("Latest version is already installed");
                            }
                        }
                    }
                    else if (App.appConfiguration.Manager.UpdateAvailable != null && App.appConfiguration.Manager.UpdateAvailable == true)
                    {
                        Update.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        App.appConfiguration.Manager.UpdateAvailable = false;
                        await App.appConfiguration.SaveAsync(Path.Combine(App.baseDirectory, "config.json"));
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"An error occurred: {ex.Message}");
                MessageBox.Show($"An error occurred: {ex.Message}");
            }
        }

        /// <summary>
        /// Maximizes the Xenia Manager window
        /// </summary>
        private void Maximize_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
            }
            else
            {
                this.WindowState = WindowState.Maximized;
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
                Log.Information("Closing the application");
                Environment.Exit(0);
            };

            FadeOutClosingAnimation.Begin(this);
            await Task.Delay(1);
        }

        /// <summary>
        /// Loads FadeOut animation and exits the application completely
        /// </summary>
        private async void Exit_Click(object sender, RoutedEventArgs e)
        {
            await ClosingAnimation();
        }

        /// <summary>
        /// Opens the Library page
        /// </summary>
        private async void Home_Click(object sender, RoutedEventArgs e)
        {
            await NavigateToPage("Library");
        }

        /// <summary>
        /// Opens the Xenia Settings page
        /// </summary>
        private async void XeniaSettings_Click(object sender, RoutedEventArgs e)
        {
            if (App.appConfiguration.XeniaStable != null && File.Exists(App.appConfiguration.XeniaStable.ConfigurationFileLocation))
            {
                await NavigateToPage("XeniaSettings");
            }
            else if (App.appConfiguration.XeniaCanary != null && File.Exists(App.appConfiguration.XeniaCanary.ConfigurationFileLocation))
            {
                await NavigateToPage("XeniaSettings");
            }
            else if (App.appConfiguration.XeniaNetplay != null && File.Exists(App.appConfiguration.XeniaNetplay.ConfigurationFileLocation))
            {
                await NavigateToPage("XeniaSettings");
            }
            else
            {
                MessageBox.Show("Xenia not found.");
            }
        }

        /// <summary>
        /// Opens Xenia Manager Settings page
        /// </summary>
        private async void Settings_Click(object sender, RoutedEventArgs e)
        {
            await NavigateToPage("Settings");
        }

        /// <summary>
        /// Opens Xenia Manager Updater
        /// </summary>
        private async void Update_Click(object sender, RoutedEventArgs e)
        {
            // Updating Xenia Manager info
            Log.Information("Updating info on Xenia Manager");
            if (latestXeniaManagerRelease == null)
            {
                await GetXeniaManagerUpdateInfo();
            }
            App.appConfiguration.Manager.Version = latestXeniaManagerRelease.Version;
            App.appConfiguration.Manager.ReleaseDate = latestXeniaManagerRelease.ReleaseDate;
            App.appConfiguration.Manager.UpdateAvailable = latestXeniaManagerRelease.UpdateAvailable;
            App.appConfiguration.Manager.LastUpdateCheckDate = latestXeniaManagerRelease.LastUpdateCheckDate;

            // Updating configuration
            await App.appConfiguration.SaveAsync(Path.Combine(App.baseDirectory, "config.json"));

            Process.Start(new ProcessStartInfo
            {
                FileName = "https://www.github.com/xenia-manager/xenia-manager/releases/latest",
                UseShellExecute = true,
            });

            // Launching Xenia Manager Updater
            Log.Information("Launching Xenia Manager Updater");
            using (Process updater = new Process())
            {
                updater.StartInfo.WorkingDirectory = App.baseDirectory;
                updater.StartInfo.FileName = "Xenia Manager Updater.exe";
                updater.StartInfo.UseShellExecute = true;
                updater.Start();
            };

            Log.Information("Closing Xenia Manager for update");
            Environment.Exit(0);
        }
    }
}