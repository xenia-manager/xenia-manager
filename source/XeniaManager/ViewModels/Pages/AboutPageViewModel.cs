using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using Microsoft.Extensions.DependencyInjection;
using XeniaManager.Core.Constants;
using XeniaManager.Core.Installation;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Models;
using XeniaManager.Core.Services;
using XeniaManager.Core.Settings;
using XeniaManager.Core.Utilities;
using XeniaManager.Services;

namespace XeniaManager.ViewModels.Pages;

public partial class AboutPageViewModel : ViewModelBase
{
    private IReleaseService _releaseService { get; set; }
    private readonly INotificationService _notificationService;
    private readonly IMessageBoxService _messageBoxService;
    private readonly Settings _settings;
    private bool _isInitialized = false;

    [ObservableProperty] private string _applicationVersion = string.Empty;
    [ObservableProperty] private bool _isDownloading;
    [ObservableProperty] private double _downloadProgress;
    [ObservableProperty] private string _downloadProgressStatus = string.Empty;
    [ObservableProperty] private bool _useExperimentalBuilds;
    partial void OnUseExperimentalBuildsChanged(bool value)
    {
        Logger.Info<AboutPageViewModel>($"Experimental build setting changed to: {value}");

        // Update the setting in Settings
        _settings.Settings.UpdateChecks.UseExperimentalBuild = value;

        // Only reset update availability when the user actively changes the setting (not on initial load)
        if (_isInitialized)
        {
            _settings.Settings.UpdateChecks.ManagerUpdateAvailable = false;
            _settings.SaveSettings();
        }

        // Reset button visibility when switching channels
        UpdateManagerButtonVisible = false;
        CheckForUpdatesButtonVisible = true;
    }

    [ObservableProperty] private bool _updatesAvailable;
    partial void OnUpdatesAvailableChanged(bool value)
    {
        Logger.Info<AboutPageViewModel>($"Update availability changed to: {value}");

        // Update button visibility based on update availability and experimental build setting
        UpdateManagerButtonVisible = value;
        CheckForUpdatesButtonVisible = !value;

        // Save the update availability to settings
        _settings.Settings.UpdateChecks.ManagerUpdateAvailable = value;
        _settings.SaveSettings();
    }

    [ObservableProperty] private bool _updateManagerButtonVisible;
    [ObservableProperty] private bool _checkForUpdatesButtonVisible = true;

    public AboutPageViewModel()
    {
        Logger.Debug<AboutPageViewModel>("AboutPageViewModel constructor called");

        _notificationService = App.Services.GetRequiredService<INotificationService>();
        _messageBoxService = App.Services.GetRequiredService<IMessageBoxService>();
        _settings = App.Services.GetRequiredService<Settings>();
        _releaseService = App.Services.GetRequiredService<IReleaseService>();

        // Get the application version from Settings
        ApplicationVersion = _settings.GetVersion();
        Logger.Debug<AboutPageViewModel>($"Application version: {ApplicationVersion}");

        // Load experimental build setting
        UseExperimentalBuilds = _settings.Settings.UpdateChecks.UseExperimentalBuild;
        Logger.Debug<AboutPageViewModel>($"Experimental build setting: {UseExperimentalBuilds}");

        // Load update availability from settings
        UpdatesAvailable = _settings.Settings.UpdateChecks.ManagerUpdateAvailable;
        Logger.Debug<AboutPageViewModel>($"Update available: {UpdatesAvailable}");

        // Simulate download progress visibility (hidden by default)
        IsDownloading = false;

        // Mark as initialized after all settings are loaded
        _isInitialized = true;

        Logger.Info<AboutPageViewModel>("AboutPageViewModel initialization completed");
    }

    [RelayCommand]
    private async Task CheckForUpdates()
    {
        Logger.Info<AboutPageViewModel>("Checking for Xenia Manager updates");
        try
        {
            // Get the current version and check for updates
            string currentVersion = _settings.GetVersion();
            bool isExperimental = _settings.Settings.UpdateChecks.UseExperimentalBuild;
            string channel = isExperimental ? "Experimental" : "Stable";

            Logger.Info<AboutPageViewModel>($"Checking for Xenia Manager updates ({channel} channel, current version: {currentVersion})");

            bool updateAvailable = await ManagerService.CheckForUpdates(_releaseService, currentVersion, isExperimental);

            // Update settings with the result
            _settings.Settings.UpdateChecks.LastManagerUpdateCheck = DateTime.Now;
            _settings.Settings.UpdateChecks.ManagerUpdateAvailable = updateAvailable;
            await _settings.SaveSettingsAsync();

            // Update the property
            UpdatesAvailable = updateAvailable;

            // Force update button visibility regardless of whether value changed
            UpdateManagerButtonVisible = updateAvailable;
            CheckForUpdatesButtonVisible = !updateAvailable;

            // Notify user if update is available
            if (updateAvailable)
            {
                Logger.Info<AboutPageViewModel>($"Xenia Manager update available (current: {currentVersion})");
                _notificationService.Show(LocalizationHelper.GetText("AboutPage.InfoBar.XeniaManagerUpdateAvailable.Message"), InfoBarSeverity.Informational);
            }
            else
            {
                Logger.Debug<AboutPageViewModel>("Xenia Manager is up to date");
                _notificationService.Show(LocalizationHelper.GetText("AboutPage.InfoBar.NoXeniaManagerUpdateAvailable.Message"), InfoBarSeverity.Informational);
            }
        }
        catch (Exception ex)
        {
            Logger.Error<AboutPageViewModel>("Failed to check for updates");
            Logger.LogExceptionDetails<AboutPageViewModel>(ex);
            await _messageBoxService.ShowErrorAsync(
                LocalizationHelper.GetText("AboutPage.CheckForUpdatesFailed.Title"),
                string.Format(LocalizationHelper.GetText("AboutPage.CheckForUpdatesFailed.Message"), ex.Message));
        }
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task UpdateXeniaManager()
    {
        Logger.Info<AboutPageViewModel>("Updating Xenia Manager");
        try
        {
            // Get the release information
            bool isExperimental = _settings.Settings.UpdateChecks.UseExperimentalBuild;
            ReleaseType releaseType = isExperimental ? ReleaseType.XeniaManagerExperimental : ReleaseType.XeniaManagerStable;
            string channel = isExperimental ? "Experimental" : "Stable";

            Logger.Info<AboutPageViewModel>($"Fetching {channel} release information");
            ManagerBuild? releaseBuild = await _releaseService.GetManagerBuildAsync(releaseType);

            if (releaseBuild == null || string.IsNullOrEmpty(releaseBuild.Url))
            {
                Logger.Error<AboutPageViewModel>($"Failed to fetch {channel} release information");
                await _messageBoxService.ShowErrorAsync(
                    LocalizationHelper.GetText("AboutPage.UpdateFailed.Title"),
                    LocalizationHelper.GetText("AboutPage.UpdateFailed.NoReleaseInfo"));
                return;
            }

            Logger.Info<AboutPageViewModel>($"Found {channel} release: {releaseBuild.Version}");

            // Show download progress
            IsDownloading = true;
            DownloadProgress = 0;
            DownloadProgressStatus = LocalizationHelper.GetText("AboutPage.DownloadingUpdate");

            // Setup download manager
            DownloadManager downloadManager = new DownloadManager();
            downloadManager.ProgressChanged += progress =>
            {
                DownloadProgress = progress;
                DownloadProgressStatus = $"{LocalizationHelper.GetText("AboutPage.DownloadingUpdate")} - {progress}%";
            };

            string archiveFileName = "XeniaManager.Update.zip";
            string archivePath = Path.Combine(AppPaths.DownloadsDirectory, archiveFileName);
            string extractPath = Path.Combine(AppPaths.DownloadsDirectory, "UpdateExtract");

            try
            {
                EventManager.Instance.DisableWindow();

                // Download the update
                Logger.Info<AboutPageViewModel>($"Downloading update from {releaseBuild.Url}");
                await downloadManager.DownloadFileAsync(releaseBuild.Url, archiveFileName);

                // Extract the update
                Logger.Info<AboutPageViewModel>("Extracting update archive");
                DownloadProgressStatus = LocalizationHelper.GetText("AboutPage.ExtractingUpdate");

                // Clean up the extraction directory if it exists
                if (Directory.Exists(extractPath))
                {
                    Directory.Delete(extractPath, true);
                }
                Directory.CreateDirectory(extractPath);

                await ArchiveExtractor.ExtractArchiveAsync(archivePath, extractPath);

                // Clean up the archive
                if (File.Exists(archivePath))
                {
                    File.Delete(archivePath);
                }

                // Create a backup directory
                string backupDir = Path.Combine(AppPaths.Backup, "Old Release");
                Directory.CreateDirectory(backupDir);

                // Back up the old executable
                string managerExecutable = AppPaths.ManagerExecutable;
                if (File.Exists(managerExecutable))
                {
                    string backupPath = Path.Combine(backupDir, $"XeniaManager.exe");
                    File.Copy(managerExecutable, backupPath, true);
                    Logger.Info<AboutPageViewModel>($"Backed up old executable to {backupPath}");
                }

                // Create the batch script
                string batPath = Path.Combine(AppPaths.DownloadsDirectory, "UpdateAndRelaunch.bat");
                string currentProcessPath = Environment.ProcessPath ?? "";
                string currentProcessName = Path.GetFileName(currentProcessPath);
                int currentPid = Process.GetCurrentProcess().Id;
                string extractDir = extractPath;
                string baseDir = AppPathResolver.BaseDirectory();

                string batContent = $$"""
                                      @echo off
                                      :: Wait for the original process to exit
                                      :waitloop
                                      tasklist /FI "PID eq {{currentPid}}" | find /I "{{currentProcessName}}" >nul
                                      if not errorlevel 1 (
                                          timeout /T 1 /NOBREAK >nul
                                          goto waitloop
                                      )

                                      echo Moving files...
                                      xcopy /E /I /Y "{{extractDir}}\*.*" "{{baseDir}}\"
                                      if %errorlevel% NEQ 0 (
                                          echo Error copying files. Error code: %errorlevel%
                                          pause
                                          exit /b %errorlevel%
                                      )

                                      :: Delete the extracted files
                                      rd /s /q "{{extractDir}}"

                                      echo Done moving files.

                                      :: Relaunch the original program
                                      start "" "{{currentProcessPath}}"

                                      :: Delete the batch script itself
                                      del "%~f0"
                                      """;

                await File.WriteAllTextAsync(batPath, batContent);
                Logger.Info<AboutPageViewModel>("Created update batch script");

                // Update settings before launching updater
                _settings.Settings.UpdateChecks.LastManagerUpdateCheck = DateTime.Now;
                _settings.Settings.UpdateChecks.ManagerUpdateAvailable = false;
                await _settings.SaveSettingsAsync();

                // Start the batch script
                Logger.Info<AboutPageViewModel>("Launching update installer");
                Process.Start(new ProcessStartInfo
                {
                    FileName = batPath,
                    UseShellExecute = true,
                    CreateNoWindow = false,
                    WindowStyle = ProcessWindowStyle.Normal
                });

                // Close the application
                Logger.Info<AboutPageViewModel>("Shutting down application for update");
                Environment.Exit(0);
            }
            finally
            {
                downloadManager.ProgressChanged -= null;
                downloadManager.Dispose();
                IsDownloading = false;
                EventManager.Instance.EnableWindow();
            }

            UpdatesAvailable = false;
            Logger.Info<AboutPageViewModel>("Update completed successfully");
        }
        catch (Exception ex)
        {
            Logger.Error<AboutPageViewModel>("Failed to update Xenia Manager");
            Logger.LogExceptionDetails<AboutPageViewModel>(ex);
            IsDownloading = false;
            EventManager.Instance.EnableWindow();
            await _messageBoxService.ShowErrorAsync(
                LocalizationHelper.GetText("AboutPage.UpdateFailed.Title"),
                string.Format(LocalizationHelper.GetText("AboutPage.UpdateFailed.Message"), ex.Message));
        }
    }

    [RelayCommand]
    private void OpenWebsite()
    {
        Logger.Debug<AboutPageViewModel>("Opening Xenia Manager website");
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://xenia-manager.github.io/",
                UseShellExecute = true
            });
            Logger.Debug<AboutPageViewModel>("Opened Xenia Manager website successfully");
        }
        catch (Exception ex)
        {
            Logger.Warning<AboutPageViewModel>($"Failed to open website: {ex.Message}");
            Logger.LogExceptionDetails<AboutPageViewModel>(ex);
        }
    }

    [RelayCommand]
    private void OpenGitHub()
    {
        Logger.Debug<AboutPageViewModel>("Opening GitHub repository");
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/xenia-manager/xenia-manager",
                UseShellExecute = true
            });
            Logger.Debug<AboutPageViewModel>("Opened GitHub repository successfully");
        }
        catch (Exception ex)
        {
            Logger.Warning<AboutPageViewModel>($"Failed to open GitHub: {ex.Message}");
            Logger.LogExceptionDetails<AboutPageViewModel>(ex);
        }
    }

    [RelayCommand]
    private void OpenUrl(string? url)
    {
        if (string.IsNullOrEmpty(url))
        {
            Logger.Warning<AboutPageViewModel>("OpenUrl called with null or empty URL");
            return;
        }

        Logger.Debug<AboutPageViewModel>($"Opening URL: {url}");
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
            Logger.Debug<AboutPageViewModel>($"Successfully opened URL: {url}");
        }
        catch (Exception ex)
        {
            Logger.Error<AboutPageViewModel>($"Failed to open URL: {url}");
            Logger.LogExceptionDetails<AboutPageViewModel>(ex);
        }
    }
}