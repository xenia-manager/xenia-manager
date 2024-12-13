using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

// Imported
using Serilog;
using XeniaManager.Database;
using XeniaManager.DesktopApp.Utilities.Animations;

namespace XeniaManager.DesktopApp.Windows
{
    /// <summary>
    /// Interaction logic for SelectGame.xaml
    /// </summary>
    public partial class SelectGame
    {
        /// <summary>
        /// Used to execute fade in animation when loading is finished
        /// </summary>
        private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.Visibility == Visibility.Visible)
            {
                Storyboard fadeInStoryboard = ((Storyboard)Application.Current.FindResource("FadeInAnimation")).Clone();
                if (fadeInStoryboard != null)
                {
                    fadeInStoryboard.Begin(this);
                }
            }
        }

        /// <summary>
        /// Closes this window
        /// </summary>
        private async void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result =
                MessageBox.Show(
                    $"Do you want to add the game without box art?\nPress 'Yes' to proceed, or 'No' to cancel.",
                    "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                await GameManager.AddUnknownGameToLibrary(gameTitle, gameid, mediaid, gamePath, xeniaVersion);
                WindowAnimations.ClosingAnimation(this);
            }
            else
            {
                WindowAnimations.ClosingAnimation(this);
            };
        }

        /// <summary>
        /// Event that triggers every time text inside SearchBox is changed
        /// </summary>
        private async void TxtSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Cancel any ongoing search if the user types more input
            cancellationTokenSource?.Cancel();
            cancellationTokenSource = new CancellationTokenSource();

            searchCompletionSource = new TaskCompletionSource<bool>(); // Reset the search

            // Run the search through the databases asynchronously
            await Task.WhenAll(XboxMarketplace.Search(TxtSearchBox.Text.ToLower()));

            // Update UI (ensure this is on the UI thread)
            await Dispatcher.InvokeAsync(() =>
            {
                // Update UI only if the search wasn't cancelled
                if (!cancellationTokenSource.IsCancellationRequested)
                {
                    // Filtering Xbox Marketplace list
                    List<string> xboxMarketplaceItems = XboxMarketplace.FilteredGames.Take(10).ToList();
                    if (LstXboxMarketplaceGames.ItemsSource == null ||
                        !xboxMarketplaceItems.SequenceEqual((IEnumerable<string>)LstXboxMarketplaceGames.ItemsSource))
                    {
                        LstXboxMarketplaceGames.ItemsSource = xboxMarketplaceItems;
                    }

                    // If Xbox Marketplace has any games, select this source as default
                    if (LstXboxMarketplaceGames.Items.Count > 0 && CmbSourceSelector.Items.Cast<ComboBoxItem>()
                            .Any(i => i.Content.ToString() == "Xbox Marketplace"))
                    {
                        CmbSourceSelector.SelectedItem = CmbSourceSelector.Items.Cast<ComboBoxItem>()
                            .FirstOrDefault(i => i.Content.ToString() == "Xbox Marketplace");
                    }
                }
            });

            // Ensure search is completed
            if (!searchCompletionSource.Task.IsCompleted)
            {
                searchCompletionSource.SetResult(true);
            }
        }

        /// <summary>
        /// Checks what source is selected and makes the corresponding list visible
        /// </summary>
        private void CmbSourceSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbSourceSelector.SelectedIndex < 0)
            {
                // Hide all the ListBoxes
                LstXboxMarketplaceGames.Visibility = Visibility.Collapsed;
                // TODO: Remove this when removing Launchbox Database or replace it with a new source
                //LstLaunchboxDatabaseGames.Visibility = Visibility.Collapsed;
                return;
            }

            // Show the selected ListBox
            Log.Information($"Selected source: {((ComboBoxItem)CmbSourceSelector.SelectedItem)?.Content}");
            switch (((ComboBoxItem)CmbSourceSelector.SelectedItem)?.Content.ToString())
            {
                case "Xbox Marketplace":
                    // Xbox Marketplace list of games
                    LstXboxMarketplaceGames.Visibility = Visibility.Visible;
                    // TODO: Remove this when removing Launchbox Database or replace it with a new source
                    //LstLaunchboxDatabaseGames.Visibility = Visibility.Collapsed;
                    break;
                /* TODO: Remove this when removing Launchbox Database or replace it with a new source
                case "Launchbox Database":
                    // Launchbox Database list of games
                    LstXboxMarketplaceGames.Visibility = Visibility.Collapsed;
                    LstLaunchboxDatabaseGames.Visibility = Visibility.Visible;
                    break;*/
                default:
                    break;
            }
        }

        /// <summary>
        /// When the user selects a game from XboxMarketplace's list of games
        /// </summary>
        private async void LstXboxMarketplaceGames_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBox listBox = sender as ListBox;

            // Checking is listbox has something selected
            if (listBox == null || listBox.SelectedItem == null)
            {
                return;
            }

            // Finding matching selected game in the list of games
            string selectedTitle = listBox.SelectedItem.ToString();
            GameInfo selectedGame = XboxMarketplace.GetGameInfo(selectedTitle);
            if (selectedGame == null)
            {
                return;
            }

            // TODO: If the gameid is not the same as the detected one, ask the user if he wants to continue
            if (gameid != selectedGame.Id || !selectedGame.AlternativeId.Contains(gameid))
            {
                // Messagebox
                MessageBoxResult result = MessageBox.Show(
                    $"Currently detected TitleId ({gameid}) is not matching with the selected game's TitleId ({selectedGame.Id}).\nDo you want to continue?", // Message
                    "Confirmation", // Title
                    MessageBoxButton.YesNo, // Buttons
                    MessageBoxImage.Question // Icon
                );

                if (result == MessageBoxResult.Yes)
                {
                    Mouse.OverrideCursor = Cursors.Wait;
                    Log.Information($"Title: {selectedGame.Title}");
                    await GameManager.AddGameToLibrary(selectedGame, selectedGame.Id, mediaid, gamePath, xeniaVersion);
                    Mouse.OverrideCursor = null;
                    WindowAnimations.ClosingAnimation(this);
                }
                else
                {
                    listBox.SelectedIndex = -1;
                }
            }
            else
            {
                Mouse.OverrideCursor = Cursors.Wait;
                Log.Information($"Title: {selectedGame.Title}");
                await GameManager.AddGameToLibrary(selectedGame, gameid, mediaid, gamePath, xeniaVersion);
                Mouse.OverrideCursor = null;
                WindowAnimations.ClosingAnimation(this);
            }
        }
    }
}