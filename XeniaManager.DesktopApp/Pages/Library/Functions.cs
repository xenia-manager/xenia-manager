using System.IO;
using System.Windows.Input;

// Imported
using Serilog;
using XeniaManager.DesktopApp.CustomControls;
using XeniaManager.DesktopApp.Windows;

namespace XeniaManager.DesktopApp.Pages
{
    public partial class Library
    {
        /// <summary>
        /// Loads the games into the Wrap panel
        /// </summary>
        private void LoadGamesIntoUI()
        {
            // Check if there are any games installed
            if (GameManager.Games == null && GameManager.Games.Count <= 0)
            {
                return;
            }

            Log.Information("Loading games into the UI");
            // Sort the games by name
            IOrderedEnumerable<Game> orderedGames = GameManager.Games.OrderBy(game => game.Title);
            Mouse.OverrideCursor = Cursors.Wait;

            // Go through every game in the list
            foreach (Game game in orderedGames)
            {
                Log.Information($"Adding {game.Title} to the Library");

                // Create a new button for the game
                GameButton button = new GameButton(game, this);

                // Adding game to WrapPanel
                WpGameLibrary.Children.Add(button);
            }

            Mouse.OverrideCursor = null;
        }

        /// <summary>
        /// Clears the WrapPanel of games and adds the games
        /// </summary>
        public void LoadGames()
        {
            WpGameLibrary.Children.Clear();
            LoadGamesIntoUI();
            ConfigurationManager.ClearTemporaryFiles();
        }

        /// <summary>
        /// Asynchronously checks for compatibility ratings and then loads the games into the ui
        /// </summary>
        public async void InitializeASync()
        {
            // Check if the compatibility ratings need an update
            if (ConfigurationManager.AppConfig.Manager.LastCompatiblityRatingUpdateCheckDate == null ||
                (DateTime.Now - ConfigurationManager.AppConfig.Manager.LastCompatiblityRatingUpdateCheckDate.Value)
                .TotalDays >= 1)
            {
                Log.Information("Updating compatibility ratings");
                await GameManager.UpdateCompatibilityRatings(); // Update compatibility ratings
                ConfigurationManager.AppConfig.Manager.LastCompatiblityRatingUpdateCheckDate =
                    DateTime.Now; // Update the last time checking for compatibility ratings has been executed
                ConfigurationManager.SaveConfigurationFile(); // Save changes
            }

            // Load games into the Wrap panel
            LoadGames();
        }

        // Adding games into Xenia Manager
        /// <summary>
        /// Goes through every game in the array, calls the function that grabs their TitleID and MediaID and opens a new window where the user selects the game
        /// </summary>
        /// <param name="newGames">Array of game ISOs/xex files</param>
        /// <param name="emulatorVersion">Tells us what Xenia version to use for this game</param>
        private async void AddGames(string[] newGames, EmulatorVersion xeniaVersion)
        {
            // Go through every game in the array
            foreach (string gamePath in newGames)
            {
                Log.Information($"File Name: {Path.GetFileName(gamePath)}");
                (string gameTitle, string gameId, string mediaId) = ("Not found", "Not found", "");
                
                // Get Title, TitleID and MediaID from the game
                // New way without using Xenia
                if (ConfigurationManager.AppConfig.AutomaticGameParsingSelection == true)
                {
                    (gameTitle, gameId, mediaId) = GameManager.GetGameDetailsWithoutXenia(gamePath);
                }
                if (gameId == "Not found" || mediaId == "")
                {
                    // Old way using Xenia
                    (gameTitle, gameId, mediaId) = await GameManager.GetGameDetailsViaXenia(gamePath, xeniaVersion);
                }
                Log.Information($"Title: {gameTitle}, Game ID: {gameId}, Media ID: {mediaId}");
                SelectGame selectGame = new SelectGame(gameTitle, gameId, mediaId, gamePath, xeniaVersion);
                selectGame.Show();
                await selectGame.WaitForCloseAsync();
            }

            // Reload the UI after adding the game to show it
            LoadGames();
        }
    }
}