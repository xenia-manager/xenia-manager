﻿using System.Windows;
using System.Windows.Controls;

// Imported
using Serilog;

namespace XeniaManager.DesktopApp.Pages
{
    public partial class Settings
    {
        /// <summary>
        /// Loads the selected theme from the configuration file
        /// </summary>
        private void LoadSelectedTheme()
        {
            // Load the selected theme into the ui
            foreach (ComboBoxItem theme in CmbThemes.Items)
            {
                if (theme.Content.ToString() == ConfigurationManager.AppConfig.SelectedTheme)
                {
                    CmbThemes.SelectedItem = theme;
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
                $"Automatic parsing and adding of games: {ConfigurationManager.AppConfig.AutomaticGameParsingSelection}");
            ChkAutoDetectAndSelectionGames.IsChecked = ConfigurationManager.AppConfig.AutomaticGameParsingSelection;
            Log.Information(
                $"Enable compatibility icons: {ConfigurationManager.AppConfig.AutomaticGameParsingSelection}");
            ChkCompatibilityIcons.IsChecked = ConfigurationManager.AppConfig.CompatibilityIcons;
            Log.Information(
                $"Enable automatic save backup: {ConfigurationManager.AppConfig.AutomaticGameParsingSelection}");
            ChkAutomaticSaveBackup.IsChecked = ConfigurationManager.AppConfig.AutomaticSaveBackup;
            if (ConfigurationManager.AppConfig.AutomaticSaveBackup == false)
            {
                BrdProfileSlotSelector.Visibility = Visibility.Collapsed;
            }

            foreach (ComboBoxItem cmbItem in CmbProfileSlot.Items)
            {
                if (int.Parse(cmbItem.Content.ToString()) == ConfigurationManager.AppConfig.ProfileSlot)
                {
                    CmbProfileSlot.SelectedItem = cmbItem;
                    break;
                }
            }

            // Showing currently installed Xenia versions
            Dictionary<string, (TextBlock Control, EmulatorInfo Version)> xeniaVersions =
                new Dictionary<string, (TextBlock Control, EmulatorInfo Version)>
                {
                    ["Xenia Canary"] = (TblkXeniaCanaryInstalledVersion, ConfigurationManager.AppConfig.XeniaCanary),
                    ["Xenia Mousehook"] = (TblkXeniaMousehookInstalledVersion,
                        ConfigurationManager.AppConfig.XeniaMousehook),
                    ["Xenia Netplay"] = (TblkXeniaNetplayInstalledVersion, ConfigurationManager.AppConfig.XeniaNetplay)
                };

            foreach (var (name, (control, version)) in xeniaVersions)
            {
                if (name != "Xenia Netplay")
                {
                    control.Text = version != null
                        ? $"{name}: {version.Version}"
                        : $"{name}: Not installed";
                }
                else
                {
                    if (version != null)
                    {
                        control.Text = version.NightlyVersion != null
                            ? $"{name}: {version.NightlyVersion}"
                            : $"{name}: {version.Version}";
                    }
                }
            }
        }
    }
}