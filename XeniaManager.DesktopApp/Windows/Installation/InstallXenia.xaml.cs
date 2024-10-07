using System;
using System.IO;
using System.Windows;
using System.Windows.Input;

// Imported
using Serilog;
using XeniaManager.DesktopApp.Utilities.Animations;
using XeniaManager.Downloader;
using XeniaManager.Installation;

namespace XeniaManager.DesktopApp.Windows
{
    /// <summary>
    /// Interaction logic for InstallXenia.xaml
    /// </summary>
    public partial class InstallXenia : Window
    {
        public InstallXenia()
        {
            InitializeComponent();
        }

        // Functions
        /// <summary>
        /// Checks all supported Xenia emulators if they're installed and shows install/uninstall option in this window
        /// </summary>
        private void CheckForInstalledXenia()
        {
            // Checking if Xenia Stable is installed
            /*
            Log.Information("Checking if Xenia Stable is installed");
            if (ConfigurationManager.AppConfig.XeniaStable != null)
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
            }*/

            // Checking if Xenia Canary is installed
            Log.Information("Checking if Xenia Canary is installed");
            if (ConfigurationManager.AppConfig.XeniaCanary != null)
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

            // Checking if Xenia Netplay is installed
            Log.Information("Checking if Xenia Netplay is installed");
            if (ConfigurationManager.AppConfig.XeniaNetplay != null)
            {
                // If it's installed, show uninstall button and hide install button
                Log.Information("Xenia Netplay is installed");
                Log.Information("Showing 'Uninstall Xenia Netplay' button");
                InstallXeniaNetplay.Visibility = Visibility.Collapsed;
                UninstallXeniaNetplay.Visibility = Visibility.Visible;
            }
            else
            {
                // If it's not installed, show install button and hide uninstall button
                Log.Information("Xenia Netplay is not installed");
                Log.Information("Showing 'Install Xenia Netplay' button");
                InstallXeniaNetplay.Visibility = Visibility.Visible;
                UninstallXeniaNetplay.Visibility = Visibility.Collapsed;
            }
        }

        // UI Interactions
        // Window interactions
        /// <summary>
        /// When window loads, check for updates
        /// </summary>
        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            WindowAnimations.OpeningAnimation(this); // Run "Fade-In" animation
            CheckForInstalledXenia();
            await Task.Delay(50);
            this.Topmost = false;
        }

        /// <summary>
        /// Enables dragging the window
        /// </summary>
        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Checks if Left Mouse is pressed and if it is, enable DragMove()
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        // TitleBar Button Interactions
        /// <summary>
        /// What happens when Exit button is pressed
        /// </summary>
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            // Run "Fade-Out" animation and then close the window
            WindowAnimations.ClosingAnimation(this);
        }

        /*     
        /// <summary>
        /// Download and setup Xenia Stable
        /// </summary>
        private async void InstallXeniaStable_Click(object sender, RoutedEventArgs e)
        {
            // Grab the URL to the latest Xenia Stable release
            string url = await InstallationManager.DownloadLinkGrabber("https://api.github.com/repos/xenia-project/release-builds-windows/releases", 0, 2);
            if (url == null)
            {
                Log.Information("No URL has been found");
                return;
            }
            Mouse.OverrideCursor = Cursors.Wait;

            // Download and extract the build
            DownloadManager.ProgressChanged += (progress) =>
            {
                Progress.Value = progress;
            };
            Log.Information("Downloading the latest Xenia Stable build");
            await DownloadManager.DownloadAndExtractAsync(url, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Downloads\xenia.zip"), Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Xenia Stable\"));

            // Download "gamecontrollerdb.txt" for SDL Input System
            Log.Information("Downloading gamecontrollerdb.txt for SDL Input System");
            await DownloadManager.DownloadFileAsync("https://raw.githubusercontent.com/mdqinc/SDL_GameControllerDB/master/gamecontrollerdb.txt", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Xenia Stable\gamecontrollerdb.txt"));

            // Running Xenia Stable setup
            Log.Information("Running Xenia Stable setup");
            InstallationManager.Xenia.StableSetup();
            Log.Information("Xenia Stable installed");

            // Hiding the install button and showing the uninstall button again
            InstallXeniaStable.Visibility = Visibility.Collapsed;
            UninstallXeniaStable.Visibility = Visibility.Visible;

        Mouse.OverrideCursor = null;
            MessageBox.Show("Xenia Stable installed.\nPlease close Xenia if it's still open. (Happens when it shows the warning)");
        }*/

        /// <summary>
        /// Download and setup Xenia Canary
        /// </summary>
        private async void InstallXeniaCanary_Click(object sender, RoutedEventArgs e)
        {
            // Grab the URL to the latest Xenia Canary release
            string url = await InstallationManager.DownloadLinkGrabber("https://api.github.com/repos/xenia-canary/xenia-canary/releases", 1);
            if (url == null)
            {
                Log.Information("No URL has been found");
                return;
            }
            Mouse.OverrideCursor = Cursors.Wait;

            // Download and extract the build
            DownloadManager.ProgressChanged += (progress) =>
            {
                Progress.Value = progress;
            };
            Log.Information("Downloading the latest Xenia Canary build");
            await DownloadManager.DownloadAndExtractAsync(url, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Downloads\xenia.zip"), Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Emulators\Xenia Canary\"));

            // Download "gamecontrollerdb.txt" for SDL Input System
            Log.Information("Downloading gamecontrollerdb.txt for SDL Input System");
            await DownloadManager.DownloadFileAsync("https://raw.githubusercontent.com/mdqinc/SDL_GameControllerDB/master/gamecontrollerdb.txt", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Emulators\Xenia Canary\gamecontrollerdb.txt"));

            // Running Xenia Canary setup
            Log.Information("Running Xenia Canary setup");
            MessageBox.Show("In the following window please create the profile for Xenia (if needed) and close the emulator.");
            InstallationManager.Xenia.CanarySetup();
            Log.Information("Xenia Canary installed");

            // Hiding the install button and showing the uninstall button again
            InstallXeniaCanary.Visibility = Visibility.Collapsed;
            UninstallXeniaCanary.Visibility = Visibility.Visible;

            Mouse.OverrideCursor = null;
            MessageBox.Show("Xenia Canary installed.");
        }

        /// <summary>
        /// Uninstalls Xenia Canary
        /// </summary>
        private void UninstallXeniaCanary_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Do you want to uninstall Xenia Canary?\nThis will remove all save files and updates alongside the emulator.", "Confirmation", MessageBoxButton.YesNo);
            if (result != MessageBoxResult.Yes)
            {
                return;
            }
            InstallationManager.Xenia.Uninstall(EmulatorVersion.Canary);

            // Hiding the uninstall button and showing install button again
            InstallXeniaCanary.Visibility = Visibility.Visible;
            UninstallXeniaCanary.Visibility = Visibility.Collapsed;

            MessageBox.Show("Xenia Canary has been uninstalled.");
        }

        /// <summary>
        /// Download and setup Xenia Netplay
        /// </summary>
        private async void InstallXeniaNetplay_Click(object sender, RoutedEventArgs e)
        {
            // Grab the URL to the latest Xenia Netplay release
            string url = await InstallationManager.DownloadLinkGrabber("https://api.github.com/repos/AdrianCassar/xenia-canary/releases");
            if (url == null)
            {
                Log.Information("No URL has been found");
                return;
            }
            Mouse.OverrideCursor = Cursors.Wait;

            // Download and extract the build
            DownloadManager.ProgressChanged += (progress) =>
            {
                Progress.Value = progress;
            };
            Log.Information("Downloading the latest Xenia Netplay build");
            await DownloadManager.DownloadAndExtractAsync(url, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Downloads\xenia.zip"), Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Emulators\Xenia Netplay\"));

            // Download "gamecontrollerdb.txt" for SDL Input System
            Log.Information("Downloading gamecontrollerdb.txt for SDL Input System");
            await DownloadManager.DownloadFileAsync("https://raw.githubusercontent.com/mdqinc/SDL_GameControllerDB/master/gamecontrollerdb.txt", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Emulators\Xenia Netplay\gamecontrollerdb.txt"));

            // Running Xenia Canary setup
            Log.Information("Running Xenia Netplay setup");
            MessageBox.Show("In the following window please create the profile for Xenia (if needed) and close the emulator.");
            InstallationManager.Xenia.NetplaySetup();
            Log.Information("Xenia Netplay installed");

            // Hiding the install button and showing the uninstall button again
            InstallXeniaNetplay.Visibility = Visibility.Collapsed;
            UninstallXeniaNetplay.Visibility = Visibility.Visible;

            Mouse.OverrideCursor = null;
            MessageBox.Show("Xenia Netplay installed.");
        }

        /// <summary>
        /// Uninstalls Xenia Netplay
        /// </summary>
        private void UninstallXeniaNetplay_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Do you want to uninstall Xenia Netplay?\nThis will remove all save files and updates alongside the emulator.", "Confirmation", MessageBoxButton.YesNo);
            if (result != MessageBoxResult.Yes)
            {
                return;
            }
            InstallationManager.Xenia.Uninstall(EmulatorVersion.Netplay);

            // Hiding the uninstall button and showing install button again
            InstallXeniaNetplay.Visibility = Visibility.Visible;
            UninstallXeniaNetplay.Visibility = Visibility.Collapsed;

            MessageBox.Show("Xenia Netplay has been uninstalled.");
        }
    }
}
