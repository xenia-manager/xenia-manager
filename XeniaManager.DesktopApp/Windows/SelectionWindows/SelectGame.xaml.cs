using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

// Imported
using Serilog;
using XeniaManager;
using XeniaManager.Database;
using XeniaManager.DesktopApp.Pages;
using XeniaManager.DesktopApp.Utilities.Animations;

namespace XeniaManager.DesktopApp.Windows
{
    /// <summary>
    /// Interaction logic for SelectGame.xaml
    /// </summary>
    public partial class SelectGame : Window
    {
        // Global variables
        // These variables get imported from Library page, used to grab the game
        private string gameTitle = "";
        private string gameid = "";
        private string mediaid = "";
        private string gamePath = "";
        private EmulatorVersion xeniaVersion = EmulatorVersion.Canary;

        // Used to send a signal that this window has been closed
        private TaskCompletionSource<bool> closeTaskCompletionSource = new TaskCompletionSource<bool>();

        // Search signals
        private TaskCompletionSource<bool> searchCompletionSource; // Search is completed
        private CancellationTokenSource cancellationTokenSource; // Cancels the ongoing search if user types something

        // Constructor
        public SelectGame(string gameTitle, string gameid, string mediaid, string gamePath, EmulatorVersion xeniaVersion)
        {
            InitializeComponent();
            if (gameTitle != null)
            {
                this.gameTitle = gameTitle;
                this.gameid = gameid;
                this.mediaid = mediaid;
            }
            this.gamePath = gamePath;
            this.xeniaVersion = xeniaVersion;
            InitializeAsync();
            Closed += (sender, args) => closeTaskCompletionSource.TrySetResult(true);
        }

        /// <summary>
        /// Used to read the games from the "databases" 
        /// </summary>
        private async Task ReadGames()
        {
            // Xbox Marketplace List
            Log.Information("Loading Xbox Marketplace list of games");
            string url = "https://raw.githubusercontent.com/xenia-manager/Database/temp-main/Database/xbox_marketplace_games.json";
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "Xenia Manager (https://github.com/xenia-manager/xenia-manager)");
                    HttpResponseMessage response = await client.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        string json = await response.Content.ReadAsStringAsync();
                        if (!XboxMarketplace.Load(json))
                        {
                            SourceSelector.Items.Remove((ComboBoxItem)SourceSelector.Items.Cast<ComboBoxItem>().FirstOrDefault(i => i.Content.ToString() == "Xbox Marketplace"));
                        }
                    }
                    else
                    {
                        Log.Error($"Failed to load Xbox Marketplace ({response.StatusCode})");
                        SourceSelector.Items.Remove((ComboBoxItem)SourceSelector.Items.Cast<ComboBoxItem>().FirstOrDefault(i => i.Content.ToString() == "Xbox Marketplace"));
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Message, "");
                    MessageBox.Show(ex.Message + "\nFull Error:\n" + ex);
                }
            };
            try
            {

                // Launchbox Database
                /*
                Log.Information("Loading Launchbox Database");
                url = "https://raw.githubusercontent.com/xenia-manager/Database/temp-main/Database/launchbox_games.json";
                using (HttpClient client = new HttpClient())
                {
                    try
                    {
                        client.DefaultRequestHeaders.Add("User-Agent", "Xenia Manager (https://github.com/xenia-manager/xenia-manager)");
                        HttpResponseMessage response = await client.GetAsync(url);
                        if (response.IsSuccessStatusCode)
                        {
                            string json = await response.Content.ReadAsStringAsync();

                            try
                            {
                                launchboxListOfGames = JsonConvert.DeserializeObject<List<GameInfo>>(json);
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                                SourceSelector.Items.Remove((ComboBoxItem)SourceSelector.Items.Cast<ComboBoxItem>().FirstOrDefault(i => i.Content.ToString() == "Launchbox Database"));
                            }
                        }
                        else
                        {
                            Log.Error($"Failed to load Launchbox Database ({response.StatusCode})");
                            SourceSelector.Items.Remove((ComboBoxItem)SourceSelector.Items.Cast<ComboBoxItem>().FirstOrDefault(i => i.Content.ToString() == "Launchbox Database"));
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.Message, "");
                        MessageBox.Show(ex.Message + "\nFull Error:\n" + ex);
                    }
                }
                */
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// Searches for the game
        /// </summary>
        /// <returns></returns>
        private async Task SearchForGame()
        {
            // Search by TitleID
            Log.Information("Doing the search by gameid");
            SearchBox.Text = gameid;
            await searchCompletionSource.Task; // Wait for search to finish before continuing
            bool successfulSearchByID = false;
            // Check if there are any games in XboxMarketplace list, if they are, show the ListBox
            if (XboxMarketplaceGames.Items.Count > 0)
            {
                SourceSelector.SelectedIndex = 0;
                successfulSearchByID = true;
            }

            // If search by TitleID fails, search by game title
            if (!successfulSearchByID)
            {
                Log.Information("No games found using id to search");
                SearchBox.Text = Regex.Replace(gameTitle, @"[^a-zA-Z0-9\s]", "");
                Log.Information("Doing search by game title");
            }
            await searchCompletionSource.Task; // Wait for search to finish before continuing

            // Check if there are any games in XboxMarketplace list, if they are, show the ListBox
            if (XboxMarketplaceGames.Items.Count > 0)
            {
                Log.Information("There are some results in Xbox Marketplace list");
                SourceSelector.SelectedIndex = 0;
            }
            else
            {
                Log.Information("No games found");
                SourceSelector.SelectedIndex = -1;
            }
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
                await ReadGames();
                await SearchForGame();
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

        /// <summary>
        /// Used to emulate a WaitForCloseAsync function that is similar to the one Process Class has
        /// </summary>
        public Task WaitForCloseAsync()
        {
            return closeTaskCompletionSource.Task;
        }

        // UI Interactions
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
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
           WindowAnimations.ClosingAnimation(this);
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
