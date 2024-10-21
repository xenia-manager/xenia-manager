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

        /// <summary>
        /// What happens when Exit button is pressed
        /// </summary>
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            // Run "Fade-Out" animation and then close the window
            WindowAnimations.ClosingAnimation(this);
        }

        /// <summary>
        /// Download and setup Xenia Canary
        /// </summary>
        private async void InstallXeniaCanary_Click(object sender, RoutedEventArgs e)
        {
            // Grab the URL to the latest Xenia Canary release
            string url = await InstallationManager.DownloadLinkGrabber("https://api.github.com/repos/xenia-canary/xenia-canary/releases", 1, 0, null);
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
            string url = await InstallationManager.DownloadLinkGrabber("https://api.github.com/repos/AdrianCassar/xenia-canary/releases", 0, 0, null);
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
            /*
            InstallXeniaNetplay.Visibility = Visibility.Collapsed;
            UninstallXeniaNetplay.Visibility = Visibility.Visible;*/

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
            /*
            InstallXeniaNetplay.Visibility = Visibility.Visible;
            UninstallXeniaNetplay.Visibility = Visibility.Collapsed;*/

            MessageBox.Show("Xenia Netplay has been uninstalled.");
        }

        /// <summary>
        /// Download and setup Xenia Mousehook
        /// </summary>
        private async void InstallXeniaMousehook_Click(object sender, RoutedEventArgs e)
        {
            // Grab the URL to the latest Xenia Canary release
            string url = await InstallationManager.DownloadLinkGrabber("https://api.github.com/repos/marinesciencedude/xenia-canary-mousehook/releases", 1, 0, "mousehook");
            if (url == null)
            {
                Log.Warning("No URL has been found");
                MessageBox.Show("No releases found");
                return;
            }

            Mouse.OverrideCursor = Cursors.Wait;

            // Download and extract the build
            DownloadManager.ProgressChanged += (progress) =>
            {
                Progress.Value = progress;
            };
            Log.Information("Downloading the latest Xenia Canary build");
            await DownloadManager.DownloadAndExtractAsync(url, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Downloads\xenia.zip"), Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Emulators\Xenia Mousehook\"));
            
            // Download "gamecontrollerdb.txt" for SDL Input System
            Log.Information("Downloading gamecontrollerdb.txt for SDL Input System");
            await DownloadManager.DownloadFileAsync("https://raw.githubusercontent.com/mdqinc/SDL_GameControllerDB/master/gamecontrollerdb.txt", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Emulators\Xenia Mousehook\gamecontrollerdb.txt"));
            
            // Running Xenia Mousehook setup
            Log.Information("Running Xenia Mousehook setup");
            MessageBox.Show("In the following window please create the profile for Xenia (if needed) and close the emulator.");
            InstallationManager.Xenia.MousehookSetup();
            Log.Information("Xenia Mousehook installed");

            // Hiding the install button and showing the uninstall button again
            InstallXeniaMousehook.Visibility = Visibility.Collapsed;
            UninstallXeniaMousehook.Visibility = Visibility.Visible;

            Mouse.OverrideCursor = null;
            MessageBox.Show("Xenia Mousehook installed.");
        }
    }
}
