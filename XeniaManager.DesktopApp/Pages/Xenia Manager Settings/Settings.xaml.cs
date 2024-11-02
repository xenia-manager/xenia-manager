using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

// Imported
using Serilog;
using XeniaManager.DesktopApp.Windows;
using XeniaManager.Downloader;

namespace XeniaManager.DesktopApp.Pages
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : Page
    {
        // UI Interactions
        /// <summary>
        /// Load selected values into the UI
        /// </summary>
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadConfigurationFile(); // Load the configuration file into the UI
        }

        /// <summary>
        /// Opens InstallXenia window where the user can install or uninstall different Xenia versions
        /// </summary>
        private void btnXeniaInstaller_Click(object sender, RoutedEventArgs e)
        {
            InstallXenia installXenia = new InstallXenia();
            installXenia.ShowDialog();
        }

        /// <summary>
        /// Loads the selected theme
        /// </summary>
        private void ThemeSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Check if the selection is valid and if it's the different theme than the one selected
            ComboBoxItem item = ThemeSelector.SelectedItem as ComboBoxItem;
            if (item == null || item.Content.ToString() == ConfigurationManager.AppConfig.SelectedTheme)
            {
                return;
            }

            // Switch the theme in the configuration
            switch (ThemeSelector.SelectedIndex)
            {
                case 1:
                    ConfigurationManager.AppConfig.SelectedTheme = "Light";
                    break;
                case 2:
                    ConfigurationManager.AppConfig.SelectedTheme = "Dark";
                    break;
                case 3:
                    ConfigurationManager.AppConfig.SelectedTheme = "AMOLED";
                    break;
                case 4:
                    ConfigurationManager.AppConfig.SelectedTheme = "Nord";
                    break;
                default:
                    ConfigurationManager.AppConfig.SelectedTheme = "System Default";
                    break;
            }

            // Load the theme and save changes to the file
            App.LoadTheme();
            ConfigurationManager.SaveConfigurationFile();
        }

        /// <summary>
        /// Executes the code whenever the checkbox has been clicked
        /// </summary>
        private void chkAutoDetectAndAddGames_Click(object sender, RoutedEventArgs e)
        {
            Log.Information($"Automatic detection and adding of games: {chkAutoDetectAndAddGames.IsChecked}");
            ConfigurationManager.AppConfig.AutoGameAdding = chkAutoDetectAndAddGames.IsChecked;
            ConfigurationManager.SaveConfigurationFile(); // Save changes to the file
        }

        /// <summary>
        /// Resets the bindings.ini file by downloading a fresh one from the internet
        /// </summary>
        private async void BtnResetMousehookBindings_Click(object sender, RoutedEventArgs e)
        {
            // Checking if the Mousehook build is installed
            if (ConfigurationManager.AppConfig.XeniaMousehook == null)
            {
                MessageBox.Show("Mousehook build is not installed.");
                return;
            }
            // Download "bindings.ini"
            Log.Information("Downloading fresh bindings.ini from the repository");
            Mouse.OverrideCursor = Cursors.Wait;
            await DownloadManager.DownloadFileAsync(
                "https://raw.githubusercontent.com/marinesciencedude/xenia-canary-mousehook/mousehook/bindings.ini",
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                    ConfigurationManager.AppConfig.XeniaMousehook.EmulatorLocation, "bindings.ini"));
            Mouse.OverrideCursor = null;
            MessageBox.Show("Mousehook bindings have been reset.");
        }
    }
}