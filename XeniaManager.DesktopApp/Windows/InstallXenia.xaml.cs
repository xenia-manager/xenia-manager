using System;
using System.Windows;
using System.Windows.Input;

// Imported
using Serilog;
using XeniaManager.DesktopApp.Utilities.Animations;

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
            }

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
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            WindowAnimations.OpeningAnimation(this); // Run "Fade-In" animation
            CheckForInstalledXenia();
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
    }
}
