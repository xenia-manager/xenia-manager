using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

// Imported
using Serilog;
using XeniaManager.DesktopApp.Windows;
using XeniaManager.Downloader;
using XeniaManager.Installation;

namespace XeniaManager.DesktopApp.Pages
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings
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
        private void BtnXeniaInstaller_Click(object sender, RoutedEventArgs e)
        {
            InstallXenia installXenia = new InstallXenia();
            installXenia.ShowDialog();
        }

        /// <summary>
        /// Loads the selected theme
        /// </summary>
        private void CmbThemeSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Check if the selection is valid and if it's the different theme than the one selected
            ComboBoxItem item = CmbThemes.SelectedItem as ComboBoxItem;
            if (item == null || item.Content.ToString() == ConfigurationManager.AppConfig.SelectedTheme)
            {
                return;
            }

            // Switch the theme in the configuration
            switch (CmbThemes.SelectedIndex)
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
        private void ChkAutoDetectAndAddGames_Click(object sender, RoutedEventArgs e)
        {
            Log.Information($"Automatic detection and adding of games: {ChkAutoDetectAndSelectionGames.IsChecked}");
            ConfigurationManager.AppConfig.AutoGameSelection = ChkAutoDetectAndSelectionGames.IsChecked;
            ConfigurationManager.SaveConfigurationFile(); // Save changes to the file
        }

        /// <summary>
        /// Executes the code whenever the checkbox has been clicked
        /// </summary>
        private void ChkCompatibilityIcons_Click(object sender, RoutedEventArgs e)
        {
            Log.Information($"Compatibility icons: {ChkCompatibilityIcons.IsChecked}");
            ConfigurationManager.AppConfig.CompatibilityIcons = ChkCompatibilityIcons.IsChecked;
            ConfigurationManager.SaveConfigurationFile(); // Save changes to the file
        }

        /// <summary>
        /// Enables/disables the "Automatic save backup" feature
        /// </summary>
        private void ChkAutomaticSaveBackup_Click(object sender, RoutedEventArgs e)
        {
            Log.Information($"Automatic detection and adding of games: {ChkAutomaticSaveBackup.IsChecked}");
            ConfigurationManager.AppConfig.AutomaticSaveBackup = ChkAutomaticSaveBackup.IsChecked;
            BrdProfileSlotSelector.Visibility = ConfigurationManager.AppConfig.AutomaticSaveBackup == true
                ? Visibility.Visible
                : Visibility.Collapsed;

            ConfigurationManager.SaveConfigurationFile(); // Save changes to the file
        }

        /// <summary>
        /// Detects the change in selected profile slot to do save backups
        /// </summary>
        private void CmbProfileSlot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cmb = sender as ComboBox;
            if (cmb == null)
            {
                return;
            }

            ConfigurationManager.AppConfig.ProfileSlot = cmb.SelectedIndex + 1;
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

        /// <summary>
        /// Allows the user to reset the configuration file for Xenia
        /// </summary>
        private async void BtnResetXeniaConfiguration_Click(object sender, RoutedEventArgs e)
        {
            // Checking for currently installed Xenia versions
            Log.Information("Checking for existing Xenia installations");
            List<EmulatorVersion> xeniaInstallations = new[]
                {
                    (ConfigurationManager.AppConfig.XeniaCanary, EmulatorVersion.Canary),
                    (ConfigurationManager.AppConfig.XeniaMousehook, EmulatorVersion.Mousehook),
                    (ConfigurationManager.AppConfig.XeniaNetplay, EmulatorVersion.Netplay)
                }
                .Where(x => x.Item1 != null)
                .Select(x => x.Item2)
                .ToList();

            switch (xeniaInstallations.Count)
            {
                case 0:
                    // If there are no Xenia versions installed, don't do anything
                    Log.Information("No Xenia installations detected");
                    MessageBox.Show("No Xenia installations detected");
                    break;
                case 1:
                    // If there is only 1 version of Xenia installed, reset configuration file of that Xenia Version
                    Log.Information($"Only Xenia {xeniaInstallations[0]} is installed");
                    await InstallationManager.ResetConfigFile(xeniaInstallations[0]);
                    MessageBox.Show($"Configuration file for Xenia {xeniaInstallations[0]} has been reset to default.");
                    break;
                default:
                    // If there are 2 or more versions of Xenia installed, ask the user which configuration file he wants to reset
                    Log.Information("Detected multiple Xenia installations");
                    Log.Information("Asking user what Xenia version's configuration file should be reset");
                    XeniaSelection xs = new XeniaSelection();
                    xs.ShowDialog();
                    Log.Information($"User selected Xenia {xs.UserSelection}");
                    await InstallationManager.ResetConfigFile(xs.UserSelection);
                    MessageBox.Show($"Configuration file for Xenia {xs.UserSelection} has been reset to default.");
                    break;
            }
        }
    }
}