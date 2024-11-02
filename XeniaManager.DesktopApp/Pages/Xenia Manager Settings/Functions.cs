using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

// Imported
using Serilog;

namespace XeniaManager.DesktopApp.Pages
{
    public partial class Settings : Page
    {
        /// <summary>
        /// Loads the selected theme from the configuration file
        /// </summary>
        private void LoadSelectedTheme()
        {
            // Load the selected theme into the ui
            foreach (ComboBoxItem theme in ThemeSelector.Items)
            {
                if (theme.Content.ToString() == ConfigurationManager.AppConfig.SelectedTheme)
                {
                    ThemeSelector.SelectedItem = theme;
                    break;
                }
            }
        }

        /// <summary>
        /// Main function that loads the configuration file
        /// </summary>
        private void LoadConfigurationFile()
        {
            Log.Information($"Selected theme: {ConfigurationManager.AppConfig.SelectedTheme}");
            LoadSelectedTheme(); // Load the selected theme
            Log.Information(
                $"Automatic detection and adding of games: {ConfigurationManager.AppConfig.AutoGameAdding}");
            chkAutoDetectAndAddGames.IsChecked = ConfigurationManager.AppConfig.AutoGameAdding;

            // Showing currently installed Xenia versions
            Dictionary<string, (TextBlock Control, EmulatorInfo Version)> xeniaVersions = new Dictionary<string, (TextBlock Control, EmulatorInfo Version)>
            {
                ["Xenia Canary"] = (txtXeniaCanaryInstalledVersion, ConfigurationManager.AppConfig.XeniaCanary),
                ["Xenia Mousehook"] = (txtXeniaMousehookInstalledVersion, ConfigurationManager.AppConfig.XeniaMousehook),
                ["Xenia Netplay"] = (txtXeniaNetplayInstalledVersion, ConfigurationManager.AppConfig.XeniaNetplay)
            };

            foreach (var (name, (control, version)) in xeniaVersions)
            {
                control.Text = version != null 
                    ? $"{name}: {version.Version}"
                    : $"{name}: Not installed";
            }
        }
    }
}