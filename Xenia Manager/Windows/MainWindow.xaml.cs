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
using Newtonsoft.Json;
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
        /// Crossfade navigation to different WPF Pages
        /// </summary>
        /// <param name="page">Page to navigate to</param>
        public void NavigateToPage(Page page)
        {
            if (PageViewer.Content != null)
            {
                Page currentPage = PageViewer.Content as Page;
                if (currentPage != null)
                {
                    DoubleAnimation fadeOutAnimation = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.15));
                    fadeOutAnimation.Completed += (s, a) =>
                    {
                        PageViewer.Navigate(page);
                    };
                    currentPage.BeginAnimation(Page.OpacityProperty, fadeOutAnimation);
                }
            }
            else
            {
                PageViewer.Navigate(page);
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
                    if (App.appConfiguration.Manager.LastUpdateCheckDate == null || (DateTime.Now - App.appConfiguration.Manager.LastUpdateCheckDate.Value).TotalDays >= 1)
                    {
                        Log.Information("Checking for Xenia Manager updates");

                        using (HttpClient client = new HttpClient())
                        {
                            client.DefaultRequestHeaders.Add("User-Agent", "C# HttpClient");
                            client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");

                            HttpResponseMessage response = await client.GetAsync("https://api.github.com/repos/xenia-manager/xenia-manager/releases/latest");

                            if (response.IsSuccessStatusCode)
                            {
                                string json = await response.Content.ReadAsStringAsync();
                                JObject latestRelease = JObject.Parse(json);
                                string version = (string)latestRelease["tag_name"];
                                DateTime releaseDate;
                                DateTime.TryParseExact(latestRelease["published_at"].Value<string>(), "MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out releaseDate);
                                if (version != App.appConfiguration.Manager.Version)
                                {
                                    Log.Information("Found newer version of Xenia Manager");
                                    Update.Visibility = Visibility.Visible;
                                    latestXeniaManagerRelease = new UpdateInfo();
                                    latestXeniaManagerRelease.Version = version;
                                    latestXeniaManagerRelease.ReleaseDate = releaseDate;
                                    latestXeniaManagerRelease.LastUpdateCheckDate = DateTime.Now;
                                    MessageBox.Show("Found newer version of Xenia Manager.\nClick on the Update button to update the Xenia Manager.");
                                }
                                else
                                {
                                    Log.Information("Latest version is already installed");
                                }
                            }
                            else
                            {
                                Log.Error($"Failed to retrieve releases (Status code: {response.StatusCode})");
                            }
                        }
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
        private void Home_Click(object sender, RoutedEventArgs e)
        {
            NavigateToPage(new Library());
        }

        /// <summary>
        /// Opens the Xenia Settings page
        /// </summary>
        private void XeniaSettings_Click(object sender, RoutedEventArgs e)
        {
            NavigateToPage(new XeniaSettings());
        }

        /// <summary>
        /// Opens Xenia Manager Settings page
        /// </summary>
        private void Settings_Click(object sender, RoutedEventArgs e)
        {

        }

        /// <summary>
        /// Opens Xenia Manager Updater
        /// </summary>
        private async void Update_Click(object sender, RoutedEventArgs e)
        {
            // Updating Xenia Manager info
            Log.Information("Updating info on Xenia Manager");
            App.appConfiguration.Manager.Version = latestXeniaManagerRelease.Version;
            App.appConfiguration.Manager.ReleaseDate = latestXeniaManagerRelease.ReleaseDate;
            App.appConfiguration.Manager.LastUpdateCheckDate = latestXeniaManagerRelease.LastUpdateCheckDate;

            // Updating configuration
            await File.WriteAllTextAsync(AppDomain.CurrentDomain.BaseDirectory + "config.json", JsonConvert.SerializeObject(App.appConfiguration, Formatting.Indented));

            // Launching Xenia Manager Updater
            Log.Information("Launching Xenia Manager Updater");
            using (Process updater = new Process())
            {
                updater.StartInfo.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;
                updater.StartInfo.FileName = "Xenia Manager Updater.exe";
                updater.StartInfo.UseShellExecute = true;
                updater.Start();
            };

            Log.Information("Closing Xenia Manager for update");
            Environment.Exit(0);
        }
    }
}