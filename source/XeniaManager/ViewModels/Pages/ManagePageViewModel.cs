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
    private MainWindowViewModel _mainWindowViewModel { get; set; }
    private IReleaseService _releaseService { get; set; }

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

    // Constructor
    public ManagePageViewModel()
    {
        _settings = App.Services.GetRequiredService<Settings>();
        _messageBoxService = App.Services.GetRequiredService<IMessageBoxService>();
        _mainWindowViewModel = App.Services.GetRequiredService<MainWindowViewModel>();
        _releaseService = App.Services.GetRequiredService<IReleaseService>();
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
    }

    [RelayCommand]
    private async Task InstallCanary()
    {
        DownloadManager downloadManager = new DownloadManager();
        downloadManager.ProgressChanged += (progress) => { DownloadProgress = progress; };

        try
        {
            _mainWindowViewModel.DisableWindow = true;
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
            _mainWindowViewModel.DisableWindow = false;
            await _messageBoxService.ShowInfoAsync(LocalizationHelper.GetText("ManagePage.Emulator.Manage.Xenia.Install.Success.Title"),
                string.Format(LocalizationHelper.GetText("ManagePage.Emulator.Manage.Xenia.Install.Success.Message"), "Canary"));
        }
        catch (Exception ex)
        {
            Logger.Error<ManagePageViewModel>("Failed to install Xenia Canary");
            Logger.LogExceptionDetails<ManagePageViewModel>(ex);
            _mainWindowViewModel.DisableWindow = false;
            await _messageBoxService.ShowErrorAsync(string.Format(LocalizationHelper.GetText("ManagePage.Emulator.Manage.Xenia.Install.Failure.Title"), "Canary"),
                string.Format(LocalizationHelper.GetText("ManagePage.Emulator.Manage.Xenia.Install.Failure.Message"), "Canary", ex));
        }
        finally
        {
            UpdateEmulatorStatus();
            downloadManager.Dispose();
            Directory.Delete(downloadManager.DownloadPath, true);
            Logger.Info<ManagePageViewModel>("Cleanup completed");
            _mainWindowViewModel.DisableWindow = false;
        }
    }
}