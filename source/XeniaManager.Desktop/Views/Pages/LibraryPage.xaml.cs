using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

// Imported Libraries
using Microsoft.Win32;
using XeniaManager.Core;
using XeniaManager.Core.Database;
using XeniaManager.Core.Game;
using XeniaManager.Desktop.Components;
using XeniaManager.Desktop.Extensions;
using XeniaManager.Desktop.Utilities;
using XeniaManager.Desktop.ViewModel.Pages;
using XeniaManager.Desktop.Views.Windows;
using EventManager = XeniaManager.Desktop.Utilities.EventManager;
using Page = System.Windows.Controls.Page;
using TextBox = System.Windows.Controls.TextBox;

namespace XeniaManager.Desktop.Views.Pages;

/// <summary>
/// Interaction logic for LibraryPage.xaml
/// </summary>
public partial class LibraryPage : Page
{
    #region Variables
    private LibraryPageViewModel _viewModel { get; }
    private readonly DispatcherTimer _searchTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
    private readonly List<DependencyPropertyDescriptor> _widthDescriptors = new();

    #endregion

    #region Constructor

    public LibraryPage()
    {
        InitializeComponent();
        _viewModel = new LibraryPageViewModel();
        DataContext = _viewModel;
        EventManager.LibraryUIiRefresh += (sender, args) =>
        {
            _viewModel.LoadSettings();
            _viewModel.RefreshGames();
            LoadGames();
        };
        Loaded += (sender, args) =>
        {
            DgdGamesList.RestoreDataGridSettings(App.Settings.Ui.Library.ListViewSettings);
            AttachColumnWidthHandlers();
            App.Settings.ClearCache(); // Clear cache after loading the games
        };
        Unloaded += (sender, args) =>
        {
            DgdGamesList.SaveDataGridSettings(App.Settings.Ui.Library.ListViewSettings);
            DetachColumnWidthHandlers();
            App.AppSettings.SaveSettings();
        };
        DgdGamesList.LoadingRow += DgdListGames_Loaded;
        _searchTimer.Tick += SearchTimer_Tick;
        EventManager.RequestLibraryUiRefresh();
    }

    #endregion

    #region Functions & Events
    public async void LoadGames()
    {
        await _viewModel.UpdateCompatibilityRatings();
        WpGameLibrary.Children.Clear();
        Logger.Info("Loading games into the UI");
        if (_viewModel.Games.Count <= 0)
        {
            Logger.Info("No games found.");
            return;
        }

        Mouse.OverrideCursor = Cursors.Wait;
        foreach (Game game in _viewModel.Games)
        {
            Logger.Info($"Adding {game.Title} to the library");
            try
            {
                LibraryGameButton gameButton = new LibraryGameButton(game, this);
                WpGameLibrary.Children.Add(gameButton);
            }
            catch (Exception ex)
            {
                Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
                await CustomMessageBox.Show(ex);
            }
        }
        _viewModel.PrecacheGameIcons();
        Mouse.OverrideCursor = null;
    }

    private void TxtSearchBar_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is not TextBox textBox) return;
        string query = textBox.Text;
        UpdateWrapPanelVisibility(query);
        _searchTimer.Stop();
        _searchTimer.Tag = query;
        _searchTimer.Start();
    }

    private void SearchTimer_Tick(object? sender, EventArgs e)
    {
        _searchTimer.Stop();
        string query = _searchTimer.Tag as string ?? "";

        ApplyDataGridFilter(query);
    }

    private void UpdateWrapPanelVisibility(string searchQuery)
    {
        if (WpGameLibrary == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(searchQuery))
        {
            foreach (object child in WpGameLibrary.Children)
            {
                if (child is LibraryGameButton btn)
                {
                    btn.Visibility = Visibility.Visible;
                }
            }
        }
        else
        {
            foreach (object child in WpGameLibrary.Children)
            {
                if (child is LibraryGameButton btn)
                {
                    btn.Visibility = btn.GameTitle.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) ? Visibility.Visible : Visibility.Collapsed;
                }
            }
        }
    }

    private void ApplyDataGridFilter(string searchQuery)
    {
        if (DgdGamesList.ItemsSource is not IEnumerable<Game> items)
        {
            return;
        }
        ;

        ICollectionView view = CollectionViewSource.GetDefaultView(items);
        if (view == null)
        {
            return;
        }
        ;

        using (view.DeferRefresh())
        {
            if (string.IsNullOrWhiteSpace(searchQuery))
            {
                view.Filter = null;
            }
            else
            {
                view.Filter = obj =>
                {
                    if (obj is Game game)
                    {
                        return game.Title.Contains(searchQuery, StringComparison.OrdinalIgnoreCase);
                    }
                    return false;
                };
            }
        }
    }

    private async void BtnAddGame_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            List<XeniaVersion> availableVersions = App.Settings.GetInstalledVersions();
            XeniaVersion xeniaVersion = XeniaVersion.Canary;
            switch (availableVersions.Count)
            {
                case 0:
                    throw new Exception("No Xenia version installed.\nInstall Xenia before continuing.");
                case 1:
                    Logger.Info($"There is only 1 Xenia version installed: {availableVersions[0]}");
                    xeniaVersion = availableVersions[0];
                    break;
                default:
                    try
                    {
                        xeniaVersion = App.Settings.SelectVersion(() =>
                        {
                            XeniaSelection xeniaSelection = new XeniaSelection();
                            xeniaSelection.ShowDialog();
                            return xeniaSelection.SelectedXenia as XeniaVersion?;
                        });
                    }
                    catch (OperationCanceledException)
                    {
                        Logger.Info("Xenia Selection was cancelled.");
                        return;
                    };
                    break;
            }
            using (new WindowDisabler(this))
            {
                Logger.Info("Opening file dialog");
                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    Title = LocalizationHelper.GetUiText("OpenFileDialog_SelectGameTitle"),
                    Filter = "All Files|*|Supported Files|*.iso;*.xex;*.zar",
                    Multiselect = true
                };

                bool? result = openFileDialog.ShowDialog();
                if (result == false)
                {
                    Logger.Info("Cancelling adding of games");
                    return;
                }

                foreach (string gamePath in openFileDialog.FileNames)
                {
                    Logger.Debug($"File Name: {Path.GetFileName(gamePath)}");
                    (string gameTitle, string gameId, string mediaId) = ("Not found", "Not found", "");
                    (gameTitle, gameId, mediaId) = GameManager.GetGameDetailsWithoutXenia(gamePath);
                    if (gameId == "Not found" || mediaId == "")
                    {
                        (gameTitle, gameId, mediaId) = await GameManager.GetGameDetailsWithXenia(gamePath, xeniaVersion);
                    }
                    Logger.Info($"Title: {gameTitle}, Game ID: {gameId}, Media ID: {mediaId}");
                    Mouse.OverrideCursor = Cursors.Wait;
                    try
                    {
                        await XboxDatabase.Load();
                        Logger.Info("Searching database by title_id");
                        await Task.WhenAll(XboxDatabase.SearchDatabase(gameId));
                        if (XboxDatabase.FilteredDatabase.Count == 1)
                        {
                            Logger.Info("Found game in database");
                            GameInfo gameInfo = XboxDatabase.GetShortGameInfo(XboxDatabase.FilteredDatabase[0]);
                            if (gameInfo != null)
                            {
                                Logger.Info("Automatically adding the game");
                                await GameManager.AddGame(gameInfo, gameId, mediaId, gamePath, xeniaVersion);
                                Mouse.OverrideCursor = null;
                            }
                        }
                        else
                        {
                            GameDatabaseWindow gameDatabaseWindow = new GameDatabaseWindow(gameTitle, gameId, mediaId, gamePath, xeniaVersion);
                            gameDatabaseWindow.ShowDialog();
                        }
                    }
                    catch (HttpRequestException httpReqEx)
                    {
                        Logger.Error($"{httpReqEx.Message}\nFull Error:\n{httpReqEx}");
                        await GameManager.AddUnknownGame(gameTitle, gameId, mediaId, gamePath, xeniaVersion);
                        EventManager.RequestLibraryUiRefresh();
                    }
                    catch (TaskCanceledException taskEx)
                    {
                        Logger.Error($"{taskEx.Message}\nFull Error:\n{taskEx}");
                        await GameManager.AddUnknownGame(gameTitle, gameId, mediaId, gamePath, xeniaVersion);
                        EventManager.RequestLibraryUiRefresh();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
            await CustomMessageBox.Show(ex);
        }
        finally
        {
            // Reload the UI to show the added game
            EventManager.RequestLibraryUiRefresh();
        }
    }

    private async void BtnScanDirectory_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            List<XeniaVersion> availableVersions = App.Settings.GetInstalledVersions();
            XeniaVersion xeniaVersion = XeniaVersion.Canary;
            switch (availableVersions.Count)
            {
                case 0:
                    throw new Exception("No Xenia version installed.\nInstall Xenia before continuing.");
                case 1:
                    Logger.Info($"There is only 1 Xenia version installed: {availableVersions[0]}");
                    xeniaVersion = availableVersions[0];
                    break;
                default:
                    try
                    {
                        xeniaVersion = App.Settings.SelectVersion(() =>
                        {
                            XeniaSelection xeniaSelection = new XeniaSelection();
                            xeniaSelection.ShowDialog();
                            return xeniaSelection.SelectedXenia as XeniaVersion?;
                        });
                    }
                    catch (OperationCanceledException)
                    {
                        Logger.Info("Xenia Selection was cancelled.");
                        return;
                    };
                    break;
            }
            using (new WindowDisabler(this))
            {
                Logger.Info("Opening file dialog");
                OpenFolderDialog openFolderDialog = new OpenFolderDialog
                {
                    Title = LocalizationHelper.GetUiText("OpenFileDialog_SelectDirectoryTitle"),
                    Multiselect = false
                };

                bool? result = openFolderDialog.ShowDialog();
                if (result == false)
                {
                    Logger.Info("Cancelling directory scan");
                    return;
                }

                foreach (string directory in Directory.EnumerateDirectories(openFolderDialog.FolderName, "*"))
                {
                    string[] gameFiles = Directory.GetFiles(directory);
                    bool foundGameFile = false;

                    string[] priorityFiles = gameFiles.Where(f => f.EndsWith(".iso", StringComparison.OrdinalIgnoreCase) || f.Contains("default.xex", StringComparison.OrdinalIgnoreCase)).ToArray();
                    string gameFilePath = string.Empty;
                    foreach (string gameFile in priorityFiles)
                    {
                        if (gameFile.EndsWith(".iso"))
                        {
                            Logger.Debug($"Game ISO: {gameFile}");
                            foundGameFile = true;
                            gameFilePath = gameFile;
                            break;
                        }
                        else if (gameFile.EndsWith(".xex"))
                        {
                            Logger.Debug($"Game XEX: {gameFile}");
                            foundGameFile = true;
                            gameFilePath = gameFile;
                            break;
                        }
                    }

                    if (!foundGameFile)
                    {
                        string[] otherFiles = gameFiles.Where(f => !f.EndsWith(".iso", StringComparison.OrdinalIgnoreCase) && !f.EndsWith(".xex", StringComparison.OrdinalIgnoreCase)).ToArray();
                        if (otherFiles.Length == 1)
                        {
                            Logger.Debug($"Game File: {otherFiles[0]}");
                            gameFilePath = otherFiles[0];
                        }
                    }

                    if (!string.IsNullOrEmpty(gameFilePath))
                    {
                        // Skip Duplicates
                        if (GameManager.CheckForDuplicateGame(gameFilePath))
                        {
                            Logger.Info($"Duplicate entry: {gameFilePath}");
                            continue;
                        }
                        Logger.Debug($"File Name: {Path.GetFileName(gameFilePath)}");
                        (string gameTitle, string gameId, string mediaId) = GameManager.GetGameDetailsWithoutXenia(gameFilePath);
                        if (gameId == "Not found" || mediaId == "")
                        {
                            (gameTitle, gameId, mediaId) = await GameManager.GetGameDetailsWithXenia(gameFilePath, xeniaVersion);
                        }
                        Logger.Info($"Title: {gameTitle}, Game ID: {gameId}, Media ID: {mediaId}");
                        Mouse.OverrideCursor = Cursors.Wait;
                        await XboxDatabase.Load();
                        Logger.Info("Searching database by title_id");
                        await Task.WhenAll(XboxDatabase.SearchDatabase(gameId));
                        if (XboxDatabase.FilteredDatabase.Count == 1)
                        {
                            Logger.Info("Found game in database");
                            GameInfo gameInfo = XboxDatabase.GetShortGameInfo(XboxDatabase.FilteredDatabase[0]);
                            if (gameInfo != null)
                            {
                                Logger.Info("Automatically adding the game");
                                await GameManager.AddGame(gameInfo, gameId, mediaId, gameFilePath, xeniaVersion);
                                Mouse.OverrideCursor = null;
                            }
                        }
                        else
                        {
                            GameDatabaseWindow gameDatabaseWindow = new GameDatabaseWindow(gameTitle, gameId, mediaId, gameFilePath, xeniaVersion);
                            gameDatabaseWindow.ShowDialog();
                        }
                    }
                }
            }

            // Reload the UI to show the added game
            EventManager.RequestLibraryUiRefresh();
        }
        catch (Exception ex)
        {
            Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
            await CustomMessageBox.Show(ex);
        }
    }

    private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        // Check if the Ctrl key is pressed
        _viewModel.HandleMouseWheelCommand.Execute(e);
    }

    private void DgdListGames_Loaded(object sender, DataGridRowEventArgs e)
    {
        if (e.Row is DataGridRow row && row.DataContext is Game game)
        {
            row.ContextMenu = GameUIHelper.CreateContextMenu(game, row);
            row.MouseLeftButtonUp += DgdListGamesSelectedItem_MouseleftButtonUp;
        }
    }

    private void DgdListGamesSelectedItem_MouseleftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is DataGridRow row && row.DataContext is Game game)
        {
            GameUIHelper.Game_Click(game, sender, e);
            row.IsSelected = false;
            e.Handled = true;
        }
    }

    private void DgdGamesList_ColumnReordered(object sender, DataGridColumnEventArgs e)
    {
        DgdGamesList.SaveDataGridSettings(App.Settings.Ui.Library.ListViewSettings);
        App.AppSettings.SaveSettings();
    }

    private void AttachColumnWidthHandlers()
    {
        DgdGamesList.Columns.CollectionChanged += Columns_CollectionChanged;

        foreach (DataGridColumn col in DgdGamesList.Columns)
        {
            AttachToColumn(col);
        }
    }

    private void DetachColumnWidthHandlers()
    {
        DgdGamesList.Columns.CollectionChanged -= Columns_CollectionChanged;

        foreach (DataGridColumn col in DgdGamesList.Columns)
        {
            DetachFromColumn(col);
        }
        _widthDescriptors.Clear();
    }

    private void Columns_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (DataGridColumn col in e.NewItems)
            {
                AttachToColumn(col);
            }
        }
        if (e.OldItems != null)
        {
            foreach (DataGridColumn col in e.OldItems)
            {
                DetachFromColumn(col);
            }
        }
    }

    private void AttachToColumn(DataGridColumn column)
    {
        DependencyPropertyDescriptor dpd = DependencyPropertyDescriptor.FromProperty(DataGridColumn.WidthProperty, typeof(DataGridColumn));
        if (dpd != null)
        {
            dpd.AddValueChanged(column, DataGridColumnWidthChanged);
            _widthDescriptors.Add(dpd);
        }
    }

    private void DetachFromColumn(DataGridColumn column)
    {
        DependencyPropertyDescriptor dpd = DependencyPropertyDescriptor.FromProperty(DataGridColumn.WidthProperty, typeof(DataGridColumn));
        if (dpd != null)
        {
            dpd.RemoveValueChanged(column, DataGridColumnWidthChanged);
        }
    }

    private void DataGridColumnWidthChanged(object? sender, EventArgs e)
    {
        DgdGamesList.SaveDataGridSettings(App.Settings.Ui.Library.ListViewSettings);
        App.AppSettings.SaveSettings();
    }

    #endregion
}