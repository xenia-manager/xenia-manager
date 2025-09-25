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
using XeniaManager.Core.Settings;
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
        try
        {
            TitleBarIcon = new BitmapImage(new Uri("pack://application:,,,/Assets/64.png", UriKind.Absolute));
        }
        catch (Exception)
        {
            TitleBarIcon = null;
        }
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
            List<XeniaVersion> updatesAvailable = new List<XeniaVersion>();

            Dictionary<XeniaVersion, EmulatorInfo?> emulators = new Dictionary<XeniaVersion, EmulatorInfo?>
            {
                { XeniaVersion.Canary,   App.Settings.Emulator.Canary },
                { XeniaVersion.Mousehook, App.Settings.Emulator.Mousehook },
                { XeniaVersion.Netplay,  App.Settings.Emulator.Netplay }
            };

            Launcher.XeniaUpdating = true;

            // Check for updates
            foreach (KeyValuePair<XeniaVersion, EmulatorInfo?> kvp in emulators)
            {
                XeniaVersion version = kvp.Key;
                EmulatorInfo? emulator = kvp.Value;

                if (emulator == null)
                {
                    continue;
                }

                bool needsUpdate = false;

                if (emulator.UpdateAvailable)
                {
                    needsUpdate = true;
                }
                else if ((DateTime.Now - emulator.LastUpdateCheckDate).TotalDays >= 1)
                {
                    Logger.Info($"Checking for {version} updates.");
                    needsUpdate = await Xenia.CheckForUpdates(emulator, version);
                }

                if (needsUpdate)
                {
                    updatesAvailable.Add(version);

                    if (App.Settings.Emulator.Settings.AutomaticallyUpdateEmulator)
                    {
                        Logger.Info($"Automatically updating Xenia {version}");
                        bool success = version switch
                        {
                            XeniaVersion.Canary => await Xenia.UpdateCanary(emulator),
                            XeniaVersion.Mousehook => await Xenia.UpdateMousehoook(emulator),
                            XeniaVersion.Netplay => await Xenia.UpdateNetplay(emulator),
                            _ => false
                        };

                        if (success)
                        {
                            Logger.Info($"Xenia {version} has been successfully updated.");
                            await CustomMessageBox.ShowAsync(
                                LocalizationHelper.GetUiText("MessageBox_Success"),
                                string.Format(LocalizationHelper.GetUiText("MessageBox_SuccessUpdateXeniaText"), version)
                            );
                        }
                    }
                }
            }

            Launcher.XeniaUpdating = false;

            // Show notification if auto-update is disabled
            if (!App.Settings.Emulator.Settings.AutomaticallyUpdateEmulator && updatesAvailable.Any() && ShowUpdateNotification)
            {
                QueueNotification(async () =>
                {
                    await ShowNotificationAsync(
                        LocalizationHelper.GetUiText("SnackbarPresenter_XeniaUpdateAvailableTitle"),
                        $"{LocalizationHelper.GetUiText("SnackbarPresenter_XeniaUpdateAvailableText")} {string.Join(", ", updatesAvailable)}",
                        ControlAppearance.Info,
                        TimeSpan.FromSeconds(3)
                    );
                });

                XeniaUpdateAvailable = true;
                ShowUpdateNotification = false; // Prevent Repeated Notifications
            }

            // Save settings
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
            else if ((DateTime.Now - App.Settings.UpdateChecks.LastManagerUpdateCheck).TotalDays >= 1)
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