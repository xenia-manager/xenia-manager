using System.Windows;

namespace XeniaManager.Desktop;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void Window_StateChanged(object sender, EventArgs e)
    {
        if (WindowState == WindowState.Maximized)
        {
            this.BorderThickness = new Thickness(7);
        }
        else
        {
            this.BorderThickness = new Thickness(0);
        }
    }

    private void BtnMaximize_Click(object sender, RoutedEventArgs e)
    {
        if (this.WindowState == WindowState.Maximized)
        {
            this.WindowState = WindowState.Normal;
        }
        else
        {
            this.WindowState = WindowState.Maximized;
        }
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Environment.Exit(0);
    }
}