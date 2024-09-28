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
        // Global variables
        // Selected game
        private Game game { get; set; }

        // Used to send a signal that this window has been closed
        private TaskCompletionSource<bool> closeWindowCheck = new TaskCompletionSource<bool>();

        /// <summary>
        /// Initializes the window for selecting the patch
        /// </summary>
        /// <param name="game">Game that we want to install patch for</param>
        public SelectGamePatch(Game game)
        {
            InitializeComponent();
            this.game = game;
            InitializeAsync();
            Closed += (s, args) => closeWindowCheck.TrySetResult(true);
        }

        // Functions
        /// <summary>
        /// Used to emulate a WaitForCloseAsync function that is similar to the one Process Class has
        /// </summary>
        /// <returns></returns>
        public Task WaitForCloseAsync()
        {
            return closeWindowCheck.Task;
        }

        /// <summary>
        /// Function that executes other functions asynchronously
        /// </summary>
        private async void InitializeAsync()
        {
            try
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    this.Visibility = Visibility.Hidden;
                    Mouse.OverrideCursor = Cursors.Wait;
                });
                await GameManager.LoadPatchesList();
                SearchBox.Text = game.GameId;

            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
            }
            finally
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    this.Visibility = Visibility.Visible;
                    Mouse.OverrideCursor = null;
                });

            }
        }

        // UI Interactions
        // Window
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

        // Button
        /// <summary>
        /// Closes this window
        /// </summary>
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            WindowAnimations.ClosingAnimation(this);
        }

        // TextBox
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

        // ListBox
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
            await GameManager.PatchDownloader(game, PatchesList.SelectedItem.ToString());
            Mouse.OverrideCursor = null;
            MessageBox.Show($"{game.Title} patch has been installed");
            WindowAnimations.ClosingAnimation(this);
        }
    }
}
