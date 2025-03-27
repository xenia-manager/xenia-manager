using System.ComponentModel;
using System.Windows;
using Wpf.Ui.Controls;

namespace XeniaManager.Desktop.Views.Windows;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : FluentWindow
{
    public MainWindow()
    {
        InitializeComponent();
        // Apply previous position, size and state of the main window
        this.Top = App.Settings.Ui.Window.Top;
        this.Left = App.Settings.Ui.Window.Left;
        this.Width = App.Settings.Ui.Window.Width;
        this.Height = App.Settings.Ui.Window.Height;
        
        this.WindowState = App.Settings.Ui.Window.State;
        
        // Show version number in the title
        TbTitle.Title += $" v{App.Settings.GetCurrentVersion()}";
    }

    private void MainWindow_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        NavigationView.IsPaneOpen = e.NewSize.Width > 1000;
    }

    private void MainWindow_OnClosing(object? sender, CancelEventArgs e)
    {
        // Save current position, size and state of the main window
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
}