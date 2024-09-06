using System;
using System.Diagnostics;
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
        /// Opens WelcomeDialog where the user can install another version of Xenia
        /// </summary>
        private async void OpenXeniaInstaller_Click(object sender, RoutedEventArgs e)
        {
            WelcomeDialog welcome = new WelcomeDialog(true);
            welcome.ShowDialog();
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

        /// <summary>
        /// Resets the configuration file of Xenia Manager and tries to assign new paths to Xenia stuff
        /// </summary>
        private async void ResetXeniaManagerConfigurationFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int numberOfXeniaInstallation = 0;
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
        /// Deletes current configuration file of selected Xenia Version and launches Xenia to generate new configuration file
        /// </summary>
        /// <param name="XeniaVersion">Selected Xenia Version</param>
        private async Task ResetXeniaEmulatorConfigurationFile(string XeniaVersion)
        {
            try
            {
                Log.Information(XeniaVersion);
                EmulatorInfo SelectedXeniaVersion = XeniaVersion switch
                {
                    "Canary" => App.appConfiguration.XeniaCanary,
                    "Stable" => App.appConfiguration.XeniaStable,
                    "Netplay" => App.appConfiguration.XeniaNetplay,
                    _ => throw new InvalidOperationException("Unexpected build type")
                };

                // Deleting the configuration file
                Log.Information("Deleting the current configuration file");
                if (File.Exists(Path.Combine(App.baseDirectory, SelectedXeniaVersion.ConfigurationFileLocation)))
                {
                    File.Delete(Path.Combine(App.baseDirectory, SelectedXeniaVersion.ConfigurationFileLocation));
                }

                // Launching Xenia to generate new configuration file
                Process xenia = new Process();
                xenia.StartInfo.FileName = Path.Combine(App.baseDirectory, SelectedXeniaVersion.ExecutableLocation);
                xenia.Start();
                Log.Information("Emulator Launched");
                Log.Information("Waiting for configuration file to be generated");
                while (!File.Exists(Path.Combine(App.baseDirectory, SelectedXeniaVersion.ConfigurationFileLocation)))
                {
                    await Task.Delay(100);
                }
                Log.Information("Configuration file found");
                Log.Information("Closing the emulator");
                xenia.Kill();
                Log.Information("Emulator closed");
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                return;
            }
        }

        /// <summary>
        /// Opens XeniaSelection dialog and resets the configuration file of the selected Xenia Emulator
        /// </summary>
        private async void ResetXeniaConfigurationFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Checking for existing Xenia versions
                Log.Information("Checking for existing Xenia versions");
                List<string> availableXeniaVersions = new List<string>();
                if (App.appConfiguration.XeniaStable != null) availableXeniaVersions.Add("Stable");
                if (App.appConfiguration.XeniaCanary != null) availableXeniaVersions.Add("Canary");
                if (App.appConfiguration.XeniaNetplay != null) availableXeniaVersions.Add("Netplay");

                // Check how many Xenia's are installed
                switch (availableXeniaVersions.Count)
                {
                    case 0:
                        // If there are no Xenia versions installed, don't do anything
                        Log.Information("No Xenia installations detected");
                        MessageBox.Show("No Xenia installations detected");
                        break;
                    case 1:
                        // If there is only 1 version of Xenia installed, reset configuration file of that Xenia Version
                        Log.Information($"Only Xenia {availableXeniaVersions[0]} is installed");
                        await ResetXeniaEmulatorConfigurationFile(availableXeniaVersions[0]);
                        MessageBox.Show($"Configuration file for Xenia {availableXeniaVersions[0]} has been reset to default.");
                        break;
                    default:
                        // If there are 2 or more versions of Xenia installed, ask the user which configuration file he wants to reset
                        Log.Information("Detected multiple Xenia installations");
                        Log.Information("Asking user what Xenia version's configuration file should be reset");
                        XeniaSelection xs = new XeniaSelection();
                        await xs.WaitForCloseAsync();
                        Log.Information($"User selected Xenia {xs.UserSelection}");
                        await ResetXeniaEmulatorConfigurationFile(xs.UserSelection);
                        MessageBox.Show($"Configuration file for Xenia {xs.UserSelection} has been reset to default.");
                        break;
                }
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
