using System.IO;
using System.Windows;
using System.Windows.Input;

// Imported
using Octokit;
using Serilog;
using Wpf.Ui.Controls;
using XeniaManager.Core;
using XeniaManager.Core.Downloader;
using XeniaManager.Core.Installation;
using XeniaManager.Desktop.Components;
using XeniaManager.Desktop.Utilities;
using MessageBoxResult = Wpf.Ui.Controls.MessageBoxResult;
using Page = System.Windows.Controls.Page;

namespace XeniaManager.Desktop.Views.Pages
{
    /// <summary>
    /// Interaction logic for ManagePage.xaml
    /// </summary>
    public partial class ManagePage : Page
    {
        public ManagePage()
        {
            InitializeComponent();

            UpdateUiVersions();
            CheckForUpdates();
        }

        private void UpdateUiVersions()
        {
            try
            {
                bool canaryInstalled = App.Settings.Emulator.Canary?.Version != null;
                if (canaryInstalled)
                {
                    TblkCanary.Text = $"Xenia Canary: {App.Settings.Emulator.Canary.Version}";
                }
                else
                {
                    TblkCanary.SetResourceReference(TextBlock.TextProperty, "ManagePage_XeniaCanaryNotInstalled");
                }

                BtnInstallCanary.IsEnabled = !canaryInstalled;
                BtnUninstallCanary.IsEnabled = canaryInstalled;

                // TODO: Mousehook and Netplay
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                CustomMessageBox.Show(ex);
            }
        }

        private async void CheckForUpdates()
        {
            try
            {
                // Xenia Canary
                // Checking if it's installed
                if (App.Settings.Emulator.Canary == null)
                {
                    BtnUpdateCanary.IsEnabled = false;
                }
                // If an update is already marked as available, enable the button.
                else if (App.Settings.Emulator.Canary.UpdateAvailable)
                {
                    BtnUpdateCanary.IsEnabled = true;
                }
                // Check if we need to do an update check
                else if ((DateTime.Now - App.Settings.Emulator.Canary.LastUpdateCheckDate).TotalDays >= 1)
                {
                    Logger.Info("Checking for Xenia Canary updates.");
                    (bool, Release) canaryUpdate = await Xenia.CheckForUpdates(App.Settings.Emulator.Canary, XeniaVersion.Canary);
                    BtnUpdateCanary.IsEnabled = canaryUpdate.Item1;
                }
                // In case we did every check, disable the Update button
                else
                {
                    BtnUpdateCanary.IsEnabled = false;
                }

                // TODO: Mousehook and Netplay

                App.AppSettings.SaveSettings();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                await CustomMessageBox.Show(ex);
            }
        }

        private async void BtnInstallCanary_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                // Fetch latest Xenia Canary release
                using (new WindowDisabler(this))
                {
                    Release canaryRelease = await Github.GetLatestRelease(XeniaVersion.Canary);
                    ReleaseAsset canaryAsset = canaryRelease.Assets.FirstOrDefault(a => a.Name.Contains("windows", StringComparison.OrdinalIgnoreCase));
                    if (canaryAsset == null)
                    {
                        throw new Exception("Windows build asset missing in the release");
                    }

                    Logger.Info("Downloading the latest Xenia Canary build");
                    DownloadManager downloadManager = new DownloadManager();
                    downloadManager.ProgressChanged += (progress) => { PbDownloadProgress.Value = progress; };

                    // Download Xenia Canary
                    await downloadManager.DownloadAndExtractAsync(canaryAsset.BrowserDownloadUrl, "xenia.zip", Path.Combine(Constants.BaseDir, Constants.Xenia.Canary.EmulatorDir));

                    // Download "gamecontrollerdb.txt" for SDL Input System
                    Logger.Info("Downloading gamecontrollerdb.txt for SDL Input System");
                    await downloadManager.DownloadFileAsync("https://raw.githubusercontent.com/mdqinc/SDL_GameControllerDB/master/gamecontrollerdb.txt", Path.Combine(Constants.BaseDir, Constants.Xenia.Canary.EmulatorDir, "gamecontrollerdb.txt"));

                    App.Settings.Emulator.Canary = Xenia.CanarySetup(canaryRelease.TagName, canaryRelease.CreatedAt.UtcDateTime);
                }

                // Reset the ProgressBar and mouse
                PbDownloadProgress.Value = 0;
                Mouse.OverrideCursor = null;

                // Save changes
                App.AppSettings.SaveSettings();

                Logger.Info("Xenia Canary has been successfully installed.");
                await CustomMessageBox.Show("Success", "Xenia Canary has been successfully installed.");
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                PbDownloadProgress.Value = 0;
                Mouse.OverrideCursor = null;

                // Clean emulator folder
                try
                {
                    if (Directory.Exists(Path.Combine(Constants.BaseDir, Constants.Xenia.Canary.EmulatorDir)))
                    {
                        Directory.Delete(Path.Combine(Constants.BaseDir, Constants.Xenia.Canary.EmulatorDir), true);
                    }
                }
                catch
                {
                }

                await CustomMessageBox.Show(ex);
            }
            finally
            {
                UpdateUiVersions();
            }
        }

        private async void BtnUninstallCanary_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MessageBoxResult result = await CustomMessageBox.YesNo("Uninstall Xenia Canary",
                    "Do you want to uninstall Xenia Canary?\nThis will remove all save files and updates alongside the emulator.");

                if (result != MessageBoxResult.Primary)
                {
                    return;
                }

                App.Settings.Emulator.Canary = Xenia.Uninstall(XeniaVersion.Canary);
                App.AppSettings.SaveSettings(); // Save changes
                await CustomMessageBox.Show("Success", "Xenia Canary has been successfully uninstalled.");
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                await CustomMessageBox.Show(ex);
            }
            finally
            {
                UpdateUiVersions();
            }
        }

        private async void BtnUpdateCanary_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                // Fetch latest Xenia Canary release
                using (new WindowDisabler(this))
                {
                    Release latestRelease = await Github.GetLatestRelease(XeniaVersion.Canary);
                    ReleaseAsset asset = latestRelease.Assets.FirstOrDefault(a => a.Name.Contains("windows", StringComparison.OrdinalIgnoreCase));
                    if (asset == null)
                    {
                        throw new Exception("Windows build asset missing in the release");
                    }

                    Logger.Info("Downloading the latest Xenia Canary build");
                    DownloadManager downloadManager = new DownloadManager();
                    downloadManager.ProgressChanged += (progress) => { PbDownloadProgress.Value = progress; };

                    // Download Xenia Canary
                    await downloadManager.DownloadAndExtractAsync(asset.BrowserDownloadUrl, "xenia.zip", Path.Combine(Constants.BaseDir, Constants.Xenia.Canary.EmulatorDir));
                    
                    // Parsing the version
                    string? version = latestRelease.TagName;
                    if (string.IsNullOrEmpty(version))
                    {
                        Logger.Warning("Couldn't find the version for the latest release of Xenia Canary");
                    }

                    // Checking if we got proper version number
                    if (version.Length != 7)
                    {
                        // Parsing version number from title
                        string releaseTitle = latestRelease.Name;
                        if (!string.IsNullOrEmpty(releaseTitle))
                        {
                            // Checking if the title has an underscore
                            if (releaseTitle.Contains('_'))
                            {
                                // Everything before the underscore is version number
                                version = releaseTitle.Substring(0, releaseTitle.IndexOf('_')); 
                            }
                            else if (releaseTitle.Length == 7)
                            {
                                version = releaseTitle;
                            }
                        }
                    }
                    
                    // Update the configuration file
                    App.Settings.Emulator.Canary.Version = version;
                    App.Settings.Emulator.Canary.ReleaseDate = latestRelease.CreatedAt.UtcDateTime;
                    App.Settings.Emulator.Canary.LastUpdateCheckDate = DateTime.Now;
                    App.Settings.Emulator.Canary.UpdateAvailable = false;
                }

                // Reset the ProgressBar and mouse
                PbDownloadProgress.Value = 0;
                Mouse.OverrideCursor = null;

                // Save changes
                App.AppSettings.SaveSettings();

                Logger.Info("Xenia Canary has been successfully installed.");
                await CustomMessageBox.Show("Success", "Xenia Canary has been successfully installed.");
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                PbDownloadProgress.Value = 0;
                Mouse.OverrideCursor = null;

                // Clean emulator folder
                try
                {
                    if (Directory.Exists(Path.Combine(Constants.BaseDir, Constants.Xenia.Canary.EmulatorDir)))
                    {
                        Directory.Delete(Path.Combine(Constants.BaseDir, Constants.Xenia.Canary.EmulatorDir), true);
                    }
                }
                catch
                {
                }

                await CustomMessageBox.Show(ex);
            }
            finally
            {
                BtnUpdateCanary.IsEnabled = false;
                UpdateUiVersions();
            }
        }
    }
}