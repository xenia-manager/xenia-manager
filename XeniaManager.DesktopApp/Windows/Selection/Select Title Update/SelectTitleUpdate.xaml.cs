using System;
using System.IO;
using System.Windows;
using System.Windows.Input;

// Imported
using Serilog;
using XeniaManager;
using XeniaManager.DesktopApp.Utilities.Animations;
using XeniaManager.Downloader;

namespace XeniaManager.DesktopApp.Windows
{
    /// <summary>
    /// Interaction logic for SelectTitleUpdate.xaml
    /// </summary>
    public partial class SelectTitleUpdate : Window
    {
        /// <summary>
        /// Used to execute fade in animation when loading is finished
        /// </summary>
        private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.Visibility == Visibility.Visible)
            {
                WindowAnimations.OpeningAnimation(this);
            }
        }

        /// <summary>
        /// Closes this window
        /// </summary>
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            WindowAnimations.ClosingAnimation(this);
        }

        /// <summary>
        /// When the user selects a title update from the list
        /// </summary>
        private async void TitleUpdatesList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // Check if the selection is valid
            if (TitleUpdatesList.SelectedIndex < 0)
            {
                return;
            }

            XboxUnityTitleUpdate selectedTitleUpdate = TitleUpdatesList.SelectedItem as XboxUnityTitleUpdate;
            if (selectedTitleUpdate == null)
            {
                return;
            }

            // Download selected title update
            Log.Information($"Downloading {selectedTitleUpdate.ToString()}");
            Mouse.OverrideCursor = Cursors.Wait;
            DownloadManager.ProgressChanged += (progress) =>
            {
                Progress.Value = progress;
            };
            string url = $"http://xboxunity.net/Resources/Lib/TitleUpdate.php?tuid={selectedTitleUpdate.Id}";
            await DownloadManager.DownloadFileAsync(url, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $@"Downloads\{selectedTitleUpdate.ToString()}"));
            TitleUpdateLocation = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $@"Downloads\{selectedTitleUpdate.ToString()}");

            Mouse.OverrideCursor = null;
            WindowAnimations.ClosingAnimation(this);
        }
    }
}
