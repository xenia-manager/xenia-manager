﻿using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

// Imported Libraries
using Microsoft.Win32;
using XeniaManager.Core;
using XeniaManager.Core.Database;
using XeniaManager.Core.Game;
using XeniaManager.Desktop.Components;
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

    private IOrderedEnumerable<Game> _games { get; set; }

    private LibraryPageViewModel _viewModel { get; }

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
            LoadGames();
        };
        Loaded += (sender, args) =>
        {
            App.Settings.ClearCache(); // Clear cache after loading the games
        };
        EventManager.RequestLibraryUiRefresh();
    }

    #endregion

    #region Functions & Events

    /// <summary>
    /// Updates Compatibility ratings
    /// </summary>
    private async Task UpdateCompatibilityRatings()
    {
        if ((DateTime.Now - App.Settings.UpdateCheckChecks.CompatibilityCheck).TotalDays <= 1)
        {
            return;
        }

        Logger.Info("Updating compatibility ratings");
        try
        {
            await CompatibilityManager.UpdateCompatibility();
        }
        catch (Exception) { }
        App.Settings.UpdateCheckChecks.CompatibilityCheck = DateTime.Now;

        // Save changes
        GameManager.SaveLibrary();
        App.AppSettings.SaveSettings();
    }

    /// <summary>
    /// Loads the games into the WrapPanel
    /// </summary>
    public async void LoadGames()
    {
        await UpdateCompatibilityRatings();
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
                Logger.Error($"{ex.Message}\nFull Error:\n{ex}");
                CustomMessageBox.Show(ex);
            }
        }

        Mouse.OverrideCursor = null;
    }

    private void TxtSearchBar_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        TextBox textBox = (TextBox)sender;
        if (string.IsNullOrWhiteSpace(textBox.Text))
        {
            // Reset the filter
            if (WpGameLibrary != null)
            {
                foreach (object childElement in WpGameLibrary.Children)
                {
                    if (childElement is LibraryGameButton libraryGameButton)
                    {
                        libraryGameButton.Visibility = Visibility.Visible;
                    }
                }
            }
            return;
        }

        string searchQuery = textBox.Text;
        foreach (object childElement in WpGameLibrary.Children)
        {
            if (childElement is LibraryGameButton libraryGameButton)
            {
                if (libraryGameButton.GameTitle.Contains(searchQuery, StringComparison.OrdinalIgnoreCase))
                {
                    libraryGameButton.Visibility = Visibility.Visible;
                }
                else
                {
                    libraryGameButton.Visibility = Visibility.Collapsed;
                }
            }
        }
    }

    private void BtnLibraryView_Click(object sender, RoutedEventArgs e)
    {
        CustomMessageBox.Show("Not implemented yet", "This isn't implemented yet.");
    }

    /// <summary>
    /// Adds the games
    /// </summary>
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
                    // TODO: Add the ability to choose what version of Xenia the game will use
                    throw new NotImplementedException();
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
                    // TODO: Add the ability to choose what version of Xenia the game will use
                    throw new NotImplementedException();
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

    #endregion
}