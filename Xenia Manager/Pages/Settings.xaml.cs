using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

// Imported
using Serilog;
using Xenia_Manager.Classes;
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
                    case "AMOLED":
                        ThemeSelector.SelectedIndex = 3;
                        break;
                    case "Nord":
                        ThemeSelector.SelectedIndex = 4;
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
                // Checking if Xenia Stable is installed
                if (App.appConfiguration.XeniaStable != null)
                {
                    Log.Information("Xenia Stable is installed");
                    XeniaVersionSelector.Items.Add("Stable");
                }

                // Checking if Xenia Canary is installed
                if (App.appConfiguration.XeniaCanary != null)
                {
                    Log.Information("Xenia Canary is installed");
                    XeniaVersionSelector.Items.Add("Canary");
                }

                // Checking if Xenia Netplay is installed
                if (App.appConfiguration.XeniaNetplay != null)
                {
                    Log.Information("Xenia Netplay is installed");
                    XeniaVersionSelector.Items.Add("Netplay");
                }

                // Checking what is the default Xenia version
                switch (App.appConfiguration.EmulatorVersion)
                {
                    case "Stable":
                        Log.Information("Xenia Stable is used by Xenia Manager");
                        XeniaVersionSelector.SelectedItem = "Stable";
                        break;
                    case "Canary":
                        Log.Information("Xenia Canary is used by Xenia Manager");
                        XeniaVersionSelector.SelectedItem = "Canary";
                        break;
                    case "Netplay":
                        Log.Information("Xenia Netplay is used by Xenia Manager");
                        XeniaVersionSelector.SelectedItem = "Netplay";
                        break;
                    default:
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
                                await App.appConfiguration.SaveAsync(Path.Combine(App.baseDirectory, "config.json"));
                                break;
                            case 2:
                                // Apply the Dark theme
                                App.appConfiguration.ThemeSelected = "Dark";
                                await App.LoadTheme();
                                await App.appConfiguration.SaveAsync(Path.Combine(App.baseDirectory, "config.json"));
                                break;
                            case 3:
                                // Apply the Dark AMOLED theme
                                App.appConfiguration.ThemeSelected = "AMOLED";
                                await App.LoadTheme();
                                await App.appConfiguration.SaveAsync(Path.Combine(App.baseDirectory, "config.json"));
                                break;
                            case 4:
                                // Apply the Nord theme
                                App.appConfiguration.ThemeSelected = "Nord";
                                await App.LoadTheme();
                                await App.appConfiguration.SaveAsync(Path.Combine(App.baseDirectory, "config.json"));
                                break;
                            default:
                                // Check system and then apply the correct one
                                App.appConfiguration.ThemeSelected = "Default";
                                await App.LoadTheme();
                                await App.appConfiguration.SaveAsync(Path.Combine(App.baseDirectory, "config.json"));
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
                        switch (XeniaVersionSelector.SelectedItem.ToString())
                        {
                            case "Stable":
                                // Stable version
                                App.appConfiguration.EmulatorVersion = "Stable";
                                App.appConfiguration.EmulatorLocation = App.appConfiguration.XeniaStable.EmulatorLocation;
                                App.appConfiguration.ExecutableLocation = App.appConfiguration.XeniaStable.ExecutableLocation;
                                App.appConfiguration.ConfigurationFileLocation = App.appConfiguration.XeniaStable.ConfigurationFileLocation;
                                break;
                            case "Canary":
                                // Canary version
                                App.appConfiguration.EmulatorVersion = "Canary";
                                App.appConfiguration.EmulatorLocation = App.appConfiguration.XeniaCanary.EmulatorLocation;
                                App.appConfiguration.ExecutableLocation = App.appConfiguration.XeniaCanary.ExecutableLocation;
                                App.appConfiguration.ConfigurationFileLocation = App.appConfiguration.XeniaCanary.ConfigurationFileLocation;
                                break;
                            case "Netplay":
                                // Netplay version
                                App.appConfiguration.EmulatorVersion = "Netplay";
                                App.appConfiguration.EmulatorLocation = App.appConfiguration.XeniaNetplay.EmulatorLocation;
                                App.appConfiguration.ExecutableLocation = App.appConfiguration.XeniaNetplay.ExecutableLocation;
                                App.appConfiguration.ConfigurationFileLocation = App.appConfiguration.XeniaNetplay.ConfigurationFileLocation;
                                break;
                            default:
                                break;
                        }

                        // Saving changes
                        await App.appConfiguration.SaveAsync(Path.Combine(App.baseDirectory, "config.json"));
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
        private async void OpenXeniaInstaller_Click(object sender, RoutedEventArgs e)
        {
            WelcomeDialog welcome = new WelcomeDialog(true);
            welcome.ShowDialog();
            await LoadInstalledXeniaVersions();
        }

        /// <summary>
        /// Resets the configuration file of Xenia Manager and tries to assign new paths to Xenia stuff
        /// </summary>
        private async void ResetConfigurationFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await Task.Delay(1);
                int numberOfXeniaInstallation = 0;
                EmulatorInfo emulatorInfo = null;
                // Checking if Xenia Stable is installed
                if (App.appConfiguration.XeniaStable != null)
                {
                    if (!Directory.Exists(Path.Combine(App.baseDirectory, App.appConfiguration.XeniaStable.EmulatorLocation)))
                    {
                        Log.Information("Configuration file has wrong path to Xenia Stable");
                        Log.Information("Checking if Xenia Stable is installed");
                        if (Directory.Exists(Path.Combine(App.baseDirectory, @"Xenia Stable\")) && File.Exists(Path.Combine(App.baseDirectory, @"Xenia Stable\xenia.exe")))
                        {
                            numberOfXeniaInstallation++;
                            Log.Information("Xenia Stable found");
                            App.appConfiguration.XeniaStable.EmulatorLocation = @"Xenia Stable\";
                            App.appConfiguration.XeniaStable.ExecutableLocation = @"Xenia Stable\xenia.exe";
                            App.appConfiguration.XeniaStable.ConfigurationFileLocation = @"Xenia Stable\xenia.config.toml";
                        }
                        else
                        {
                            Log.Information("Xenia Stable not found");
                            App.appConfiguration.XeniaStable = null;
                        }
                    }
                    else
                    {
                        Log.Information("Configuration file has the correct path to Xenia Stable");
                        numberOfXeniaInstallation++;
                    }
                }

                // Checking if Xenia Canary is installed
                if (App.appConfiguration.XeniaCanary != null)
                {
                    if (!Directory.Exists(Path.Combine(App.baseDirectory, App.appConfiguration.XeniaCanary.EmulatorLocation)))
                    {
                        Log.Information("Configuration file has wrong path to Xenia Canary");
                        Log.Information("Checking if Xenia Canary is installed");
                        if (Directory.Exists(Path.Combine(App.baseDirectory, @"Xenia Canary\")) && File.Exists(Path.Combine(App.baseDirectory, @"Xenia Canary\xenia_canary.exe")))
                        {
                            numberOfXeniaInstallation++;
                            Log.Information("Xenia Canary found");
                            App.appConfiguration.XeniaCanary.EmulatorLocation = @"Xenia Canary\";
                            App.appConfiguration.XeniaCanary.ExecutableLocation = @"Xenia Canary\xenia_canary.exe";
                            App.appConfiguration.XeniaCanary.ConfigurationFileLocation = @"Xenia Canary\xenia-canary.config.toml";
                        }
                        else
                        {
                            Log.Information("Xenia Canary not found");
                            App.appConfiguration.XeniaCanary = null;
                        }
                    }
                    else
                    {
                        Log.Information("Configuration file has the correct path to Xenia Canary");
                        numberOfXeniaInstallation++;
                    }
                }

                // Checking if Xenia Netplay is installed
                if (App.appConfiguration.XeniaNetplay != null)
                {
                    if (!Directory.Exists(Path.Combine(App.baseDirectory, App.appConfiguration.XeniaNetplay.EmulatorLocation)))
                    {
                        Log.Information("Configuration file has wrong path to Xenia Netplay");
                        Log.Information("Checking if Xenia Netplay is installed");
                        if (Directory.Exists(Path.Combine(App.baseDirectory, @"Xenia Netplay\")) && File.Exists(Path.Combine(App.baseDirectory, @"Xenia Netplay\xenia_canary_netplay.exe")))
                        {
                            numberOfXeniaInstallation++;
                            Log.Information("Xenia Netplay found");
                            App.appConfiguration.XeniaNetplay.EmulatorLocation = @"Xenia Netplay\";
                            App.appConfiguration.XeniaNetplay.ExecutableLocation = @"Xenia Netplay\xenia_canary_netplay.exe";
                            App.appConfiguration.XeniaNetplay.ConfigurationFileLocation = @"Xenia Netplay\xenia-canary-netplay.config.toml";
                        }
                        else
                        {
                            Log.Information("Xenia Netplay not found");
                            App.appConfiguration.XeniaNetplay = null;
                        }
                    }
                    else
                    {
                        Log.Information("Configuration file has the correct path to Xenia Netplay");
                        numberOfXeniaInstallation++;
                    }
                }

                // Checking if Xenia VFS Dump Tool is installed
                if (!File.Exists(Path.Combine(App.baseDirectory, App.appConfiguration.VFSDumpToolLocation)))
                {
                    Log.Information("Configuration file has wrong path to Xenia VFS Dump tool");
                    Log.Information("Checking if Xenia VFS Dump tool is installed");
                    if (Directory.Exists(Path.Combine(App.baseDirectory, @"Xenia VFS Dump Tool\")) && File.Exists(Path.Combine(App.baseDirectory, @"Xenia VFS Dump Tool\xenia-vfs-dump.exe")))
                    {
                        Log.Information("Xenia VFS Dump tool found");
                        App.appConfiguration.VFSDumpToolLocation = Path.Combine(App.baseDirectory, @"Xenia VFS Dump Tool\xenia-vfs-dump.exe");
                    }
                    else
                    {
                        Log.Information("Xenia VFS Dump tool not found");
                        Log.Information("Installing it now");
                        await App.DownloadXeniaVFSDumper();
                        App.appConfiguration.VFSDumpToolLocation = Path.Combine(App.baseDirectory, @"Xenia VFS Dump Tool\xenia-vfs-dump.exe");
                    }
                }
                else
                {
                    Log.Information("Configuration file has the correct path to Xenia VFS Dump tool");
                }

                if (numberOfXeniaInstallation >= 2)
                {
                    XeniaSelection xs = new XeniaSelection();
                    await xs.WaitForCloseAsync();
                    Log.Information($"User selected Xenia {xs.UserSelection}");
                    switch (xs.UserSelection)
                    {
                        case "Stable":
                            emulatorInfo = App.appConfiguration.XeniaStable;
                            break;
                        case "Canary":
                            emulatorInfo = App.appConfiguration.XeniaCanary;
                            break;
                        case "Netplay":
                            emulatorInfo = App.appConfiguration.XeniaNetplay;
                            break;
                        default:
                            break;
                    }
                    App.appConfiguration.EmulatorVersion = xs.UserSelection;
                    App.appConfiguration.EmulatorLocation = emulatorInfo.EmulatorLocation;
                    App.appConfiguration.ExecutableLocation = emulatorInfo.ExecutableLocation;
                    App.appConfiguration.ConfigurationFileLocation = emulatorInfo.ConfigurationFileLocation;
                }
                else if (numberOfXeniaInstallation == 0)
                {
                    MessageBox.Show("No Xenia Installation found.\nUse the Xenia Installer to install a version of Xenia.");
                    App.appConfiguration.EmulatorVersion = null;
                    App.appConfiguration.EmulatorLocation = null;
                    App.appConfiguration.ExecutableLocation = null;
                    App.appConfiguration.ConfigurationFileLocation = null;
                }
                else
                {
                    if (App.appConfiguration.XeniaStable != null)
                    {
                        App.appConfiguration.EmulatorVersion = "Stable";
                        App.appConfiguration.EmulatorLocation = App.appConfiguration.XeniaStable.EmulatorLocation;
                        App.appConfiguration.ExecutableLocation = App.appConfiguration.XeniaStable.ExecutableLocation;
                        App.appConfiguration.ConfigurationFileLocation = App.appConfiguration.XeniaStable.ConfigurationFileLocation;
                    }
                    else if (App.appConfiguration.XeniaCanary != null)
                    {
                        App.appConfiguration.EmulatorVersion = "Canary";
                        App.appConfiguration.EmulatorLocation = App.appConfiguration.XeniaCanary.EmulatorLocation;
                        App.appConfiguration.ExecutableLocation = App.appConfiguration.XeniaCanary.ExecutableLocation;
                        App.appConfiguration.ConfigurationFileLocation = App.appConfiguration.XeniaCanary.ConfigurationFileLocation;
                    }
                    else if (App.appConfiguration.XeniaNetplay != null)
                    {
                        App.appConfiguration.EmulatorVersion = "Netplay";
                        App.appConfiguration.EmulatorLocation = App.appConfiguration.XeniaNetplay.EmulatorLocation;
                        App.appConfiguration.ExecutableLocation = App.appConfiguration.XeniaNetplay.ExecutableLocation;
                        App.appConfiguration.ConfigurationFileLocation = App.appConfiguration.XeniaNetplay.ConfigurationFileLocation;
                    }
                }
                await App.appConfiguration.SaveAsync(Path.Combine(App.baseDirectory, "config.json"));
                InitializeAsync();
                MessageBox.Show("The configuration file has been repaired.");
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
                return;
            }
        }

        /// <summary>
        /// Shows changelog
        /// </summary>
        private async void OpenChangelog_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ChangelogWindow changelogWindow = new ChangelogWindow();
                changelogWindow.Show();
                await changelogWindow.WaitForCloseAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
                return;
            }
        }
    }
}
