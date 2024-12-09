using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

// Imported
using Newtonsoft.Json.Linq;
using Serilog;

namespace XeniaManager.DesktopApp.Pages
{
    /// <summary>
    /// Interaction logic for XeniaSettings.xaml
    /// </summary>
    public partial class XeniaSettings
    {
        /// <summary>
        /// Executes when the page is loaded
        /// </summary>
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            HideNonUniversalSettings(); // Hides all the non-universal settings like the Netplay stuff
            LoadCountryComboBox(); // Load all the countries into the "user_country" ComboBox
            LoadInstalledGames(); // Load the installed games into the ComboBox
            CmbConfigurationFiles.SelectedIndex = 0; // Select the first game
            LoadNvidiaDriverSettings();
            Mouse.OverrideCursor = null;
        }

        /// <summary>
        /// Loads the selected configuration file into the UI
        /// </summary>
        private void CmbConfigurationFiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Check if the input is correct
            if (CmbConfigurationFiles.SelectedIndex < 0)
            {
                return;
            }

            // Grabbing the selected game and checking if we found the game
            SelectedGame =
                GameManager.Games.FirstOrDefault(game => game.Title == CmbConfigurationFiles.SelectedItem.ToString());
            if (SelectedGame == null)
            {
                return;
            }

            HideNonUniversalSettings(); // Hides all the non-universal settings like the Netplay stuff
            // Loading the configuration file
            // Games with "Custom" Xenia have absolute path to the configuration file while others have relative path
            Log.Information($"Loading the configuration file for {SelectedGame.Title}");
            if (SelectedGame.EmulatorVersion == EmulatorVersion.Custom)
            {
                ReadConfigFile(SelectedGame.FileLocations.ConfigFilePath);
            }
            else
            {
                ReadConfigFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                    SelectedGame.FileLocations.ConfigFilePath));
            }

            TxtSearchBar_TextChanged(TxtSearchBar,
                new TextChangedEventArgs(TextBox.TextChangedEvent, UndoAction.None)); // Redo the search
        }

        // Buttons
        /// <summary>
        /// Resets the currently selected game configuration to the default one used by the emulator
        /// </summary>
        private void BtnResetSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Game selectedGame =
                    GameManager.Games.First(game => game.Title == CmbConfigurationFiles.SelectedItem.ToString());
                switch (selectedGame.EmulatorVersion)
                {
                    case EmulatorVersion.Canary:
                        Log.Information("Loading default Xenia Canary configuration");
                        ReadConfigFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                            ConfigurationManager.AppConfig.XeniaCanary.ConfigurationFileLocation));
                        break;
                    case EmulatorVersion.Mousehook:
                        Log.Information("Loading default Xenia Mousehook configuration");
                        ReadConfigFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                            ConfigurationManager.AppConfig.XeniaMousehook.ConfigurationFileLocation));
                        break;
                    case EmulatorVersion.Netplay:
                        Log.Information("Loading default Xenia Netplay configuration");
                        ReadConfigFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                            ConfigurationManager.AppConfig.XeniaNetplay.ConfigurationFileLocation));
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// Searches for optimized settings on the repository and applies the to the UI, but doesn't save them
        /// </summary>
        private async void BtnOptimizeSettings_Click(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            Game selectedGame =
                GameManager.Games.First(game => game.Title == CmbConfigurationFiles.SelectedItem.ToString());
            JToken optimizedSettings = await GameManager.SearchForOptimizedSettings(selectedGame.GameId);
            if (optimizedSettings == null)
            {
                Mouse.OverrideCursor = null;
                MessageBox.Show("We couldn't find optimized settings in our repository");
                return;
            }

            // Apply optimized settings to settings
            OptimizeSettings(optimizedSettings);
            Log.Information("Reloading the UI");
            ReadConfigFile(selectedGame.FileLocations.ConfigFilePath, false); // This is to reload the UI
            Mouse.OverrideCursor = null;
            MessageBox.Show(
                "Optimized settings have been loaded.\nTo apply them, press on the 'Save Changes' button.\nDo note that some changes are not visible in the UI because those settings are not in the UI.");
        }

        /// <summary>
        /// Opens the configuration file in an editor (Usually Notepad if no default app is found)
        /// </summary>
        private void BtnOpenInEditor_Click(object sender, RoutedEventArgs e)
        {
            string configPath = "";
            ProcessStartInfo startInfo = new ProcessStartInfo();
            Process process;
            try
            {
                Game selectedGame =
                    GameManager.Games.First(game => game.Title == CmbConfigurationFiles.SelectedItem.ToString());
                Log.Information($"{selectedGame.Title} is selected");
                configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                    selectedGame.FileLocations.ConfigFilePath);
                startInfo = new ProcessStartInfo
                {
                    FileName = configPath,
                    UseShellExecute = true
                };

                Log.Information("Loading the configuration file in the default app");
                process = Process.Start(startInfo);
                if (process == null)
                {
                    // If process is null, it means it didn't start successfully
                    throw new Exception("Process did not start successfully.");
                }
            }
            catch (Win32Exception)
            {
                Log.Warning("Default application not found");
                Log.Information("Trying to open the file with notepad");
                startInfo.FileName = "notepad.exe";
                startInfo.Arguments = configPath;
                process = Process.Start(startInfo);
                if (process == null)
                {
                    // If process is null, it means it didn't start successfully
                    throw new Exception("Process did not start successfully.");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// When button "Save changes" is pressed, save changes to the configuration file
        /// </summary>
        private void BtnSaveSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Log.Information("Saving changes");
                // Grabbing the game & checking if there is a selectedGame
                Game selectedGame = GameManager.Games.FirstOrDefault(game =>
                    game.Title == CmbConfigurationFiles.SelectedItem.ToString());
                if (selectedGame == null)
                {
                    return;
                }

                // Save changes
                // Games with "Custom" Xenia have absolute path to the configuration file while others have relative path
                if (selectedGame.EmulatorVersion == EmulatorVersion.Custom)
                {
                    SaveChanges(selectedGame.FileLocations.ConfigFilePath);
                }
                else
                {
                    SaveChanges(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                        selectedGame.FileLocations.ConfigFilePath));
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
            }
        }

        // SearchBar
        /// <summary>
        /// If SearchBar is focused, check if it has placeholder text and remove it and reset the foreground color
        /// </summary>
        private void TxtSearchBar_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox.Text == "Search settings")
            {
                textBox.Text = "";
                textBox.Foreground = (Brush)textBox.TryFindResource("ForegroundColor"); // Change text color to normal
            }
        }

        /// <summary>
        /// If SearchBar lost focus, check if it has any text and if it doesn't, apply placeholder text
        /// </summary>
        private void TxtSearchBar_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (string.IsNullOrWhiteSpace(textBox.Text))
            {
                textBox.Text = "Search settings";
                textBox.Foreground =
                    (Brush)textBox.TryFindResource("PlaceholderText"); // Change text color to gray for placeholder
            }
        }

        /// <summary>
        /// Executes code only when text has been changed
        /// </summary>
        private void TxtSearchBar_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            string searchQuery = textBox.Text.Trim().ToLower();

            // List of all setting categories to filter
            List<StackPanel> settingsCategories =
            [
                SpAudioSettings, SpDisplaySettings, SpNvidiaDriverSettings, SpGraphicalSettings,
                SpGeneralSettings, SpNetplaySettings, SpUserInputSettings, SpMousehookSettings, SpStorageSettings,
                HackSettings
            ];

            // If search query is empty or default placeholder, reset all categories
            if (string.IsNullOrWhiteSpace(searchQuery) || textBox.Text == "Search settings")
            {
                foreach (var category in settingsCategories)
                {
                    if (category != null)
                    {
                        category.Visibility = FilterSettings(category, string.Empty)
                            ? Visibility.Visible
                            : Visibility.Collapsed;
                    }
                }

                return;
            }

            // Iterate through each settings category, applying search filtering
            foreach (var category in settingsCategories)
            {
                if (category != null)
                {
                    category.Visibility = FilterSettings(category, searchQuery)
                        ? Visibility.Visible
                        : Visibility.Collapsed;
                }
            }
        }

        // Xenia Settings
        /// <summary>
        /// This checks for input to be less than 12 characters
        /// If it is, change it back to default setting
        /// </summary>
        private void TxtAudioMaxQueuedFrames_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is not TextBox textBox)
            {
                return;
            }

            // Checking if the input is bigger than allowed
            if (textBox.Text.Length > 12)
            {
                textBox.Text = "8";
                MessageBox.Show("You went over the allowed limit");
            }
        }

        /// <summary>
        /// Checks which option is selected and then shows specific settings for that Graphics API
        /// </summary>
        private void CmbGPUApi_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Check if the selection is correct
            if (CmbGpuApi.SelectedIndex < 0)
            {
                return;
            }

            // TODO: Show GPU specific settings
            switch (CmbGpuApi.SelectedIndex)
            {
                case 1:
                    // "D3D12"
                    // Show D3D12 settings
                    SpD3D12Settings.Visibility = Visibility.Visible;
                    SpD3D12Settings.Tag = null;

                    // Hide Vulkan settings
                    SpVulkanSettings.Visibility = Visibility.Collapsed;
                    SpVulkanSettings.Tag = "Ignore";
                    break;
                case 2:
                    // "Vulkan"
                    // Hide D3D12 settings
                    SpD3D12Settings.Visibility = Visibility.Collapsed;
                    SpD3D12Settings.Tag = "Ignore";

                    // Show vulkan settings
                    SpVulkanSettings.Visibility = Visibility.Visible;
                    SpVulkanSettings.Tag = null;
                    break;
                default:
                    // "any" option
                    // Show D3D12 settings
                    SpD3D12Settings.Visibility = Visibility.Visible;
                    SpD3D12Settings.Tag = null;

                    // Show Vulkan settings
                    SpVulkanSettings.Visibility = Visibility.Visible;
                    SpVulkanSettings.Tag = null;
                    break;
            }

            TxtSearchBar_TextChanged(TxtSearchBar,
                new TextChangedEventArgs(TextBox.TextChangedEvent, UndoAction.None)); // Redo the search
        }

        /// <summary>
        /// Displays the value on the textbox below this slider
        /// </summary>
        private void SldXeniaFramerate_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (sender is not Slider slider)
            {
                return;
            }

            // Text shown under the slider
            TxtSldXeniaFramerate.Text = slider.Value == 0 ? "Off" : $"{slider.Value} FPS";

            AutomationProperties.SetName(SldXeniaFramerate, $"Xenia Framerate Limiter: {slider.Value} FPS");
        }

        /// <summary>
        /// Checks for value changes on DrawResolutionScale
        /// </summary>
        private void SldDrawResolutionScale_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            AutomationProperties.SetName(SldDrawResolutionScale,
                $"Draw Resolution Scale: {SldDrawResolutionScale.Value}");
        }

        /// <summary>
        /// Checks for value changes on FSRSharpnessReduction slider and shows them on the textbox
        /// </summary>
        private void SldFsrSharpnessReduction_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            TxtSldFsrSharpnessReduction.Text = Math.Round((SldFsrSharpnessReduction.Value / 1000), 3).ToString();
            AutomationProperties.SetName(SldFsrSharpnessReduction,
                $"FSR Sharpness Reduction: {Math.Round((SldFsrSharpnessReduction.Value / 1000), 3)}");
        }

        /// <summary>
        /// Checks for value changes on CASAdditionalSharpness and shows them on the textbox
        /// </summary>
        private void SldCasAdditionalSharpness_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            TxtSldCasAdditionalSharpness.Text = Math.Round((SldCasAdditionalSharpness.Value / 1000), 3).ToString();
            AutomationProperties.SetName(SldCasAdditionalSharpness,
                $"CAS Additional Sharpness: {Math.Round((SldCasAdditionalSharpness.Value / 1000), 3)}");
        }

        /// <summary>
        /// Checks for value changes on FSRMaxUpsamplingPasses slider and shows them on the textbox
        /// </summary>
        private void SldFsrMaxUpsamplingPasses_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            AutomationProperties.SetName(SldFsrMaxUpsamplingPasses,
                $"FSR MaxUpsampling Passes: {SldFsrMaxUpsamplingPasses.Value}");
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
        private void SldLeftStickDeadzone_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            TxtSldLeftStickDeadzoneValue.Text = Math.Round((SldLeftStickDeadzone.Value / 10), 1).ToString();
            AutomationProperties.SetName(SldLeftStickDeadzone,
                $"Left Stick Deadzone Percentage: {Math.Round((SldLeftStickDeadzone.Value / 10), 1)}");
        }

        /// <summary>
        /// Checks for value changes on RightStickDeadzone slider and shows them on the textbox
        /// </summary>
        private void SldRightStickDeadzone_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            TxtSldRightStickDeadzoneValue.Text = Math.Round((SldRightStickDeadzone.Value / 10), 1).ToString();
            AutomationProperties.SetName(SldRightStickDeadzone,
                $"Left Stick Deadzone Percentage: {Math.Round((SldRightStickDeadzone.Value / 10), 1)}");
        }

        /// <summary>
        /// Checks for value changes on AimTurnDistance slider and shows them on the textbox
        /// </summary>
        private void SldAimTurnDistance_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            TxtSldAimTurnDistance.Text = Math.Round((SldAimTurnDistance.Value / 1000), 3).ToString();
            AutomationProperties.SetName(SldAimTurnDistance,
                $"Aim Turn Distance Sensitivity: {Math.Round((SldAimTurnDistance.Value / 1000), 3)}");
        }

        /// <summary>
        /// Checks for value changes on FOVSensitivity slider and shows them on the textbox
        /// </summary>
        private void SldFovSensitivity_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            TxtSldFovSensitivity.Text = Math.Round((SldFovSensitivity.Value / 10), 1).ToString();
            AutomationProperties.SetName(SldFovSensitivity,
                $"FOV Sensitivity: {Math.Round((SldFovSensitivity.Value / 10), 1)}");
        }

        /// <summary>
        /// Checks for value changes on MouseSensitivity slider and shows them on the textbox
        /// </summary>
        private void SldMouseSensitivity_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            TxtSldMouseSensitivity.Text = Math.Round((SldMouseSensitivity.Value / 10), 1).ToString();
            AutomationProperties.SetName(SldMouseSensitivity,
                $"Mouse Sensitivity: {Math.Round((SldMouseSensitivity.Value / 10), 1)}");
        }

        /// <summary>
        /// Checks for value changes on GoldenEyeMenuSensitivity slider and shows them on the textbox
        /// </summary>
        private void SldGoldenEyeMenuSensitivity_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            TxtSldGoldenEyeMenuSensitivity.Text = Math.Round((SldGoldenEyeMenuSensitivity.Value / 10), 1).ToString();
            AutomationProperties.SetName(SldGoldenEyeMenuSensitivity,
                $"GoldenEye Menu Sensitivity: {Math.Round((SldGoldenEyeMenuSensitivity.Value / 10), 1)}");
        }
    }
}