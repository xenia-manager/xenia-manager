using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

// Imported
using Microsoft.Win32;
using XeniaManager.Core;
using XeniaManager.Core.Game;
using XeniaManager.Desktop.Components;
using XeniaManager.Desktop.Utilities;
using XeniaManager.Desktop.Views.Windows;

namespace XeniaManager.Desktop.Views.Pages;

/// <summary>
/// Interaction logic for LibraryPage.xaml
/// </summary>
public partial class LibraryPage : Page
{
    // Variables
    private IOrderedEnumerable<Game> _games {get; set;}
    
    // Constructor
    public LibraryPage()
    {
        InitializeComponent();
        LoadGames();
    }

    // Functions

    public void LoadGames()
    {
        WpGameLibrary.Children.Clear();
        Logger.Info("Loading games into the UI");
        if (GameManager.Games.Count <= 0)
        {
            Logger.Info("No games found.");
            return;
        }
        Mouse.OverrideCursor = Cursors.Wait;
        _games = GameManager.Games.OrderBy(game => game.Title);
        foreach (Game game in _games)
        {
            Logger.Info($"Adding {game.Title} to the library");
            LibraryGameButton gameButton = new LibraryGameButton(game, this);
            WpGameLibrary.Children.Add(gameButton);
        }
        
        Mouse.OverrideCursor = null;
    }
    
    private async void BtnAddGame_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            //throw new NotImplementedException();
            using (new WindowDisabler(this))
            {
                Logger.Info("Opening file dialog");
                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    Title = "Select a game",
                    Filter = "All Files|*|Supported Files|*.iso;*.xex;*.zar",
                    Multiselect = true
                };
            
                bool? result = openFileDialog.ShowDialog();
                if (result == false)
                {
                    Logger.Info("Cancelling adding of games");
                    return;
                }
            
                // TODO: Add the ability to choose what version of Xenia the game will use
                XeniaVersion xeniaVersion = XeniaVersion.Canary;
                foreach (string gamePath in openFileDialog.FileNames)
                {
                    Logger.Debug($"File Name: {Path.GetFileName(gamePath)}");
                    (string gameTitle, string gameId, string mediaId) = ("Not found", "Not found", "");
                    (gameTitle, gameId, mediaId) = await GameManager.GetGameDetailsWithXenia(gamePath, xeniaVersion);
                    Logger.Info($"Title: {gameTitle}, Game ID: {gameId}, Media ID: {mediaId}");
                    GameDatabaseWindow gameDatabaseWindow = new GameDatabaseWindow(gameTitle, gameId, mediaId, gamePath, xeniaVersion);
                    gameDatabaseWindow.ShowDialog();
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
            await CustomMessageBox.Show(ex);
        }
    }
}