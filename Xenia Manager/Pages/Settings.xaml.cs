using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

// Imported
using Serilog;
using Xenia_Manager.Windows;

namespace Xenia_Manager.Pages
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : Page
    {
        public Settings()
        {
            InitializeComponent();
            InitializeAsync();
        }

        /// <summary>
        /// This prevents the SelectionChanged event to trigger on launch for no reason
        /// </summary>
        private bool startup = false;

        /// <summary>
        /// Reads what theme is selected and selects it in the combobox
        /// </summary>
        private async Task SelectTheme()
        {
            try
            {
                switch (App.appConfiguration.ThemeSelected)
                {
                    case "Light":
                        ThemeSelector.SelectedIndex = 1;
                        break;
                    case "Dark":
                        ThemeSelector.SelectedIndex = 2;
                        break;
                    case "Nord":
                        ThemeSelector.SelectedIndex = 3;
                        break;
                    default:
                        ThemeSelector.SelectedIndex = 0;
                        break;
                }
                await Task.Delay(1);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// Loads installed versions of Xenia and selects the one used by default by Xenia Manager
        /// </summary>
        private async Task LoadInstalledXeniaVersions()
        {
            try
            {
                XeniaVersionSelector.Items.Clear();
                if (App.appConfiguration.XeniaStable != null)
                {
                    Log.Information("Xenia Stable is installed");
                    XeniaVersionSelector.Items.Add("Stable");
                }
                if (App.appConfiguration.XeniaCanary != null)
                {
                    Log.Information("Xenia Canary is installed");
                    XeniaVersionSelector.Items.Add("Canary");
                }
                if (App.appConfiguration.EmulatorVersion == "Stable")
                {
                    Log.Information("Xenia Stable is used by Xenia Manager");
                    XeniaVersionSelector.SelectedItem = "Stable";
                }
                else
                {
                    Log.Information("Xenia Canary is used by Xenia Manager");
                    XeniaVersionSelector.SelectedItem = "Canary";
                }
                await Task.Delay(1);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// Function that executes other functions asynchronously
        /// </summary>
        public async void InitializeAsync()
        {
            try
            {
                await Dispatcher.InvokeAsync(async () =>
                {
                    Mouse.OverrideCursor = Cursors.Wait;
                    await SelectTheme();
                    await LoadInstalledXeniaVersions();
                    GC.Collect();
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
            }
            finally
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    Mouse.OverrideCursor = null;
                });
            }
        }

        /// <summary>
        /// Checks for a change and then applies the correct theme
        /// </summary>
        private async void ThemeSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (startup)
                {
                    if (ThemeSelector.SelectedIndex >= 0)
                    {
                        switch (ThemeSelector.SelectedIndex)
                        {
                            case 1:
                                // Apply the Light theme
                                App.appConfiguration.ThemeSelected = "Light";
                                await App.LoadTheme();
                                await App.appConfiguration.SaveAsync(AppDomain.CurrentDomain.BaseDirectory + "config.json");
                                break;
                            case 2:
                                // Apply the Dark theme
                                App.appConfiguration.ThemeSelected = "Dark";
                                await App.LoadTheme();
                                await App.appConfiguration.SaveAsync(AppDomain.CurrentDomain.BaseDirectory + "config.json");
                                break;
                            case 3:
                                // Apply the Nord theme
                                App.appConfiguration.ThemeSelected = "Nord";
                                await App.LoadTheme();
                                await App.appConfiguration.SaveAsync(AppDomain.CurrentDomain.BaseDirectory + "config.json");
                                break;
                            default:
                                // Check system and then apply the correct one
                                App.appConfiguration.ThemeSelected = "Default";
                                await App.LoadTheme();
                                await App.appConfiguration.SaveAsync(AppDomain.CurrentDomain.BaseDirectory + "config.json");
                                break;
                        }
                    }
                }
                else
                {
                    startup = true;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
                return;
            }
        }

        /// <summary>
        /// Checks what Xenia Version is selected as default and saves the changes to the configuration file
        /// </summary>
        private async void XeniaVersionSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (startup)
                {
                    if (XeniaVersionSelector.SelectedIndex >= 0)
                    {
                        if (XeniaVersionSelector.SelectedItem.ToString() == "Stable")
                        {
                            // Stable version
                            App.appConfiguration.EmulatorVersion = "Stable";
                            App.appConfiguration.EmulatorLocation = App.appConfiguration.XeniaStable.EmulatorLocation;
                            App.appConfiguration.ExecutableLocation = App.appConfiguration.XeniaStable.EmulatorLocation + @"xenia.exe";
                            App.appConfiguration.ConfigurationFileLocation = App.appConfiguration.XeniaStable.EmulatorLocation + @"\xenia.config.toml";

                            // Saving changes
                            await App.appConfiguration.SaveAsync(AppDomain.CurrentDomain.BaseDirectory + "config.json");
                        }
                        else
                        {
                            // Canary version
                            App.appConfiguration.EmulatorVersion = "Canary";
                            App.appConfiguration.EmulatorLocation = App.appConfiguration.XeniaCanary.EmulatorLocation;
                            App.appConfiguration.ExecutableLocation = App.appConfiguration.XeniaCanary.EmulatorLocation + @"xenia_canary.exe";
                            App.appConfiguration.ConfigurationFileLocation = App.appConfiguration.XeniaCanary.EmulatorLocation + @"\xenia-canary.config.toml";

                            // Saving changes
                            await App.appConfiguration.SaveAsync(AppDomain.CurrentDomain.BaseDirectory + "config.json");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
                return;
            }
        }

        /// <summary>
        /// Opens WelcomeDialog where the user can install another version of Xenia
        /// </summary>
        private void OpenXeniaInstaller_Click(object sender, RoutedEventArgs e)
        {
            WelcomeDialog welcome = new WelcomeDialog();
            welcome.Show();
        }
    }
}
