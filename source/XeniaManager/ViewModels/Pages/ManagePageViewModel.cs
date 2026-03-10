using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using XeniaManager.Controls;
using XeniaManager.Core.Constants;
using XeniaManager.Core.Installation;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Manage;
using XeniaManager.Core.Models;
using XeniaManager.Core.Models.Files.Account;
using XeniaManager.Core.Services;
using XeniaManager.Core.Settings;
using XeniaManager.Core.Utilities;
using XeniaManager.Core.Utilities.Paths;
using XeniaManager.Services;

namespace XeniaManager.ViewModels.Pages;

public partial class ManagePageViewModel : ViewModelBase
{
    // Variables
    private Settings _settings { get; set; }
    private IMessageBoxService _messageBoxService { get; set; }
    private IReleaseService _releaseService { get; set; }
    private LibraryPageViewModel _libraryPageViewModel { get; set; }

    // Download Progress Card
    [ObservableProperty] private int downloadProgress;
    [ObservableProperty] private bool isDownloading = false;
    [ObservableProperty] private string downloadProgressStatus = string.Empty;

    // Xenia Canary
    [ObservableProperty] private bool canaryInstalled;
    [ObservableProperty] private string canaryVersion = string.Empty;
    [ObservableProperty] private bool canaryInstall;
    [ObservableProperty] private bool canaryUninstall;
    [ObservableProperty] private bool canaryUpdate;
    [ObservableProperty] private bool canaryCheckForUpdates;

    // Emulator Settings
    [ObservableProperty] private bool isXeniaInstalled;
    [ObservableProperty] private bool automaticSaveBackup;
    partial void OnAutomaticSaveBackupChanged(bool value)
    {
        _settings.Settings.Emulator.Settings.Profile.AutomaticSaveBackup = value;
        _settings.SaveSettings();
    }

    // Profiles
    [ObservableProperty] private ObservableCollection<ProfileDisplayInfo> profiles = [];
    [ObservableProperty] private ProfileDisplayInfo? selectedProfile;
    partial void OnSelectedProfileChanged(ProfileDisplayInfo? value)
    {
        if (value != null)
        {
            _settings.Settings.Emulator.Settings.Profile.ProfileXuid = value.DisplayXuid.ToString();
            _settings.SaveSettings();
        }
    }

    [ObservableProperty] private bool unifiedContentFolder;
    [ObservableProperty] private bool isAdministrator;

    // Constructor
    public ManagePageViewModel()
    {
        _settings = App.Services.GetRequiredService<Settings>();
        _messageBoxService = App.Services.GetRequiredService<IMessageBoxService>();
        _releaseService = App.Services.GetRequiredService<IReleaseService>();
        _libraryPageViewModel = App.Services.GetRequiredService<LibraryPageViewModel>();
        IsAdministrator = SecurityUtilities.IsRunAsAdministrator();
        AutomaticSaveBackup = _settings.Settings.Emulator.Settings.Profile.AutomaticSaveBackup;
        UnifiedContentFolder = _settings.Settings.Emulator.Settings.UnifiedContentFolder;
        UpdateEmulatorStatus();
        LoadAllProfiles();
    }

    // Functions
    public void UpdateEmulatorStatus()
    {
        // Xenia Canary
        CanaryInstalled = _settings.Settings.Emulator.Canary != null;
        if (_settings.Settings.Emulator.Canary != null)
        {
            CanaryVersion = CanaryInstalled ? _settings.Settings.Emulator.Canary.Version : LocalizationHelper.GetText("ManagePage.Emulator.Manage.Xenia.NotInstalled");
            Logger.Info<ManagePageViewModel>($"Xenia Canary is installed ({_settings.Settings.Emulator.Canary.Version})");
        }
        else
        {
            CanaryVersion = LocalizationHelper.GetText("ManagePage.Emulator.Manage.Xenia.NotInstalled");
            Logger.Info<ManagePageViewModel>("Xenia Canary is not installed");
        }
        CanaryInstall = !CanaryInstalled;
        CanaryUpdate = _settings.Settings.Emulator.Canary is { UpdateAvailable: true };
        CanaryUninstall = CanaryInstalled;
        CanaryCheckForUpdates = CanaryInstalled && !CanaryUpdate;

        IsXeniaInstalled = _settings.GetInstalledVersions(_settings).Count > 0;
    }

    /// <summary>
    /// Loads all profiles from all installed Xenia versions and populates the Profiles collection.
    /// Also selects the profile based on the saved ProfileXuid setting.
    /// If UnifiedContentFolder is enabled, only loads from one version since all versions share the same folder.
    /// </summary>
    public void LoadAllProfiles()
    {
        Profiles.Clear();
        List<XeniaVersion> installedVersions = _settings.GetInstalledVersions(_settings);

        if (installedVersions.Count == 0)
        {
            Logger.Warning<ManagePageViewModel>("No Xenia versions installed, cannot load profiles");
            return;
        }

        Logger.Info<ManagePageViewModel>($"Loading profiles from {installedVersions.Count} installed Xenia version(s)");

        // Use a HashSet to track unique profiles by XUID to avoid duplicates
        HashSet<string> seenXuids = [];
        ProfileDisplayInfo? profileToSelect = null;
        string savedProfileXuid = _settings.Settings.Emulator.Settings.Profile.ProfileXuid;

        // If UnifiedContentFolder is enabled, only load from one version since all versions share the same folder
        List<XeniaVersion> versionsToLoad = UnifiedContentFolder
            ? [installedVersions.First()]
            : installedVersions;

        Logger.Debug<ManagePageViewModel>($"UnifiedContentFolder: {UnifiedContentFolder}. Loading from {versionsToLoad.Count} version(s)");

        foreach (XeniaVersion version in versionsToLoad)
        {
            try
            {
                List<AccountInfo> accounts = ProfileManager.LoadProfiles(version);
                Logger.Debug<ManagePageViewModel>($"Loaded {accounts.Count} profiles from {version}");

                foreach (AccountInfo account in accounts)
                {
                    if (account.PathXuid == 0 && account.Xuid == 0)
                    {
                        Logger.Debug<ManagePageViewModel>($"Skipping content folder ({account.PathXuid}, {account.Xuid})");
                        continue;
                    }
                    // Skip if we've already seen this XUID
                    string xuidKey = account.PathXuid.ToString() ?? account.Xuid.ToString();
                    if (seenXuids.Contains(xuidKey))
                    {
                        Logger.Debug<ManagePageViewModel>($"Skipping duplicate profile: {account.Gamertag} ({xuidKey})");
                        continue;
                    }

                    ProfileDisplayInfo profileInfo = new ProfileDisplayInfo(
                        account.Gamertag,
                        account.PathXuid,
                        account.Xuid
                    );
                    Profiles.Add(profileInfo);
                    Logger.Info<ManagePageViewModel>($"Added profile: {account.Gamertag} ({account.Xuid}) from {version}");

                    // Check if this is the profile we should select
                    if (!string.IsNullOrEmpty(savedProfileXuid) && xuidKey == savedProfileXuid)
                    {
                        profileToSelect = profileInfo;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error<ManagePageViewModel>($"Failed to load profiles for {version}");
                Logger.LogExceptionDetails<ManagePageViewModel>(ex);
            }
        }

        // Set the selected profile (will be null if savedProfileXuid is empty/invalid)
        SelectedProfile = profileToSelect;
        Logger.Info<ManagePageViewModel>($"Loaded {Profiles.Count} unique profiles total. Selected: {SelectedProfile?.Gamertag ?? "None"}");
    }

    [RelayCommand]
    private async Task InstallCanary()
    {
        if (_settings.Settings.Emulator.Settings.UnifiedContentFolder)
        {
            // Check for administration permissions before continuing because of SymbolicLink
            if (!OperatingSystem.IsLinux() && !SecurityUtilities.IsRunAsAdministrator())
            {
                return;
            }
        }

        DownloadManager downloadManager = new DownloadManager();
        downloadManager.ProgressChanged += progress => { DownloadProgress = progress; };

        try
        {
            EventManager.Instance.DisableWindow();
            IsDownloading = true;

            // Fetching the emulator
            CachedBuild? releaseBuild = await _releaseService.GetCachedBuildAsync(ReleaseType.XeniaCanary);
            if (releaseBuild == null)
            {
                throw new Exception("Failed to fetch Xenia Canary build information");
            }

            // Download the emulator
            DownloadProgressStatus = string.Format(LocalizationHelper.GetText("ManagePage.Emulator.Manage.Xenia.Downloading"), "Canary");
            await downloadManager.DownloadFileAsync(releaseBuild.Url, "xenia.zip");

            // Extract the emulator
            await ArchiveExtractor.ExtractArchiveAsync(Path.Combine(downloadManager.DownloadPath, "xenia.zip"),
                AppPathResolver.GetFullPath(XeniaPaths.Canary.EmulatorDir));

            // Download "gamecontrollerdb.txt" for SDL Input System
            try
            {
                DownloadProgressStatus = LocalizationHelper.GetText("ManagePage.Emulator.Manage.SDL.Downloading");
                await downloadManager.DownloadFileFromMultipleUrlsAsync(Urls.GameControllerDatabase, "gamecontrollerdb.txt");

                // Move the file to the emulator directory
                File.Move(Path.Combine(downloadManager.DownloadPath, "gamecontrollerdb.txt"),
                    Path.Combine(AppPathResolver.GetFullPath(XeniaPaths.Canary.EmulatorDir), "gamecontrollerdb.txt"));
            }
            catch (Exception)
            {
                Logger.Warning<ManagePageViewModel>("Failed to download gamecontrollerdb.txt (Skipping)");
            }

            // Set up the emulator
            _settings.Settings.Emulator.Canary = XeniaService.SetupEmulator(XeniaVersion.Canary, releaseBuild.TagName,
                _settings.Settings.Emulator.Settings.UnifiedContentFolder);
            await _settings.SaveSettingsAsync();

            IsDownloading = false;
            EventManager.Instance.EnableWindow();
            await _messageBoxService.ShowInfoAsync(LocalizationHelper.GetText("ManagePage.Emulator.Manage.Xenia.Install.Success.Title"),
                string.Format(LocalizationHelper.GetText("ManagePage.Emulator.Manage.Xenia.Install.Success.Message"), "Canary"));
        }
        catch (Exception ex)
        {
            Logger.Error<ManagePageViewModel>("Failed to install Xenia Canary");
            Logger.LogExceptionDetails<ManagePageViewModel>(ex);
            IsDownloading = false;
            EventManager.Instance.EnableWindow();
            await _messageBoxService.ShowErrorAsync(string.Format(LocalizationHelper.GetText("ManagePage.Emulator.Manage.Xenia.Install.Failure.Title"), "Canary"),
                string.Format(LocalizationHelper.GetText("ManagePage.Emulator.Manage.Xenia.Install.Failure.Message"), "Canary", ex));
        }
        finally
        {
            UpdateEmulatorStatus();
            LoadAllProfiles();
            downloadManager.Dispose();
            Directory.Delete(downloadManager.DownloadPath, true);
            Logger.Info<ManagePageViewModel>("Cleanup completed");
            EventManager.Instance.EnableWindow();
        }
    }

    [RelayCommand]
    private async Task UpdateCanary()
    {
        DownloadManager downloadManager = new DownloadManager();
        downloadManager.ProgressChanged += progress => { DownloadProgress = progress; };
        try
        {
            EventManager.Instance.DisableWindow();
            IsDownloading = true;

            // Fetching the emulator
            CachedBuild? releaseBuild = await _releaseService.GetCachedBuildAsync(ReleaseType.XeniaCanary);
            if (releaseBuild == null)
            {
                throw new Exception("Failed to fetch Xenia Canary build information");
            }

            // Download the emulator
            DownloadProgressStatus = string.Format(LocalizationHelper.GetText("ManagePage.Emulator.Manage.Xenia.Downloading"), "Canary");
            await downloadManager.DownloadFileAsync(releaseBuild.Url, "xenia.zip");

            // Extract the emulator
            await ArchiveExtractor.ExtractArchiveAsync(Path.Combine(downloadManager.DownloadPath, "xenia.zip"),
                AppPathResolver.GetFullPath(XeniaPaths.Canary.EmulatorDir));

            // Update Emulator Details
            XeniaService.UpdateEmulator(_settings.Settings.Emulator.Canary, XeniaVersion.Canary, releaseBuild.TagName);
            await _settings.SaveSettingsAsync();

            IsDownloading = false;
            EventManager.Instance.EnableWindow();
            await _messageBoxService.ShowInfoAsync(LocalizationHelper.GetText("ManagePage.Emulator.Manage.Xenia.Update.Success.Title"),
                string.Format(LocalizationHelper.GetText("ManagePage.Emulator.Manage.Xenia.Update.Success.Message"), "Canary", releaseBuild.TagName));
        }
        catch (Exception ex)
        {
            Logger.Error<ManagePageViewModel>("Failed to update Xenia Canary");
            Logger.LogExceptionDetails<ManagePageViewModel>(ex);
            IsDownloading = false;
            EventManager.Instance.EnableWindow();
            await _messageBoxService.ShowErrorAsync(string.Format(LocalizationHelper.GetText("ManagePage.Emulator.Manage.Xenia.Update.Failure.Title"), "Canary"),
                string.Format(LocalizationHelper.GetText("ManagePage.Emulator.Manage.Xenia.Update.Failure.Message"), "Canary", ex));
        }
        finally
        {
            UpdateEmulatorStatus();
            downloadManager.Dispose();
            Directory.Delete(downloadManager.DownloadPath, true);
            Logger.Info<ManagePageViewModel>("Cleanup completed");
            EventManager.Instance.EnableWindow();
        }
    }

    [RelayCommand]
    private async Task CheckForUpdates()
    {
        Logger.Info<ManagePageViewModel>("Checking for Xenia Canary updates");
        try
        {
            EventManager.Instance.DisableWindow();

            if (_settings.Settings.Emulator.Canary == null)
            {
                throw new Exception("Xenia Canary is not installed");
            }

            // Check for updates with forced cache refresh
            (bool isUpdateAvailable, string latestVersion) = await XeniaService.CheckForUpdatesAsync(_releaseService, _settings.Settings.Emulator.Canary,
                ReleaseType.XeniaCanary, forceRefresh: true);

            if (isUpdateAvailable)
            {
                string currentVersion = _settings.Settings.Emulator.Canary.Version;

                // Update the settings to mark the update as available
                _settings.Settings.Emulator.Canary.UpdateAvailable = true;
                await _settings.SaveSettingsAsync();
                UpdateEmulatorStatus();

                Logger.Info<ManagePageViewModel>($"New update available: {currentVersion} -> {latestVersion}");
                EventManager.Instance.EnableWindow();
                await _messageBoxService.ShowInfoAsync(LocalizationHelper.GetText("ManagePage.Emulator.Manage.Xenia.UpdateAvailable.Title"),
                    string.Format(LocalizationHelper.GetText("ManagePage.Emulator.Manage.Xenia.UpdateAvailable.Message"), currentVersion, latestVersion));
            }
            else
            {
                Logger.Info<ManagePageViewModel>("Xenia Canary is up to date");
                EventManager.Instance.EnableWindow();
                await _messageBoxService.ShowInfoAsync(LocalizationHelper.GetText("ManagePage.Emulator.Manage.Xenia.UpToDate.Title"),
                    LocalizationHelper.GetText("ManagePage.Emulator.Manage.Xenia.UpToDate.Message"));
            }
        }
        catch (Exception ex)
        {
            Logger.Error<ManagePageViewModel>("Failed to check for updates");
            Logger.LogExceptionDetails<ManagePageViewModel>(ex);
            await _messageBoxService.ShowErrorAsync(LocalizationHelper.GetText("ManagePage.Emulator.Manage.Xenia.CheckUpdateFailed.Title"),
                string.Format(LocalizationHelper.GetText("ManagePage.Emulator.Manage.Xenia.CheckUpdateFailed.Message"), ex));
        }
        finally
        {
            EventManager.Instance.EnableWindow();
        }
    }

    [RelayCommand]
    private async Task UninstallCanary()
    {
        Logger.Info<ManagePageViewModel>("Starting uninstallation of Xenia Canary");
        bool completedUninstallation = true;
        try
        {
            Logger.Debug<ManagePageViewModel>("Showing uninstall confirmation dialog to user");
            bool result = await _messageBoxService.ShowConfirmationAsync(LocalizationHelper.GetText("ManagePage.Emulator.Manage.Xenia.Uninstall.Confirmation.Title"),
                string.Format(LocalizationHelper.GetText("ManagePage.Emulator.Manage.Xenia.Uninstall.Confirmation.Message"), "Canary"));
            if (!result)
            {
                Logger.Info<ManagePageViewModel>("Uninstall cancelled by the user");
                return;
            }

            Logger.Info<ManagePageViewModel>("User confirmed uninstallation, disabling window");
            EventManager.Instance.DisableWindow();

            Logger.Info<ManagePageViewModel>("Initiating Xenia Canary uninstallation process");
            _settings.Settings.Emulator.Canary = XeniaService.UninstallEmulator(XeniaVersion.Canary);

            Logger.Debug<ManagePageViewModel>("Saving updated settings after uninstallation");
            await _settings.SaveSettingsAsync();

            Logger.Debug<ManagePageViewModel>("Updating emulator status after uninstallation");
            UpdateEmulatorStatus();

            Logger.Debug<ManagePageViewModel>($"Refreshing game library to reflect Xenia {XeniaVersion.Canary} removal");
            _libraryPageViewModel.RefreshLibrary();

            Logger.Debug<ManagePageViewModel>("Re-enabling window after uninstallation");
            EventManager.Instance.EnableWindow();

            Logger.Info<ManagePageViewModel>("Showing success message to user");
            await _messageBoxService.ShowInfoAsync(LocalizationHelper.GetText("ManagePage.Emulator.Manage.Xenia.Uninstall.Success.Title"),
                string.Format(LocalizationHelper.GetText("ManagePage.Emulator.Manage.Xenia.Uninstall.Success.Message"), "Canary"));

            Logger.Info<ManagePageViewModel>("Xenia Canary uninstallation completed successfully");
        }
        catch (Exception ex)
        {
            Logger.Error<ManagePageViewModel>("Failed to uninstall Xenia Canary");
            Logger.LogExceptionDetails<ManagePageViewModel>(ex);
            EventManager.Instance.EnableWindow();
            completedUninstallation = false;
            await _messageBoxService.ShowErrorAsync(string.Format(LocalizationHelper.GetText("ManagePage.Emulator.Manage.Xenia.Uninstall.Failure.Title"), "Canary"),
                string.Format(LocalizationHelper.GetText("ManagePage.Emulator.Manage.Xenia.Uninstall.Failure.Message"), "Canary", ex));
        }
        finally
        {
            if (!completedUninstallation)
            {
                Logger.Debug<ManagePageViewModel>("Running cleanup in finally block");
                UpdateEmulatorStatus();
                EventManager.Instance.EnableWindow();
            }
        }
    }

    [RelayCommand]
    private async Task InstallContent()
    {
        try
        {
            Logger.Info<ManagePageViewModel>("Initializing content installation");

            // Get all installed Xenia versions
            List<XeniaVersion> installedVersions = _settings.GetInstalledVersions(_settings);

            if (installedVersions.Count == 0)
            {
                Logger.Warning<ManagePageViewModel>("No Xenia versions installed");
                await _messageBoxService.ShowWarningAsync(
                    LocalizationHelper.GetText("ManagePage.Content.Install.NoEmulator.Title"),
                    LocalizationHelper.GetText("ManagePage.Content.Install.NoEmulator.Message"));
                return;
            }

            XeniaVersion selectedVersion;

            // If Unified Content Folder is enabled, it doesn't matter what Xenia Version will be used since it will install for all the Xenia versions
            if (UnifiedContentFolder)
            {
                selectedVersion = installedVersions.First();
                Logger.Info<ManagePageViewModel>($"Using Unified Content Folder. Selected: {selectedVersion}");
            }
            // Unified Content Folder is disabled and the user installed more than 1 Xenia version
            else if (installedVersions.Count > 1)
            {
                Logger.Info<ManagePageViewModel>($"Multiple Xenia versions detected: {installedVersions.Count}");
                XeniaVersion? chosen = await XeniaSelectionDialog.ShowAsync(installedVersions);

                if (chosen == null)
                {
                    Logger.Info<ManagePageViewModel>("User canceled Xenia version selection");
                    return;
                }

                selectedVersion = chosen.Value;
                Logger.Info<ManagePageViewModel>($"User selected Xenia version: {selectedVersion}");
            }
            // Unified Content Folder is disabled and the user has only 1 Xenia version
            else
            {
                selectedVersion = installedVersions.First();
                Logger.Info<ManagePageViewModel>($"Using single installed Xenia version: {selectedVersion}");
            }

            // Show the installation content dialog with the selected Xenia version
            await InstallContentDialog.ShowAsync(selectedVersion);
        }
        catch (Exception ex)
        {
            Logger.Error<ManagePageViewModel>("Failed to install content");
            Logger.LogExceptionDetails<ManagePageViewModel>(ex);
            await _messageBoxService.ShowErrorAsync(
                LocalizationHelper.GetText("ManagePage.Content.Install.Failed.Title"),
                string.Format(LocalizationHelper.GetText("ManagePage.Content.Install.Failed.Message"), ex.Message));
        }
    }

    [RelayCommand]
    private async Task ManageProfiles()
    {
        try
        {
            Logger.Info<ManagePageViewModel>("Initializing content installation");

            // Get all installed Xenia versions
            List<XeniaVersion> installedVersions = _settings.GetInstalledVersions(_settings);

            if (installedVersions.Count == 0)
            {
                Logger.Warning<ManagePageViewModel>("No Xenia versions installed");
                await _messageBoxService.ShowWarningAsync(
                    LocalizationHelper.GetText("ManagePage.Content.Install.NoEmulator.Title"),
                    LocalizationHelper.GetText("ManagePage.Content.Install.NoEmulator.Message"));
                return;
            }

            XeniaVersion selectedVersion;

            // If Unified Content Folder is enabled, it doesn't matter what Xenia Version will be used since it will install for all the Xenia versions
            if (UnifiedContentFolder)
            {
                selectedVersion = installedVersions.First();
                Logger.Info<ManagePageViewModel>($"Using Unified Content Folder. Selected: {selectedVersion}");
            }
            // Unified Content Folder is disabled and the user installed more than 1 Xenia version
            else if (installedVersions.Count > 1)
            {
                Logger.Info<ManagePageViewModel>($"Multiple Xenia versions detected: {installedVersions.Count}");
                XeniaVersion? chosen = await XeniaSelectionDialog.ShowAsync(installedVersions);

                if (chosen == null)
                {
                    Logger.Info<ManagePageViewModel>("User canceled Xenia version selection");
                    return;
                }

                selectedVersion = chosen.Value;
                Logger.Info<ManagePageViewModel>($"User selected Xenia version: {selectedVersion}");
            }
            // Unified Content Folder is disabled and the user has only 1 Xenia version
            else
            {
                selectedVersion = installedVersions.First();
                Logger.Info<ManagePageViewModel>($"Using single installed Xenia version: {selectedVersion}");
            }

            // Load profiles for the selected Xenia Version
            List<AccountInfo> accounts = ProfileManager.LoadProfiles(XeniaVersion.Canary);
            await ManageProfilesDialog.ShowAsync(accounts, selectedVersion);
        }
        catch (Exception ex)
        {
            Logger.Error<ManagePageViewModel>("Failed to install content");
            Logger.LogExceptionDetails<ManagePageViewModel>(ex);
            await _messageBoxService.ShowErrorAsync(
                LocalizationHelper.GetText("ManagePage.Content.Install.Failed.Title"),
                string.Format(LocalizationHelper.GetText("ManagePage.Content.Install.Failed.Message"), ex.Message));
        }
        finally
        {
            LoadAllProfiles();
        }
    }

    [RelayCommand]
    private async Task UnifyContentFolder()
    {
        List<XeniaVersion> installedVersions = _settings.GetInstalledVersions(_settings);
        try
        {
            if (UnifiedContentFolder)
            {
                // Check for administrative rights
                if (!SecurityUtilities.IsRunAsAdministrator())
                {
                    await _messageBoxService.ShowErrorAsync(
                        LocalizationHelper.GetText("ManagePage.Emulator.Settings.UnifiedContentFolder.AdminRequired.Title"),
                        LocalizationHelper.GetText("ManagePage.Emulator.Settings.UnifiedContentFolder.AdminRequired.Message"));
                    UnifiedContentFolder = false;
                    return;
                }
                try
                {
                    if (installedVersions.Count > 0)
                    {
                        // Show confirmation dialog
                        bool confirm = await _messageBoxService.ShowConfirmationAsync(
                            LocalizationHelper.GetText("ManagePage.Emulator.Settings.UnifiedContentFolder.Unify.Title"),
                            LocalizationHelper.GetText("ManagePage.Emulator.Settings.UnifiedContentFolder.Unify.Message"));

                        if (confirm)
                        {
                            // Ask the user to select his main Xenia Version
                            XeniaVersion? selectedVersion = await XeniaSelectionDialog.ShowAsync(installedVersions);
                            if (selectedVersion.HasValue)
                            {
                                // Unify content folder
                                InstallationHelper.UnifyContentFolder(selectedVersion.Value, installedVersions);
                                await _messageBoxService.ShowInfoAsync(
                                    LocalizationHelper.GetText("ManagePage.Emulator.Settings.UnifiedContentFolder.Unify.Success.Title"),
                                    LocalizationHelper.GetText("ManagePage.Emulator.Settings.UnifiedContentFolder.Unify.Success.Message"));
                            }
                            else
                            {
                                // Restore ToggleSwitch to Off
                                UnifiedContentFolder = false;
                            }
                        }
                        else
                        {
                            // Restore ToggleSwitch to Off
                            UnifiedContentFolder = false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Restore ToggleSwitch to Off
                    UnifiedContentFolder = false;
                    Logger.LogExceptionDetails<ManagePageViewModel>(ex);
                    await _messageBoxService.ShowErrorAsync(
                        LocalizationHelper.GetText("ManagePage.Emulator.Settings.UnifiedContentFolder.Operation.Failed.Title"),
                        string.Format(LocalizationHelper.GetText("ManagePage.Emulator.Settings.UnifiedContentFolder.Operation.Failed.Message"), "unify", ex.Message));
                }
            }
            else
            {
                // Separate content folders
                bool confirm = await _messageBoxService.ShowConfirmationAsync(
                    LocalizationHelper.GetText("ManagePage.Emulator.Settings.UnifiedContentFolder.Separate.Title"),
                    LocalizationHelper.GetText("ManagePage.Emulator.Settings.UnifiedContentFolder.Separate.Message"));

                if (confirm)
                {
                    // Separate content folder
                    InstallationHelper.SeparateContentFolder(installedVersions);
                    await _messageBoxService.ShowInfoAsync(
                        LocalizationHelper.GetText("ManagePage.Emulator.Settings.UnifiedContentFolder.Separate.Success.Title"),
                        LocalizationHelper.GetText("ManagePage.Emulator.Settings.UnifiedContentFolder.Separate.Success.Message"));
                }
                else
                {
                    // Restore ToggleSwitch to On
                    UnifiedContentFolder = true;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogExceptionDetails<ManagePageViewModel>(ex);
            UnifiedContentFolder = !UnifiedContentFolder;
            await _messageBoxService.ShowErrorAsync(
                LocalizationHelper.GetText("ManagePage.Emulator.Settings.UnifiedContentFolder.Operation.Failed.Title"),
                string.Format(LocalizationHelper.GetText("ManagePage.Emulator.Settings.UnifiedContentFolder.Operation.Failed.Message"), ex));
        }
        finally
        {
            if (_settings.Settings.Emulator.Settings.UnifiedContentFolder != UnifiedContentFolder)
            {
                _settings.Settings.Emulator.Settings.UnifiedContentFolder = UnifiedContentFolder;
                await _settings.SaveSettingsAsync();
            }
        }
    }
}