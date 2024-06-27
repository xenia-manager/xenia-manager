using System;
using System.Windows;
using System.Net.Http;

// Imported
using Serilog;
using Newtonsoft.Json;
using Xenia_Manager.Classes;
using ImageMagick;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace Xenia_Manager.Windows
{
    /// <summary>
    /// Interaction logic for SelectGame.xaml
    /// </summary>
    public partial class SelectGame : Window
    {
        List<GameInfo> AndyListOfGames = new List<GameInfo>();
        private List<string> ADfilteredGames = new List<string>();

        List<GameInfo> wikipediaListOfGames = new List<GameInfo>();
        private List<string> wikipediafilteredGames = new List<string>();

        private string gameTitle = "";
        private string gameid = "";
        private string GameFilePath = "";

        public SelectGame()
        {
            InitializeComponent();
            InitializeAsync();
        }

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
        /// Used to read the games from the "database" 
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
                            SearchBox.Text = gameTitle;
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
                            SearchBox.Text = gameTitle;
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
        /// Closes this window
        /// </summary>
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
