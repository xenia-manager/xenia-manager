using System;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using XeniaManager.Core.Constants;
using XeniaManager.Core.Installation;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Models;
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

    // Constructor
    public ManagePageViewModel()
    {
        _settings = App.Services.GetRequiredService<Settings>();
        _messageBoxService = App.Services.GetRequiredService<IMessageBoxService>();
        _releaseService = App.Services.GetRequiredService<IReleaseService>();
        _libraryPageViewModel = App.Services.GetRequiredService<LibraryPageViewModel>();
        UpdateEmulatorStatus();
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
    }

    [RelayCommand]
    private async Task InstallCanary()
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
            _settings.Settings.Emulator.Canary = XeniaService.SetupEmulator(XeniaVersion.Canary, releaseBuild.TagName);
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
            EventManager.Instance.EnableWindow();
            await _messageBoxService.ShowErrorAsync(string.Format(LocalizationHelper.GetText("ManagePage.Emulator.Manage.Xenia.Install.Failure.Title"), "Canary"),
                string.Format(LocalizationHelper.GetText("ManagePage.Emulator.Manage.Xenia.Install.Failure.Message"), "Canary", ex));
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
    private async Task UpdateCanary()
    {
        // TODO: Create universal Update function
        await _messageBoxService.ShowErrorAsync("Not implemented", "This feature is not implemented yet.");
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
}