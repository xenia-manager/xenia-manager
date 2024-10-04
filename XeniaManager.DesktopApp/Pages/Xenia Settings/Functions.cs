using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;

// Imported
using Serilog;
using Tomlyn.Model;
using Tomlyn;

namespace XeniaManager.DesktopApp.Pages
{
    public partial class XeniaSettings : Page
    {
        /// <summary>
        /// Loads all of the installed games into the ComboBox
        /// </summary>
        private void LoadInstalledGames()
        {
            Log.Information("Loading games into the ComboBox");
            cmbConfigurationFiles.Items.Clear(); // Clear the ComboBox
            foreach (Game game in GameManager.Games)
            {
                if (game.FileLocations.ConfigFilePath != null)
                {
                    cmbConfigurationFiles.Items.Add(game.Title);
                }
            }
        }

        /// <summary>
        /// Method to populate the ComboBox with country names
        /// </summary>
        private void LoadCountryComboBox()
        {
            var sortedCountries = countryIDMap.OrderBy(c => c.Value).ToList();

            foreach (KeyValuePair<int, string> country in sortedCountries)
            {
                cmbUserCountry.Items.Add(country.Value);
            }
        }

        /// <summary>
        /// Read the .toml file of the emulator
        /// </summary>
        /// <param name="configurationLocation">Location to the configuration file</param>
        private void ReadConfigFile(string configurationLocation)
        {
            // Checking if the file exists before reading it
            if (!File.Exists(configurationLocation))
            {
                return;
            }
            try
            {
                string configText = File.ReadAllText(configurationLocation);
                TomlTable configFile = Toml.Parse(configText).ToModel(); // Convert string to TomlTable
                foreach (var section in configFile)
                {
                    TomlTable sectionTable = section.Value as TomlTable;
                    if (sectionTable == null)
                    {
                        continue;
                    };
                    switch (section.Key)
                    {
                        case "APU":
                            Log.Information("APU");
                            LoadAudioSettings(sectionTable);
                            break;
                        case "Content":
                            Log.Information("Content");
                            LoadContentSettings(sectionTable);
                            break;
                        case "CPU":
                            Log.Information("CPU");
                            LoadCPUSettings(sectionTable);
                            break;
                        case "D3D12":
                            Log.Information("D3D12");
                            LoadD3D12Settings(sectionTable);
                            break;
                        case "Display":
                            Log.Information("Display");
                            LoadDisplaySettings(sectionTable);
                            break;
                        case "General":
                            Log.Information("General");
                            LoadGeneralSettings(sectionTable);
                            break;
                        case "GPU":
                            Log.Information("GPU");
                            LoadGPUSettings(sectionTable);
                            break;
                        case "HID":
                            Log.Information("HID");
                            LoadHIDSettings(sectionTable);
                            break;
                        case "Kernel":
                            Log.Information("Kernel");
                            LoadKernelSettings(sectionTable);
                            break;
                        case "Live":
                            Log.Information("Live");
                            LoadLiveSettings(sectionTable);
                            break;
                        case "Memory":
                            Log.Information("Memory");
                            LoadMemorySettings(sectionTable);
                            break;
                        case "Storage":
                            Log.Information("Storage");
                            LoadStorageSettings(sectionTable);
                            break;
                        case "UI":
                            Log.Information("UI");
                            LoadUISettings(sectionTable);
                            break;
                        case "User":
                            Log.Information("User");
                            LoadUserSettings(sectionTable);
                            break;
                        case "Video":
                            Log.Information("Video");
                            LoadVideoSettings(sectionTable);
                            break;
                        case "Vulkan":
                            Log.Information("Vulkan");
                            LoadVulkanSettings(sectionTable);
                            break;
                        case "XConfig":
                            Log.Information("XConfig");
                            LoadXConfigSettings(sectionTable);
                            break;
                        default:
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
            }
        }

        // SearchBox functions
        /// <summary>
        /// Filters individual settings categories
        /// </summary>
        /// <returns>true if there are any settings in the category that are visible, otherwise false</returns>
        private bool FilterSettings(StackPanel settingsPanel, string searchQuery)
        {
            bool isAnySettingVisible = false;
            // Go through every child
            foreach (var child in settingsPanel.Children)
            {
                if (child is Border border)
                {
                    if (border.Child is Grid grid && grid.Children.Count > 0)
                    {
                        TextBlock txtSetting = null;
                        foreach (var gridChild in grid.Children)
                        {
                            if (gridChild is TextBlock tb)
                            {
                                txtSetting = tb;
                                break;
                            }
                        }

                        if (txtSetting != null)
                        {
                            string settingName = txtSetting.Text.ToLower();
                            if (settingName.Contains(searchQuery) || string.IsNullOrEmpty(searchQuery))
                            {
                                border.Visibility = Visibility.Visible;
                                isAnySettingVisible = true;  // At least one setting is visible
                            }
                            else
                            {
                                border.Visibility = Visibility.Collapsed;
                            }
                        }
                    }
                }
            }
            return isAnySettingVisible;
        }
    }
}
