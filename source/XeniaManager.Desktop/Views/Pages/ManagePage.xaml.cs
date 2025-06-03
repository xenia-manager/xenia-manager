using System.IO;
using System.Windows;
using Page = System.Windows.Controls.Page;
using System.Windows.Input;

// Imported Libraries
using Octokit;
using MessageBoxResult = Wpf.Ui.Controls.MessageBoxResult;
using Wpf.Ui.Controls;
using XeniaManager.Core;
using XeniaManager.Core.Downloader;
using XeniaManager.Core.Installation;
using XeniaManager.Desktop.Components;
using XeniaManager.Desktop.Utilities;
using EventManager = XeniaManager.Desktop.Utilities.EventManager;
using XeniaManager.Desktop.ViewModel.Pages;

namespace XeniaManager.Desktop.Views.Pages
{
    /// <summary>
    /// Interaction logic for ManagePage.xaml
    /// </summary>
    public partial class ManagePage : Page
    {
        #region Variables

        private ManagePageViewModel _viewModel { get; set; }

        #endregion

        #region Constructor

        public ManagePage()
        {
            InitializeComponent();
            _viewModel = new ManagePageViewModel();
            DataContext = _viewModel;
            //UpdateUi();
        }

        #endregion

        #region Functions & Events

        /// <summary>
        /// Installs the Xenia Canary when clicked
        /// </summary>
        private async void BtnInstallCanary_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                _viewModel.IsDownloading = true;
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
                    await downloadManager.DownloadAndExtractAsync(canaryAsset.BrowserDownloadUrl, "xenia.zip", Path.Combine(Constants.DirectoryPaths.Base, Constants.Xenia.Canary.EmulatorDir));

                    // Download "gamecontrollerdb.txt" for SDL Input System
                    Logger.Info("Downloading gamecontrollerdb.txt for SDL Input System");
                    await downloadManager.DownloadFileAsync("https://raw.githubusercontent.com/mdqinc/SDL_GameControllerDB/master/gamecontrollerdb.txt",
                        Path.Combine(Constants.DirectoryPaths.Base, Constants.Xenia.Canary.EmulatorDir, "gamecontrollerdb.txt"));

                    App.Settings.Emulator.Canary = Xenia.CanarySetup(canaryRelease.TagName, canaryRelease.CreatedAt.UtcDateTime);
                }

                // Reset the ProgressBar and mouse
                PbDownloadProgress.Value = 0;
                Mouse.OverrideCursor = null;

                // Save changes
                App.AppSettings.SaveSettings();
                _viewModel.IsDownloading = false;
                Logger.Info("Xenia Canary has been successfully installed.");
                await CustomMessageBox.Show(LocalizationHelper.GetUiText("MessageBox_Success"), LocalizationHelper.GetUiText("MessageBox_SuccessInstallXeniaCanaryText"));
            }
            catch (Exception ex)
            {
                Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
                PbDownloadProgress.Value = 0;
                Mouse.OverrideCursor = null;

                // Clean emulator folder
                try
                {
                    if (Directory.Exists(Path.Combine(Constants.DirectoryPaths.Base, Constants.Xenia.Canary.EmulatorDir)))
                    {
                        Directory.Delete(Path.Combine(Constants.DirectoryPaths.Base, Constants.Xenia.Canary.EmulatorDir), true);
                    }
                }
                catch
                {
                }

                await CustomMessageBox.Show(ex);
            }
            finally
            {
                _viewModel.UpdateEmulatorStatus();
                _viewModel.IsDownloading = false;
            }
        }

        /// <summary>
        /// Asks the user if he wants to uninstall Xenia Canary and uninstalls the Xenia Canary when clicked
        /// </summary>
        private async void BtnUninstallCanary_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MessageBoxResult result = await CustomMessageBox.YesNo(LocalizationHelper.GetUiText("MessageBox_DeleteXeniaCanaryTitle"),
                    LocalizationHelper.GetUiText("MessageBox_DeleteXeniaCanaryText"));

                if (result != MessageBoxResult.Primary)
                {
                    return;
                }

                App.Settings.Emulator.Canary = Xenia.Uninstall(XeniaVersion.Canary);
                App.AppSettings.SaveSettings(); // Save changes
                EventManager.RequestLibraryUiRefresh();
                _viewModel.UpdateEmulatorStatus();
                await CustomMessageBox.Show(LocalizationHelper.GetUiText("MessageBox_Success"),
                    LocalizationHelper.GetUiText("MessageBox_SuccessUninstallXeniaCanaryText"));
            }
            catch (Exception ex)
            {
                Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
                await CustomMessageBox.Show(ex);
            }
            finally
            {
                _viewModel.UpdateEmulatorStatus();
            }
        }

        /// <summary>
        /// Updates Xenia Canary to the latest release when clicked
        /// </summary>
        private async void BtnUpdateCanary_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                _viewModel.IsDownloading = true;
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
                    await downloadManager.DownloadAndExtractAsync(asset.BrowserDownloadUrl, "xenia.zip", Path.Combine(Constants.DirectoryPaths.Base, Constants.Xenia.Canary.EmulatorDir));

                    // Parsing the version
                    string? version = latestRelease.TagName;
                    if (string.IsNullOrEmpty(version))
                    {
                        Logger.Warning("Couldn't find the version for the latest release of Xenia Canary");
                    }

                    // Checking if we got proper version number
                    if (version?.Length != 7)
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
                _viewModel.IsDownloading = false;
                Logger.Info("Xenia Canary has been successfully updated.");
                await CustomMessageBox.Show(LocalizationHelper.GetUiText("MessageBox_Success"),
                    LocalizationHelper.GetUiText("MessageBox_SucessUpdateXeniaCanaryText"));
            }
            catch (Exception ex)
            {
                Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
                PbDownloadProgress.Value = 0;
                Mouse.OverrideCursor = null;

                // Clean emulator folder
                try
                {
                    if (Directory.Exists(Path.Combine(Constants.DirectoryPaths.Base, Constants.Xenia.Canary.EmulatorDir)))
                    {
                        Directory.Delete(Path.Combine(Constants.DirectoryPaths.Base, Constants.Xenia.Canary.EmulatorDir), true);
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
                _viewModel.UpdateEmulatorStatus();
                _viewModel.IsDownloading = false;
            }
        }

        #endregion
    }
}