using System.Net.Http;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

// Imported
using Serilog;
using XeniaManager.Database;
using XeniaManager.DesktopApp.Utilities.Animations;

namespace XeniaManager.DesktopApp.Windows
{
    public partial class SelectGame
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
                    HttpResponseMessage response;
                    try
                    {
                        response = await client.GetAsync(url);
                    }
                    catch (HttpRequestException)
                    {
                        Log.Error("Unable to load the Xbox Marketplace source");
                        XboxMarketplace.Load("[]");
                        return;
                    }
                    if (response.IsSuccessStatusCode)
                    {
                        string json = await response.Content.ReadAsStringAsync();
                        if (!XboxMarketplace.Load(json))
                        {
                            CmbSourceSelector.Items.Remove((ComboBoxItem)CmbSourceSelector.Items.Cast<ComboBoxItem>().FirstOrDefault(i => i.Content.ToString() == "Xbox Marketplace"));
                        }
                    }
                    else
                    {
                        Log.Error($"Failed to load Xbox Marketplace ({response.StatusCode})");
                        CmbSourceSelector.Items.Remove((ComboBoxItem)CmbSourceSelector.Items.Cast<ComboBoxItem>().FirstOrDefault(i => i.Content.ToString() == "Xbox Marketplace"));
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
                // TODO: Remove this when removing Launchbox Database or replace it with a new source
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
            TxtSearchBox.Text = gameid;
            await searchCompletionSource.Task; // Wait for search to finish before continuing
            bool successfulSearchById = false;
            // Check if there are any games in XboxMarketplace list, if they are, show the ListBox
            if (LstXboxMarketplaceGames.Items.Count > 0)
            {
                CmbSourceSelector.SelectedIndex = 0;
                successfulSearchById = true;
            }

            // If search by TitleID fails, search by game title
            if (!successfulSearchById)
            {
                Log.Information("No games found using id to search");
                TxtSearchBox.Text = Regex.Replace(gameTitle, @"[^a-zA-Z0-9\s]", "");
                Log.Information("Doing search by game title");
            }
            await searchCompletionSource.Task; // Wait for search to finish before continuing

            // Check if there are any games in XboxMarketplace list, if they are, show the ListBox
            if (LstXboxMarketplaceGames.Items.Count > 0)
            {
                Log.Information("There are some results in Xbox Marketplace list");
                CmbSourceSelector.SelectedIndex = 0;
            }
            else
            {
                Log.Information("No games found");
                MessageBoxResult result = MessageBox.Show($"'{gameTitle}' was not found in our database, possibly due to formatting differences.\nWould you like to use the default disc icon instead? (Select No if you prefer to search for the game manually.)", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    await GameManager.AddUnknownGameToLibrary(gameTitle, gameid, mediaid, gamePath, xeniaVersion);
                    WindowAnimations.ClosingAnimation(this);
                }
                else
                {
                    CmbSourceSelector.SelectedIndex = 0;
                };
            }

            // Do automatic adding if there's only 1 game left after the search
            if (LstXboxMarketplaceGames.Items.Count == 1 && ConfigurationManager.AppConfig.AutomaticGameParsingSelection == true)
            {
                // Finding matching selected game in the list of games
                string selectedTitle = LstXboxMarketplaceGames.Items[0]?.ToString();
                GameInfo selectedGame = XboxMarketplace.GetGameInfo(selectedTitle);
                if (selectedGame != null)
                {
                    Log.Information("Automatically adding the game");
                    Log.Information($"Title: {selectedGame.Title}");
                    await GameManager.AddGameToLibrary(selectedGame, gameid, mediaid, gamePath, xeniaVersion);
                    gameFound = true; // This is to ensure the window doesn't show since we already added the game
                    WindowAnimations.ClosingAnimation(this);
                }
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
                    // This is a check for automatic adding of games so the window doesn't show for no reason
                    if (!gameFound)
                    {
                        this.Visibility = Visibility.Visible;
                    }
                    Mouse.OverrideCursor = null;
                });
            }
        }
    }
}
