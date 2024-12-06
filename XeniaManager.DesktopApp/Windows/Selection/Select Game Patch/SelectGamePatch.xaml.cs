using System.Windows;
using System.Windows.Input;

// Imported
using XeniaManager.DesktopApp.Utilities.Animations;

namespace XeniaManager.DesktopApp.Windows
{
    /// <summary>
    /// Interaction logic for SelectGamePatch.xaml
    /// </summary>
    public partial class SelectGamePatch
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
        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            WindowAnimations.ClosingAnimation(this);
        }

        /// <summary>
        /// This filters the Listbox items based on what's in the SearchBox
        /// </summary>
        private void TxtSearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            string searchQuery = TxtSearchBox.Text.ToLower();
            List<string> searchResults = GameManager.PatchSearch(searchQuery);
            if (LstPatchesList.ItemsSource == null ||
                !searchResults.SequenceEqual((IEnumerable<string>)LstPatchesList.ItemsSource))
            {
                LstPatchesList.ItemsSource = searchResults.Take(8); // Taking only 8
            }
        }

        /// <summary>
        /// When the user selects a patch from the list
        /// </summary>
        private async void PatchesList_SelectionChanged(object sender,
            System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // Checking if the selection is valid
            if (LstPatchesList == null || LstPatchesList.SelectedItem == null)
            {
                return;
            }

            Mouse.OverrideCursor = Cursors.Wait;
            await GameManager.DownloadPatch(game, LstPatchesList.SelectedItem.ToString());
            Mouse.OverrideCursor = null;
            MessageBox.Show($"{game.Title} patch has been installed");
            WindowAnimations.ClosingAnimation(this);
        }
    }
}