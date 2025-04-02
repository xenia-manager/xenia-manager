using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

// Imported
using Microsoft.Win32;
using Wpf.Ui.Controls;
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
    /// <summary>
    /// Contains all of the games being displayed in the WrapPanel
    /// </summary>
    private IOrderedEnumerable<Game> _games { get; set; }

    // Constructor
    public LibraryPage()
    {
        InitializeComponent();
        UpdateUI();
        LoadGames();
        App.Settings.ClearCache(); // Clear cache after loading the games
    }

    // Functions
    /// <summary>
    /// Updates the UI based on the selected settings in the configuration file
    /// </summary>
    private void UpdateUI()
    {
        // Update the "Display Game Title" Button Icon
        if (App.Settings.Ui.DisplayGameTitle)
        {
            BtnDisplayGameTitle.Icon = new SymbolIcon { Symbol = SymbolRegular.Eye24 };
        }
        else
        {
            BtnDisplayGameTitle.Icon = new SymbolIcon { Symbol = SymbolRegular.EyeOff24 };
        }
    }

    /// <summary>
    /// Loads the games into the WrapPanel
    /// </summary>
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
            try
            {
                LibraryGameButton gameButton = new LibraryGameButton(game, this);
                WpGameLibrary.Children.Add(gameButton);
            }
            catch (Exception ex)
            {
                Logger.Error($"{ex.Message}\n{ex.StackTrace}");
                CustomMessageBox.Show(ex);
            }
        }

        Mouse.OverrideCursor = null;
    }
    
    /// <summary>
    /// Shows/Hides game title on the boxart
    /// </summary>
    private void BtnDisplayGameTitle_Click(object sender, RoutedEventArgs e)
    {
        // Invert the option
        App.Settings.Ui.DisplayGameTitle = !App.Settings.Ui.DisplayGameTitle;
        
        // Reload UI
        UpdateUI();
        LoadGames();
        
        // Save changes
        App.AppSettings.SaveSettings();
    }

    /// <summary>
    /// Adds the games
    /// </summary>
    private async void BtnAddGame_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Add check for installed emulators before continuing with the adding of games
        try
        {
            using (new WindowDisabler(this))
            {
                Logger.Info("Opening file dialog");
                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    Title = LocalizationHelper.GetUIText("OpenFileDialog_SelectGameTitle"),
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
                    // TODO: Add getting game details without Xenia
                    (gameTitle, gameId, mediaId) = await GameManager.GetGameDetailsWithXenia(gamePath, xeniaVersion);
                    Logger.Info($"Title: {gameTitle}, Game ID: {gameId}, Media ID: {mediaId}");
                    GameDatabaseWindow gameDatabaseWindow = new GameDatabaseWindow(gameTitle, gameId, mediaId, gamePath, xeniaVersion);
                    gameDatabaseWindow.ShowDialog();
                }
            }

            // Reload the UI to show added game
            LoadGames();
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
            await CustomMessageBox.Show(ex);
        }
    }
}