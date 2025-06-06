using System.Windows.Media.Imaging;
using System.Windows;
using System.Windows.Input;

// Imported Libraries
using Wpf.Ui.Controls;

namespace XeniaManager.Desktop.Components;

public class FullscreenImageWindow : FluentWindow
{
    public FullscreenImageWindow(string imagePath)
    {
        WindowState = WindowState.Maximized;
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        Background = System.Windows.Media.Brushes.Black;

        Image image = new Image
        {
            Source = new BitmapImage(new Uri(imagePath)),
            Stretch = System.Windows.Media.Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        Content = image;
        Focusable = true;

        Focus();
        // Close on click or escape
        this.Loaded += async (_, _) =>
        {
            await Task.Delay(250);
            //MouseLeftButtonDown += (s, e) => Close();
            KeyDown += (s, e) =>
            {
                if (e.Key == Key.Escape) Close();
            };
        };
    }
}