using ImageMagick;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Xenia_Manager.Classes;

namespace Xenia_Manager.Windows
{
    /// <summary>
    /// Interaction logic for SelectGamePatch.xaml
    /// </summary>
    public partial class SelectGamePatch : Window
    {
        // We store the selected game here
        private InstalledGame selectedGame;

        // These 2 lists hold unfiltered and filtered list of Xenia Canary game patches
        List<GamePatch> patches = new List<GamePatch>();
        private List<string> filteredPatches = new List<string>();

        /// <summary>
        /// Default starting constructor
        /// </summary>
        public SelectGamePatch()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Constructor when adding a patch to the game
        /// </summary>
        /// <param name="game">This holds the game that we're adding a patch for</param>
        public SelectGamePatch(InstalledGame game)
        {
            InitializeComponent();
            this.selectedGame = game;
            InitializeAsync();
        }

        /// <summary>
        /// Function that grabs all of the Xenia Canary game patches
        /// </summary>
        /// <returns></returns>
        private async Task ReadGamePatches()
        {
            try
            {
                string url = "https://raw.githubusercontent.com/xenia-manager/xenia-manager-database/main/game-patches.json";
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "C# HttpClient");
                    HttpResponseMessage response = await client.GetAsync(url);

                    if (response.IsSuccessStatusCode)
                    {
                        string json = await response.Content.ReadAsStringAsync();
                        patches = JsonConvert.DeserializeObject<List<GamePatch>>(json);
                        foreach (GamePatch patch in patches)
                        {
                            PatchesList.Items.Add(patch.gameName);
                        }
                        if (selectedGame != null)
                        {
                            SearchBox.Text = selectedGame.GameId;
                        }
                    }
                    else
                    {
                        Log.Error($"Failed to fetch folder contents. Status code: {response.StatusCode}");
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
                await ReadGamePatches();
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
        private void Window_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
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
        /// Closes this window
        /// </summary>
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Log.Information("Closing SelectGamePatch window");
            this.Close();
        }

        /// <summary>
        /// This filters the Listbox items based on what's in the SearchBox
        /// </summary>
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchQuery = SearchBox.Text.ToLower();
            filteredPatches = patches.Where(game => game.gameName.ToLower().Contains(searchQuery)).Select(game => game.gameName).ToList();

            PatchesList.Items.Clear();
            PatchesList.ItemsSource = filteredPatches;
        }
    }
}
