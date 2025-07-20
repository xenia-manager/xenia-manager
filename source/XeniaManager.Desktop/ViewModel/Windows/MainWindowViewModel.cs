// Imported Libraries
using Octokit;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;
using Wpf.Ui;
using Wpf.Ui.Controls;
using XeniaManager.Core;
using XeniaManager.Core.Enum;
using XeniaManager.Core.Game;
using XeniaManager.Core.Installation;
using XeniaManager.Desktop.Components;
using XeniaManager.Desktop.Utilities;
using XeniaManager.Desktop.Views.Windows;
using static XeniaManager.Core.Settings.UiSettings;

namespace XeniaManager.Desktop.ViewModel.Windows;

public class MainWindowViewModel : INotifyPropertyChanged
{
    #region Variables
    private string _windowTitle = $"Xenia Manager";
    public string WindowTitle
    {
        get => _windowTitle;
        set => SetProperty(ref _windowTitle, value);
    }

    public BitmapImage TitleBarIcon { get; set; }

    private WindowProperties _windowProperties = App.Settings.Ui.Window;
    public WindowProperties WindowProperties
    {
        get => _windowProperties;
        set
        {
            if (SetProperty(ref _windowProperties, value))
            {
                App.Settings.Ui.Window = _windowProperties;
                App.AppSettings.SaveSettings();
            }
        }
    }

    public SnackbarService UpdateNotification { get; }

    private bool _showUpdateNotification = true;
    public bool ShowUpdateNotification
    {
        get => _showUpdateNotification;
        set => SetProperty(ref _showUpdateNotification, value);
    }

    private bool _isProcessingNotifications = false;
    public bool IsProcessingNotifications
    {
        get => _isProcessingNotifications;
        set => SetProperty(ref _isProcessingNotifications, value);
    }

    private bool _xeniaUpdateAvailable = false;
    public bool XeniaUpdateAvailable
    {
        get => _xeniaUpdateAvailable;
        set => SetProperty(ref _xeniaUpdateAvailable, value);
    }

    private bool _managerUpdateAvailable = false;
    public bool ManagerUpdateAvailable
    {
        get => _managerUpdateAvailable;
        set => SetProperty(ref _managerUpdateAvailable, value);
    }

    private readonly Queue<Func<Task>> _notificationQueue = new Queue<Func<Task>>();
    private readonly MainWindow _window;
    #endregion

    #region Constructors
    public MainWindowViewModel(MainWindow window)
    {
        _window = window;
        TitleBarIcon = new BitmapImage(new Uri("pack://application:,,,/Assets/64.png", UriKind.Absolute));
        UpdateNotification = new SnackbarService();
        RestoreWindowProperties(window);
        UpdateNotification.SetSnackbarPresenter(window.SbUpdateNotification);
    }
    #endregion

    #region Window Management Functions
    public void RestoreWindowProperties(MainWindow window)
    {
        window.Top = WindowProperties.Top;
        window.Left = WindowProperties.Left;
        window.Width = WindowProperties.Width;
        window.Height = WindowProperties.Height;
        window.WindowState = WindowProperties.State;
    }

    public void SaveWindowProperties(MainWindow window)
    {
        WindowProperties newProperties = new WindowProperties();
        if (window.WindowState == System.Windows.WindowState.Normal)
        {
            newProperties.Top = window.Top;
            newProperties.Left = window.Left;
            newProperties.Width = window.Width;
            newProperties.Height = window.Height;
        }
        else
        {
            newProperties.Top = window.RestoreBounds.Top;
            newProperties.Left = window.RestoreBounds.Left;
            newProperties.Width = window.RestoreBounds.Width;
            newProperties.Height = window.RestoreBounds.Height;
        }
        newProperties.State = window.WindowState;
        WindowProperties = newProperties;
    }
    #endregion

    #region Notification Management
    private async Task ShowNotificationAsync(string title, string message, ControlAppearance appearance, TimeSpan duration)
    {
        // Show the notification
        UpdateNotification.Show(title, message, appearance, null, duration);

        // Wait for the duration of the notification
        await Task.Delay(duration);

        // Optional: Add a small buffer between notifications
        await Task.Delay(TimeSpan.FromMilliseconds(300));
    }

    private async Task ProcessNotificationQueue()
    {
        if (IsProcessingNotifications)
        {
            return;
        }

        IsProcessingNotifications = true;

        while (_notificationQueue.Count > 0)
        {
            Func<Task> notification = _notificationQueue.Dequeue();
            await notification();
        }

        IsProcessingNotifications = false;
    }

    private void QueueNotification(Func<Task> notificationAction)
    {
        _notificationQueue.Enqueue(notificationAction);
    }

    public async Task StartNotificationProcessing()
    {
        _ = ProcessNotificationQueue();
    }
    #endregion

    #region Update Checking
    public async Task CheckForXeniaUpdates()
    {
        try
        {
            bool updateAvailable = false;
            Launcher.XeniaUpdating = true;
            List<XeniaVersion> xeniaUpdates = new List<XeniaVersion>();

            // Check for Xenia Canary updates
            if (App.Settings.Emulator.Canary != null)
            {
                // If an update was previously detected and is still pending
                if (App.Settings.Emulator.Canary.UpdateAvailable)
                {
                    // Show Update Notification
                    updateAvailable = true;
                    xeniaUpdates.Add(XeniaVersion.Canary);
                }
                // Check if it's time to perform a new update check (daily interval)
                else if ((DateTime.Now - App.Settings.Emulator.Canary.LastUpdateCheckDate).TotalDays >= 1)
                {
                    Logger.Info("Checking for Xenia Canary updates.");
                    // Perform the actual update check against the repository
                    bool canaryUpdate = await Xenia.CheckForUpdates(App.Settings.Emulator.Canary, XeniaVersion.Canary);
                    if (canaryUpdate)
                    {
                        // Show Update Notification
                        updateAvailable = true;
                        xeniaUpdates.Add(XeniaVersion.Canary);
                    }
                }
            }

            // Auto update Xenia Canary
            if (App.Settings.Emulator.Settings.AutomaticallyUpdateEmulator && updateAvailable)
            {
                Logger.Info("Automatically updating Xenia Canary");
                bool success = await Xenia.UpdateCanary(App.Settings.Emulator.Canary);
                if (success)
                {
                    Logger.Info("Xenia Canary has been successfully updated.");
                    await CustomMessageBox.ShowAsync(LocalizationHelper.GetUiText("MessageBox_Success"), string.Format(LocalizationHelper.GetUiText("MessageBox_SuccessUpdateXeniaText"), XeniaVersion.Canary));
                }
            }

            // Check for Xenia Mousehook updates
            if (App.Settings.Emulator.Mousehook != null)
            {
                // If an update was previously detected and is still pending
                if (App.Settings.Emulator.Mousehook.UpdateAvailable)
                {
                    // Show Update Notification
                    updateAvailable = true;
                    xeniaUpdates.Add(XeniaVersion.Mousehook);
                }
                // Check if it's time to perform a new update check (daily interval)
                else if ((DateTime.Now - App.Settings.Emulator.Mousehook.LastUpdateCheckDate).TotalDays >= 1)
                {
                    Logger.Info("Checking for Xenia Mousehook updates.");
                    // Perform the actual update check against the repository
                    bool mousehookUpdate = await Xenia.CheckForUpdates(App.Settings.Emulator.Mousehook, XeniaVersion.Mousehook);
                    if (mousehookUpdate)
                    {
                        // Show Update Notification
                        updateAvailable = true;
                        xeniaUpdates.Add(XeniaVersion.Mousehook);
                    }
                }
            }

            // Auto update Xenia Mousehook
            if (App.Settings.Emulator.Settings.AutomaticallyUpdateEmulator && updateAvailable)
            {
                Logger.Info("Automatically updating Xenia Mousehook");
                bool success = await Xenia.UpdateMousehoook(App.Settings.Emulator.Mousehook);
                if (success)
                {
                    Logger.Info("Xenia Mousehook has been successfully updated.");
                    await CustomMessageBox.ShowAsync(LocalizationHelper.GetUiText("MessageBox_Success"), string.Format(LocalizationHelper.GetUiText("MessageBox_SuccessUpdateXeniaText"), XeniaVersion.Mousehook));
                }
            }

            // Check for Xenia Netplay updates
            if (App.Settings.Emulator.Netplay != null)
            {
                // If an update was previously detected and is still pending
                if (App.Settings.Emulator.Netplay.UpdateAvailable)
                {
                    // Show Update Notification
                    updateAvailable = true;
                    xeniaUpdates.Add(XeniaVersion.Netplay);
                }
                // Check if it's time to perform a new update check (daily interval)
                else if ((DateTime.Now - App.Settings.Emulator.Netplay.LastUpdateCheckDate).TotalDays >= 1)
                {
                    Logger.Info("Checking for Xenia Netplay updates.");
                    // Perform the actual update check against the repository
                    bool netplayUpdate = await Xenia.CheckForUpdates(App.Settings.Emulator.Netplay, XeniaVersion.Netplay);
                    if (netplayUpdate)
                    {
                        // Show Update Notification
                        updateAvailable = true;
                        xeniaUpdates.Add(XeniaVersion.Netplay);
                    }
                }
            }

            // Auto update Xenia Netplay
            if (App.Settings.Emulator.Settings.AutomaticallyUpdateEmulator && updateAvailable)
            {
                Logger.Info("Automatically updating Xenia Netplay");
                bool success = await Xenia.UpdateNetplay(App.Settings.Emulator.Netplay);
                if (success)
                {
                    Logger.Info("Xenia Netplay has been successfully updated.");
                    await CustomMessageBox.ShowAsync(LocalizationHelper.GetUiText("MessageBox_Success"), string.Format(LocalizationHelper.GetUiText("MessageBox_SuccessUpdateXeniaText"), XeniaVersion.Netplay));
                }
            }

            Launcher.XeniaUpdating = false;

            // Display update notification if updates are available and notifications are enabled
            if (!App.Settings.Emulator.Settings.AutomaticallyUpdateEmulator && (updateAvailable && ShowUpdateNotification))
            {
                // Queue the first notification
                QueueNotification(async () =>
                {
                    await ShowNotificationAsync(
                        LocalizationHelper.GetUiText("SnackbarPresenter_XeniaUpdateAvailableTitle"),
                        $"{LocalizationHelper.GetUiText("SnackbarPresenter_XeniaUpdateAvailableText")} {string.Join(", ", xeniaUpdates)}",
                        ControlAppearance.Info,
                        TimeSpan.FromSeconds(3)
                    );
                });

                XeniaUpdateAvailable = true;
                // Prevent additional notifications during this session
                ShowUpdateNotification = false;
            }

            // Persist any changes made during the update check process
            App.AppSettings.SaveSettings();
        }
        catch (Exception ex)
        {
            Launcher.XeniaUpdating = false;
            Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
            await CustomMessageBox.ShowAsync(ex);
        }
    }

    public async Task CheckForXeniaManagerUpdates()
    {
        try
        {
            if (App.Settings.Notification.ManagerUpdateAvailable)
            {
                ShowUpdateNotification = true;
            }
            else if ((DateTime.Now - App.Settings.UpdateCheckChecks.LastManagerUpdateCheck).TotalDays >= 1)
            {
                Logger.Info("Checking for Xenia Manager updates");
                if (App.Settings.UpdateCheckChecks.UseExperimentalBuild)
                {
                    App.Settings.Notification.ManagerUpdateAvailable = await ManagerUpdater.CheckForUpdates(App.Settings.GetManagerVersion(), "xenia-manager", "experimental-builds");
                }
                else
                {
                    App.Settings.Notification.ManagerUpdateAvailable = await ManagerUpdater.CheckForUpdates(App.Settings.GetManagerVersion());
                }
                App.Settings.UpdateCheckChecks.LastManagerUpdateCheck = DateTime.Now;
                ShowUpdateNotification = true;
            }

            if (App.Settings.Notification.ManagerUpdateAvailable && ShowUpdateNotification)
            {
                QueueNotification(async () =>
                {
                    await ShowNotificationAsync(
                        LocalizationHelper.GetUiText("SnackbarPresenter_XeniaManagerUpdateAvailableTitle"),
                        LocalizationHelper.GetUiText("SnackbarPresenter_XeniaManagerUpdateAvailableText"),
                        ControlAppearance.Info,
                        TimeSpan.FromSeconds(3)
                    );
                });
                ManagerUpdateAvailable = true;
                ShowUpdateNotification = false;
            }

            App.AppSettings.SaveSettings();
        }
        catch (Exception ex)
        {
            Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
            await CustomMessageBox.ShowAsync(ex);
        }
    }
    #endregion

    #region Property Changed Implementation
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    #endregion
}