using System.ComponentModel;
using System.Windows;
using Octokit;
using Wpf.Ui;

// Imported
using Wpf.Ui.Controls;
using XeniaManager.Core;
using XeniaManager.Core.Game;
using XeniaManager.Core.Installation;
using XeniaManager.Desktop.Components;
using XeniaManager.Desktop.Utilities;
using XeniaManager.Desktop.Views.Pages;

namespace XeniaManager.Desktop.Views.Windows;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    private bool _showUpdateNotification = true;
    private readonly SnackbarService _updateNotification = new SnackbarService();

    public MainWindow()
    {
        InitializeComponent();
        // Apply the previous position, size and state of the main window
        this.Top = App.Settings.Ui.Window.Top;
        this.Left = App.Settings.Ui.Window.Left;
        this.Width = App.Settings.Ui.Window.Width;
        this.Height = App.Settings.Ui.Window.Height;

        this.WindowState = App.Settings.Ui.Window.State;

        // Show the version number in the title
        TbTitle.Title += $" v{App.Settings.GetCurrentVersion()}";

        Loaded += (_, _) =>
        {
            NvMain.Navigate(typeof(LibraryPage)); // Default Page
            CheckForXeniaUpdates();
        };
    }

    /// <summary>
    /// Checks for Xenia emulator updates
    /// </summary>
    private async void CheckForXeniaUpdates()
    {
        try
        {
            bool updateAvailable = false;
            string xeniaVersionUpdateAvailable = string.Empty;

            // Xenia Canary
            // Checking if it's installed
            if (App.Settings.Emulator.Canary != null)
            {
                if (App.Settings.Emulator.Canary.UpdateAvailable)
                {
                    // Show Snackbar
                    updateAvailable = true;
                    xeniaVersionUpdateAvailable += XeniaVersion.Canary;
                }
                // Check if we need to do an update check
                else if ((DateTime.Now - App.Settings.Emulator.Canary.LastUpdateCheckDate).TotalDays >= 1)
                {
                    Logger.Info("Checking for Xenia Canary updates.");
                    (bool, Release) canaryUpdate = await Xenia.CheckForUpdates(App.Settings.Emulator.Canary, XeniaVersion.Canary);
                    if (canaryUpdate.Item1)
                    {
                        // Show Snackbar
                        updateAvailable = true;
                        xeniaVersionUpdateAvailable += XeniaVersion.Canary;
                    }
                }
            }

            // TODO: Add checking for updates for Mousehook and Netplay


            // Show update notification
            if (updateAvailable && _showUpdateNotification)
            {
                _updateNotification.SetSnackbarPresenter(SbXeniaUpdateNotification);
                _updateNotification.Show(LocalizationHelper.GetUiText("SnackbarPresenter_XeniaUpdateAvailableTitle"),
                    $"{LocalizationHelper.GetUiText("SnackbarPresenter_XeniaUpdateAvailableText")} {xeniaVersionUpdateAvailable}",
                    ControlAppearance.Info, null, TimeSpan.FromSeconds(5));
                _showUpdateNotification = false;
            }

            App.AppSettings.SaveSettings();
        }
        catch (Exception ex)
        {
            Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
            await CustomMessageBox.Show(ex);
        }
    }

    /// <summary>
    /// If the window is wide enough, open the pane
    /// </summary>
    private void MainWindow_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (e.NewSize.Width > 1000)
        {
            NvMain.IsPaneOpen = true;
            NvMain.IsPaneToggleVisible = true;
        }
        else
        {
            NvMain.IsPaneOpen = false;
            NvMain.IsPaneToggleVisible = false;
        }
    }

    /// <summary>
    /// Saves the current position, size and state of the window
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void MainWindow_OnClosing(object? sender, CancelEventArgs e)
    {
        // Save the current position, size and state of the main window
        if (this.WindowState == WindowState.Normal)
        {
            App.Settings.Ui.Window.Top = this.Top;
            App.Settings.Ui.Window.Left = this.Left;
            App.Settings.Ui.Window.Width = this.Width;
            App.Settings.Ui.Window.Height = this.Height;
        }
        else
        {
            App.Settings.Ui.Window.Top = this.RestoreBounds.Top;
            App.Settings.Ui.Window.Left = this.RestoreBounds.Left;
            App.Settings.Ui.Window.Width = this.RestoreBounds.Width;
            App.Settings.Ui.Window.Height = this.RestoreBounds.Height;
        }

        App.Settings.Ui.Window.State = this.WindowState;
    }

    private void NvMain_OnPaneOpened(NavigationView sender, RoutedEventArgs args)
    {
        NvMain.IsPaneOpen = ActualWidth > 1000;
    }

    /// <summary>
    /// Launches the emulator without a game
    /// </summary>
    private void NviOpenXenia_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            List<XeniaVersion> availableVersions = App.Settings.GetInstalledVersions();
            switch (availableVersions.Count)
            {
                case 0:
                    throw new Exception("No Xenia version installed.\nInstall Xenia before continuing.");
                case 1:
                    Logger.Info($"There is only 1 Xenia version installed: {availableVersions[0]}");
                    Launcher.LaunchEmulator(availableVersions[0]);
                    break;
                default:
                    // TODO: Add the ability to choose what version of Xenia the game will use
                    throw new NotImplementedException();
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
            CustomMessageBox.Show(ex);
        }
    }

    private void NviXeniaSettings_Click(object sender, RoutedEventArgs e)
    {
        if (App.Settings.GetInstalledVersions().Count == 0)
        {
            CustomMessageBox.Show("No Xenia found", "Please install a version of Xenia to access this.");
        }
        else
        {
            NvMain.Navigate(typeof(XeniaSettingsPage));
        }
    }
}