using System.IO;
using System.Windows;
using System.Windows.Input;

// Imported
using Octokit;
using XeniaManager.Core;
using XeniaManager.Core.Downloader;
using XeniaManager.Core.Installation;
using XeniaManager.Desktop.Components;
using XeniaManager.Desktop.Utilities;
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
        }

        private void UpdateUiVersions()
        {
            if (App.Settings.Emulator.Canary != null && App.Settings.Emulator.Canary.Version != null)
            {
                TblkCanary.Text = $"Xenia Canary: {App.Settings.Emulator.Canary.Version}";
                BtnInstallCanary.IsEnabled = false;
                BtnUninstallCanary.IsEnabled = true;
            }
            else
            {
                BtnInstallCanary.IsEnabled = true;
                BtnUninstallCanary.IsEnabled = false;
                BtnUpdateCanary.IsEnabled = false;
            }
            
            // TODO: Mousehook and Netplay
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
                    await downloadManager.DownloadFileAsync("https://raw.githubusercontent.com/mdqinc/SDL_GameControllerDB/master/gamecontrollerdb.txt", Path.Combine(Constants.BaseDir, Constants.Xenia.Canary.EmulatorDir ,"gamecontrollerdb.txt"));

                    Xenia.CanarySetup(App.Settings.Emulator, canaryRelease.TagName, canaryAsset.CreatedAt.UtcDateTime);
                }
                
                // Reset the ProgressBar and mouse
                PbDownloadProgress.Value = 0;
                Mouse.OverrideCursor = null;

                // Save changes
                App.AppSettings.SaveSettings();

                Logger.Info("Xenia Canary has been successfully installed.");
                await CustomMessageBox.Show("Success", "Xenia Canary has been successfully installed.");
            }
            catch (Exception exception)
            {
                Logger.Error(exception);
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
                catch {}
                await CustomMessageBox.Show(exception);
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
                throw new NotImplementedException();
            }
            catch (Exception exception)
            {
                Logger.Error(exception);
                await CustomMessageBox.Show(exception);
            }
            finally
            {
                UpdateUiVersions();
            }
        }
    }
}