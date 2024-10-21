using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

// Imported
using Serilog;
using XeniaManager;
using XeniaManager.Database;
using XeniaManager.DesktopApp.Utilities.Animations;

namespace XeniaManager.DesktopApp.Windows
{
    /// <summary>
    /// Interaction logic for SelectGame.xaml
    /// </summary>
    public partial class SelectGame : Window
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
        private async void Exit_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show($"Do you want to add the game without box art?\nPress 'Yes' to proceed, or 'No' to cancel.", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
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
        /// Event that triggers every time text inside of SearchBox is changed
        /// </summary>
        private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Cancel any ongoing search if the user types more input
            cancellationTokenSource?.Cancel();
            cancellationTokenSource = new CancellationTokenSource();

            searchCompletionSource = new TaskCompletionSource<bool>(); // Reset the search

            // Run the search through the databases asynchronously
            await Task.WhenAll(XboxMarketplace.Search(SearchBox.Text.ToLower()));

            // Update UI (ensure this is on the UI thread)
            await Dispatcher.InvokeAsync(() =>
            {
                // Update UI only if the search wasn't cancelled
                if (!cancellationTokenSource.IsCancellationRequested)
                {
                    // Filtering Xbox Marketplace list
                    List<string> XboxMarketplaceItems = XboxMarketplace.FilteredGames.Take(10).ToList();
                    if (XboxMarketplaceGames.ItemsSource == null || !XboxMarketplaceItems.SequenceEqual((IEnumerable<string>)XboxMarketplaceGames.ItemsSource))
                    {
                        XboxMarketplaceGames.ItemsSource = XboxMarketplaceItems;
                    }

                    // If Xbox Marketplace has any games, select this source as default
                    if (XboxMarketplaceGames.Items.Count > 0 && SourceSelector.Items.Cast<ComboBoxItem>().Any(i => i.Content.ToString() == "Xbox Marketplace"))
                    {
                        SourceSelector.SelectedItem = SourceSelector.Items.Cast<ComboBoxItem>().FirstOrDefault(i => i.Content.ToString() == "Xbox Marketplace");
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
        private void SourceSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SourceSelector.SelectedIndex < 0)
            {
                // Hide all of the ListBoxes
                XboxMarketplaceGames.Visibility = Visibility.Collapsed;
                LaunchboxDatabaseGames.Visibility = Visibility.Collapsed;
                return;
            }

            // Show the selected ListBox
            Log.Information($"Selected source: {((ComboBoxItem)SourceSelector.SelectedItem)?.Content.ToString()}");
            switch (((ComboBoxItem)SourceSelector.SelectedItem)?.Content.ToString())
            {
                case "Xbox Marketplace":
                    // Xbox Marketplace list of games
                    XboxMarketplaceGames.Visibility = Visibility.Visible;
                    LaunchboxDatabaseGames.Visibility = Visibility.Collapsed;
                    break;
                case "Launchbox Database":
                    // Launchbox Database list of games
                    XboxMarketplaceGames.Visibility = Visibility.Collapsed;
                    LaunchboxDatabaseGames.Visibility = Visibility.Visible;
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// When the user selects a game from XboxMarketplace's list of games
        /// </summary>
        private async void XboxMarketplaceGames_SelectionChanged(object sender, SelectionChangedEventArgs e)
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
            Log.Information($"Title: {selectedGame.Title}");
            await GameManager.AddGameToLibrary(selectedGame, gameid, mediaid, gamePath, xeniaVersion);
            WindowAnimations.ClosingAnimation(this);
        }
    }
}
