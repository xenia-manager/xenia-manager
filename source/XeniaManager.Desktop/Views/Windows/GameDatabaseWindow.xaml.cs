using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Input;

// Imported
using Wpf.Ui.Controls;
using XeniaManager.Core;
using XeniaManager.Core.Database;
using XeniaManager.Core.Game;
using XeniaManager.Desktop.Components;
using XeniaManager.Desktop.Utilities;

namespace XeniaManager.Desktop.Views.Windows;

public partial class GameDatabaseWindow : FluentWindow
{
    // Variables
    private string _gameTitle { get; set; }
    private string _titleId { get; set; }
    private string _mediaId { get; set; }
    private string _gamePath { get; set; }
    private XeniaVersion _version { get; set; }
    private TaskCompletionSource<bool> _searchtcs; // Search is completed
    private CancellationTokenSource _cts; // Cancels the ongoing search if user types something
    private List<string> _xboxFilteredDatabase;
    
    // Constructor
    public GameDatabaseWindow(string gameTitle, string titleId, string mediaId, string gamePath, XeniaVersion version)
    {
        InitializeComponent();
        _gameTitle = gameTitle;
        _titleId = titleId;
        _mediaId = mediaId;
        _gamePath = gamePath;
        _version = version;
        InitializeAsync();
    }

    // Functions
    private async Task LoadDatabase()
    {
        Logger.Info("Loading Xbox games database");
        await XboxDatabase.Load();
        _xboxFilteredDatabase = XboxDatabase.FilteredDatabase.Take(12).ToList();
        LstGamesDatabase.ItemsSource = _xboxFilteredDatabase;
    }

    private async Task SearchGames()
    {
        // Search by TitleID
        Logger.Info("Searching database by title_id");
        TxtSearchBar.Text = _titleId;
        await _searchtcs.Task;
        bool successfulSearchById = LstGamesDatabase.Items.Count > 0;

        if (!successfulSearchById)
        {
            Logger.Info("No games found using id to search");
            Logger.Info("Doing search by game title");
            TxtSearchBar.Text = Regex.Replace(_gameTitle, @"[^a-zA-Z0-9\s]", "");
        }
        
        await _searchtcs.Task;

        if (LstGamesDatabase.Items.Count == 0)
        {
            Logger.Warning("No games found");
        }
    }
    
    private async void InitializeAsync()
    {
        try
        {
            Mouse.OverrideCursor = Cursors.Wait;
            using (new WindowDisabler(this))
            {
                await LoadDatabase();
                await SearchGames();
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

    protected override async void OnClosing(CancelEventArgs e)
    {
        MessageBoxResult result = await CustomMessageBox.YesNo("Confirm Exit", "Do you want to add the game without box art?\nPress 'Yes' to proceed, or 'No' to cancel.");
        if (result == MessageBoxResult.Primary)
        {
            // Add game
            Logger.Info("Adding the game with default boxart");
            await GameManager.AddUnknownGame(_gameTitle, _titleId, _mediaId, _gamePath, _version);
        }
        base.OnClosing(e);
    }

    private async void TxtSearchBar_TextChanged(object sender, TextChangedEventArgs e)
    {
        try
        { 
            // Cancel any ongoing search if the user types more input
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            _searchtcs = new TaskCompletionSource<bool>(); // Reset the search

            await Task.WhenAll(XboxDatabase.SearchDatabase(TxtSearchBar.Text));
            
            // Update UI (ensure this is on the UI thread)
            await Dispatcher.InvokeAsync(() =>
            {
                // Update UI only if the search wasn't cancelled
                if (!_cts.IsCancellationRequested)
                {
                    // Filtering Xbox Marketplace list
                    _xboxFilteredDatabase = XboxDatabase.FilteredDatabase.Take(12).ToList();
                    if (LstGamesDatabase.ItemsSource == null || !_xboxFilteredDatabase.SequenceEqual((IEnumerable<string>)LstGamesDatabase.ItemsSource))
                    {
                        LstGamesDatabase.ItemsSource = _xboxFilteredDatabase;
                    }
                }
            });

            // Ensure search is completed
            if (!_searchtcs.Task.IsCompleted)
            {
                _searchtcs.SetResult(true);
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
            await CustomMessageBox.Show(ex);
            return;
        }
    }
}