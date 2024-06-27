using System;
using System.Windows;
using System.Net.Http;

// Imported
using Serilog;
using Newtonsoft.Json;
using Xenia_Manager.Classes;
using Xenia_Manager.Pages;
using Library = Xenia_Manager.Pages.Library;
using ImageMagick;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using Microsoft.Extensions.DependencyModel;
using System.Windows.Input;
using System.IO;

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
        public SelectGame(Library library, string selectedGame, string selectedGameid, string selectedGamePath)
        {
            InitializeComponent();
            if (selectedGame != null)
            {
                this.gameTitle = selectedGame;
                this.gameid = selectedGameid;
            }
            this.GameFilePath = selectedGamePath;
            this.library = library;
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
                SearchBox.Text = gameTitle;
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
                await Dispatcher.InvokeAsync(() => this.Visibility = Visibility.Hidden);
                await ReadGames();
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
            }
            finally
            {
                await Dispatcher.InvokeAsync(() => this.Visibility = Visibility.Visible);
            }
        }

        /// <summary>
        /// Used to execute fade in animation when loading is finished
        /// </summary>
        private void MainWindow_VisibilityChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.Visibility == Visibility.Visible)
            {
                Storyboard fadeInStoryboard = this.FindResource("FadeInStoryboard") as Storyboard;
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
        /// Closes this window
        /// </summary>
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
