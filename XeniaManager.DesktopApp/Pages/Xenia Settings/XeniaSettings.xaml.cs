using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

// Imported
using Serilog;

namespace XeniaManager.DesktopApp.Pages
{
    /// <summary>
    /// Interaction logic for XeniaSettings.xaml
    /// </summary>
    public partial class XeniaSettings : Page
    {
        public XeniaSettings()
        {
            InitializeComponent();
        }

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
