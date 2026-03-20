using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using Microsoft.Extensions.DependencyInjection;
using XeniaManager.Core.Installation;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Settings;
using XeniaManager.Core.Utilities;
using XeniaManager.Services;
using XeniaManager.Views;

namespace XeniaManager.ViewModels.Pages;

public partial class AboutPageViewModel : ViewModelBase
{
    private IReleaseService _releaseService { get; set; }
    private readonly INotificationService _notificationService;
    private readonly IMessageBoxService _messageBoxService;
    private readonly Settings _settings;

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
        _settings.SaveSettings();

        // Toggle button visibility based on an experimental build setting
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
        ApplicationVersion = string.Format(CultureInfo.CurrentCulture, "v{0}", _settings.GetVersion());
        Logger.Debug<AboutPageViewModel>($"Application version: {ApplicationVersion}");

        // Load experimental build setting
        UseExperimentalBuilds = _settings.Settings.UpdateChecks.UseExperimentalBuild;
        Logger.Debug<AboutPageViewModel>($"Experimental build setting: {UseExperimentalBuilds}");

        // Load update availability from settings
        UpdatesAvailable = _settings.Settings.UpdateChecks.ManagerUpdateAvailable;
        Logger.Debug<AboutPageViewModel>($"Update available: {UpdatesAvailable}");

        // Simulate download progress visibility (hidden by default)
        IsDownloading = false;

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

            UpdatesAvailable = await ManagerService.CheckForUpdates(_releaseService, currentVersion, isExperimental);

            // Update settings with the result
            _settings.Settings.UpdateChecks.LastManagerUpdateCheck = DateTime.Now;
            await _settings.SaveSettingsAsync();

            // Notify user if update is available
            if (UpdatesAvailable)
            {
                Logger.Info<AboutPageViewModel>($"Xenia Manager update available (current: {currentVersion})");
                _notificationService.Show(LocalizationHelper.GetText("InfoBar.XeniaManagerUpdateAvailable"), InfoBarSeverity.Informational);
            }
            else
            {
                Logger.Debug<AboutPageViewModel>("Xenia Manager is up to date");
            }
        }
        catch (Exception ex)
        {
            Logger.Error<AboutPageViewModel>("Failed to check for updates");
            Logger.LogExceptionDetails<AboutPageViewModel>(ex);
            await _messageBoxService.ShowErrorAsync(
                LocalizationHelper.GetText("AboutPage.CheckForUpdatesFailedTitle"),
                string.Format(LocalizationHelper.GetText("AboutPage.CheckForUpdatesFailedMessage"), ex.Message));
        }
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task UpdateXeniaManager()
    {
        Logger.Info<AboutPageViewModel>("Updating Xenia Manager");
        try
        {
            // TODO: Implement update logic
            UpdatesAvailable = false;
            Logger.Info<AboutPageViewModel>("Update completed successfully");
        }
        catch (Exception ex)
        {
            Logger.Error<AboutPageViewModel>("Failed to update Xenia Manager");
            Logger.LogExceptionDetails<AboutPageViewModel>(ex);
            await _messageBoxService.ShowErrorAsync(
                LocalizationHelper.GetText("AboutPage.UpdateFailedTitle"),
                string.Format(LocalizationHelper.GetText("AboutPage.UpdateFailedMessage"), ex.Message));
        }
        await Task.CompletedTask;
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