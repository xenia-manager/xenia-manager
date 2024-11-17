using System;
using System.Windows;

namespace XeniaManager.Updater;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await DownloadNewVersion();
            DeleteOldVersion();
            Installation();
            LaunchXeniaManager();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message + "\n" + ex);
        }
    }
}