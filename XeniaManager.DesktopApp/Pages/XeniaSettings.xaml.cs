using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;


// Imported
using Serilog;
using Tomlyn;
using Tomlyn.Model;
using XeniaManager.DesktopApp.CustomControls;

namespace XeniaManager.DesktopApp.Pages
{
    /// <summary>
    /// Interaction logic for XeniaSettings.xaml
    /// </summary>
    public partial class XeniaSettings : Page
    {
        // Global variables

        public XeniaSettings()
        {
            InitializeComponent();
        }

        // Functions
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
                            Log.Information("Audio Settings");
                            LoadAudioSettings(sectionTable);
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

        // UI Interactions
        // Page
        /// <summary>
        /// Executes when the page is loaded
        /// </summary>
        private void Page_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            LoadInstalledGames(); // Load the installed games into the ComboBox
            cmbConfigurationFiles.SelectedIndex = 0; // Select the first game
            Mouse.OverrideCursor = null;
        }

        // ComboBox
        /// <summary>
        /// Loads the selected configuration file into the UI
        /// </summary>
        private void cmbConfigurationFiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Check if the input is correct
            if (cmbConfigurationFiles.SelectedIndex < 0)
            {
                return;
            }

            // Grabbing the selected game and checking if we found the game
            Game selectedGame = GameManager.Games.FirstOrDefault(game => game.Title == cmbConfigurationFiles.SelectedItem.ToString());
            if (selectedGame == null)
            {
                return;
            }

            // Loading the configuration file
            // Games with "Custom" Xenia have absolute path to the configuration file while others have relative path
            Log.Information($"Loading the configuration file for {selectedGame.Title}");
            if (selectedGame.EmulatorVersion == EmulatorVersion.Custom)
            {
                ReadConfigFile(selectedGame.FileLocations.ConfigFilePath);
            }
            else
            {
                ReadConfigFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, selectedGame.FileLocations.ConfigFilePath));
            }
        }

        // Textbox
        /// <summary>
        /// If SearchBox is focused, check if it has placeholder text and remove it and reset the foreground color
        /// </summary>
        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox.Text == "Search settings")
            {
                textBox.Text = "";
                textBox.Foreground = (Brush)textBox.TryFindResource("ForegroundColor"); // Change text color to normal
            }
        }

        /// <summary>
        /// If Searchbox lost focus, check if it has any text and if it doesn't, apply placeholder text
        /// </summary>
        private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (string.IsNullOrWhiteSpace(textBox.Text))
            {
                textBox.Text = "Search settings";
                textBox.Foreground = (Brush)textBox.TryFindResource("PlaceholderText"); // Change text color to gray for placeholder
            }
        }

        /// <summary>
        /// Executes code only when text has been changed
        /// </summary>
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = sender as TextBox;

            // Don't execute search if it has placeholder text or it's empty
            if (textBox.Text == "Search settings" || string.IsNullOrWhiteSpace(textBox.Text))
            {
                if (AudioSettings != null)
                {
                    AudioSettings.Visibility = FilterSettings(AudioSettings, string.Empty) ? Visibility.Visible : Visibility.Collapsed;
                }
                if (DisplaySettings != null)
                {
                    DisplaySettings.Visibility = FilterSettings(DisplaySettings, string.Empty) ? Visibility.Visible : Visibility.Collapsed;
                }
                return;
            }

            // Grab the searchQuery
            string searchQuery = textBox.Text.Trim().ToLower();

            // Iterating through
            AudioSettings.Visibility = FilterSettings(AudioSettings, searchQuery) ? Visibility.Visible : Visibility.Collapsed; // If there are no settings visible, collapse the whole category
            DisplaySettings.Visibility = FilterSettings(DisplaySettings, searchQuery) ? Visibility.Visible : Visibility.Collapsed; // If there are no settings visible, collapse the whole category
        }

        /*
         * Alternative if TextChanged is too slow
        private void SearchBox_KeyUp(object sender, KeyEventArgs e)
        {
            TextBox textBox = sender as TextBox;

            // Don't execute search if it has placeholder text or it's empty
            if (textBox.Text == "Search settings" || string.IsNullOrWhiteSpace(textBox.Text))
            {
                if (AudioSettings != null)
                {
                    AudioSettings.Visibility = FilterSettings(AudioSettings, string.Empty) ? Visibility.Visible : Visibility.Collapsed;
                }
                return;
            }

            // Grab the searchQuery
            string searchQuery = textBox.Text.Trim().ToLower();

            // Iterating through
            bool isAnyAudioSettingVisible = FilterSettings(AudioSettings, searchQuery);
            AudioSettings.Visibility = isAnyAudioSettingVisible ? Visibility.Visible : Visibility.Collapsed; // If there are no settings visible, collapse the whole category
        }*/
    }
}
