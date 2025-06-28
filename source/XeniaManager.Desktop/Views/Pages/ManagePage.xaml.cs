using System.IO;
using System.Windows;
using System.Windows.Input;

// Imported Libraries
using Octokit;
using XeniaManager.Core;
using XeniaManager.Core.Downloader;
using XeniaManager.Core.Game;
using XeniaManager.Core.Installation;
using XeniaManager.Desktop.Components;
using XeniaManager.Desktop.Utilities;
using XeniaManager.Desktop.ViewModel.Pages;
using XeniaManager.Desktop.Views.Windows;
using EventManager = XeniaManager.Desktop.Utilities.EventManager;
using MessageBoxResult = Wpf.Ui.Controls.MessageBoxResult;
using Page = System.Windows.Controls.Page;

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
        }

        #endregion

        #region Functions & Events

        /// <summary>
        /// Installs the Xenia Canary when clicked
        /// </summary>
        private async void BtnInstallCanary_Click(object sender, RoutedEventArgs e)
        {
            if (App.Settings.Emulator.Settings.UnifiedContentFolder)
            {
                if (!Core.Utilities.IsRunAsAdministrator())
                {
                    await CustomMessageBox.Show(LocalizationHelper.GetUiText("MessageBox_Error"), LocalizationHelper.GetUiText("MessageBox_AdministratorRequiredText"));
                    return;
                }
            }
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

                    App.Settings.Emulator.Canary = Xenia.CanarySetup(canaryRelease.TagName, canaryRelease.CreatedAt.UtcDateTime, App.Settings.Emulator.Settings.UnifiedContentFolder);
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
            Mouse.OverrideCursor = Cursors.Wait;
            _viewModel.IsDownloading = true;
            Launcher.XeniaUpdating = true;
            try
            {
                using (new WindowDisabler(this))
                {
                    Progress<double> downloadProgress = new Progress<double>(progress => PbDownloadProgress.Value = progress);
                    bool sucess = await Xenia.UpdateCanary(App.Settings.Emulator.Canary, downloadProgress);
                    App.AppSettings.SaveSettings();
                    _viewModel.IsDownloading = false;
                    Mouse.OverrideCursor = null;
                    if (sucess)
                    {
                        BtnUpdateCanary.IsEnabled = false;
                        Logger.Info("Xenia Canary has been successfully updated.");
                        await CustomMessageBox.Show(LocalizationHelper.GetUiText("MessageBox_Success"), LocalizationHelper.GetUiText("MessageBox_SuccessUpdateXeniaCanaryText"));
                    }
                }
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
                Mouse.OverrideCursor = null;
                Launcher.XeniaUpdating = false;
            }
        }

        private async void BtnInstallMousehook_Click(object sender, RoutedEventArgs e)
        {
            if (App.Settings.Emulator.Settings.UnifiedContentFolder)
            {
                if (!Core.Utilities.IsRunAsAdministrator())
                {
                    await CustomMessageBox.Show(LocalizationHelper.GetUiText("MessageBox_Error"), LocalizationHelper.GetUiText("MessageBox_AdministratorRequiredText"));
                    return;
                }
            }
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                _viewModel.IsDownloading = true;
                // Fetch latest Xenia Canary release
                using (new WindowDisabler(this))
                {
                    Release latestRelease = await Github.GetLatestRelease(XeniaVersion.Mousehook);
                    ReleaseAsset? releaseAsset = latestRelease.Assets.FirstOrDefault();
                    if (releaseAsset == null)
                    {
                        throw new Exception("Mousehook asset missing in the release");
                    }
                    Logger.Info("Downloading the latest Xenia Mousehook build");
                    DownloadManager downloadManager = new DownloadManager();
                    downloadManager.ProgressChanged += (progress) => { PbDownloadProgress.Value = progress; };

                    // Download Xenia Mousehook
                    await downloadManager.DownloadAndExtractAsync(releaseAsset.BrowserDownloadUrl, "xenia.zip", Path.Combine(Constants.DirectoryPaths.Base, Constants.Xenia.Mousehook.EmulatorDir));

                    // Download "gamecontrollerdb.txt" for SDL Input System
                    Logger.Info("Downloading gamecontrollerdb.txt for SDL Input System");
                    await downloadManager.DownloadFileAsync("https://raw.githubusercontent.com/mdqinc/SDL_GameControllerDB/master/gamecontrollerdb.txt", Path.Combine(Constants.DirectoryPaths.Base, Constants.Xenia.Mousehook.EmulatorDir, "gamecontrollerdb.txt"));

                    App.Settings.Emulator.Mousehook = Xenia.MousehookSetup(latestRelease.TagName, latestRelease.CreatedAt.UtcDateTime, App.Settings.Emulator.Settings.UnifiedContentFolder);
                }

                // Reset the ProgressBar and mouse
                PbDownloadProgress.Value = 0;
                Mouse.OverrideCursor = null;

                // Save changes
                App.AppSettings.SaveSettings();
                _viewModel.IsDownloading = false;
                Logger.Info("Xenia Mousehook has been successfully installed.");
                await CustomMessageBox.Show(LocalizationHelper.GetUiText("MessageBox_Success"), LocalizationHelper.GetUiText("MessageBox_SuccessInstallXeniaMousehookText"));
            }
            catch (Exception ex)
            {
                Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
                PbDownloadProgress.Value = 0;
                Mouse.OverrideCursor = null;
                await CustomMessageBox.Show(ex);
            }
            finally
            {
                _viewModel.UpdateEmulatorStatus();
                _viewModel.IsDownloading = false;
            }
        }

        private async void BtnUpdateMousehook_Click(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            _viewModel.IsDownloading = true;
            Launcher.XeniaUpdating = true;
            try
            {
                using (new WindowDisabler(this))
                {
                    Progress<double> downloadProgress = new Progress<double>(progress => PbDownloadProgress.Value = progress);
                    bool sucess = await Xenia.UpdateMousehoook(App.Settings.Emulator.Mousehook, downloadProgress);
                    App.AppSettings.SaveSettings();
                    _viewModel.IsDownloading = false;
                    Mouse.OverrideCursor = null;
                    if (sucess)
                    {
                        BtnUpdateMousehook.IsEnabled = false;
                        Logger.Info("Xenia Mousehook has been successfully updated.");
                        await CustomMessageBox.Show(LocalizationHelper.GetUiText("MessageBox_Success"), LocalizationHelper.GetUiText("MessageBox_SuccessUpdateXeniaMousehookText"));
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
                PbDownloadProgress.Value = 0;
                Mouse.OverrideCursor = null;

                // Clean emulator folder
                try
                {
                    if (Directory.Exists(Path.Combine(Constants.DirectoryPaths.Base, Constants.Xenia.Mousehook.EmulatorDir)))
                    {
                        Directory.Delete(Path.Combine(Constants.DirectoryPaths.Base, Constants.Xenia.Mousehook.EmulatorDir), true);
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
                Mouse.OverrideCursor = null;
                Launcher.XeniaUpdating = false;
            }
        }

        private async void BtnUninstallMousehoook_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MessageBoxResult result = await CustomMessageBox.YesNo(LocalizationHelper.GetUiText("MessageBox_DeleteXeniaCanaryTitle"), LocalizationHelper.GetUiText("MessageBox_DeleteXeniaMousehookText"));

                if (result != MessageBoxResult.Primary)
                {
                    return;
                }

                App.Settings.Emulator.Mousehook = Xenia.Uninstall(XeniaVersion.Mousehook);
                App.AppSettings.SaveSettings(); // Save changes
                EventManager.RequestLibraryUiRefresh();
                _viewModel.UpdateEmulatorStatus();
                await CustomMessageBox.Show(LocalizationHelper.GetUiText("MessageBox_Success"), LocalizationHelper.GetUiText("MessageBox_SuccessUninstallXeniaMousehookText"));
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

        private void BtnExportLogs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Xenia.ExportLogs(XeniaVersion.Canary);
                CustomMessageBox.Show(LocalizationHelper.GetUiText("MessageBox_Success"), string.Format(LocalizationHelper.GetUiText("MessageBox_SuccessExportLogsText"), XeniaVersion.Canary));
            }
            catch (Exception ex)
            {
                Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
                CustomMessageBox.Show(ex);
            }
        }

        private async void BtnRedownloadXenia_Click(object sender, RoutedEventArgs e)
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
                }

                // Reset the ProgressBar and mouse
                PbDownloadProgress.Value = 0;
                Mouse.OverrideCursor = null;
                _viewModel.IsDownloading = false;

                Logger.Info("Xenia Canary has been successfully redownloaded");
                await CustomMessageBox.Show(LocalizationHelper.GetUiText("MessageBox_Success"), LocalizationHelper.GetUiText("MessageBox_SuccessReinstallXeniaCanaryText"));
            }
            catch (Exception ex)
            {
                Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
                PbDownloadProgress.Value = 0;
                Mouse.OverrideCursor = null;

                // Clean emulator folder
                try
                {
                    if (File.Exists(Path.Combine(Constants.DirectoryPaths.Downloads, "xenia.zip")))
                    {
                        File.Delete(Path.Combine(Constants.DirectoryPaths.Downloads, "xenia.zip"));
                    }
                }
                catch
                {
                }

                await CustomMessageBox.Show(ex);
            }
            finally
            {
                _viewModel.IsDownloading = false;
            }
        }

        private async void BtnUpdateSDLGameControllerDB_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                _viewModel.IsDownloading = true;
                // Fetch latest Xenia Canary release
                using (new WindowDisabler(this))
                {
                    DownloadManager downloadManager = new DownloadManager();
                    downloadManager.ProgressChanged += (progress) => { PbDownloadProgress.Value = progress; };
                    Logger.Info("Downloading gamecontrollerdb.txt for SDL Input System");
                    await downloadManager.DownloadFileAsync("https://raw.githubusercontent.com/mdqinc/SDL_GameControllerDB/master/gamecontrollerdb.txt",
                        Path.Combine(Constants.DirectoryPaths.Base, Constants.Xenia.Canary.EmulatorDir, "gamecontrollerdb.txt"));
                }

                // Reset the ProgressBar and mouse
                Mouse.OverrideCursor = null;
                Logger.Info("Successfully updated gamecontrollerdb.txt for SDL Input System for Xenia Canary");
                await CustomMessageBox.Show(LocalizationHelper.GetUiText("MessageBox_Success"), string.Format(LocalizationHelper.GetUiText("MessageBox_SuccessUpdateGameControllerDatabaseText"), XeniaVersion.Canary));
            }
            catch (Exception ex)
            {
                Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
                PbDownloadProgress.Value = 0;
                Mouse.OverrideCursor = null;
                await CustomMessageBox.Show(ex);
            }
            finally
            {
                PbDownloadProgress.Value = 0;
                _viewModel.IsDownloading = false;
            }
        }

        private void ChkUnifiedContent_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (App.Settings.Emulator.Settings.UnifiedContentFolder)
                {
                    if (!Core.Utilities.IsRunAsAdministrator())
                    {
                        CustomMessageBox.Show(LocalizationHelper.GetUiText("MessageBox_Error"), LocalizationHelper.GetUiText("MessageBox_AdministratorRequiredText"));
                        return;
                    }
                    try
                    {
                        Xenia.UnifyContentFolder(App.Settings.GetInstalledVersions(), App.Settings.SelectVersion(() =>
                        {
                            XeniaSelection xeniaSelection = new XeniaSelection();
                            xeniaSelection.ShowDialog();
                            return xeniaSelection.SelectedXenia as XeniaVersion?;
                        }));
                    }
                    catch (OperationCanceledException)
                    {
                        _viewModel.UnifiedContentFolder = false;
                        return;
                    }
                }
                else
                {
                    Xenia.SeparateContentFolder(App.Settings.GetInstalledVersions());
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
                CustomMessageBox.Show(ex);
                _viewModel.UnifiedContentFolder = false;
            }
        }

        #endregion
    }
}