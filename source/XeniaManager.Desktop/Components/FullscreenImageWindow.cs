using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

// Imported Libraries
using Wpf.Ui.Controls;
using XeniaManager.Core.Game;

namespace XeniaManager.Desktop.Components;

public class FullscreenImageWindow : FluentWindow
{
    public FullscreenImageWindow(ImageSource imageSource, bool gameLaunch = false)
    {
        InitializeWindow(imageSource);

        if (gameLaunch)
        {
            return;
        }

        this.Loaded += async (_, _) =>
        {
            await Task.Delay(250);
            KeyDown += (s, e) =>
            {
                if (e.Key == Key.Escape) Close();
            };
        };
    }

    private void InitializeWindow(ImageSource imageSource)
    {
        WindowState = WindowState.Maximized;
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        Background = Brushes.Black;
        Topmost = true;

        Image image = new Image
        {
            Source = imageSource,
            Stretch = Stretch.UniformToFill,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        Content = image;
        Focusable = true;
        Focus();
    }
}
