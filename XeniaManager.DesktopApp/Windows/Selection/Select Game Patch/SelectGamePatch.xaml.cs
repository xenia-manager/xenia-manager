using Serilog;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;


// Imported
using XeniaManager.DesktopApp.Utilities.Animations;

namespace XeniaManager.DesktopApp.Windows
{
    /// <summary>
    /// Interaction logic for SelectGamePatch.xaml
    /// </summary>
    public partial class SelectGamePatch : Window
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
        /// This filters the Listbox items based on what's in the SearchBox
        /// </summary>
        private void SearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            string searchQuery = SearchBox.Text.ToLower();
            List<string> searchResults = GameManager.PatchSearch(searchQuery);
            if (PatchesList.ItemsSource == null || !searchResults.SequenceEqual((IEnumerable<string>)PatchesList.ItemsSource))
            {
                PatchesList.ItemsSource = searchResults.Take(8); // Taking only 8
            }
        }

        /// <summary>
        /// When the user selects a patch from the list
        /// </summary>
        private async void PatchesList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // Checking if the selection is valid
            if (PatchesList == null || PatchesList.SelectedItem == null)
            {
                return;
            }
            Mouse.OverrideCursor = Cursors.Wait;
            await GameManager.DownloadPatch(game, PatchesList.SelectedItem.ToString());
            Mouse.OverrideCursor = null;
            MessageBox.Show($"{game.Title} patch has been installed");
            WindowAnimations.ClosingAnimation(this);
        }
    }
}
