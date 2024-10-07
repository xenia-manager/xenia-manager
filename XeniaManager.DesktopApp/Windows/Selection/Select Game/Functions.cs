using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

// Imported
using Serilog;
using XeniaManager.Database;

namespace XeniaManager.DesktopApp.Windows
{
    public partial class SelectGame : Window
    {

        /// <summary>
        /// Used to emulate a WaitForCloseAsync function that is similar to the one Process Class has
        /// </summary>
        public Task WaitForCloseAsync()
        {
            return closeTaskCompletionSource.Task;
        }

        /// <summary>
        /// Used to read the games from the "databases" 
        /// </summary>
        private async Task ReadGames()
        {
            // Xbox Marketplace List
            Log.Information("Loading Xbox Marketplace list of games");
            string url = "https://raw.githubusercontent.com/xenia-manager/Database/refs/heads/main/Database/xbox_marketplace_games.json";
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
    }
}
