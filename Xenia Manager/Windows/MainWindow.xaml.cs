using System;
using System.Diagnostics;
using System.IO;
using System.Globalization;
using System.Net.Http;
using System.Windows;
using System.Windows.Input;

// Imported
using Serilog;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xenia_Manager.Classes;
using System.Windows.Media.Animation;

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
        }

        /// <summary>
        /// Executes FadeInAnimation
        /// </summary>
        public void FadeInAnimation()
        {
            Storyboard fadeInStoryboard = (Storyboard)this.Resources["FadeInStoryboard"];
            fadeInStoryboard.Begin(this);
        }

        /// <summary>
        /// Executes Fade out animation
        /// </summary>
        public void FadeOutAnimation()
        {
            Storyboard fadeOutStoryboard = (Storyboard)this.Resources["FadeOutStoryboard"];
            fadeOutStoryboard.Begin(this);
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
        /// Exits the application completely
        /// </summary>
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Log.Information("Closing the application");
            Environment.Exit(0);
        }

        /// <summary>
        /// Opens the Library page
        /// </summary>
        private void Home_Click(object sender, RoutedEventArgs e)
        {
            PageViewer.Source = new Uri("../Pages/Library.XAML", UriKind.Relative);
        }

        /// <summary>
        /// Opens the Settings page
        /// </summary>
        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            PageViewer.Source = new Uri("../Pages/Settings.XAML", UriKind.Relative);
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