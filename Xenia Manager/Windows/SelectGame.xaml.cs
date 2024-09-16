using System;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

// Imported
using ImageMagick;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using Xenia_Manager.Classes;
using Xenia_Manager.Pages;

namespace Xenia_Manager.Windows
{
    /// <summary>
    /// Interaction logic for SelectGame.xaml
    /// </summary>
    public partial class SelectGame : Window
    {
        /// <summary>
        /// Used to track if it's the initial search and if it is, search based on GameID for Xbox Marketplace and every other based on game title
        /// </summary>
        private bool isFirstSearch = true;

        // Game lists
        // These 2 lists hold unfiltered and filtered list of games in Xbox Marketplace's list of games
        List<GameInfo> XboxMarketplaceListOfGames = new List<GameInfo>();
        private List<string> XboxMarketplaceFilteredGames = new List<string>();
        private HashSet<string> XboxMarketplaceAllTitleIDs; // Contains both main and alterantive id's
        private Dictionary<string, GameInfo> XboxMarketplaceIDGameMap; // Maps TitleID's to Game
        private Dictionary<string, List<GameInfo>> titleGameMap; // Maps Game titles to Game

        // These 2 lists hold unfiltered and filtered list of games in Launchbox Database
        List<GameInfo> launchboxListOfGames = new List<GameInfo>();
        private List<string> launchboxfilteredGames = new List<string>();

        // These 2 lists hold unfiltered and filtered list of games in Wikipedia's list of games
        List<GameInfo> wikipediaListOfGames = new List<GameInfo>();
        private List<string> wikipediafilteredGames = new List<string>();

        // These variables get imported from Library page, used to grab the game
        private Library library;
        private string gameTitle = "";
        private string gameid = "";
        private string mediaid = "";
        private string GameFilePath = "";
        private string XeniaVersion = "";
        private EmulatorInfo EmulatorInfo;

        // Holds game that user wants to add to the Manager
        public InstalledGame newGame = new InstalledGame();

        // Signals
        // Signal that is used when first search is completed
        private TaskCompletionSource<bool> _searchCompletionSource;

        // Used to send a signal that this window has been closed
        private TaskCompletionSource<bool> _closeTaskCompletionSource = new TaskCompletionSource<bool>();

        /// <summary>
        /// Default starting constructor
        /// </summary>
        public SelectGame()
        {
            InitializeComponent();
            InitializeAsync();
            Closed += (sender, args) => _closeTaskCompletionSource.TrySetResult(true);
        }

        /// <summary>
        /// Constructor when we're adding a game
        /// </summary>
        /// <param name="library"></param>
        /// <param name="selectedGame"></param>
        /// <param name="selectedGameid"></param>
        /// <param name="selectedGamePath"></param>
        public SelectGame(Library library, string selectedGame, string selectedGameid, string selectedMediaid, string selectedGamePath, string XeniaVersion,EmulatorInfo emulatorInfo)
        {
            InitializeComponent();
            if (selectedGame != null)
            {
                this.gameTitle = selectedGame;
                this.gameid = selectedGameid;
                this.mediaid = selectedMediaid;
            }
            this.GameFilePath = selectedGamePath;
            this.library = library;
            this.XeniaVersion = XeniaVersion;
            this.EmulatorInfo = emulatorInfo;
            InitializeAsync();
            Closed += (sender, args) => _closeTaskCompletionSource.TrySetResult(true);
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
        /// Used to read the games from the "databases" 
        /// </summary>
        private async Task ReadGames()
        {
            try
            {
                // Xbox Marketplace List
                Log.Information("Loading Xbox Marketplace list of games");
                List<string> displayItems = new List<string>();
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
                            try
                            {
                                XboxMarketplaceListOfGames = JsonConvert.DeserializeObject<List<GameInfo>>(json); // Loading .JSON file
                                
                                XboxMarketplaceAllTitleIDs = new HashSet<string>();
                                XboxMarketplaceIDGameMap = new Dictionary<string, GameInfo>();
                                titleGameMap = new Dictionary<string, List<GameInfo>>();

                                foreach (var game in XboxMarketplaceListOfGames)
                                {
                                    string primaryId = game.Id.ToLower();
                                    if (!XboxMarketplaceIDGameMap.ContainsKey(primaryId))
                                    {
                                        XboxMarketplaceIDGameMap[primaryId] = game;
                                        XboxMarketplaceAllTitleIDs.Add(primaryId);
                                    }

                                    if (game.AlternativeId != null)
                                    {
                                        foreach (var altId in game.AlternativeId)
                                        {
                                            string lowerAltId = altId.ToLower();
                                            if (!XboxMarketplaceIDGameMap.ContainsKey(lowerAltId))
                                            {
                                                XboxMarketplaceIDGameMap[lowerAltId] = game;
                                                XboxMarketplaceAllTitleIDs.Add(lowerAltId);
                                            }
                                        }
                                    }

                                    string title = game.Title.ToLower();
                                    if (!titleGameMap.ContainsKey(title))
                                    {
                                        titleGameMap[title] = new List<GameInfo>();
                                    }
                                    titleGameMap[title].Add(game);
                                }
                                displayItems = XboxMarketplaceListOfGames.Select(game => game.Title).ToList();

                                XboxMarketplaceGames.Items.Clear();
                                XboxMarketplaceGames.ItemsSource = displayItems;
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex.Message + "\nFull Error:\n" + ex);
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
                }
                SourceSelector.Items.Remove((ComboBoxItem)SourceSelector.Items.Cast<ComboBoxItem>().FirstOrDefault(i => i.Content.ToString() == "Launchbox Database"));
                SourceSelector.Items.Remove((ComboBoxItem)SourceSelector.Items.Cast<ComboBoxItem>().FirstOrDefault(i => i.Content.ToString() == "Wikipedia"));
                // Launchbox Database
                /*
                Log.Information("Loading Launchbox Database");
                url = "https://raw.githubusercontent.com/xenia-manager/Database/main/Database/launchbox_games.json";
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
                                displayItems = launchboxListOfGames.Select(game => game.Title).ToList();

                                LaunchboxDatabaseGames.Items.Clear();
                                LaunchboxDatabaseGames.ItemsSource = displayItems;
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

                // Wikipedia's list
                Log.Information("Loading Wikipedia's list of games");
                url = "https://gist.githubusercontent.com/shazzaam7/1729e5d444eb79efc16b2a52a1f59737/raw/2eca66f8c5571554496182300227ed6db8d8a829/xbox360_wikipedia_games_list.json";
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
                                wikipediaListOfGames = JsonConvert.DeserializeObject<List<GameInfo>>(json);
                                displayItems = wikipediaListOfGames.Select(game => game.Title).ToList();

                                WikipediaGames.Items.Clear();
                                WikipediaGames.ItemsSource = displayItems;
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                                SourceSelector.Items.Remove((ComboBoxItem)SourceSelector.Items.Cast<ComboBoxItem>().FirstOrDefault(i => i.Content.ToString() == "Wikipedia"));
                            }
                        }
                        else
                        {
                            Log.Error($"Failed to load Wikipedia ({response.StatusCode})");
                            SourceSelector.Items.Remove((ComboBoxItem)SourceSelector.Items.Cast<ComboBoxItem>().FirstOrDefault(i => i.Content.ToString() == "Wikipedia"));
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
        /// This function is for games that aren't in the lists
        /// </summary>
        private async Task AddUnknownGames()
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                newGame.Title = gameTitle.Replace(":", " -").Replace('\\', ' ').Replace('/', ' ');
                newGame.GameId = gameid;
                newGame.GameCompatibilityURL = null;
                newGame.GameFilePath = GameFilePath;
                if (File.Exists(Path.Combine(App.baseDirectory, EmulatorInfo.ConfigurationFileLocation)))
                {
                    File.Copy(Path.Combine(App.baseDirectory, EmulatorInfo.ConfigurationFileLocation), Path.Combine(App.baseDirectory, EmulatorInfo.EmulatorLocation, $@"config\{newGame.Title}.config.toml"), true);
                }
                newGame.ConfigFilePath = Path.Combine(EmulatorInfo.EmulatorLocation, $@"config\{newGame.Title}.config.toml");
                newGame.EmulatorVersion = XeniaVersion;
                if (!library.Games.Any(game => game.Title == newGame.Title))
                {
                    // Downloading boxart
                    Log.Information("Downloading boxart");
                    await GetGameIcon($@"https://raw.githubusercontent.com/xenia-manager/Assets/main/Assets/Boxart.jpg", Path.Combine(App.baseDirectory, @$"Icons\{newGame.Title}.ico"));
                    newGame.BoxartFilePath = @$"Icons\{newGame.Title}.ico";

                    // Download icon for shortcut
                    Log.Information("Downloading icon for shortcuts");
                    await GetGameIcon(@"https://raw.githubusercontent.com/xenia-manager/Assets/main/Assets/Disc.png", Path.Combine(App.baseDirectory, @$"Icons\{newGame.Title} Icon.ico"), 64, 64);
                    newGame.ShortcutIconFilePath = @$"Icons\{newGame.Title} Icon.ico";
                    Log.Information("Adding the game to the Xenia Manager");
                    library.Games.Add(newGame);
                }
                else
                {
                    Log.Information("Game is already in the Xenia Manager");
                }
                Mouse.OverrideCursor = null;
                await ClosingAnimation();
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
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
                SearchBox.Text = gameid; // Initial search is by Game ID 
                Log.Information("Doing the search by gameid");
                await _searchCompletionSource.Task; // This waits for the search to be done before continuing with the code
                bool successfulSearchByID = false;
                if (XboxMarketplaceFilteredGames.Count > 0)
                {
                    SourceSelector.SelectedIndex = 0;
                    successfulSearchByID = true;
                }
                else
                {
                    Log.Information("No games found using id to search");
                    // If no game has been found by id, do the search by gameTitle
                    SearchBox.Text = Regex.Replace(gameTitle, @"[^a-zA-Z0-9\s]", "");
                    Log.Information("Doing search by game title");
                }
                await _searchCompletionSource.Task; // This waits for the search to be done before continuing with the code
                if (!successfulSearchByID)
                {
                    // This is a check if there are no games in the list after the initial search
                    if (XboxMarketplaceFilteredGames.Count > 0)
                    {
                        Log.Information("There are some results in Xbox Marketplace list");
                        SourceSelector.SelectedIndex = 0;
                    }
                    else if (launchboxfilteredGames.Count > 0)
                    {
                        Log.Information("There are some results in Launchbox Database");
                        SourceSelector.SelectedIndex = 1;
                    }
                    else if (wikipediafilteredGames.Count > 0)
                    {
                        Log.Information("There are some results in Wikipedia's list");
                        SourceSelector.SelectedIndex = 2;
                    }
                    else
                    {
                        Log.Information("No game found");
                        MessageBoxResult result = MessageBox.Show($"'{gameTitle}' was not found in our database, possibly due to formatting differences.\nWould you like to use the default disc icon instead? (Select No if you prefer to search for the game manually.)", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (result == MessageBoxResult.Yes)
                        {
                            await AddUnknownGames();
                        }
                        else
                        {
                            SourceSelector.SelectedIndex = 0;
                        };
                    }
                }
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
        /// Used to execute fade in animation when loading is finished
        /// </summary>
        private void MainWindow_VisibilityChanged(object sender, DependencyPropertyChangedEventArgs e)
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
        /// This updates the Listbox with the filtered items
        /// </summary>
        private void UpdateListBoxes()
        {
            // Xbox Marketplace filtering
            XboxMarketplaceGames.ItemsSource = XboxMarketplaceFilteredGames;

            // Launchbox filtering
            //LaunchboxDatabaseGames.ItemsSource = launchboxfilteredGames;

            // Wikipedia filtering
            //WikipediaGames.ItemsSource = wikipediafilteredGames;

            if (XboxMarketplaceGames.Items.Count > 0 && SourceSelector.Items.Cast<ComboBoxItem>().Any(i => i.Content.ToString() == "Xbox Marketplace"))
            {
                SourceSelector.SelectedItem = SourceSelector.Items.Cast<ComboBoxItem>().FirstOrDefault(i => i.Content.ToString() == "Xbox Marketplace");
            }
            /*else if (LaunchboxDatabaseGames.Items.Count > 0 && SourceSelector.Items.Cast<ComboBoxItem>().Any(i => i.Content.ToString() == "Launchbox Database"))
            {
                SourceSelector.SelectedItem = SourceSelector.Items.Cast<ComboBoxItem>().FirstOrDefault(i => i.Content.ToString() == "Launchbox Database");
            }
            else if (WikipediaGames.Items.Count > 0 && SourceSelector.Items.Cast<ComboBoxItem>().Any(i => i.Content.ToString() == "Wikipedia"))
            {
                SourceSelector.SelectedItem = SourceSelector.Items.Cast<ComboBoxItem>().FirstOrDefault(i => i.Content.ToString() == "Wikipedia");
            }*/
        }

        /// <summary>
        /// This filters the Listbox items to the searchbox
        /// </summary>
        private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchCompletionSource = new TaskCompletionSource<bool>();
            Mouse.OverrideCursor = Cursors.Wait;
            string searchQuery = SearchBox.Text.ToLower();

            // Search through "Xbox Marketplace"
            await Task.Run(() =>
            {
                if (isFirstSearch)
                {
                    // Initial search by GameID
                    /*
                    XboxMarketplaceFilteredGames = XboxMarketplaceListOfGames
                    .Where(game => game.Id.ToLower().Contains(searchQuery))
                    .Select(game => game.Title)
                    .ToList();*/
                    List<string> filteredIds = XboxMarketplaceAllTitleIDs
                    .Where(id => id.Contains(searchQuery))
                    .ToList();

                    XboxMarketplaceFilteredGames = filteredIds
                    .Where(id => id.Contains(searchQuery))
                    .Select(id => XboxMarketplaceIDGameMap[id].Title)
                    .ToList();

                    // Set the flag to false after the first search
                    isFirstSearch = false;
                }
                else
                {
                    // Subsequent searches by Title
                    /*
                    XboxMarketplaceFilteredGames = XboxMarketplaceListOfGames
                    .Where(game => game.Title.ToLower().Contains(searchQuery))
                    .Select(game => game.Title)
                    .ToList();*/
                    // Search for titles containing the search query
                    List<string> filteredGames = titleGameMap
                        .Where(pair => pair.Key.Contains(searchQuery))
                        .SelectMany(pair => pair.Value)
                        .Select(game => game.Title)
                        .Distinct()
                        .ToList();

                    XboxMarketplaceFilteredGames = filteredGames;
                }
            });

            // Search through "Launchbox Database"
            /*
            await Task.Run(() =>
            {
                launchboxfilteredGames = launchboxListOfGames
                .Where(game => game.Title.ToLower().Contains(searchQuery))
                .Select(game => game.Title)
                .ToList();
                GC.Collect();
            });

            // Search through "Wikipedia"
            await Task.Run(() =>
            {
                wikipediafilteredGames = wikipediaListOfGames
                .Where(game => game.Title.ToLower().Contains(searchQuery))
                .Select(game => game.Title)
                .ToList();
                GC.Collect();
            });*/
            UpdateListBoxes();
            GC.Collect();
            Mouse.OverrideCursor = null;
            _searchCompletionSource.SetResult(true);
        }

        /// <summary>
        /// Used to emulate a WaitForCloseAsync function that is similar to the one Process Class has
        /// </summary>
        /// <returns></returns>
        public Task WaitForCloseAsync()
        {
            return _closeTaskCompletionSource.Task;
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
                    WikipediaGames.Visibility = Visibility.Collapsed;
                    break;
                case "Launchbox Database":
                    // Launchbox Database list of games
                    XboxMarketplaceGames.Visibility = Visibility.Collapsed;
                    LaunchboxDatabaseGames.Visibility = Visibility.Visible;
                    WikipediaGames.Visibility = Visibility.Collapsed;
                    break;
                case "Wikipedia":
                    // Wikipedia list of games
                    XboxMarketplaceGames.Visibility = Visibility.Collapsed;
                    LaunchboxDatabaseGames.Visibility = Visibility.Collapsed;
                    WikipediaGames.Visibility = Visibility.Visible;
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Does fade out animation before closing the window
        /// </summary>
        private async Task ClosingAnimation()
        {
            Storyboard FadeOutClosingAnimation = ((Storyboard)Application.Current.FindResource("FadeOutAnimation")).Clone();

            FadeOutClosingAnimation.Completed += (sender, e) =>
            {
                Log.Information("Closing SelectGame window");
                this.Close();
            };

            FadeOutClosingAnimation.Begin(this);
            await Task.Delay(1);
        }

        /// <summary>
        /// Closes this window
        /// </summary>
        private async void Exit_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show($"Do you want to add the game without box art?\nPress 'Yes' to proceed, or 'No' to cancel.", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                await AddUnknownGames();
            }
            else
            {
                await ClosingAnimation();
            };
        }

        /// <summary>
        /// Downloads game info from Xbox Marketplace source
        /// </summary>
        /// <returns></returns>
        private async Task<XboxMarketplaceGameInfo> DownloadGameInfo(string gameId)
        {
            Log.Information("Trying to fetch game info");
            string url = $"https://raw.githubusercontent.com/xenia-manager/Database/temp-main/Database/Xbox%20Marketplace/{gameid}/{gameid}.json";
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
                            XboxMarketplaceGameInfo GameInfo = JsonConvert.DeserializeObject<XboxMarketplaceGameInfo>(json);
                            Log.Information("Successfully fetched game info");
                            return GameInfo;
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex.Message + "\nFull Error:\n" + ex);
                            return null;
                        }
                    }
                    else
                    {
                        Log.Error($"Failed to fetch game info from Xbox Marketplace ({response.StatusCode})");
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Message, "");
                    return null;
                }
            }
        }

        /// <summary>
        /// Used to check if the URL is working
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private async Task<bool> CheckIfURLWorks(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                //client.Timeout = TimeSpan.FromSeconds(10);
                client.DefaultRequestHeaders.Add("User-Agent", "Xenia Manager (https://github.com/xenia-manager/xenia-manager)");
                try
                {
                    HttpResponseMessage response = await client.GetAsync(url);
                    if (response.Content.Headers.ContentType.MediaType.StartsWith("image/"))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                catch (HttpRequestException)
                {
                    return false; // URL is not reachable
                }
            }
        }

        /// <summary>
        /// Function that grabs the game box art from the database and converts it to .ico
        /// </summary>
        /// <param name="imageUrl">Image URL</param>
        /// <param name="outputPath">Where the file will be stored after conversion</param>
        /// <param name="width">Width of the box art. Default is 150</param>
        /// <param name="height">Height of the box art. Default is 207</param>
        /// <returns></returns>
        private async Task GetGameIcon(string imageUrl, string outputPath, int width = 150, int height = 207)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "Xenia Manager (https://github.com/xenia-manager/xenia-manager)");

                    byte[] imageData = await client.GetByteArrayAsync(imageUrl);

                    using (MemoryStream memoryStream = new MemoryStream(imageData))
                    {
                        using (MagickImage magickImage = new MagickImage(memoryStream))
                        {
                            // Resize the image to the specified dimensions (this will stretch the image)
                            magickImage.Resize(width, height);

                            // Convert to ICO format
                            magickImage.Format = MagickFormat.Ico;
                            magickImage.Write(outputPath);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// Grabs the URL to the compatibility page of the game
        /// </summary>
        private async Task GetGameCompatibilityPageURL(string gameTitle, string gameId)
        {
            try
            {
                Log.Information($"Trying to find the compatibility page for {gameTitle}");
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "Xenia Manager (https://github.com/xenia-manager/xenia-manager)");
                    client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");

                    HttpResponseMessage response = await client.GetAsync($"https://api.github.com/search/issues?q={gameId}%20in%3Atitle%20repo%3Axenia-project%2Fgame-compatibility");

                    if (response.IsSuccessStatusCode)
                    {
                        string json = await response.Content.ReadAsStringAsync();
                        JObject jsonObject = JObject.Parse(json);
                        JArray searchResults = (JArray)jsonObject["items"];
                        switch (searchResults.Count)
                        {
                            case 0:
                                Log.Information($"The compatibility page for {gameTitle} isn't found");
                                newGame.GameCompatibilityURL = null;
                                break;
                            case 1:
                                Log.Information($"Found the compatibility page for {gameTitle}");
                                Log.Information($"URL: {searchResults[0]["html_url"].ToString()}");
                                newGame.GameCompatibilityURL = searchResults[0]["html_url"].ToString();
                                break;
                            default:
                                Log.Information($"Multiple compatibility pages found");
                                Log.Information($"Trying to parse them");
                                foreach (JToken result in searchResults)
                                {
                                    string originalResultTitle = result["title"].ToString();
                                    string[] parts = originalResultTitle.Split(new string[] { " - " }, StringSplitOptions.None);
                                    string resultTitle = parts[1];
                                    if (resultTitle == gameTitle)
                                    {
                                        Log.Information($"Found the compatibility page for {gameTitle}");
                                        Log.Information($"URL: {result["html_url"].ToString()}");
                                        newGame.GameCompatibilityURL = result["html_url"].ToString();
                                        break;
                                    }
                                }
                                break;
                        }
                    }
                }
                await Task.Delay(1);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// Checks for the compatibility of the game with the emulator
        /// </summary>
        private async Task GetCompatibilityRating()
        {
            try
            {
                Log.Information($"Trying to find the compatibility page for {newGame.Title}");
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "Xenia Manager (https://github.com/xenia-manager/xenia-manager)");
                    client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");

                    HttpResponseMessage response = await client.GetAsync(newGame.GameCompatibilityURL.Replace("https://github.com/", "https://api.github.com/repos/"));

                    if (!response.IsSuccessStatusCode)
                    {
                        return;
                    }

                    string json = await response.Content.ReadAsStringAsync();
                    JObject jsonObject = JObject.Parse(json);
                    JArray labels = (JArray)jsonObject["labels"];
                    if (labels.Count > 0)
                    {
                        bool foundCompatibility = false;
                        foreach (JObject label in labels)
                        {
                            string labelName = (string)label["name"];
                            if (labelName.Contains("state-"))
                            {
                                foundCompatibility = true;
                                string[] split = labelName.Split('-');
                                switch (split[1].ToLower())
                                {
                                    case "nothing":
                                    case "crash":
                                        newGame.CompatibilityRating = "Unplayable";
                                        break;
                                    case "intro":
                                    case "hang":
                                    case "load":
                                    case "title":   
                                    case "menus":
                                        newGame.CompatibilityRating = "Loads";
                                        break;
                                    case "gameplay":
                                        newGame.CompatibilityRating = "Gameplay";
                                        break;
                                    case "playable":
                                        newGame.CompatibilityRating = "Playable";
                                        break;
                                    default:
                                        newGame.CompatibilityRating = "Unknown";
                                        break;
                                }
                                Log.Information($"Current compatibility: {newGame.CompatibilityRating}");
                                break;
                            }
                            if (!foundCompatibility)
                            {
                                newGame.CompatibilityRating = "Unknown";
                            }
                        }
                    }
                    else
                    {
                        newGame.CompatibilityRating = "Unknown";
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                newGame.CompatibilityRating = "Unknown";
            }
        }

        /// <summary>
        /// When the user selects a game from XboxMarketplace's list of games
        /// </summary>
        private async void XboxMarketplaceGames_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                ListBox listBox = sender as ListBox;

                // Checking is listbox has something selected
                if (listBox == null || listBox.SelectedItem == null)
                {
                    return;
                }

                // Finding matching selected game in the list of games
                string selectedTitle = listBox.SelectedItem.ToString();
                GameInfo selectedGame = XboxMarketplaceListOfGames.FirstOrDefault(game => game.Title == selectedTitle);
                if (selectedGame == null || (selectedGame.Id != gameid && !selectedGame.AlternativeId.Contains(gameid)))
                {
                    listBox.SelectedItem = null;
                    return;
                }

                if (selectedGame.Id == gameid || selectedGame.AlternativeId.Contains(gameid))
                {
                    Log.Information($"{selectedGame.Title}, {selectedGame.Id}");
                }

                Mouse.OverrideCursor = Cursors.Wait;

                // Adding the game to the library
                Log.Information($"Selected Game: {selectedGame.Title}");
                newGame.Title = selectedGame.Title.Replace(":", " -").Replace('\\', ' ').Replace('/', ' ');
                newGame.GameId = gameid;
                newGame.MediaId = mediaid;

                // Try to grab Compatibility Page with default ID
                await GetGameCompatibilityPageURL(selectedGame.Title, selectedGame.Id);

                // If it fails, try alternative id's
                if (newGame.GameCompatibilityURL == null)
                {
                    foreach (string gameId in selectedGame.AlternativeId)
                    {
                        await GetGameCompatibilityPageURL(selectedGame.Title, gameId);
                        if (newGame.GameCompatibilityURL != null)
                        {
                            break;
                        }
                    }
                }

                // Check if game has compatibility page
                if (newGame.GameCompatibilityURL != null)
                {
                    await GetCompatibilityRating();
                }
                else
                {
                    newGame.CompatibilityRating = "Unknown";
                }

                // Checking if this is a duplicate
                if (library.Games.Any(game => game.Title == newGame.Title))
                {
                    Log.Information("This game title is already in use");
                    Log.Information("Adding it as a duplicate");
                    int counter = 1;
                    string OriginalGameTitle = newGame.Title;
                    while (library.Games.Any(game => game.Title == newGame.Title))
                    {
                        newGame.Title = $"{OriginalGameTitle} ({counter})";
                        counter++;
                    }
                }
                newGame.GameFilePath = GameFilePath;
                Log.Information($"Creating a new configuration file for {newGame.Title}");
                if (File.Exists(Path.Combine(App.baseDirectory, EmulatorInfo.ConfigurationFileLocation)))
                {
                    File.Copy(Path.Combine(App.baseDirectory, EmulatorInfo.ConfigurationFileLocation), Path.Combine(App.baseDirectory, EmulatorInfo.EmulatorLocation, $@"config\{newGame.Title}.config.toml"), true);
                }
                newGame.ConfigFilePath = Path.Combine(EmulatorInfo.EmulatorLocation, $@"config\{newGame.Title}.config.toml");
                newGame.EmulatorVersion = XeniaVersion;

                // Fetching game info for artwork
                XboxMarketplaceGameInfo GameInfo = await DownloadGameInfo(gameid);
                if (GameInfo == null)
                {
                    Log.Error("Couldn't fetch game information");
                    return;
                }

                // Download Artwork
                // Download Boxart
                if (!Directory.Exists(Path.Combine(App.baseDirectory, @$"Icons\{newGame.Title}")))
                {
                    Directory.CreateDirectory(Path.Combine(App.baseDirectory, @$"Icons\{newGame.Title}"));
                }
                Log.Information("Downloading boxart");
                if (GameInfo.Artwork.Boxart == null)
                {
                    GameInfo.Artwork.Boxart = @"https://raw.githubusercontent.com/xenia-manager/Assets/main/Assets/Boxart.jpg";
                    Log.Information("Using default boxart since the game doesn't have boxart");
                    await GetGameIcon(GameInfo.Artwork.Boxart, Path.Combine(App.baseDirectory, @$"Icons\{newGame.Title}\boxart.ico"));
                }
                else
                {
                    if (await CheckIfURLWorks(GameInfo.Artwork.Boxart))
                    {
                        Log.Information("Using boxart from Xbox Marketplace");
                        await GetGameIcon(GameInfo.Artwork.Boxart, Path.Combine(App.baseDirectory, @$"Icons\{newGame.Title}\boxart.ico"));
                    }
                    else if (await CheckIfURLWorks($"https://raw.githubusercontent.com/xenia-manager/Assets/main/Assets/Marketplace/Boxart/{gameid}.jpg"))
                    {
                        Log.Information("Using boxart from Xbox Marketplace backup");
                        await GetGameIcon($"https://raw.githubusercontent.com/xenia-manager/Assets/main/Assets/Marketplace/Boxart/{gameid}.jpg", Path.Combine(App.baseDirectory, @$"Icons\{newGame.Title}\boxart.ico"));
                    }
                    else
                    {
                        Log.Information("Using default boxart as the last option");
                        await GetGameIcon($@"https://raw.githubusercontent.com/xenia-manager/Assets/main/Assets/Boxart.jpg", Path.Combine(App.baseDirectory, @$"Icons\{newGame.Title}\boxart.ico"));
                    }
                }
                newGame.BoxartFilePath = @$"Icons\{newGame.Title}\boxart.ico";

                // Download icon for shortcut
                Log.Information("Downloading icon for shortcuts");
                if (GameInfo.Artwork.Icon == null)
                {
                    GameInfo.Artwork.Icon = @"https://raw.githubusercontent.com/xenia-manager/Assets/main/Assets/Disc.png";
                    Log.Information("Using default disc image since the game doesn't have icon");
                    await GetGameIcon(GameInfo.Artwork.Icon, Path.Combine(App.baseDirectory, @$"Icons\{newGame.Title}\icon.ico"), 64, 64);
                }
                else
                {
                    if (await CheckIfURLWorks(GameInfo.Artwork.Icon))
                    {
                        Log.Information("Using game icon for shortcut icons from Xbox Marketplace");
                        await GetGameIcon(GameInfo.Artwork.Icon, Path.Combine(App.baseDirectory, @$"Icons\{newGame.Title}\icon.ico"), 64, 64);
                    }
                    else if (await CheckIfURLWorks($"https://raw.githubusercontent.com/xenia-manager/Assets/main/Assets/Marketplace/Icons/{gameid}.jpg"))
                    {
                        Log.Information("Using game icon for shortcut icons from Xbox Marketplace backup");
                        await GetGameIcon($"https://raw.githubusercontent.com/xenia-manager/Assets/main/Assets/Marketplace/Icons/{gameid}.jpg", Path.Combine(App.baseDirectory, @$"Icons\{newGame.Title}\icon.ico"), 64, 64);
                    }
                    else
                    {
                        Log.Information("Using default disc image as the last option");
                        await GetGameIcon($@"https://raw.githubusercontent.com/xenia-manager/Assets/main/Assets/Disc.png", Path.Combine(App.baseDirectory, @$"Icons\{newGame.Title}\icon.ico"), 64, 64);
                    }
                }
                newGame.ShortcutIconFilePath = @$"Icons\{newGame.Title}\icon.ico";

                Log.Information("Adding the game to the Xenia Manager");
                library.Games.Add(newGame);
                Mouse.OverrideCursor = null;
                await ClosingAnimation();
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
                Mouse.OverrideCursor = null;
            }
        }

        /// <summary>
        /// When the user selects a game from Launchbox Database
        /// </summary>
        private async void LaunchboxDatabaseGames_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            /*
            try
            {
                ListBox listBox = sender as ListBox;
                // Checking is listbox has something selected
                if (listBox == null || listBox.SelectedItem == null)
                {
                    return;
                }

                // Finding matching selected game in the list of games
                string selectedTitle = listBox.SelectedItem.ToString();
                GameInfo selectedGame = launchboxListOfGames.FirstOrDefault(game => game.Title == selectedTitle);

                if (selectedGame == null)
                {
                    listBox.SelectedItem = null;
                    return;
                }

                Mouse.OverrideCursor = Cursors.Wait;

                // Adding the game to the library
                Log.Information($"Selected Game: {selectedGame.Title}");
                newGame.Title = selectedGame.Title.Replace(":", " -").Replace('\\', ' ').Replace('/', ' ');

                newGame.GameId = gameid;
                newGame.MediaId = mediaid;
                await GetGameCompatibilityPageURL();
                if (newGame.GameCompatibilityURL != null)
                {
                    await GetCompatibilityRating();
                }
                else
                {
                    newGame.CompatibilityRating = "Unknown";
                }

                // Checking if this is a duplicate
                if (library.Games.Any(game => game.Title == newGame.Title))
                {
                    Log.Information("This game title is already in use");
                    Log.Information("Adding it as a duplicate");
                    int counter = 1;
                    string OriginalGameTitle = newGame.Title;
                    while (library.Games.Any(game => game.Title == newGame.Title))
                    {
                        newGame.Title = $"{OriginalGameTitle} ({counter})";
                        counter++;
                    }
                }

                newGame.GameFilePath = GameFilePath;
                Log.Information($"Creating a new configuration file for {newGame.Title}");
                if (File.Exists(Path.Combine(App.baseDirectory, EmulatorInfo.ConfigurationFileLocation)))
                {
                    File.Copy(Path.Combine(App.baseDirectory, EmulatorInfo.ConfigurationFileLocation), Path.Combine(App.baseDirectory, EmulatorInfo.EmulatorLocation, $@"config\{newGame.Title}.config.toml"), true);
                }
                newGame.ConfigFilePath = Path.Combine(EmulatorInfo.EmulatorLocation, $@"config\{newGame.Title}.config.toml");
                newGame.EmulatorVersion = XeniaVersion;

                // Download Boxart
                Log.Information("Downloading boxart");
                if (selectedGame.Artwork.Boxart == null)
                {
                    selectedGame.Artwork.Boxart = @"https://raw.githubusercontent.com/xenia-manager/Assets/main/Assets/Boxart.jpg";
                    Log.Information("Using default disc image since the game doesn't have boxart");
                    await GetGameIcon(selectedGame.Artwork.Boxart, Path.Combine(App.baseDirectory, @$"Icons\{newGame.Title}.ico"));
                }
                else
                {
                    if (await CheckIfURLWorks(selectedGame.Artwork.Boxart))
                    {
                        Log.Information("Using the image from Launchbox Database");
                        await GetGameIcon(selectedGame.Artwork.Boxart, Path.Combine(App.baseDirectory, @$"Icons\{newGame.Title}.ico"));
                    }
                    else
                    {
                        Log.Information("Using default disc image as the last option");
                        await GetGameIcon($@"https://raw.githubusercontent.com/xenia-manager/Assets/main/Assets/Boxart.jpg", Path.Combine(App.baseDirectory, @$"Icons\{newGame.Title}.ico"));
                    }
                }
                newGame.BoxartFilePath = @$"Icons\{newGame.Title}.ico";

                // Download icon for shortcut
                Log.Information("Downloading icon for shortcuts");
                if (selectedGame.Artwork.Disc == null)
                {
                    selectedGame.Artwork.Disc = @"https://raw.githubusercontent.com/xenia-manager/Assets/main/Assets/Disc.png";
                    Log.Information("Using default disc image since the game doesn't have icon");
                    await GetGameIcon(selectedGame.Artwork.Disc, Path.Combine(App.baseDirectory, @$"Icons\{newGame.Title} Icon.ico"), 64, 64);
                }
                else
                {
                    if (await CheckIfURLWorks(selectedGame.Artwork.Disc))
                    {
                        Log.Information("Using game disc as shortcut icon");
                        await GetGameIcon(selectedGame.Artwork.Disc, Path.Combine(App.baseDirectory, @$"Icons\{newGame.Title} Icon.ico"), 64, 64);
                    }
                    else
                    {
                        Log.Information("Using default disc image as the last option");
                        await GetGameIcon($@"https://raw.githubusercontent.com/xenia-manager/Assets/main/Assets/Disc.png", Path.Combine(App.baseDirectory, @$"Icons\{newGame.Title}.ico"), 64, 64);
                    }
                }
                newGame.ShortcutIconFilePath = @$"Icons\{newGame.Title} Icon.ico";

                Log.Information("Adding the game to the Xenia Manager");
                library.Games.Add(newGame);
                Mouse.OverrideCursor = null;
                await ClosingAnimation();
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
                Mouse.OverrideCursor = null;
            }
            */
        }

        /// <summary>
        /// When the user selects a game from Wikipedia's list
        /// </summary>
        private async void WikipediaGames_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            /*
            try
            {
                ListBox listBox = sender as ListBox;
                // Checking is listbox has something selected
                if (listBox == null || listBox.SelectedItem == null)
                {
                    return;
                }

                // Finding matching selected game in the list of games
                string selectedTitle = listBox.SelectedItem.ToString();
                GameInfo selectedGame = wikipediaListOfGames.FirstOrDefault(game => game.Title == selectedTitle);

                if (selectedGame == null)
                {
                    listBox.SelectedItem = null;
                    return;
                }

                Mouse.OverrideCursor = Cursors.Wait;

                // Adding the game to the library
                Log.Information($"Selected Game: {selectedGame.Title}");
                newGame.Title = selectedGame.Title.Replace(":", " -").Replace('\\', ' ').Replace('/', ' ');

                newGame.GameId = gameid;
                newGame.MediaId = mediaid;
                await GetGameCompatibilityPageURL();
                if (newGame.GameCompatibilityURL != null)
                {
                    await GetCompatibilityRating();
                }
                else
                {
                    newGame.CompatibilityRating = "Unknown";
                }

                // Checking if this is a duplicate
                if (library.Games.Any(game => game.Title == newGame.Title))
                {
                    Log.Information("This game title is already in use");
                    Log.Information("Adding it as a duplicate");
                    int counter = 1;
                    string OriginalGameTitle = newGame.Title;
                    while (library.Games.Any(game => game.Title == newGame.Title))
                    {
                        newGame.Title = $"{OriginalGameTitle} ({counter})";
                        counter++;
                    }
                }
                newGame.GameFilePath = GameFilePath;
                Log.Information($"Creating a new configuration file for {newGame.Title}");
                if (File.Exists(Path.Combine(App.baseDirectory, EmulatorInfo.ConfigurationFileLocation)))
                {
                    File.Copy(Path.Combine(App.baseDirectory, EmulatorInfo.ConfigurationFileLocation), Path.Combine(App.baseDirectory, EmulatorInfo.EmulatorLocation, $@"config\{newGame.Title}.config.toml"), true);
                }
                newGame.ConfigFilePath = Path.Combine(EmulatorInfo.EmulatorLocation, $@"config\{newGame.Title}.config.toml");
                newGame.EmulatorVersion = XeniaVersion;

                // Download Boxart
                Log.Information("Downloading boxart");
                if (selectedGame.Artwork.Boxart == null)
                {
                    selectedGame.Artwork.Boxart = @"https://raw.githubusercontent.com/xenia-manager/Assets/main/Assets/Boxart.jpg";
                    Log.Information("Using default disc image since the game doesn't have boxart on Wikipedia");
                    await GetGameIcon(selectedGame.Artwork.Boxart, Path.Combine(App.baseDirectory, @$"Icons\{newGame.Title}.ico"));
                }
                else
                {
                    if (await CheckIfURLWorks(selectedGame.Artwork.Boxart))
                    {
                        Log.Information("Using the image from Wikipedia");
                        await GetGameIcon(selectedGame.Artwork.Boxart, Path.Combine(App.baseDirectory, @$"Icons\{newGame.Title}.ico"));
                    }
                    else
                    {
                        // Using the default disc box art
                        Log.Information("Using default disc image as the last option");
                        await GetGameIcon($@"https://raw.githubusercontent.com/xenia-manager/Assets/main/Assets/Boxart.jpg", Path.Combine(App.baseDirectory, @$"Icons\{newGame.Title}.ico"));
                    }
                }
                newGame.BoxartFilePath = @$"Icons\{newGame.Title}.ico";

                // Download icon for shortcut
                Log.Information("Downloading icon for shortcuts");
                await GetGameIcon(@"https://raw.githubusercontent.com/xenia-manager/Assets/main/Assets/Disc.png", Path.Combine(App.baseDirectory, @$"Icons\{newGame.Title} Icon.ico"), 64, 64);
                newGame.ShortcutIconFilePath = @$"Icons\{newGame.Title} Icon.ico";

                Log.Information("Adding the game to the Xenia Manager");
                library.Games.Add(newGame);
                Mouse.OverrideCursor = null;
                await ClosingAnimation();
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
                Mouse.OverrideCursor = null;
            }
            */
        }
    }
}
