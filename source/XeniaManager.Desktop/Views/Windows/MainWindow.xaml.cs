using System.ComponentModel;
using System.Windows;

// Imported libraries
using Wpf.Ui.Controls;
using XeniaManager.Core;
using XeniaManager.Core.Enum;
using XeniaManager.Core.Game;
using XeniaManager.Desktop.Components;
using XeniaManager.Desktop.Utilities;
using XeniaManager.Desktop.ViewModel.Windows;
using XeniaManager.Desktop.Views.Pages;

namespace XeniaManager.Desktop.Views.Windows;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    #region Variables
    private MainWindowViewModel _viewModel { get; }
    #endregion

    #region Constructors
    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainWindowViewModel(this);
        DataContext = _viewModel;

        Loaded += async (_, _) =>
        {
            NvMain.Navigate(typeof(LibraryPage));
            await _viewModel.CheckForXeniaUpdates();
            await _viewModel.CheckForXeniaManagerUpdates();
            await _viewModel.StartNotificationProcessing();
        };

        // Subscribe to property changes to update UI elements
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    #endregion

    #region Functions & Events

    private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(_viewModel.XeniaUpdateAvailable):
                NviManageXeniaInfoBadge.Visibility = _viewModel.XeniaUpdateAvailable ? Visibility.Visible : Visibility.Collapsed;
                break;
            case nameof(_viewModel.ManagerUpdateAvailable):
                NviAboutInfoBadge.Visibility = _viewModel.ManagerUpdateAvailable ? Visibility.Visible : Visibility.Collapsed;
                break;
        }
    }

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

    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        _viewModel.SaveWindowProperties(this);
    }

    private void NvMain_OnPaneOpened(NavigationView sender, RoutedEventArgs args)
    {
        NvMain.IsPaneOpen = ActualWidth > 1000;
    }

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
                    try
                    {
                        Launcher.LaunchEmulator(App.Settings.SelectVersion(() =>
                        {
                            XeniaSelection xeniaSelection = new XeniaSelection();
                            xeniaSelection.ShowDialog();
                            return xeniaSelection.SelectedXenia as XeniaVersion?;
                        }));
                    }
                    catch (OperationCanceledException)
                    {
                        Logger.Info("Xenia Selection was cancelled.");
                    }
                    break;
            };
        }
        catch (Exception ex)
        {
            Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
            CustomMessageBox.ShowAsync(ex);
        }
    }

    private void NviXeniaSettings_Click(object sender, RoutedEventArgs e)
    {
        if (App.Settings.GetInstalledVersions().Count == 0)
        {
            CustomMessageBox.Show(LocalizationHelper.GetUiText("MessageBox_MissingXenia"), LocalizationHelper.GetUiText("MessageBox_InstallXeniaToAccess"));
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
        _viewModel.XeniaUpdateAvailable = false;
        NvMain.Navigate(typeof(ManagePage));
    }

    private void NviAbout_Click(object sender, RoutedEventArgs e)
    {
        if (NviAbout.InfoBadge != null)
        {
            NviAbout.InfoBadge = null;
        }
        _viewModel.ManagerUpdateAvailable = false;
        NvMain.Navigate(typeof(AboutPage));
    }

    #endregion
}