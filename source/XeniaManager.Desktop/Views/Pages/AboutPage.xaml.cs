﻿using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

// Imported Libraries
using Wpf.Ui.Controls;
using XeniaManager.Core;
using XeniaManager.Core.Constants;
using XeniaManager.Core.Downloader;
using XeniaManager.Desktop.Components;
using XeniaManager.Desktop.Utilities;
using XeniaManager.Desktop.ViewModel.Pages;

namespace XeniaManager.Desktop.Views.Pages;

/// <summary>
/// Interaction logic for AboutPage.xaml
/// </summary>
public partial class AboutPage : Page
{
    #region Variables
    public AboutPageViewModel ViewModel;

    #endregion

    #region Constructors

    public AboutPage()
    {
        InitializeComponent();
        ViewModel = new AboutPageViewModel();
        DataContext = ViewModel;
    }

    #endregion

    #region Functions & Events

    private void BtnWebsite_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = @"https://xenia-manager.github.io/",
                UseShellExecute = true
            });
            e.Handled = true;
        }
        catch (Exception ex)
        {
            Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
            CustomMessageBox.Show(ex);
        }
    }

    private void BtnGitHub_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = @"https://github.com/xenia-manager/xenia-manager",
                UseShellExecute = true
            });
            e.Handled = true;
        }
        catch (Exception ex)
        {
            Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
            CustomMessageBox.Show(ex);
        }
    }

    private async void BtnCheckUpdates_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Logger.Info("Checking for Xenia Manager updates");
            if (App.Settings.UpdateChecks.UseExperimentalBuild)
            {
                App.Settings.Notification.ManagerUpdateAvailable = await ManagerUpdater.CheckForUpdates(App.Settings.GetManagerVersion(), "xenia-manager", "experimental-builds");
            }
            else
            {
                App.Settings.Notification.ManagerUpdateAvailable = await ManagerUpdater.CheckForUpdates(App.Settings.GetManagerVersion());
            }
            App.Settings.UpdateChecks.LastManagerUpdateCheck = DateTime.Now;
            App.AppSettings.SaveSettings();
            if (App.Settings.Notification.ManagerUpdateAvailable)
            {
                ViewModel.CheckForUpdatesButtonVisible = !App.Settings.Notification.ManagerUpdateAvailable;
                ViewModel.UpdateManagerButtonVisible = App.Settings.Notification.ManagerUpdateAvailable;
                IbUpdatesAvailable.Title = LocalizationHelper.GetUiText("InfoBar_UpdatesAvailableTitle");
                IbUpdatesAvailable.Message = LocalizationHelper.GetUiText("InfoBar_ManagerUpdatesAvailableText");
                IbUpdatesAvailable.Severity = InfoBarSeverity.Informational;
            }
            else
            {
                IbUpdatesAvailable.Title = LocalizationHelper.GetUiText("InfoBar_NoUpdatesAvailableTitle");
                IbUpdatesAvailable.Message = LocalizationHelper.GetUiText("InfoBar_ManagerNoUpdatesAvailableText");
                IbUpdatesAvailable.Severity = InfoBarSeverity.Success;
            }
            IbUpdatesAvailable.IsOpen = true;
        }
        catch (Exception ex)
        {
            Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
            await CustomMessageBox.ShowAsync(ex);
        }
    }

    private async void BtnUpdateXeniaManager_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Logger.Info("Downloading latest version of Xenia Manager");
            ViewModel.IsDownloading = true;
            string downloadLink = string.Empty;
            if (App.Settings.UpdateChecks.UseExperimentalBuild)
            {
                downloadLink = await ManagerUpdater.GrabDownloadLink("xenia-manager", "experimental-builds");
            }
            else
            {
                downloadLink = await ManagerUpdater.GrabDownloadLink();
            }

            DownloadManager downloadManager = new DownloadManager();
            downloadManager.ProgressChanged += (progress) => { PbDownloadProgress.Value = progress; };

            // Download the latest version of Xenia Manager
            await downloadManager.DownloadAndExtractAsync(downloadLink, "xenia-manager.zip", DirectoryPaths.Downloads);
            // Create the batch script content
            string batContent = $@"
@echo off
:: Wait for the original process to exit
:waitloop
tasklist /FI ""PID eq {Process.GetCurrentProcess().Id}"" | find /I ""{Path.GetFileName(Environment.ProcessPath)}"" >nul
if not errorlevel 1 (
    timeout /T 1 /NOBREAK >nul
    goto waitloop
)

echo Moving files...
xcopy ""{DirectoryPaths.Downloads}\*.*"" ""{DirectoryPaths.Base}\\"" /E /I /Y
if %errorlevel% NEQ 0 (
    echo Error copying files.
    pause
    exit /b %errorlevel%
)

:: Delete the original files and subdirectories
rd /s /q ""{DirectoryPaths.Downloads}""
mkdir ""{DirectoryPaths.Downloads}""

echo Done moving files.

:: Relaunch the original program
start """" ""{Environment.ProcessPath}""
";

            // Write the batch content to a file
            Directory.CreateDirectory(DirectoryPaths.Cache);
            await File.WriteAllTextAsync(Path.Combine(DirectoryPaths.Cache, "update-script.bat"), batContent);
            Process.Start(new ProcessStartInfo
            {
                FileName = Path.Combine(DirectoryPaths.Cache, "update-script.bat"),
                UseShellExecute = true
            });
            App.Settings.Notification.ManagerUpdateAvailable = false;
            App.Settings.UpdateChecks.LastManagerUpdateCheck = DateTime.Now;
            App.AppSettings.SaveSettings();
            Logger.Info("Closing the app for update");
            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
            await CustomMessageBox.ShowAsync(ex);
        }
        finally
        {
            ViewModel.IsDownloading = false;
        }
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = e.Uri.AbsoluteUri,
                UseShellExecute = true
            });
            e.Handled = true;
        }
        catch (Exception ex)
        {
            Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
            CustomMessageBox.Show(ex);
        }
    }

    #endregion
}