using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

// Imported
using Newtonsoft.Json.Linq;
using Serilog;
using Tomlyn.Model;
using Tomlyn;

namespace XeniaManager.DesktopApp.Pages
{
    public partial class XeniaSettings
    {
        /// <summary>
        /// Loads all the installed games into the ComboBox
        /// </summary>
        private void LoadInstalledGames()
        {
            Log.Information("Loading games into the ComboBox");
            CmbConfigurationFiles.Items.Clear(); // Clear the ComboBox
            foreach (Game game in GameManager.Games.OrderBy(game => game.Title))
            {
                if (game.FileLocations.ConfigFilePath != null)
                {
                    CmbConfigurationFiles.Items.Add(game.Title);
                }
            }
            
            if (ConfigurationManager.AppConfig.XeniaCanary != null)
            {
                CmbConfigurationFiles.Items.Add("Default Xenia Canary");
            }
            if (ConfigurationManager.AppConfig.XeniaMousehook != null)
            {
                CmbConfigurationFiles.Items.Add("Default Xenia Mousehook");
            }
            if (ConfigurationManager.AppConfig.XeniaNetplay != null)
            {
                CmbConfigurationFiles.Items.Add("Default Xenia Netplay");
            }
        }

        /// <summary>
        /// Method to populate the ComboBox with country names
        /// </summary>
        private void LoadCountryComboBox()
        {
            var sortedCountries = countryIdMap.OrderBy(c => c.Value).ToList();

            foreach (KeyValuePair<int, string> country in sortedCountries)
            {
                CmbUserCountry.Items.Add(country.Value);
            }
        }

        /// <summary>
        /// Hides all the settings that aren't in all versions of Xenia
        /// </summary>
        private void HideNonUniversalSettings()
        {
            // Hiding Mousehook settings
            SpMousehookSettings.Visibility = Visibility.Collapsed;
            SpMousehookSettings.Tag = "Ignore";

            // Hiding Netplay settings
            SpNetplaySettings.Visibility = Visibility.Collapsed;
            SpNetplaySettings.Tag = "Ignore";

            // Hiding NVIDIA settings
            SpNvidiaDriverSettings.Visibility = Visibility.Collapsed;
            SpNvidiaDriverSettings.Tag = "Ignore";
        }

        /// <summary>
        /// Read the .toml file of the emulator
        /// </summary>
        /// <param name="configurationLocation">Location to the configuration file</param>
        private void ReadConfigFile(string configurationLocation, bool readConfigFile = true)
        {
            // Checking if the file exists before reading it
            if (!File.Exists(configurationLocation))
            {
                return;
            }

            try
            {
                // Check if it's needed to read the configuration file
                if (readConfigFile)
                {
                    string configText = File.ReadAllText(configurationLocation);
                    CurrentConfigFile = Toml.Parse(configText).ToModel(); // Convert string to TomlTable
                }

                // Create a dictionary mapping section names to their respective loading methods
                Dictionary<string, Action<TomlTable>> sectionLoaders = new Dictionary<string, Action<TomlTable>>()
                {
                    { "APU", LoadAudioSettings },
                    { "Content", LoadContentSettings },
                    { "CPU", LoadCpuSettings },
                    { "D3D12", LoadD3D12Settings },
                    { "Display", LoadDisplaySettings },
                    { "General", LoadGeneralSettings },
                    { "GPU", LoadGpuSettings },
                    { "HID", LoadHidSettings },
                    { "Kernel", LoadKernelSettings },
                    { "Live", LoadLiveSettings },
                    { "Memory", LoadMemorySettings },
                    { "MouseHook", LoadMousehookSettings },
                    { "Storage", LoadStorageSettings },
                    { "UI", LoadUiSettings },
                    { "User", LoadUserSettings },
                    { "Video", LoadVideoSettings },
                    { "Vulkan", LoadVulkanSettings },
                    { "XConfig", LoadXConfigSettings }
                };

                // Going through every section in the configuration file
                foreach (var section in CurrentConfigFile)
                {
                    // Checking if the section is supported
                    if (section.Value is TomlTable sectionTable &&
                        sectionLoaders.TryGetValue(section.Key, out var loader))
                    {
                        // If the section is supported, read it into the UI
                        Log.Information($"Section: {section.Key}");
                        loader(sectionTable);
                    }
                    else
                    {
                        // If it's not supported, just write the name of it is in the log file and move on
                        Log.Warning($"Unknown section '{section.Key}' in the configuration file");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// Function that tries to load NvAPI and if it's successful, shows the NVIDIA Driver specific settings
        /// </summary>
        private void LoadNvidiaDriverSettings()
        {
            if (!NVAPI.Initialize())
            {
                Log.Error("No NVIDIA GPU Found");
                return;
            }

            List<string> gpus = NVAPI.GetGPUInfo();
            if (gpus == null)
            {
                Log.Error("No NVIDIA GPU Found");
                return;
            }

            // Unload NvAPI at the end from the memory
            NVAPI.UnloadNvApiLibrary();
        }

        // Optimized Settings
        /// <summary>
        /// Function that converts JToken into a proper type
        /// </summary>
        /// <param name="token"></param>
        private object ConvertJToken(JToken token)
        {
            // Handle different JToken types
            return token.Type switch
            {
                JTokenType.String => token.ToString(),
                JTokenType.Integer => (int)token,
                JTokenType.Float => (float)token,
                JTokenType.Boolean => (bool)token,
                JTokenType.Null => null,
                _ => token.ToString(),
            };
        }

        /// <summary>
        /// Applies optimized settings to the loaded .TOML file
        /// </summary>
        /// <param name="optimizedSettings">Optimized settings as JToken</param>
        private string OptimizeSettings(JToken optimizedSettings)
        {
            Log.Information("Applying optimized settings");
            string changedSettings = string.Empty;
            foreach (var section in optimizedSettings.Children<JProperty>())
            {
                // Check if the section exists in the TOML
                if (CurrentConfigFile.TryGetValue(section.Name, out object value))
                {
                    if (value is TomlTable tomlSection)
                    {
                        foreach (var property in section.Value.Children<JProperty>())
                        {
                            if (tomlSection.ContainsKey(property.Name))
                            {
                                Log.Information($"{property.Name} - {property.Value}");
                                changedSettings += $"{property.Name} = {property.Value}\n";
                                tomlSection[property.Name] = ConvertJToken(property.Value);
                            }
                            else
                            {
                                Log.Warning($"Setting '{property.Name}' not found in section '{section.Name}'");
                            }
                        }
                    }
                }
                else
                {
                    Log.Warning($"{section.Name} is not found in this configuration file");
                }
            }
            
            return changedSettings;
        }

        /// <summary>
        /// Saves all the changes to the existing configuration file
        /// </summary>
        /// <param name="configurationLocation">Location of the configuration file</param>
        private void SaveChanges(string configurationLocation)
        {
            try
            {
                Log.Information("Saving changes");
                CurrentConfigFile ??= Toml.Parse(File.ReadAllText(configurationLocation)).ToModel();

                // Create a dictionary mapping section names to their respective loading methods
                Dictionary<string, Action<TomlTable>> sectionHandler = new Dictionary<string, Action<TomlTable>>()
                {
                    { "APU", SaveAudioSettings },
                    { "Content", SaveContentSettings },
                    { "CPU", SaveCpuSettings },
                    { "D3D12", SaveD3D12Settings },
                    { "Display", SaveDisplaySettings },
                    { "General", SaveGeneralSettings },
                    { "GPU", SaveGpuSettings },
                    { "HID", SaveHidSettings },
                    { "Kernel", SaveKernelSettings },
                    { "Live", SaveLiveSettings },
                    { "Memory", SaveMemorySettings },
                    { "MouseHook", SaveMousehookSettings },
                    { "Storage", SaveStorageSettings },
                    { "UI", SaveUiSettings },
                    { "User", SaveUserSettings },
                    { "Video", SaveVideoSettings },
                    { "Vulkan", SaveVulkanSettings },
                    { "XConfig", SaveXConfigSettings }
                };

                // Going through every section in the configuration file
                foreach (var section in CurrentConfigFile)
                {
                    // Checking if the section is supported
                    if (section.Value is TomlTable sectionTable &&
                        sectionHandler.TryGetValue(section.Key, out var handler))
                    {
                        // If the section is supported, save the changes
                        Log.Information($"Section: {section.Key}");
                        handler(sectionTable);
                    }
                    else
                    {
                        // If it's not supported, just write the name of it is in the log file and move on
                        Log.Warning($"Unknown section '{section.Key}' in the configuration file");
                    }
                }

                // Save the changes into the file
                File.WriteAllText(configurationLocation, Toml.FromModel(CurrentConfigFile));
                Log.Information("Changes have been saved");
                MessageBox.Show("Settings have been saved");
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
            }
        }

        // SearchBar functions
        /// <summary>
        /// Extracts all text content from a TextBlock, including text separated by LineBreaks.
        /// </summary>
        /// <param name="textBlock">The TextBlock to extract text from.</param>
        /// <returns>A string containing all the text in the TextBlock.</returns>
        private string ParseTextFromSettingName(TextBlock textBlock)
        {
            StringBuilder textContent = new StringBuilder();

            foreach (var inline in textBlock.Inlines)
            {
                if (inline is Run run)
                {
                    textContent.Append(run.Text);
                }
                else if (inline is LineBreak)
                {
                    textContent.Append(" "); // Convert LineBreak
                }
            }

            return textContent.ToString(); // Return parsed text
        }

        /// <summary>
        /// Filters individual settings categories
        /// </summary>
        /// <returns>true if there are any settings in the category that are visible, otherwise false</returns>
        private bool FilterSettings(StackPanel settingsPanel, string searchQuery)
        {
            bool isAnySettingVisible = false; // Check if there are any nested settings that fit the search query
            if (settingsPanel.Tag != null && settingsPanel.Tag.ToString() == "Ignore")
            {
                return false; // Don't filter this category
            }

            // Go through every child of the panel
            foreach (var child in settingsPanel.Children)
            {
                // Check if the child is a setting (Border element)
                if (child is Border border)
                {
                    if (border.Tag == "Ignore")
                    {
                        continue;
                    }
                    if (border.Child is Grid { Children.Count: > 0 } grid)
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
                            string settingName = ParseTextFromSettingName(txtSetting).ToLower();
                            if ((settingName.Contains(searchQuery) || string.IsNullOrEmpty(searchQuery)))
                            {
                                border.Visibility = Visibility.Visible;
                                isAnySettingVisible = true; // At least one setting is visible
                            }
                            else
                            {
                                border.Visibility = Visibility.Collapsed;
                            }
                        }
                    }
                }
                // Check if the child is a group of settings (another StackPanel)
                else if (child is StackPanel settingsGroup)
                {
                    // Recursively filter nested groups
                    bool nestedChild =
                        FilterSettings(settingsGroup,
                            searchQuery); // Recursively call this function to filter out stuff
                    if (nestedChild)
                    {
                        isAnySettingVisible = true;
                    }
                }
            }

            return isAnySettingVisible;
        }
    }
}