using System;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

// Imported
using Serilog;
using XeniaManager;
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
        private Library library;
        private string gameTitle = "";
        private string gameid = "";
        private string mediaid = "";
        private string gamePath = "";
        private string xeniaVersion = "";

        // Used to send a signal that this window has been closed
        private TaskCompletionSource<bool> _closeTaskCompletionSource = new TaskCompletionSource<bool>();

        // Search signals
        private TaskCompletionSource<bool> _searchCompletionSource; // Search is completed
        private CancellationTokenSource _cancellationTokenSource; // Cancels the ongoing search if user types something

        // Constructor
        public SelectGame(Library library, string gameTitle, string gameid, string mediaid, string gamePath, string xeniaVersion)
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
            Closed += (sender, args) => _closeTaskCompletionSource.TrySetResult(true);
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
                        if (!Database.ReadXboxMarketplaceDatabase(json))
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
            SearchBox.Text = gameid;
            Log.Information("Doing the search by gameid");
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
            return _closeTaskCompletionSource.Task;
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
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = new CancellationTokenSource();

            _searchCompletionSource = new TaskCompletionSource<bool>(); // Reset the search

            // Run the search asynchronously
            await Task.WhenAll(Database.SearchXboxMarketplace(SearchBox.Text.ToLower()));

            // Update UI (ensure this is on the UI thread)
            await Dispatcher.InvokeAsync(() =>
            {
                // Update UI only if the search wasn't cancelled
                if (!_cancellationTokenSource.IsCancellationRequested)
                {
                    // Filtering Xbox Marketplace list
                    List<string> XboxMarketplaceItems = Database.XboxMarketplaceFilteredGames.Take(10).ToList();
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
        }

        /// <summary>
        /// Checks what source is selected and makes the corresponding list visible
        /// </summary>
        private void SourceSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SourceSelector.SelectedIndex < 0)
            {
                return;
            }
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
    }
}
