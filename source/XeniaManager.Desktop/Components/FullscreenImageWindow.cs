using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

// Imported Libraries
using Wpf.Ui.Controls;

namespace XeniaManager.Desktop.Components
{
    public class FullscreenImageWindow : FluentWindow
    {
        public FullscreenImageWindow(string imagePath, bool gameLaunch = false)
        {
            InitializeWindow(imagePath);

            if (gameLaunch)
            {
                return;
            }
            ;

            this.Loaded += async (_, _) =>
            {
                await Task.Delay(250);
                KeyDown += (s, e) =>
                {
                    if (e.Key == Key.Escape) Close();
                };
            };
        }

        private void InitializeWindow(string imagePath)
        {
            WindowState = WindowState.Maximized;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            Background = Brushes.Black;
            Topmost = true;

            Image image = new Image
            {
                Source = new BitmapImage(new Uri(imagePath)),
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            Content = image;
            Focusable = true;
            Focus();
        }
    }
}