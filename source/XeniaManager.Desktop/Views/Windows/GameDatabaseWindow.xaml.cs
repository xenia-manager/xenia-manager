using System.Windows.Input;

// Imported
using Wpf.Ui.Controls;
using XeniaManager.Core;
using XeniaManager.Core.Database;
using XeniaManager.Desktop.Utilities;

namespace XeniaManager.Desktop.Views.Windows;

public partial class GameDatabaseWindow : FluentWindow
{
    // Variables
    private string _gameTitle { get; set; }
    private string _titleId { get; set; }
    private string _mediaId { get; set; }
    private string _gamePath { get; set; }
    
    // Constructor
    public GameDatabaseWindow(string gameTitle, string titleId, string mediaId, string gamePath)
    {
        InitializeComponent();
        _gameTitle = gameTitle;
        _titleId = titleId;
        _mediaId = mediaId;
        _gamePath = gamePath;
        InitializeAsync();
    }

    // Functions
    private async Task LoadDatabase()
    {
        Logger.Info("Loading Xbox games database");
        await XboxDatabase.Load();
        LstGamesDatabase.ItemsSource = XboxDatabase.FilteredDatabase.Take(12).ToList();
    }

    private async Task SearchGames()
    {
        
    }
    
    private async void InitializeAsync()
    {
        try
        {
            Mouse.OverrideCursor = Cursors.Wait;
            using (new WindowDisabler(this))
            {
                await LoadDatabase();
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }
    }
}