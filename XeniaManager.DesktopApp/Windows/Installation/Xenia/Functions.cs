using System;
using System.Windows;

// Imported
using Serilog;

namespace XeniaManager.DesktopApp.Windows
{
    /// <summary>
    /// Interaction logic for InstallXenia.xaml
    /// </summary>
    public partial class InstallXenia : Window
    {
        /// <summary>
        /// Checks all supported Xenia emulators if they're installed and shows install/uninstall option in this window
        /// </summary>
        private void CheckForInstalledXenia()
        {
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
            /*
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
            }*/

            // Checking if Xenia Mousehook is installed
            Log.Information("Checking if Xenia Mousehook is installed");
            if (ConfigurationManager.AppConfig.XeniaMousehook != null)
            {
                // If it's installed, show uninstall button and hide install button
                Log.Information("Xenia Mousehook is installed");
                Log.Information("Showing 'Uninstall Xenia Mousehook' button");
                InstallXeniaMousehook.Visibility = Visibility.Collapsed;
                UninstallXeniaMousehook.Visibility = Visibility.Visible;
            }
            else
            {
                // If it's not installed, show install button and hide uninstall button
                Log.Information("Xenia Mousehook is not installed");
                Log.Information("Showing 'Install Xenia Mousehook' button");
                InstallXeniaMousehook.Visibility = Visibility.Visible;
                UninstallXeniaMousehook.Visibility = Visibility.Collapsed;
            }
        }
    }
}
