using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

// Imported Libraries
using Wpf.Ui.Controls;
using XeniaManager.Core;
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

    private AboutPageViewModel _viewModel;

    #endregion


    #region Constructors

    public AboutPage()
    {
        InitializeComponent();
        _viewModel = new AboutPageViewModel();
        DataContext = _viewModel;
    }

    #endregion

    #region Functions & Events

    private void BtnWebsite_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
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
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
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
            App.Settings.Notification.ManagerUpdateAvailable = await ManagerUpdater.CheckForUpdates(App.Settings.GetInformationalVersion());
            App.Settings.UpdateCheckChecks.LastManagerUpdateCheck = DateTime.Now;
            App.AppSettings.SaveSettings();
            if (App.Settings.Notification.ManagerUpdateAvailable)
            {
                _viewModel.CheckForUpdatesButtonVisible = !App.Settings.Notification.ManagerUpdateAvailable;
                _viewModel.UpdateManagerButtonVisible = App.Settings.Notification.ManagerUpdateAvailable;
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
            await CustomMessageBox.Show(ex);
        }
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
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

    private void BtnLicense_Click(object sender, RoutedEventArgs e)
    {
        CustomMessageBox.Show("Not implemented yet", "This isn't implemented yet.");
    }

    private void BtnCredits_Click(object sender, RoutedEventArgs e)
    {
        CustomMessageBox.Show("Not implemented yet", "This isn't implemented yet.");
    }

    #endregion
}