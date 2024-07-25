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
        // These 2 lists hold unfiltered and filtered list of games in Xbox Marketplace's list of games
        List<GameInfo> XboxMarketplaceListOfGames = new List<GameInfo>();
        private List<string> XboxMarketplaceFilteredGames = new List<string>();

        /// <summary>
        /// Used to track if it's the initial search and if it is, search based on GameID for Xbox Marketplace and every other based on game title
        /// </summary>
        private bool isFirstSearch = true;

        // These 2 lists hold unfiltered and filtered list of games in Wikipedia's list of games
        List<GameInfo> wikipediaListOfGames = new List<GameInfo>();
        private List<string> wikipediafilteredGames = new List<string>();

        // These 2 lists hold unfiltered and filtered list of games in Andy Decarli's list of games
        List<GameInfo> AndyListOfGames = new List<GameInfo>();
        private List<string> ADfilteredGames = new List<string>();

        // These variables get imported from Library page, used to grab the game
        private Library library;
        private string gameTitle = "";
        private string gameid = "";
        private string GameFilePath = "";
        private string XeniaVersion = "";
        private EmulatorInfo EmulatorInfo;

        // Holds game that user wants to add to the Manager
        public InstalledGame newGame = new InstalledGame();

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
        public SelectGame(Library library, string selectedGame, string selectedGameid, string selectedGamePath, string XeniaVersion,EmulatorInfo emulatorInfo)
        {
            InitializeComponent();
            if (selectedGame != null)
            {
                this.gameTitle = selectedGame;
                this.gameid = selectedGameid;
            }
            this.GameFilePath = selectedGamePath;
            this.library = library;
            this.XeniaVersion = XeniaVersion;
            this.EmulatorInfo = emulatorInfo;
            InitializeAsync();
            Closed += (sender, args) => _closeTaskCompletionSource.TrySetResult(true);
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
                string url = "https://gist.githubusercontent.com/shazzaam7/16586d083134186e31ac8e95a32c9185/raw/0388624d557bb94c7c4074528f467b69363c3b93/xbox360_marketplace_games_list.json";
                using (HttpClient client = new HttpClient())
                {
                    try
                    {
                        client.DefaultRequestHeaders.Add("User-Agent", "Xenia Manager (https://github.com/xenia-manager/xenia-manager)");
                        HttpResponseMessage response = await client.GetAsync(url);
                        if (response.IsSuccessStatusCode)
                        {
                            string json = await response.Content.ReadAsStringAsync();

                            XboxMarketplaceListOfGames = JsonConvert.DeserializeObject<List<GameInfo>>(json);
                            displayItems = XboxMarketplaceListOfGames.Select(game => game.Title).ToList();

                            XboxMarketplaceGames.Items.Clear();
                            XboxMarketplaceGames.ItemsSource = displayItems;
                        }
                        else
                        {
                            Log.Error($"Failed to load data. Status code: {response.StatusCode}");
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

                            wikipediaListOfGames = JsonConvert.DeserializeObject<List<GameInfo>>(json);
                            displayItems = wikipediaListOfGames.Select(game => game.Title).ToList();

                            WikipediaGames.Items.Clear();
                            WikipediaGames.ItemsSource = displayItems;
                        }
                        else
                        {
                            Log.Error($"Failed to load data. Status code: {response.StatusCode}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.Message, "");
                        MessageBox.Show(ex.Message + "\nFull Error:\n" + ex);
                    }
                }

                // Andy Decarli's list
                Log.Information("Loading Andy Decarli's list of games");
                url = "https://gist.githubusercontent.com/shazzaam7/8963fadefcdafa697ab8506375aecfed/raw/b4803145e94329cad513e8777dd56f611d30bc5e/xbox360_andydecarli_games_list.json";
                using (HttpClient client = new HttpClient())
                {
                    try
                    {
                        client.DefaultRequestHeaders.Add("User-Agent", "Xenia Manager (https://github.com/xenia-manager/xenia-manager)");
                        HttpResponseMessage response = await client.GetAsync(url);
                        if (response.IsSuccessStatusCode)
                        {
                            string json = await response.Content.ReadAsStringAsync();

                            AndyListOfGames = JsonConvert.DeserializeObject<List<GameInfo>>(json);
                            displayItems = AndyListOfGames.Select(game => game.Title).ToList();

                            AndyDecarliGames.Items.Clear();
                            AndyDecarliGames.ItemsSource = displayItems;
                        }
                        else
                        {
                            Log.Error($"Failed to load data. Status code: {response.StatusCode}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.Message, "");
                        MessageBox.Show(ex.Message + "\nFull Error:\n" + ex);
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
        /// This function is for games that aren't in the lists
        /// </summary>
        private async Task AddUnknownGames()
        {
            try
            {
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
                    await GetGameIcon($@"https://raw.githubusercontent.com/xenia-manager/xenia-manager-database/main/Assets/disc.png", Path.Combine(App.baseDirectory, @$"Icons\{newGame.Title}.ico"));
                    newGame.IconFilePath = @$"Icons\{newGame.Title}.ico";
                    Log.Information("Adding the game to the Xenia Manager");
                    library.Games.Add(newGame);
                }
                else
                {
                    Log.Information("Game is already in the Xenia Manager");
                }
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
                SearchBox.Text = gameid; // Initial search is by gameID
                Log.Information("Doing the search by gameid");
                bool successfulSearchByID = false;
                if (XboxMarketplaceGames.Items.Count > 0)
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

                if (!successfulSearchByID)
                {
                    // This is a check if there are no games in the list after the initial search
                    if (XboxMarketplaceGames.Items.Count > 0)
                    {
                        Log.Information("There are some results in Xbox Marketplace list");
                        SourceSelector.SelectedIndex = 0;
                    }
                    else if (WikipediaGames.Items.Count > 0)
                    {
                        Log.Information("There are some results in Wikipedia's list");
                        SourceSelector.SelectedIndex = 1;
                    }
                    else if (AndyDecarliGames.Items.Count > 0)
                    {
                        Log.Information("There are some results in Andy Decarli's list");
                        SourceSelector.SelectedIndex = 2;
                    }
                    else
                    {
                        Log.Information("No game found");
                        MessageBoxResult result = MessageBox.Show($"Couldn't find {gameTitle} in our lists of games. This can be due to formatting.\nDo you want to use the default disc icon? (Press No if you want to search for the game yourself)", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (result == MessageBoxResult.Yes)
                        {
                            await AddUnknownGames();
                        }
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

            // Wikipedia filtering
            WikipediaGames.ItemsSource = wikipediafilteredGames;

            // Andy Decarli's filtering
            AndyDecarliGames.ItemsSource = ADfilteredGames;

            if (XboxMarketplaceGames.Items.Count > 0)
            {
                SourceSelector.SelectedIndex = 0;
            }
            else if (WikipediaGames.Items.Count > 0)
            {
                SourceSelector.SelectedIndex = 1;
            }
            else if (AndyDecarliGames.Items.Count > 0)
            {
                SourceSelector.SelectedIndex = 2;
            }
        }

        /// <summary>
        /// This filters the Listbox items to the searchbox
        /// </summary>
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchQuery = SearchBox.Text.ToLower();
            if (isFirstSearch)
            {
                // Initial search by GameID
                XboxMarketplaceFilteredGames = XboxMarketplaceListOfGames
                    .Where(game => game.GameID.ToLower().Contains(searchQuery))
                    .Select(game => game.Title)
                    .ToList();

                // Set the flag to false after the first search
                isFirstSearch = false;
            }
            else
            {
                // Subsequent searches by Title
                XboxMarketplaceFilteredGames = XboxMarketplaceListOfGames
                    .Where(game => game.Title.ToLower().Contains(searchQuery))
                    .Select(game => game.Title)
                    .ToList();
            }
            wikipediafilteredGames = wikipediaListOfGames.Where(game => game.Title.ToLower().Contains(searchQuery)).Select(game => game.Title).ToList();
            ADfilteredGames = AndyListOfGames.Where(game => game.Title.ToLower().Contains(searchQuery)).Select(game => game.Title).ToList();
            UpdateListBoxes();
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

            switch (SourceSelector.SelectedIndex)
            {
                case 1:
                    // Wikipedia list of games
                    XboxMarketplaceGames.Visibility = Visibility.Collapsed;
                    WikipediaGames.Visibility = Visibility.Visible;
                    AndyDecarliGames.Visibility = Visibility.Collapsed;
                    break;
                case 2:
                    // Andy Decarli's list of games
                    XboxMarketplaceGames.Visibility = Visibility.Collapsed;
                    WikipediaGames.Visibility = Visibility.Collapsed;
                    AndyDecarliGames.Visibility = Visibility.Visible;
                    break;
                default:
                    // Xbox Marketplace list of games
                    XboxMarketplaceGames.Visibility = Visibility.Visible;
                    WikipediaGames.Visibility = Visibility.Collapsed;
                    AndyDecarliGames.Visibility = Visibility.Collapsed;
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
            await ClosingAnimation();
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
                    return response.IsSuccessStatusCode;
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
                            double aspectRatio = (double)width / height;
                            magickImage.Resize(width, height);

                            double imageRatio = (double)magickImage.Width / magickImage.Height;
                            int newWidth, newHeight, offsetX, offsetY;

                            if (imageRatio > aspectRatio)
                            {
                                newWidth = width;
                                newHeight = (int)Math.Round(width / imageRatio);
                                offsetX = 0;
                                offsetY = (height - newHeight) / 2;
                            }
                            else
                            {
                                newWidth = (int)Math.Round(height * imageRatio);
                                newHeight = height;
                                offsetX = (width - newWidth) / 2;
                                offsetY = 0;
                            }

                            // Create a canvas with black background
                            using (var canvas = new MagickImage(MagickColors.Black, width, height))
                            {
                                // Composite the resized image onto the canvas
                                canvas.Composite(magickImage, offsetX, offsetY, CompositeOperator.SrcOver);

                                // Convert to ICO format
                                canvas.Format = MagickFormat.Ico;
                                canvas.Write(outputPath);
                            }
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
        private async Task GetGameCompatibilityPageURL()
        {
            try
            {
                Log.Information($"Trying to find the compatibility page for {newGame.Title}");
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "Xenia Manager (https://github.com/xenia-manager/xenia-manager)");
                    client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");

                    HttpResponseMessage response = await client.GetAsync($"https://api.github.com/search/issues?q={newGame.GameId}%20in%3Atitle%20repo%3Axenia-project%2Fgame-compatibility");

                    if (response.IsSuccessStatusCode)
                    {
                        string json = await response.Content.ReadAsStringAsync();
                        JObject jsonObject = JObject.Parse(json);
                        JArray searchResults = (JArray)jsonObject["items"];
                        switch (searchResults.Count)
                        {
                            case 0:
                                Log.Information($"The compatibility page for {newGame.Title} isn't found");
                                newGame.GameCompatibilityURL = null;
                                break;
                            case 1:
                                Log.Information($"Found the compatibility page for {newGame.Title}");
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
                                    if (resultTitle == newGame.Title)
                                    {
                                        Log.Information($"Found the compatibility page for {newGame.Title}");
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
        /// When the user selects a game from XboxMarketplace's list of games
        /// </summary>
        private async void XboxMarketplaceGames_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                ListBox listBox = sender as ListBox;
                if (listBox != null && listBox.SelectedItem != null)
                {
                    string selectedTitle = listBox.SelectedItem.ToString();
                    GameInfo selectedGame = XboxMarketplaceListOfGames.FirstOrDefault(game => game.Title == selectedTitle);
                    if (selectedGame != null)
                    {
                        Mouse.OverrideCursor = Cursors.Wait;
                        Log.Information($"Selected Game: {selectedGame.Title}");
                        newGame.Title = selectedGame.Title.Replace(":", " -").Replace('\\', ' ').Replace('/', ' ');
                        newGame.GameId = selectedGame.GameID;
                        await GetGameCompatibilityPageURL();
                        newGame.GameFilePath = GameFilePath;
                        Log.Information($"Creating a new configuration file for {newGame.Title}");
                        if (File.Exists(Path.Combine(App.baseDirectory, EmulatorInfo.ConfigurationFileLocation)))
                        {
                            File.Copy(Path.Combine(App.baseDirectory, EmulatorInfo.ConfigurationFileLocation), Path.Combine(App.baseDirectory, EmulatorInfo.EmulatorLocation, $@"config\{newGame.Title}.config.toml"), true);
                        }
                        newGame.ConfigFilePath = Path.Combine(EmulatorInfo.EmulatorLocation, $@"config\{newGame.Title}.config.toml");
                        newGame.EmulatorVersion = XeniaVersion;
                        if (!library.Games.Any(game => game.Title == newGame.Title))
                        {
                            if (selectedGame.BoxArt == null)
                            {
                                selectedGame.BoxArt = @"https://raw.githubusercontent.com/xenia-manager/xenia-manager-database/main/Assets/disc.png";
                                Log.Information("Using default disc image since the game doesn't have boxart");
                                await GetGameIcon(selectedGame.BoxArt, Path.Combine(App.baseDirectory, @$"Icons\{newGame.Title}.ico"));
                            }
                            else
                            {
                                if (await CheckIfURLWorks(selectedGame.BoxArt))
                                {
                                    Log.Information("Using the image from Xbox Marketplace");
                                    await GetGameIcon(selectedGame.BoxArt, Path.Combine(App.baseDirectory, @$"Icons\{newGame.Title}.ico"));
                                }
                                else
                                {
                                    Log.Information("Using default disc image as the last option");
                                    await GetGameIcon($@"https://raw.githubusercontent.com/xenia-manager/xenia-manager-database/main/Assets/disc.png", Path.Combine(App.baseDirectory, @$"Icons\{newGame.Title}.ico"));
                                }
                            }
                            newGame.IconFilePath = @$"Icons\{newGame.Title}.ico";
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
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
                Mouse.OverrideCursor = null;
            }
        }

        /// <summary>
        /// When the user selects a game from Wikipedia's list
        /// </summary>
        private async void WikipediaGames_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                ListBox listBox = sender as ListBox;
                if (listBox != null && listBox.SelectedItem != null)
                {
                    string selectedItem = listBox.SelectedItem.ToString();
                    GameInfo selectedGame = wikipediaListOfGames.FirstOrDefault(game => game.Title == selectedItem);
                    if (selectedGame != null)
                    {
                        Mouse.OverrideCursor = Cursors.Wait;
                        Log.Information($"Selected Game: {selectedGame.Title}");
                        newGame.Title = selectedGame.Title.Replace(":", " -").Replace('\\', ' ').Replace('/', ' ');
                        newGame.GameId = gameid;
                        await GetGameCompatibilityPageURL();
                        newGame.GameFilePath = GameFilePath;
                        Log.Information($"Creating a new configuration file for {newGame.Title}");
                        if (File.Exists(Path.Combine(App.baseDirectory, EmulatorInfo.ConfigurationFileLocation)))
                        {
                            File.Copy(Path.Combine(App.baseDirectory, EmulatorInfo.ConfigurationFileLocation), Path.Combine(App.baseDirectory, EmulatorInfo.EmulatorLocation, $@"config\{newGame.Title}.config.toml"), true);
                        }
                        newGame.ConfigFilePath = Path.Combine(EmulatorInfo.EmulatorLocation, $@"config\{newGame.Title}.config.toml");
                        newGame.EmulatorVersion = XeniaVersion;
                        if (!library.Games.Any(game => game.Title == newGame.Title))
                        {
                            // Checking if the URL Works
                            if (selectedGame.ImageUrl == null)
                            {
                                selectedGame.ImageUrl = @"https://raw.githubusercontent.com/xenia-manager/xenia-manager-database/main/Assets/disc.png";
                                Log.Information("Using default disc image since the game doesn't have boxart on Wikipedia");
                                await GetGameIcon(selectedGame.ImageUrl, Path.Combine(App.baseDirectory, @$"Icons\{newGame.Title}.ico"));
                            }
                            else
                            {
                                if (await CheckIfURLWorks(selectedGame.ImageUrl))
                                {
                                    Log.Information("Using the image from Wikipedia");
                                    await GetGameIcon(selectedGame.ImageUrl, Path.Combine(App.baseDirectory, @$"Icons\{newGame.Title}.ico"));
                                }
                                else
                                {
                                    // Using the default disc box art
                                    Log.Information("Using default disc image as the last option");
                                    await GetGameIcon($@"https://raw.githubusercontent.com/xenia-manager/xenia-manager-database/main/Assets/disc.png", Path.Combine(App.baseDirectory, @$"Icons\{newGame.Title}.ico"));
                                }
                            }
                            newGame.IconFilePath = @$"Icons\{newGame.Title}.ico";
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
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
                Mouse.OverrideCursor = null;
            }
        }

        /// <summary>
        /// When the user selects a game from Andy Declari's list
        /// </summary>
        private async void AndyDecarliGames_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                ListBox listBox = sender as ListBox;
                if (listBox != null && listBox.SelectedItem != null)
                {
                    string selectedItem = listBox.SelectedItem.ToString();
                    GameInfo selectedGame = AndyListOfGames.FirstOrDefault(game => game.Title == selectedItem);
                    if (selectedGame != null)
                    {
                        Mouse.OverrideCursor = Cursors.Wait;
                        Log.Information($"Selected Game: {selectedGame.Title}");
                        newGame.Title = selectedGame.Title.Replace(":", " -").Replace('\\', ' ').Replace('/', ' ');
                        newGame.GameId = gameid;
                        await GetGameCompatibilityPageURL();
                        newGame.GameFilePath = GameFilePath;
                        Log.Information($"Creating a new configuration file for {newGame.Title}");
                        if (File.Exists(Path.Combine(App.baseDirectory, EmulatorInfo.ConfigurationFileLocation)))
                        {
                            File.Copy(Path.Combine(App.baseDirectory, EmulatorInfo.ConfigurationFileLocation), Path.Combine(App.baseDirectory, EmulatorInfo.EmulatorLocation, $@"config\{newGame.Title}.config.toml"), true);
                        }
                        newGame.ConfigFilePath = Path.Combine(EmulatorInfo.EmulatorLocation, $@"config\{newGame.Title}.config.toml");
                        newGame.EmulatorVersion = XeniaVersion;
                        if (!library.Games.Any(game => game.Title == newGame.Title))
                        {
                            // Checking if the URL Works
                            if (await CheckIfURLWorks(selectedGame.Front.Thumbnail))
                            {
                                Log.Information("Using the image from Andy Decarli's website");
                                await GetGameIcon(selectedGame.Front.Thumbnail, Path.Combine(App.baseDirectory, @$"Icons\{newGame.Title}.ico"));
                            }
                            else if (await CheckIfURLWorks($@"https://raw.githubusercontent.com/xenia-manager/xenia-manager-database/main/Assets/Front/Thumbnail/{selectedGame.Title.Replace(" ", "_")}.jpg"))
                            {
                                Log.Information("Using the image from xenia-manager-database repository");
                                await GetGameIcon($@"https://raw.githubusercontent.com/xenia-manager/xenia-manager-database/main/Assets/Front/Thumbnail/{selectedGame.Title.Replace(" ", "_")}.jpg", Path.Combine(App.baseDirectory, @$"Icons\{newGame.Title}.ico"));
                            }
                            else
                            {
                                Log.Information("Using default disc image as the last option");
                                // Using the default disc box art
                                await GetGameIcon($@"https://raw.githubusercontent.com/xenia-manager/xenia-manager-database/main/Assets/disc.png", Path.Combine(App.baseDirectory, @$"Icons\{newGame.Title}.ico"));
                            }
                            newGame.IconFilePath = @$"Icons\{newGame.Title}.ico";
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
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
                Mouse.OverrideCursor = null;
            }
        }
    }
}
