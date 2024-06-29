using System;
using System.Windows;
using System.Windows.Input;

// Imported
using Serilog;

namespace Xenia_Manager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Used for dragging the window around
        /// </summary>
        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        /// <summary>
        /// Exits the application completely
        /// </summary>
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Log.Information("Closing the application");
            Environment.Exit(0);
        }

        /// <summary>
        /// Opens the Library page
        /// </summary>
        private void Home_Click(object sender, RoutedEventArgs e)
        {
            PageViewer.Source = new Uri("../Pages/Library.XAML", UriKind.Relative);
        }

        /// <summary>
        /// Opens the Settings page
        /// </summary>
        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            PageViewer.Source = new Uri("../Pages/Settings.XAML", UriKind.Relative);
        }
    }
}