using System;
using System.IO;
using System.Windows;
using System.Windows.Automation;
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
        /// <summary>
        /// Constructor for the Xenia Settings Page
        /// </summary>
        public XeniaSettings()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Executes when the page is loaded
        /// </summary>
        private void Page_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            HideNonUniversalSettings(); // Hides all of the non universal settings like the Netplay stuff
            LoadCountryComboBox(); // Load all of the countries into the "user_country" ComboBox
            LoadInstalledGames(); // Load the installed games into the ComboBox
            cmbConfigurationFiles.SelectedIndex = 0; // Select the first game
            Mouse.OverrideCursor = null;
        }

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
            HideNonUniversalSettings(); // Hides all of the non universal settings like the Netplay stuff
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
            SearchBox_TextChanged(txtSearchBox, new TextChangedEventArgs(TextBox.TextChangedEvent, UndoAction.None)); // Redo the search
        }

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
            string searchQuery = textBox.Text.Trim().ToLower();

            // List of all setting categories to filter
            List<StackPanel> settingsCategories = new List<StackPanel>
            {
                AudioSettings, DisplaySettings, NvidiaDriverSettings, GraphicalSettings,
                GeneralSettings, NetplaySettings, UserInputSettings, StorageSettings, HackSettings
            };

            // If search query is empty or default placeholder, reset all categories
            if (string.IsNullOrWhiteSpace(searchQuery) || textBox.Text == "Search settings")
            {
                foreach (var category in settingsCategories)
                {
                    if (category != null)
                    {
                        category.Visibility = FilterSettings(category, string.Empty) ? Visibility.Visible : Visibility.Collapsed;
                    }
                }
                return;
            }

            // Iterate through each settings category, applying search filtering
            foreach (var category in settingsCategories)
            {
                if (category != null)
                {
                    category.Visibility = FilterSettings(category, searchQuery) ? Visibility.Visible : Visibility.Collapsed;
                }
            }
        }

        // Xenia Settings
        /// <summary>
        /// Checks which option is selected and then shows specific settings for that Graphics API
        /// </summary>
        private void cmbGPUApi_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Check if the selection is correct
            if (cmbGPUApi.SelectedIndex < 0)
            {
                return;
            }

            // TODO: Show GPU specific settings
            switch (cmbGPUApi.SelectedIndex)
            {
                case 1:
                    // "D3D12"
                    // Show D3D12 settings
                    D3D12Settings.Visibility = Visibility.Visible;
                    D3D12Settings.Tag = null;

                    // Hide Vulkan settings
                    VulkanSettings.Visibility = Visibility.Collapsed;
                    VulkanSettings.Tag = "Ignore";
                    break;
                case 2:
                    // "Vulkan"
                    // Hide D3D12 settings
                    D3D12Settings.Visibility = Visibility.Collapsed;
                    D3D12Settings.Tag = "Ignore";

                    // Show vulkan settings
                    VulkanSettings.Visibility = Visibility.Visible;
                    VulkanSettings.Tag = null;
                    break;
                default:
                    // "any" option
                    // Show D3D12 settings
                    D3D12Settings.Visibility = Visibility.Visible;
                    D3D12Settings.Tag = null;

                    // Show Vulkan settings
                    VulkanSettings.Visibility = Visibility.Visible;
                    VulkanSettings.Tag = null;
                    break;
            }
            SearchBox_TextChanged(txtSearchBox, new TextChangedEventArgs(TextBox.TextChangedEvent, UndoAction.None)); // Redo the search
        }

        /// <summary>
        /// Displays the value on the textbox below this slider
        /// </summary>
        private void sldXeniaFramerate_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider slider = sender as Slider;
            if (slider == null)
            {
                return;
            }

            // Text shown under the slider
            if (slider.Value == 0)
            {
                txtsldXeniaFramerate.Text = "Off";
            }
            else
            {
                txtsldXeniaFramerate.Text = $"{slider.Value} FPS";
            }
            AutomationProperties.SetName(sldXeniaFramerate, $"Xenia Framerate Limiter: {slider.Value} FPS");
        }

        /// <summary>
        /// Checks for value changes on DrawResolutionScale
        /// </summary>
        private void sldDrawResolutionScale_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            AutomationProperties.SetName(sldDrawResolutionScale, $"Draw Resolution Scale: {sldDrawResolutionScale.Value}");
        }

        /// <summary>
        /// Checks for value changes on FSRSharpnessReduction slider and shows them on the textbox
        /// </summary>
        private void sldFSRSharpnessReduction_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            txtsldFSRSharpnessReduction.Text = Math.Round((sldFSRSharpnessReduction.Value / 1000), 3).ToString();
            AutomationProperties.SetName(sldFSRSharpnessReduction, $"FSR Sharpness Reduction: {Math.Round((sldFSRSharpnessReduction.Value / 1000), 3)}");
        }

        /// <summary>
        /// Checks for value changes on CASAdditionalSharpness and shows them on the textbox
        /// </summary>
        private void sldCASAdditionalSharpness_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            txtsldCASAdditionalSharpness.Text = Math.Round((sldCASAdditionalSharpness.Value / 1000), 3).ToString();
            AutomationProperties.SetName(sldCASAdditionalSharpness, $"CAS Additional Sharpness: {Math.Round((sldCASAdditionalSharpness.Value / 1000), 3)}");
        }

        /// <summary>
        /// Checks for value changes on FSRMaxUpsamplingPasses slider and shows them on the textbox
        /// </summary>
        private void sldFSRMaxUpsamplingPasses_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            AutomationProperties.SetName(sldFSRMaxUpsamplingPasses, $"FSR MaxUpsampling Passes: {sldFSRMaxUpsamplingPasses.Value}");
        }

        /// <summary>
        /// Check to see if there are less than 15 characters in the gamertag textbox
        /// </summary>
        private void GamerTagTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox.Text.Length > 15)
            {
                int selectionStart = textBox.SelectionStart;
                textBox.Text = textBox.Text.Substring(0, 15);
                textBox.SelectionStart = selectionStart > 15 ? 15 : selectionStart;
            }
        }

        /// <summary>
        /// Checks for value changes on LeftStickDeadzone slider and shows them on the textbox
        /// </summary>
        private void sldLeftStickDeadzone_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            txtsldLeftStickDeadzoneValue.Text = Math.Round((sldLeftStickDeadzone.Value / 10), 1).ToString();
            AutomationProperties.SetName(sldLeftStickDeadzone, $"Left Stick Deadzone Percentage: {Math.Round((sldLeftStickDeadzone.Value / 10), 1)}");
        }

        /// <summary>
        /// Checks for value changes on RightStickDeadzone slider and shows them on the textbox
        /// </summary>
        private void sldRightStickDeadzone_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            txtsldRightStickDeadzoneValue.Text = Math.Round((sldRightStickDeadzone.Value / 10), 1).ToString();
            AutomationProperties.SetName(sldRightStickDeadzone, $"Left Stick Deadzone Percentage: {Math.Round((sldRightStickDeadzone.Value / 10), 1)}");
        }
    }
}
