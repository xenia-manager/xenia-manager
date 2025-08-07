using Page = System.Windows.Controls.Page;
using System.IO;
using System.Windows;
using System.Windows.Input;

// Imported Libraries
using Octokit;
using MessageBoxResult = Wpf.Ui.Controls.MessageBoxResult;
using XeniaManager.Core;
using XeniaManager.Core.Constants;
using XeniaManager.Core.Constants.Emulators;
using XeniaManager.Core.Downloader;
using XeniaManager.Core.Enum;
using XeniaManager.Core.Game;
using XeniaManager.Core.Installation;
using XeniaManager.Desktop.Components;
using XeniaManager.Desktop.Utilities;
using EventManager = XeniaManager.Desktop.Utilities.EventManager;
using XeniaManager.Desktop.ViewModel.Pages;
using XeniaManager.Desktop.Views.Windows;
using XeniaManager.Core.Utilities;

namespace XeniaManager.Desktop.Views.Pages
{
    /// <summary>
    /// Interaction logic for ManagePage.xaml
    /// </summary>
    public partial class ManagePage : Page
    {
        #region Variables
        public ManagePageViewModel ViewModel { get; set; }

        #endregion

        #region Constructor

        public ManagePage()
        {
            InitializeComponent();
            ViewModel = new ManagePageViewModel();
            DataContext = ViewModel;
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
                if (!Security.IsRunAsAdministrator())
                {
                    await CustomMessageBox.ShowAsync(LocalizationHelper.GetUiText("MessageBox_Error"), LocalizationHelper.GetUiText("MessageBox_UnifiedContentFolderAdministratorRequiredText"));
                    return;
                }
            }
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                ViewModel.IsDownloading = true;
                // Fetch latest Xenia Canary release
                using (new WindowDisabler(this))
                {
                    Release canaryRelease = await Github.GetLatestRelease(XeniaVersion.Canary);
                    ReleaseAsset? canaryAsset = canaryRelease.Assets.FirstOrDefault(a => a.Name.Contains("windows", StringComparison.OrdinalIgnoreCase));
                    if (canaryAsset == null)
                    {
                        throw new Exception("Windows build asset missing in the release");
                    }

                    Logger.Info("Downloading the latest Xenia Canary build");
                    DownloadManager downloadManager = new DownloadManager();
                    downloadManager.ProgressChanged += (progress) => { PbDownloadProgress.Value = progress; };

                    // Download Xenia Canary
                    await downloadManager.DownloadAndExtractAsync(canaryAsset.BrowserDownloadUrl, "xenia.zip", Path.Combine(DirectoryPaths.Base, XeniaCanary.EmulatorDir));

                    // Download "gamecontrollerdb.txt" for SDL Input System
                    try
                    {
                        Logger.Info("Downloading gamecontrollerdb.txt for SDL Input System");
                        await downloadManager.DownloadFileAsync("https://raw.githubusercontent.com/mdqinc/SDL_GameControllerDB/master/gamecontrollerdb.txt", Path.Combine(DirectoryPaths.Base, XeniaCanary.EmulatorDir, "gamecontrollerdb.txt"));
                    }
                    catch (Exception ex)
                    {
                        Logger.Warning($"Failed to download gamecontrollerdb.txt: {ex.Message}");
                        await CustomMessageBox.ShowAsync(LocalizationHelper.GetUiText("MessageBox_Warning"), LocalizationHelper.GetUiText("MessageBox_FailedGamecontrollerdbDownload"));
                    }

                    App.Settings.Emulator.Canary = Xenia.CanarySetup(canaryRelease.TagName, canaryRelease.CreatedAt.UtcDateTime, App.Settings.Emulator.Settings.UnifiedContentFolder);
                }

                // Reset the ProgressBar and mouse
                PbDownloadProgress.Value = 0;
                Mouse.OverrideCursor = null;

                // Save changes
                App.AppSettings.SaveSettings();
                ViewModel.IsDownloading = false;
                Logger.Info("Xenia Canary has been successfully installed.");
                await CustomMessageBox.ShowAsync(LocalizationHelper.GetUiText("MessageBox_Success"), string.Format(LocalizationHelper.GetUiText("MessageBox_SuccessInstallXeniaText"), XeniaVersion.Canary));
            }
            catch (Exception ex)
            {
                Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
                PbDownloadProgress.Value = 0;
                Mouse.OverrideCursor = null;

                // Clean emulator folder
                try
                {
                    if (Directory.Exists(Path.Combine(DirectoryPaths.Base, XeniaCanary.EmulatorDir)))
                    {
                        Directory.Delete(Path.Combine(DirectoryPaths.Base, XeniaCanary.EmulatorDir), true);
                    }
                }
                catch
                {
                }

                await CustomMessageBox.ShowAsync(ex);
            }
            finally
            {
                ViewModel.UpdateEmulatorStatus();
                ViewModel.IsDownloading = false;
                PbDownloadProgress.Value = 0;
            }
        }

        /// <summary>
        /// Asks the user if he wants to uninstall Xenia Canary and uninstalls the Xenia Canary when clicked
        /// </summary>
        private async void BtnUninstallCanary_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MessageBoxResult result = await CustomMessageBox.YesNoAsync(string.Format(LocalizationHelper.GetUiText("MessageBox_DeleteXeniaTitle"), XeniaVersion.Canary), string.Format(LocalizationHelper.GetUiText("MessageBox_DeleteXeniaText"), XeniaVersion.Canary));

                if (result != MessageBoxResult.Primary)
                {
                    return;
                }

                App.Settings.Emulator.Canary = Xenia.Uninstall(XeniaVersion.Canary);
                App.AppSettings.SaveSettings(); // Save changes
                EventManager.RequestLibraryUiRefresh();
                ViewModel.UpdateEmulatorStatus();
                await CustomMessageBox.ShowAsync(LocalizationHelper.GetUiText("MessageBox_Success"), string.Format(LocalizationHelper.GetUiText("MessageBox_SuccessUninstallXeniaText"), XeniaVersion.Canary));
            }
            catch (Exception ex)
            {
                Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
                await CustomMessageBox.ShowAsync(ex);
            }
            finally
            {
                ViewModel.UpdateEmulatorStatus();
            }
        }

        /// <summary>
        /// Updates Xenia Canary to the latest release when clicked
        /// </summary>
        private async void BtnUpdateCanary_Click(object sender, RoutedEventArgs e)
        {
            if (App.Settings.Emulator.Canary == null)
            {
                Logger.Error("Canary emulator information is null. Cannot proceed with the update.");
                await CustomMessageBox.ShowAsync(LocalizationHelper.GetUiText("MessageBox_Error"), string.Format(LocalizationHelper.GetUiText("MessageBox_MissingEmulatorInfo"), XeniaVersion.Canary));
                return;
            }

            Mouse.OverrideCursor = Cursors.Wait;
            ViewModel.IsDownloading = true;
            Launcher.XeniaUpdating = true;
            try
            {
                using (new WindowDisabler(this))
                {
                    Progress<double> downloadProgress = new Progress<double>(progress => PbDownloadProgress.Value = progress);
                    bool sucess = await Xenia.UpdateCanary(App.Settings.Emulator.Canary, downloadProgress);
                    App.AppSettings.SaveSettings();
                    ViewModel.IsDownloading = false;
                    Mouse.OverrideCursor = null;
                    if (sucess)
                    {
                        BtnUpdateCanary.IsEnabled = false;
                        Logger.Info("Xenia Canary has been successfully updated.");
                        await CustomMessageBox.ShowAsync(LocalizationHelper.GetUiText("MessageBox_Success"), string.Format(LocalizationHelper.GetUiText("MessageBox_SuccessUpdateXeniaText"), XeniaVersion.Canary));
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
                    if (Directory.Exists(Path.Combine(DirectoryPaths.Base, XeniaCanary.EmulatorDir)))
                    {
                        Directory.Delete(Path.Combine(DirectoryPaths.Base, XeniaCanary.EmulatorDir), true);
                    }
                }
                catch
                {
                }

                await CustomMessageBox.ShowAsync(ex);
            }
            finally
            {
                ViewModel.UpdateEmulatorStatus();
                ViewModel.IsDownloading = false;
                Mouse.OverrideCursor = null;
                Launcher.XeniaUpdating = false;
                PbDownloadProgress.Value = 0;
            }
        }

        private async void BtnInstallMousehook_Click(object sender, RoutedEventArgs e)
        {
            if (App.Settings.Emulator.Settings.UnifiedContentFolder)
            {
                if (!Security.IsRunAsAdministrator())
                {
                    await CustomMessageBox.ShowAsync(LocalizationHelper.GetUiText("MessageBox_Error"), LocalizationHelper.GetUiText("MessageBox_UnifiedContentFolderAdministratorRequiredText"));
                    return;
                }
            }
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                ViewModel.IsDownloading = true;
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
                    await downloadManager.DownloadAndExtractAsync(releaseAsset.BrowserDownloadUrl, "xenia.zip", Path.Combine(DirectoryPaths.Base, XeniaMousehook.EmulatorDir));

                    // Download "gamecontrollerdb.txt" for SDL Input System
                    try
                    {
                        Logger.Info("Downloading gamecontrollerdb.txt for SDL Input System");
                        await downloadManager.DownloadFileAsync("https://raw.githubusercontent.com/mdqinc/SDL_GameControllerDB/master/gamecontrollerdb.txt", Path.Combine(DirectoryPaths.Base, XeniaMousehook.EmulatorDir, "gamecontrollerdb.txt"));
                    }
                    catch (Exception ex)
                    {
                        Logger.Warning($"Failed to download gamecontrollerdb.txt: {ex.Message}");
                        await CustomMessageBox.ShowAsync(LocalizationHelper.GetUiText("MessageBox_Warning"), LocalizationHelper.GetUiText("MessageBox_FailedGamecontrollerdbDownload"));
                    }

                    App.Settings.Emulator.Mousehook = Xenia.MousehookSetup(latestRelease.TagName, latestRelease.CreatedAt.UtcDateTime, App.Settings.Emulator.Settings.UnifiedContentFolder);
                }

                // Reset the ProgressBar and mouse
                PbDownloadProgress.Value = 0;
                Mouse.OverrideCursor = null;

                // Save changes
                App.AppSettings.SaveSettings();
                ViewModel.IsDownloading = false;
                Logger.Info("Xenia Mousehook has been successfully installed.");
                await CustomMessageBox.ShowAsync(LocalizationHelper.GetUiText("MessageBox_Success"), string.Format(LocalizationHelper.GetUiText("MessageBox_SuccessInstallXeniaText"), XeniaVersion.Mousehook));
            }
            catch (Exception ex)
            {
                Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
                PbDownloadProgress.Value = 0;
                Mouse.OverrideCursor = null;
                await CustomMessageBox.ShowAsync(ex);
            }
            finally
            {
                ViewModel.UpdateEmulatorStatus();
                ViewModel.IsDownloading = false;
                PbDownloadProgress.Value = 0;
            }
        }

        private async void BtnUpdateMousehook_Click(object sender, RoutedEventArgs e)
        {
            if (App.Settings.Emulator.Mousehook == null)
            {
                Logger.Error("Mousehook emulator information is null. Cannot proceed with the update.");
                await CustomMessageBox.ShowAsync(LocalizationHelper.GetUiText("MessageBox_Error"), string.Format(LocalizationHelper.GetUiText("MessageBox_MissingEmulatorInfo"), XeniaVersion.Mousehook));
                return;
            }

            Mouse.OverrideCursor = Cursors.Wait;
            ViewModel.IsDownloading = true;
            Launcher.XeniaUpdating = true;
            try
            {
                using (new WindowDisabler(this))
                {
                    Progress<double> downloadProgress = new Progress<double>(progress => PbDownloadProgress.Value = progress);
                    bool sucess = await Xenia.UpdateMousehoook(App.Settings.Emulator.Mousehook, downloadProgress);
                    App.AppSettings.SaveSettings();
                    ViewModel.IsDownloading = false;
                    Mouse.OverrideCursor = null;
                    if (sucess)
                    {
                        BtnUpdateMousehook.IsEnabled = false;
                        Logger.Info("Xenia Mousehook has been successfully updated.");
                        await CustomMessageBox.ShowAsync(LocalizationHelper.GetUiText("MessageBox_Success"), string.Format(LocalizationHelper.GetUiText("MessageBox_SuccessUpdateXeniaText"), XeniaVersion.Mousehook));
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
                    if (Directory.Exists(Path.Combine(DirectoryPaths.Base, XeniaMousehook.EmulatorDir)))
                    {
                        Directory.Delete(Path.Combine(DirectoryPaths.Base, XeniaMousehook.EmulatorDir), true);
                    }
                }
                catch
                {
                }

                await CustomMessageBox.ShowAsync(ex);
            }
            finally
            {
                ViewModel.UpdateEmulatorStatus();
                ViewModel.IsDownloading = false;
                Mouse.OverrideCursor = null;
                Launcher.XeniaUpdating = false;
                PbDownloadProgress.Value = 0;
            }
        }

        private async void BtnUninstallMousehoook_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MessageBoxResult result = await CustomMessageBox.YesNoAsync(string.Format(LocalizationHelper.GetUiText("MessageBox_DeleteXeniaTitle"), XeniaVersion.Mousehook), string.Format(LocalizationHelper.GetUiText("MessageBox_DeleteXeniaText"), XeniaVersion.Mousehook));

                if (result != MessageBoxResult.Primary)
                {
                    return;
                }

                App.Settings.Emulator.Mousehook = Xenia.Uninstall(XeniaVersion.Mousehook);
                App.AppSettings.SaveSettings(); // Save changes
                EventManager.RequestLibraryUiRefresh();
                ViewModel.UpdateEmulatorStatus();
                await CustomMessageBox.ShowAsync(LocalizationHelper.GetUiText("MessageBox_Success"), string.Format(LocalizationHelper.GetUiText("MessageBox_SuccessUninstallXeniaText"), XeniaVersion.Mousehook));
            }
            catch (Exception ex)
            {
                Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
                await CustomMessageBox.ShowAsync(ex);
            }
            finally
            {
                ViewModel.UpdateEmulatorStatus();
            }
        }

        public async Task CheckForXeniaUpdates(XeniaVersion xeniaVersion)
        {
            switch (xeniaVersion)
            {
                case XeniaVersion.Canary:
                    // Check for Xenia Canary updates
                    if (App.Settings.Emulator.Canary != null)
                    {
                        Logger.Info("Checking for Xenia Canary updates.");
                        // Perform the actual update check against the repository
                        ViewModel.CanaryUpdate = await Xenia.CheckForUpdates(App.Settings.Emulator.Canary, XeniaVersion.Canary);
                    }
                    break;
                case XeniaVersion.Mousehook:
                    // Check for Xenia Mousehook updates
                    if (App.Settings.Emulator.Mousehook != null)
                    {
                        Logger.Info("Checking for Xenia Mousehook updates.");
                        // Perform the actual update check against the repository
                        ViewModel.MousehookUpdate = await Xenia.CheckForUpdates(App.Settings.Emulator.Mousehook, XeniaVersion.Mousehook);
                    }
                    break;
                case XeniaVersion.Netplay:
                    // Check for Xenia Netplay updates
                    if (App.Settings.Emulator.Netplay != null)
                    {
                        Logger.Info("Checking for Xenia Netplay updates.");
                        ViewModel.NetplayUpdate = await Xenia.CheckForUpdates(App.Settings.Emulator.Netplay, XeniaVersion.Netplay);
                    }
                    break;
                default:
                    throw new NotImplementedException($"Xenia {xeniaVersion} is not implemented");
            }
            // Persist any changes made during the update check process
            App.AppSettings.SaveSettings();
        }

        private async void ChkNetplayNightly_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = await CustomMessageBox.YesNoAsync(string.Format(LocalizationHelper.GetUiText("MessageBox_SwitchNightlyBuildTitle"), XeniaVersion.Netplay), string.Format(LocalizationHelper.GetUiText("MessageBox_SwitchNightlyBuildText"), XeniaVersion.Netplay));
            if (result != MessageBoxResult.Primary)
            {
                return;
            }

            try
            {
                await CheckForXeniaUpdates(XeniaVersion.Netplay);
            }
            catch (Exception ex)
            {
                Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
                await CustomMessageBox.ShowAsync(ex);
            }
        }

        private async void BtnInstallNetplay_Click(object sender, RoutedEventArgs e)
        {
            if (App.Settings.Emulator.Settings.UnifiedContentFolder)
            {
                if (!Security.IsRunAsAdministrator())
                {
                    await CustomMessageBox.ShowAsync(LocalizationHelper.GetUiText("MessageBox_Error"), LocalizationHelper.GetUiText("MessageBox_UnifiedContentFolderAdministratorRequiredText"));
                    return;
                }
            }
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                ViewModel.IsDownloading = true;
                // Fetch latest Xenia Netplay release
                using (new WindowDisabler(this))
                {
                    Release latestRelease = await Github.GetLatestRelease(XeniaVersion.Netplay);
                    ReleaseAsset? releaseAsset = latestRelease.Assets.FirstOrDefault(a => !a.Name.Contains("WSASendTo", StringComparison.OrdinalIgnoreCase));
                    if (releaseAsset == null)
                    {
                        throw new Exception("Mousehook asset missing in the release");
                    }
                    Logger.Info("Downloading the latest Xenia Netplay build");
                    DownloadManager downloadManager = new DownloadManager();
                    downloadManager.ProgressChanged += (progress) => { PbDownloadProgress.Value = progress; };

                    // Download Xenia Netplay
                    await downloadManager.DownloadAndExtractAsync(releaseAsset.BrowserDownloadUrl, "xenia.zip", Path.Combine(DirectoryPaths.Base, XeniaNetplay.EmulatorDir));

                    // Download "gamecontrollerdb.txt" for SDL Input System
                    try
                    {
                        Logger.Info("Downloading gamecontrollerdb.txt for SDL Input System");
                        await downloadManager.DownloadFileAsync("https://raw.githubusercontent.com/mdqinc/SDL_GameControllerDB/master/gamecontrollerdb.txt", Path.Combine(DirectoryPaths.Base, XeniaNetplay.EmulatorDir, "gamecontrollerdb.txt"));
                    }
                    catch (Exception ex)
                    {
                        Logger.Warning($"Failed to download gamecontrollerdb.txt: {ex.Message}");
                        await CustomMessageBox.ShowAsync(LocalizationHelper.GetUiText("MessageBox_Warning"), LocalizationHelper.GetUiText("MessageBox_FailedGamecontrollerdbDownload"));
                    }

                    App.Settings.Emulator.Netplay = Xenia.NetplaySetup(latestRelease.TagName, latestRelease.CreatedAt.UtcDateTime, App.Settings.Emulator.Settings.UnifiedContentFolder);
                }

                // Reset the ProgressBar and mouse
                PbDownloadProgress.Value = 0;
                Mouse.OverrideCursor = null;

                // Save changes
                App.AppSettings.SaveSettings();
                ViewModel.IsDownloading = false;
                Logger.Info("Xenia Netplay has been successfully installed.");
                await CustomMessageBox.ShowAsync(LocalizationHelper.GetUiText("MessageBox_Success"), string.Format(LocalizationHelper.GetUiText("MessageBox_SuccessInstallXeniaText"), XeniaVersion.Netplay));
            }
            catch (Exception ex)
            {
                Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
                PbDownloadProgress.Value = 0;
                Mouse.OverrideCursor = null;
                await CustomMessageBox.ShowAsync(ex);
            }
            finally
            {
                ViewModel.UpdateEmulatorStatus();
                ViewModel.IsDownloading = false;
                PbDownloadProgress.Value = 0;
            }
        }

        private async void BtnUpdateNetplay_Click(object sender, RoutedEventArgs e)
        {
            if (App.Settings.Emulator.Netplay == null)
            {
                Logger.Error("Netplay emulator information is null. Cannot proceed with the update.");
                await CustomMessageBox.ShowAsync(LocalizationHelper.GetUiText("MessageBox_Error"), string.Format(LocalizationHelper.GetUiText("MessageBox_MissingEmulatorInfo"), XeniaVersion.Netplay));
                return;
            }

            Mouse.OverrideCursor = Cursors.Wait;
            ViewModel.IsDownloading = true;
            Launcher.XeniaUpdating = true;
            try
            {
                using (new WindowDisabler(this))
                {
                    Progress<double> downloadProgress = new Progress<double>(progress => PbDownloadProgress.Value = progress);
                    bool sucess = await Xenia.UpdateNetplay(App.Settings.Emulator.Netplay, downloadProgress);
                    App.AppSettings.SaveSettings();
                    ViewModel.IsDownloading = false;
                    Mouse.OverrideCursor = null;
                    if (sucess)
                    {
                        BtnUpdateNetplay.IsEnabled = false;
                        Logger.Info("Xenia Netplay has been successfully updated.");
                        await CustomMessageBox.ShowAsync(LocalizationHelper.GetUiText("MessageBox_Success"), string.Format(LocalizationHelper.GetUiText("MessageBox_SuccessUpdateXeniaText"), XeniaVersion.Netplay));
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
                    if (Directory.Exists(Path.Combine(DirectoryPaths.Base, XeniaMousehook.EmulatorDir)))
                    {
                        Directory.Delete(Path.Combine(DirectoryPaths.Base, XeniaMousehook.EmulatorDir), true);
                    }
                }
                catch
                {
                }

                await CustomMessageBox.ShowAsync(ex);
            }
            finally
            {
                ViewModel.UpdateEmulatorStatus();
                ViewModel.IsDownloading = false;
                Mouse.OverrideCursor = null;
                Launcher.XeniaUpdating = false;
                PbDownloadProgress.Value = 0;
            }
        }

        private async void BtnUninstallNetplay_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MessageBoxResult result = await CustomMessageBox.YesNoAsync(string.Format(LocalizationHelper.GetUiText("MessageBox_DeleteXeniaTitle"), XeniaVersion.Netplay), string.Format(LocalizationHelper.GetUiText("MessageBox_DeleteXeniaText"), XeniaVersion.Netplay));

                if (result != MessageBoxResult.Primary)
                {
                    return;
                }

                App.Settings.Emulator.Netplay = Xenia.Uninstall(XeniaVersion.Netplay);
                App.AppSettings.SaveSettings(); // Save changes
                EventManager.RequestLibraryUiRefresh();
                ViewModel.UpdateEmulatorStatus();
                await CustomMessageBox.ShowAsync(LocalizationHelper.GetUiText("MessageBox_Success"), string.Format(LocalizationHelper.GetUiText("MessageBox_SuccessUninstallXeniaText"), XeniaVersion.Netplay));
            }
            catch (Exception ex)
            {
                Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
                await CustomMessageBox.ShowAsync(ex);
            }
            finally
            {
                ViewModel.UpdateEmulatorStatus();
            }
        }

        private void BtnExportLogs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                XeniaVersion xeniaVersion;
                switch (App.Settings.GetInstalledVersions().Count())
                {
                    case 0:
                        throw new Exception("No Xenia version installed.\nInstall Xenia before continuing.");
                    case 1:
                        xeniaVersion = App.Settings.GetInstalledVersions().First();
                        break;
                    default:
                        try
                        {
                            xeniaVersion = App.Settings.SelectVersion(() =>
                            {
                                XeniaSelection xeniaSelection = new XeniaSelection();
                                xeniaSelection.ShowDialog();
                                return xeniaSelection.SelectedXenia as XeniaVersion?;
                            });
                        }
                        catch (OperationCanceledException)
                        {
                            Logger.Debug("User canceled the Xenia selection dialog for exporting logs");
                            return;
                        }
                        break;
                }
                Xenia.ExportLogs(xeniaVersion);
                CustomMessageBox.Show(LocalizationHelper.GetUiText("MessageBox_Success"), string.Format(LocalizationHelper.GetUiText("MessageBox_SuccessExportLogsText"), xeniaVersion));
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
                XeniaVersion xeniaVersion;
                switch (App.Settings.GetInstalledVersions().Count())
                {
                    case 0:
                        throw new Exception("No Xenia version installed.\nInstall Xenia before continuing.");
                    case 1:
                        xeniaVersion = App.Settings.GetInstalledVersions().First();
                        break;
                    default:
                        try
                        {
                            xeniaVersion = App.Settings.SelectVersion(() =>
                            {
                                XeniaSelection xeniaSelection = new XeniaSelection();
                                xeniaSelection.ShowDialog();
                                return xeniaSelection.SelectedXenia as XeniaVersion?;
                            });
                        }
                        catch (OperationCanceledException)
                        {
                            Logger.Debug("User canceled the Xenia selection dialog for redownloading Xenia");
                            return;
                        }
                        break;
                }
                string emulatorDir = xeniaVersion switch
                {
                    XeniaVersion.Canary => Path.Combine(DirectoryPaths.Base, XeniaCanary.EmulatorDir),
                    XeniaVersion.Mousehook => Path.Combine(DirectoryPaths.Base, XeniaMousehook.EmulatorDir),
                    XeniaVersion.Netplay => Path.Combine(DirectoryPaths.Base, XeniaNetplay.EmulatorDir),
                    _ => throw new ArgumentOutOfRangeException(nameof(xeniaVersion), "Unsupported Xenia version")
                };
                Mouse.OverrideCursor = Cursors.Wait;
                ViewModel.IsDownloading = true;
                // Fetch latest Xenia release
                using (new WindowDisabler(this))
                {
                    Release xeniaRelease = await Github.GetLatestRelease(xeniaVersion);
                    ReleaseAsset? xeniaAsset = xeniaVersion switch
                    {
                        XeniaVersion.Canary => xeniaRelease.Assets.FirstOrDefault(a => a.Name.Contains("windows", StringComparison.OrdinalIgnoreCase)),
                        XeniaVersion.Mousehook => xeniaRelease.Assets.FirstOrDefault(),
                        _ => throw new ArgumentOutOfRangeException(nameof(xeniaVersion), "Unsupported Xenia version")
                    };
                    if (xeniaAsset == null)
                    {
                        throw new Exception("Windows build asset missing in the release");
                    }

                    Logger.Info($"Downloading the latest Xenia {xeniaVersion} build");
                    DownloadManager downloadManager = new DownloadManager();
                    downloadManager.ProgressChanged += (progress) => { PbDownloadProgress.Value = progress; };

                    // Download Xenia
                    await downloadManager.DownloadAndExtractAsync(xeniaAsset.BrowserDownloadUrl, "xenia.zip", emulatorDir);
                }

                // Reset the ProgressBar and mouse
                PbDownloadProgress.Value = 0;
                Mouse.OverrideCursor = null;
                ViewModel.IsDownloading = false;

                Logger.Info($"Xenia {xeniaVersion} has been successfully redownloaded");
                await CustomMessageBox.ShowAsync(LocalizationHelper.GetUiText("MessageBox_Success"), string.Format(LocalizationHelper.GetUiText("MessageBox_SuccessReinstallXeniaText"), xeniaVersion));
            }
            catch (Exception ex)
            {
                Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
                PbDownloadProgress.Value = 0;
                Mouse.OverrideCursor = null;

                // Clean emulator folder
                try
                {
                    if (File.Exists(Path.Combine(DirectoryPaths.Downloads, "xenia.zip")))
                    {
                        File.Delete(Path.Combine(DirectoryPaths.Downloads, "xenia.zip"));
                    }
                }
                catch
                {
                }

                await CustomMessageBox.ShowAsync(ex);
            }
            finally
            {
                ViewModel.IsDownloading = false;
                PbDownloadProgress.Value = 0;
            }
        }

        private async void BtnUpdateSDLGameControllerDB_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                XeniaVersion xeniaVersion;
                switch (App.Settings.GetInstalledVersions().Count())
                {
                    case 0:
                        throw new Exception("No Xenia version installed.\nInstall Xenia before continuing.");
                    case 1:
                        xeniaVersion = App.Settings.GetInstalledVersions().First();
                        break;
                    default:
                        try
                        {
                            xeniaVersion = App.Settings.SelectVersion(() =>
                            {
                                XeniaSelection xeniaSelection = new XeniaSelection();
                                xeniaSelection.ShowDialog();
                                return xeniaSelection.SelectedXenia as XeniaVersion?;
                            });
                        }
                        catch (OperationCanceledException)
                        {
                            Logger.Debug("User canceled the Xenia selection dialog for redownloading Xenia");
                            return;
                        }
                        break;
                }
                string emulatorDir = xeniaVersion switch
                {
                    XeniaVersion.Canary => Path.Combine(DirectoryPaths.Base, XeniaCanary.EmulatorDir),
                    XeniaVersion.Mousehook => Path.Combine(DirectoryPaths.Base, XeniaMousehook.EmulatorDir),
                    XeniaVersion.Netplay => Path.Combine(DirectoryPaths.Base, XeniaNetplay.EmulatorDir),
                    _ => throw new ArgumentOutOfRangeException(nameof(xeniaVersion), "Unsupported Xenia version")
                };
                Mouse.OverrideCursor = Cursors.Wait;
                ViewModel.IsDownloading = true;
                // Fetch latest Xenia release
                using (new WindowDisabler(this))
                {
                    DownloadManager downloadManager = new DownloadManager();
                    downloadManager.ProgressChanged += (progress) => { PbDownloadProgress.Value = progress; };
                    Logger.Info("Downloading gamecontrollerdb.txt for SDL Input System");
                    await downloadManager.DownloadFileAsync("https://raw.githubusercontent.com/mdqinc/SDL_GameControllerDB/master/gamecontrollerdb.txt", Path.Combine(emulatorDir, "gamecontrollerdb.txt"));
                }

                // Reset the ProgressBar and mouse
                Mouse.OverrideCursor = null;
                Logger.Info($"Successfully updated gamecontrollerdb.txt for SDL Input System for Xenia {xeniaVersion}");
                await CustomMessageBox.ShowAsync(LocalizationHelper.GetUiText("MessageBox_Success"), string.Format(LocalizationHelper.GetUiText("MessageBox_SuccessUpdateGameControllerDatabaseText"), xeniaVersion));
            }
            catch (Exception ex)
            {
                Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
                PbDownloadProgress.Value = 0;
                Mouse.OverrideCursor = null;
                await CustomMessageBox.ShowAsync(ex);
            }
            finally
            {
                PbDownloadProgress.Value = 0;
                ViewModel.IsDownloading = false;
            }
        }

        private async void BtnRedownloadMousehookBindings_Click(object sender, RoutedEventArgs e)
        {
            if (App.Settings.Emulator.Mousehook == null)
            {
                Logger.Error("Mousehook emulator information is null. Cannot proceed with redownloading bindings.");
                return;
            }
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                ViewModel.IsDownloading = true;
                // Fetch latest Xenia release
                using (new WindowDisabler(this))
                {
                    DownloadManager downloadManager = new DownloadManager();
                    downloadManager.ProgressChanged += (progress) => { PbDownloadProgress.Value = progress; };
                    Logger.Info("Downloading bindings.ini for Xenia Mousehook");
                    await downloadManager.DownloadFileAsync(Urls.MousehookBindings, Path.Combine(DirectoryPaths.Base, XeniaMousehook.BindingsLocation));
                }

                // Reset the ProgressBar and mouse
                Mouse.OverrideCursor = null;
                PbDownloadProgress.Value = 0;
                Logger.Info($"Successfully redownloaded Xenia Mousehook Bindings.");
                await CustomMessageBox.ShowAsync(LocalizationHelper.GetUiText("MessageBox_Success"), LocalizationHelper.GetUiText("MessageBox_SuccessRedownloadMousehookBindingsText"));
            }
            catch (Exception ex)
            {
                Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
                PbDownloadProgress.Value = 0;
                Mouse.OverrideCursor = null;
                await CustomMessageBox.ShowAsync(ex);
            }
            finally
            {
                PbDownloadProgress.Value = 0;
                ViewModel.IsDownloading = false;
            }
        }

        private void ChkUnifiedContent_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (App.Settings.Emulator.Settings.UnifiedContentFolder)
                {
                    if (!Security.IsRunAsAdministrator())
                    {
                        ViewModel.UnifiedContentFolder = false;
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
                        ViewModel.UnifiedContentFolder = false;
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
                ViewModel.UnifiedContentFolder = false;
            }
        }

        #endregion
    }
}