using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;

// Imported
using Microsoft.Win32;
using Serilog;
using XeniaManager.DesktopApp.Windows;

namespace XeniaManager.DesktopApp.Pages
{
    /// <summary>
    /// Interaction logic for Library.xaml
    /// </summary>
    public partial class Library : Page
    {
        public Library()
        {
            InitializeComponent();
        }

        // Functions
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
                (string gameTitle, string gameId, string mediaId) = GameManager.GetGameDetails(gamePath, xeniaVersion); // Get Title, TitleID and MediaID
                Log.Information($"Title: {gameTitle}, Game ID: {gameId}, Media ID: {mediaId}");
                SelectGame selectGame = new SelectGame(this, gameTitle, gameId, mediaId, gamePath, xeniaVersion);
                selectGame.Show();
                await selectGame.WaitForCloseAsync();
            }
        }

        // UI Interactions
        /// <summary>
        /// Opens FileDialog where user selects the game/games they want to add to Xenia Manager
        /// </summary>
        private void AddGame_Click(object sender, RoutedEventArgs e)
        {
            Log.Information("Opening file dialog");
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "Select a game";
            openFileDialog.Filter = "All Files|*|Supported Files|*.iso;*.xex;*.zar";
            openFileDialog.Multiselect = true;
            bool? result = openFileDialog.ShowDialog();
            if (result == false)
            {
                Log.Information("Cancelling adding of games");
                return;
            }

            // Calls for the function that adds the game into Xenia Manager
            AddGames(openFileDialog.FileNames, EmulatorVersion.Canary);
        }
    }
}
