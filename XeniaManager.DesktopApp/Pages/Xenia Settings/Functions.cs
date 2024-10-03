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

        // Functions for loading Settings into the UI
        /// <summary>
        /// Loads the Audio Settings into the UI
        /// </summary>
        /// <param name="sectionTable">Portion of .toml file dedicated to Audio Settings</param>
        private void LoadAudioSettings(TomlTable sectionTable)
        {
            // "apu" setting
            Log.Information($"apu - {sectionTable["apu"].ToString()}");
            foreach (var item in cmbAudioSystem.Items)
            {
                if (item is ComboBoxItem comboBoxItem && comboBoxItem.Content.ToString() == sectionTable["apu"].ToString())
                {
                    cmbAudioSystem.SelectedItem = comboBoxItem;
                    continue;
                }
            }

            // "apu_max_queued_frames" setting
            if (sectionTable.ContainsKey("apu_max_queued_frames"))
            {
                Log.Information($"apu_max_queued_frames - {sectionTable["apu_max_queued_frames"].ToString()}");
                txtAudioMaxQueuedFrames.Text = sectionTable["apu_max_queued_frames"].ToString();
                txtAudioMaxQueuedFrames.Visibility = Visibility.Visible;
            }
            else
            {
                // Disable the option because it's not in the configuration file
                txtAudioMaxQueuedFrames.Visibility = Visibility.Collapsed;
            }

            // "mute" setting
            Log.Information($"mute - {(bool)sectionTable["mute"]}");
            chkMute.IsChecked = (bool)sectionTable["mute"];

            // "use_dedicated_xma_thread" setting
            if (sectionTable.ContainsKey("use_dedicated_xma_thread"))
            {
                Log.Information($"use_dedicated_xma_thread - {(bool)sectionTable["use_dedicated_xma_thread"]}");
                chkDedicatedXMAThread.IsChecked = (bool)sectionTable["use_dedicated_xma_thread"];
                chkDedicatedXMAThread.Visibility = Visibility.Visible;
            }
            else
            {
                // Disable the option because it's not in the configuration file
                chkDedicatedXMAThread.Visibility = Visibility.Collapsed;
            }

            // "use_new_decoder" setting
            if (sectionTable.ContainsKey("use_new_decoder"))
            {
                Log.Information($"use_new_decoder - {(bool)sectionTable["use_new_decoder"]}");
                chkXmaAudioDecoder.IsChecked = (bool)sectionTable["use_new_decoder"];
                chkXmaAudioDecoder.Visibility = Visibility.Visible;
            }
            else
            {
                // Disable the option because it's not in the configuration file
                chkXmaAudioDecoder.Visibility = Visibility.Collapsed;
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
                        case "CPU":
                            Log.Information("CPU");
                            break;
                        case "Content":
                            Log.Information("Content");
                            break;
                        case "D3D12":
                            Log.Information("D3D12");
                            break;
                        case "Display":
                            Log.Information("Display");
                            break;
                        case "GPU":
                            Log.Information("GPU");
                            break;
                        case "General":
                            Log.Information("General");
                            break;
                        case "HID":
                            Log.Information("HID");
                            break;
                        case "Kernel":
                            Log.Information("Kernel");
                            break;
                        case "Live":
                            Log.Information("Live");
                            break;
                        case "Memory":
                            Log.Information("Memory");
                            break;
                        case "Storage":
                            Log.Information("Storage");
                            break;
                        case "UI":
                            Log.Information("UI");
                            break;
                        case "User":
                            Log.Information("User");
                            break;
                        case "Video":
                            Log.Information("Video");
                            break;
                        case "Vulkan":
                            Log.Information("Vulkan");
                            break;
                        case "XConfig":
                            Log.Information("XConfig");
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
