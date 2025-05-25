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
    private bool _skipClosingPrompt = false; // Bypassed OnClosing prompt

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
        _xboxFilteredDatabase = XboxDatabase.FilteredDatabase.Take(12).ToList();
        LstGamesDatabase.ItemsSource = _xboxFilteredDatabase;
    }

    // Functions
    /// <summary>
    /// Searches for games by title_id and then title on startup
    /// </summary>
    private async Task SearchGames()
    {
        // Search by TitleID
        Logger.Info("Searching database by title_id in GameDatabaseWindow");
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

    /// <summary>
    /// Asynchronous initalization (loading database and searching for games)
    /// </summary>
    private async void InitializeAsync()
    {
        try
        {
            Mouse.OverrideCursor = Cursors.Wait;
            using (new WindowDisabler(this))
            {
                await SearchGames();
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }
    }

    /// <summary>
    /// Ask the user on closing of this window if he wants to add game as unknown game with default artwork or cancel the process of adding the game
    /// </summary>
    protected override async void OnClosing(CancelEventArgs e)
    {
        if (_skipClosingPrompt)
        {
            base.OnClosing(e);
            return;
        }

        MessageBoxResult result = await CustomMessageBox.YesNo(LocalizationHelper.GetUiText("MessageBox_ConfirmExit"), 
            LocalizationHelper.GetUiText("MessageBox_AddGameWithDefaultArtworkText"));
        if (result == MessageBoxResult.Primary)
        {
            // Add game
            Logger.Info("Adding the game with default boxart");
            await GameManager.AddUnknownGame(_gameTitle, _titleId, _mediaId, _gamePath, _version);
        }

        base.OnClosing(e);
    }

    /// <summary>
    /// Searchbar functionality triggers when the text in the searchbar has changed
    /// </summary>
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
            Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
            await CustomMessageBox.Show(ex);
            return;
        }
    }

    /// <summary>
    /// Adds the selected game to Xenia Manager
    /// </summary>
    private async void LstGamesDatabase_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Checking is user selected something before continuing
        ListBox listBox = sender as ListBox;
        if (listBox?.SelectedItem == null)
        {
            return;
        }

        // Finding the selected game in the database
        GameInfo selectedGameInfo = XboxDatabase.GetShortGameInfo((string)listBox.SelectedItem);
        if (selectedGameInfo == null)
        {
            Logger.Error($"Couldn't find the selected game: {(string)listBox.SelectedItem}");
            if (await CustomMessageBox.YesNo(LocalizationHelper.GetUiText("MessageBox_UnableFindSelectedGameTitle"),
                    LocalizationHelper.GetUiText("MessageBox_UnableFindSelectedGameText")) == MessageBoxResult.Primary)
            {
                Logger.Info("Adding the game with default boxart");
                await GameManager.AddUnknownGame(_gameTitle, _titleId, _mediaId, _gamePath, _version);
                _skipClosingPrompt = true;
                this.Close();
            }
            else
            {
                // Reset the selection and return
                listBox.SelectedIndex = -1;
                return;
            }
        }

        try
        {
            // Check if the selected game titleid matches with the one we found
            if (selectedGameInfo.Id == _titleId || selectedGameInfo.AlternativeId.Contains(_titleId))
            {
                Mouse.OverrideCursor = Cursors.Wait;
                Logger.Info($"Selected game: {selectedGameInfo.Title}");
                await GameManager.AddGame(selectedGameInfo, _titleId, _mediaId, _gamePath, _version);
                Mouse.OverrideCursor = null;
                _skipClosingPrompt = true;
                this.Close();
            }
            else
            {
                Logger.Error($"Couldn't find the selected game: {(string)listBox.SelectedItem}");
                if (await CustomMessageBox.YesNo(LocalizationHelper.GetUiText("MessageBox_MissmatchedTitleIdTitle"),
                        string.Format(LocalizationHelper.GetUiText("MessageBox_MissmatchedTitleIdText"), _titleId, selectedGameInfo.Id)) == MessageBoxResult.Primary)
                {
                    Mouse.OverrideCursor = Cursors.Wait;
                    Logger.Info($"Selected game: {selectedGameInfo.Title}");
                    await GameManager.AddGame(selectedGameInfo, selectedGameInfo.Id, _mediaId, _gamePath, _version);
                    Mouse.OverrideCursor = null;
                    _skipClosingPrompt = true;
                    this.Close();
                }
                else
                {
                    // Reset the selection and return
                    listBox.SelectedIndex = -1;
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
            Logger.Info("Adding the game with default boxart");
            Mouse.OverrideCursor = null;
            await GameManager.AddUnknownGame(_gameTitle, _titleId, _mediaId, _gamePath, _version);
            _skipClosingPrompt = true;
            this.Close();
        }
    }
}