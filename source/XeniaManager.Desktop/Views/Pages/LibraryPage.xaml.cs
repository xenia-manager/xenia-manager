using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

// Imported
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
    // Constructor
    public LibraryPage()
    {
        InitializeComponent();
    }

    // Functions
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
                    GameDatabaseWindow gameDatabaseWindow = new GameDatabaseWindow(gameTitle, gameId, mediaId, gamePath);
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