using System;
using System.IO;
using System.Net.Http;
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
        // These 2 lists hold unfiltered and filtered list of games in Andy Decarli's list of games
        List<GameInfo> AndyListOfGames = new List<GameInfo>();
        private List<string> ADfilteredGames = new List<string>();

        // These 2 lists hold unfiltered and filtered list of games in Wikipedia's list of games
        List<GameInfo> wikipediaListOfGames = new List<GameInfo>();
        private List<string> wikipediafilteredGames = new List<string>();

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
                // Andy Decarli's list
                string url = "https://raw.githubusercontent.com/xenia-manager/xenia-manager-database/main/games_database.json";
                using (HttpClient client = new HttpClient())
                {
                    try
                    {
                        HttpResponseMessage response = await client.GetAsync(url);
                        if (response.IsSuccessStatusCode)
                        {
                            string json = await response.Content.ReadAsStringAsync();

                            AndyListOfGames = JsonConvert.DeserializeObject<List<GameInfo>>(json);
                            foreach (GameInfo game in AndyListOfGames)
                            {
                                AndyDecarliGames.Items.Add(game.Title);
                            }
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
                url = "https://raw.githubusercontent.com/xenia-manager/xenia-manager-database/main/gamesdb.json";
                using (HttpClient client = new HttpClient())
                {
                    try
                    {
                        HttpResponseMessage response = await client.GetAsync(url);
                        if (response.IsSuccessStatusCode)
                        {
                            string json = await response.Content.ReadAsStringAsync();

                            wikipediaListOfGames = JsonConvert.DeserializeObject<List<GameInfo>>(json);
                            foreach (GameInfo game in wikipediaListOfGames)
                            {
                                WikipediaGames.Items.Add(game.Title);
                            }
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
                newGame.Title = gameTitle.Replace(":", " -");
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
                SearchBox.Text = gameTitle;

                // This is a check if there are no games in the list after the initial search
                if (AndyDecarliGames.Items.Count == 0 && WikipediaGames.Items.Count == 0)
                {
                    Log.Information("No game found in both of the databases");
                    await AddUnknownGames();
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
            AndyDecarliGames.ItemsSource = null;
            AndyDecarliGames.Items.Clear();
            AndyDecarliGames.ItemsSource = ADfilteredGames;

            WikipediaGames.ItemsSource = null;
            WikipediaGames.Items.Clear();
            WikipediaGames.ItemsSource = wikipediafilteredGames;

            if (AndyDecarliGames.Items.Count > 0 && WikipediaGames.Items.Count == 0)
            {
                Log.Information("Disabling Wikipedia's list since there are no games found there");
                AndyDecarliRadioButton.IsChecked = true;
            }
            else if (AndyDecarliGames.Items.Count == 0 && WikipediaGames.Items.Count > 0)
            {
                Log.Information("Disabling AndyDecarli's list since there are no games found there");
                WikipediaRadioButton.IsChecked = true;
            }
            else
            {
                AndyDecarliRadioButton.IsChecked = true;
                WikipediaRadioButton.IsChecked = false;
            }
        }

        /// <summary>
        /// This filters the Listbox items to the searchbox
        /// </summary>
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchQuery = SearchBox.Text.ToLower();
            ADfilteredGames = AndyListOfGames.Where(game => game.Title.ToLower().Contains(searchQuery)).Select(game => game.Title).ToList();
            wikipediafilteredGames = wikipediaListOfGames.Where(game => game.Title.ToLower().Contains(searchQuery)).Select(game => game.Title).ToList();
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
        /// Checks which radio button is pressed and makes the corresponding list visible
        /// </summary>
        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (sender == AndyDecarliRadioButton)
            {
                Log.Information("Making Andy Declari's list visible");
                AndyDecarliGames.Visibility = Visibility.Visible;
                WikipediaGames.Visibility = Visibility.Collapsed;
            }
            else if (sender == WikipediaRadioButton)
            {
                Log.Information("Making Wikipedia's list visible");
                AndyDecarliGames.Visibility = Visibility.Collapsed;
                WikipediaGames.Visibility = Visibility.Visible;
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
                    client.DefaultRequestHeaders.Add("User-Agent", "C# HttpClient");

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
                    client.DefaultRequestHeaders.Add("User-Agent", "C# HttpClient");
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
                        newGame.Title = selectedGame.Title.Replace(":", " -");
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
                            await GetGameIcon($@"https://raw.githubusercontent.com/xenia-manager/xenia-manager-database/main/Assets/Front/Thumbnail/{selectedGame.Title.Replace(" ", "_")}.jpg", Path.Combine(App.baseDirectory, @$"Icons\{newGame.Title}.ico"));
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
                        if (selectedGame.ImageUrl == null)
                        {
                            selectedGame.ImageUrl = @"https://raw.githubusercontent.com/xenia-manager/xenia-manager-database/main/Assets/disc.png";
                        }
                        newGame.Title = selectedGame.Title.Replace(":", " -");
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
                            await GetGameIcon(selectedGame.ImageUrl, Path.Combine(App.baseDirectory, @$"Icons\{newGame.Title}.ico"));
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
