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
    }

    private void MainWindow_OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        NavigationView.IsPaneOpen = e.NewSize.Width > 1000;
    }
}