using System.ComponentModel;
using System.Windows;

// Imported libraries
using Octokit;
using Wpf.Ui;
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
    #region Variables

    /// <summary>
    /// Flag to control whether the update notification should be displayed.
    /// Set to false after the first notification to prevent spam.
    /// </summary>
    private bool _showUpdateNotification = true;

    /// <summary>
    /// Service for displaying update notifications via a snackbar UI element.
    /// Provides non-intrusive notifications to the user about available updates.
    /// </summary>
    private readonly SnackbarService _updateNotification = new SnackbarService();

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the MainWindow class.
    /// Sets up the window with previously saved position, size, and state,
    /// configures the title with version information, and sets up event handlers.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();

        // Restore the previous window position and dimensions from saved settings
        // This ensures the window appears where the user last positioned it
        this.Top = App.Settings.Ui.Window.Top;
        this.Left = App.Settings.Ui.Window.Left;
        this.Width = App.Settings.Ui.Window.Width;
        this.Height = App.Settings.Ui.Window.Height;
        this.WindowState = App.Settings.Ui.Window.State; // Restore the previous window state (Normal, Maximized, Minimized)

        // Display the current application version in the window title
        // This helps users identify which version they're running
        TbTitle.Title += $" v{App.Settings.GetInformationalVersion()}";

        // Set up the window-loaded event handler
        // This ensures initialization code runs after the window is fully loaded
        Loaded += (_, _) =>
        {
            NvMain.Navigate(typeof(LibraryPage)); // Navigate to the default page (Game Library) when the application starts
            CheckForXeniaUpdates(); // Begin checking for available Xenia emulator updates
        };
    }

    #endregion

    #region Functions & Events

    /// <summary>
    /// Asynchronously checks for available updates to the Xenia emulator.
    /// Examines installed Xenia versions and compares them with the latest releases.
    /// Displays a notification if updates are available and handles update checking intervals.
    /// </summary>
    /// <remarks>
    /// This method currently supports Xenia Canary updates. Future versions will include
    /// support for additional Xenia variants like Mousehook and Netplay.
    /// Update checks are performed daily to balance freshness with performance.
    /// </remarks>
    private async void CheckForXeniaUpdates()
    {
        try
        {
            bool updateAvailable = false;
            string xeniaVersionUpdateAvailable = string.Empty;

            // Check for Xenia Canary updates
            if (App.Settings.Emulator.Canary != null)
            {
                // If an update was previously detected and is still pending
                if (App.Settings.Emulator.Canary.UpdateAvailable)
                {
                    // Show Update Notification
                    updateAvailable = true;
                    xeniaVersionUpdateAvailable += XeniaVersion.Canary;
                }
                // Check if it's time to perform a new update check (daily interval)
                else if ((DateTime.Now - App.Settings.Emulator.Canary.LastUpdateCheckDate).TotalDays >= 1)
                {
                    Logger.Info("Checking for Xenia Canary updates.");
                    // Perform the actual update check against the repository
                    (bool, Release) canaryUpdate = await Xenia.CheckForUpdates(App.Settings.Emulator.Canary, XeniaVersion.Canary);
                    if (canaryUpdate.Item1)
                    {
                        // Show Update Notification
                        updateAvailable = true;
                        xeniaVersionUpdateAvailable += XeniaVersion.Canary;
                    }
                }
            }

            // TODO: Add checking for updates for Mousehook and Netplay


            // Display update notification if updates are available and notifications are enabled
            if (updateAvailable && _showUpdateNotification)
            {
                // Configure the snackbar presenter for displaying the notification
                _updateNotification.SetSnackbarPresenter(SbUpdateNotification);

                // Show the update notification with localized text
                _updateNotification.Show(LocalizationHelper.GetUiText("SnackbarPresenter_XeniaUpdateAvailableTitle"),
                    $"{LocalizationHelper.GetUiText("SnackbarPresenter_XeniaUpdateAvailableText")} {xeniaVersionUpdateAvailable}",
                    ControlAppearance.Info, null, TimeSpan.FromSeconds(5));

                // Prevent additional notifications during this session
                _showUpdateNotification = false;

            }
            else
            {
                NviManageXeniaInfoBadge.Visibility = Visibility.Collapsed;
            }

            // Persist any changes made during the update check process
            App.AppSettings.SaveSettings();
        }
        catch (Exception ex)
        {
            Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
            await CustomMessageBox.Show(ex);
        }
    }

    /// <summary>
    /// Handles window size changes to automatically manage navigation pane visibility.
    /// Opens the navigation pane when the window is wide enough (>1000px) to provide
    /// a better user experience on larger screens and closes it on smaller screens to
    /// maximize the content area.
    /// </summary>
    private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
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
    /// Handles the window closing event to persist the current window state.
    /// Saves the window's position, dimensions, and state (Normal/Maximized/Minimized)
    /// to application settings for restoration on the next startup.
    /// </summary>
    private void MainWindow_Closing(object? sender, CancelEventArgs e)
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
        // Always save the current window state for proper restoration
        App.Settings.Ui.Window.State = this.WindowState;
    }

    private void NvMain_OnPaneOpened(NavigationView sender, RoutedEventArgs args)
    {
        NvMain.IsPaneOpen = ActualWidth > 1000;
    }

    /// <summary>
    /// Handles the "Open Xenia" navigation item click event.
    /// Launches the Xenia emulator without loading a specific game.
    /// Automatically selects the appropriate Xenia version if only one is installed,
    /// or presents a selection dialog if multiple versions are available.
    /// </summary>
    /// <remarks>
    /// Currently supports launching when exactly one Xenia version is installed.
    /// Multi-version selection functionality is planned for future implementation.
    /// </remarks>
    private void NviOpenXenia_Click(object sender, RoutedEventArgs e)
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

    /// <summary>
    /// Handles the "Xenia Settings" navigation item click event.
    /// Navigates to the Xenia configuration page where users can modify
    /// emulator settings such as graphics, audio, and input options.
    /// Requires at least one Xenia installation to be present.
    /// </summary>
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

    private void NviManageXenia_Click(object sender, RoutedEventArgs e)
    {
        if (NviManageXenia.InfoBadge != null)
        {
            NviManageXenia.InfoBadge = null;
        }
        NvMain.Navigate(typeof(ManagePage));
    }

    #endregion
}