using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentIcons.Common;
using Microsoft.Extensions.DependencyInjection;
using XeniaManager.Controls;
using XeniaManager.Core.Database;
using XeniaManager.Core.Files;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Manage;
using XeniaManager.Core.Models;
using XeniaManager.Core.Models.Database.Xbox;
using XeniaManager.Core.Models.Files.Config;
using XeniaManager.Core.Models.Game;
using XeniaManager.Core.Services;
using XeniaManager.Core.Settings;
using XeniaManager.Core.Settings.Sections;
using XeniaManager.Core.Utilities;
using XeniaManager.Services;
using XeniaManager.ViewModels.Items;

namespace XeniaManager.ViewModels.Pages;

public partial class LibraryPageViewModel : ViewModelBase
{
    // Variables
    private Settings _settings { get; set; }
    private IMessageBoxService _messageBoxService { get; set; }

    // Search optimization
    private CancellationTokenSource? _debounceCts;
    private const int DebounceDelayMs = 100;

    // Library properties
    [ObservableProperty] private bool _isGridView = true;
    [ObservableProperty] private bool _showGameTitle = false;
    partial void OnShowGameTitleChanged(bool value)
    {
        _settings.Settings.Ui.Window.Library.GameTitle = ShowGameTitle;
        _settings.SaveSettings();
    }

    [ObservableProperty] private bool _showCompatibilityRating = false;

    partial void OnShowCompatibilityRatingChanged(bool value)
    {
        _settings.Settings.Ui.Window.Library.CompatibilityRating = ShowCompatibilityRating;
        _settings.SaveSettings();
    }

    // Zoom Properties
    [ObservableProperty] private double _zoomValue = 100;
    public double ZoomMinimum => 50;
    public double ZoomMaximum => 300;
    public double ZoomTickFrequency => 10;
    public string ZoomToolTip => $"{ZoomValue}%";
    public double MinItemWidth => 150 * (ZoomValue / 100.0);
    public double MinItemHeight => 200 * (ZoomValue / 100.0);
    public double ItemSpacing => 8 * (ZoomValue / 100.0);

    partial void OnZoomValueChanged(double value)
    {
        // Notify dependent properties when zoom value changes
        OnPropertyChanged(nameof(ZoomToolTip));
        OnPropertyChanged(nameof(MinItemWidth));
        OnPropertyChanged(nameof(MinItemHeight));
        OnPropertyChanged(nameof(ItemSpacing));
        _settings.Settings.Ui.Window.Library.Zoom = ZoomValue / 100;
        _settings.SaveSettings();
    }

    [ObservableProperty] private bool _doubleClickLaunch = false;

    partial void OnDoubleClickLaunchChanged(bool value)
    {
        _settings.Settings.Ui.Window.Library.DoubleClickLaunch = value;
        _settings.SaveSettings();
    }

    [RelayCommand]
    private void ToggleDoubleClickLaunch()
    {
        DoubleClickLaunch = !DoubleClickLaunch;
    }

    [ObservableProperty] private bool _loadingScreen = true;

    partial void OnLoadingScreenChanged(bool value)
    {
        _settings.Settings.Ui.Window.LoadingScreen = LoadingScreen;
        _settings.SaveSettings();
    }

    [RelayCommand]
    private void ToggleLoadingScreen()
    {
        LoadingScreen = !LoadingScreen;
    }

    // Games List
    [ObservableProperty] private ObservableCollection<GameItemViewModel> _games = [];
    private List<GameItemViewModel> _allGames = [];

    // Search
    [ObservableProperty] private string _searchQuery = string.Empty;

    partial void OnSearchQueryChanged(string value)
    {
        // Debounce search to avoid filtering on every keystroke
        _debounceCts?.Cancel();
        _debounceCts = new CancellationTokenSource();
        CancellationToken token = _debounceCts.Token;

        Task.Delay(DebounceDelayMs, token).ContinueWith(_ =>
        {
            if (!token.IsCancellationRequested)
            {
                FilterGames();
            }
        }, TaskScheduler.FromCurrentSynchronizationContext());
    }

    // Constructor
    public LibraryPageViewModel()
    {
        _settings = App.Services.GetRequiredService<Settings>();
        _messageBoxService = App.Services.GetRequiredService<IMessageBoxService>();

        // Load UI settings
        LoadUiSettings();

        RefreshLibrary();
    }

    /// <summary>
    /// Loads UI settings from the settings file
    /// </summary>
    private void LoadUiSettings()
    {
        UiSettings.WindowProperties.LibraryProperties librarySettings = _settings.Settings.Ui.Window.Library;
        ShowGameTitle = librarySettings.GameTitle;
        ShowCompatibilityRating = librarySettings.CompatibilityRating;
        ZoomValue = librarySettings.Zoom * 100; // Convert from 1.0 scale to percentage
        DoubleClickLaunch = librarySettings.DoubleClickLaunch;
        LoadingScreen = _settings.Settings.Ui.Window.LoadingScreen;
    }

    /// <summary>
    /// Refreshes the entire game list from the manager
    /// </summary>
    public void RefreshLibrary()
    {
        // Ensure we're on the UI thread when modifying the ObservableCollection
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => RefreshLibrary());
            return;
        }

        _allGames.Clear();
        foreach (Game game in GameManager.Games)
        {
            _allGames.Add(new GameItemViewModel(game, this));
        }
        FilterGames();
    }

    /// <summary>
    /// Filters the games collection based on the search query.
    /// Searches by Game.Title and Game.GameId.
    /// </summary>
    private void FilterGames()
    {
        Games.Clear();

        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            // Show all games when the search is empty
            foreach (GameItemViewModel item in _allGames)
            {
                Games.Add(item);
            }
        }
        else
        {
            string query = SearchQuery.ToLowerInvariant();
            foreach (GameItemViewModel item in _allGames)
            {
                if (item.Title.Contains(query, StringComparison.InvariantCultureIgnoreCase) ||
                    item.Game.GameId.Contains(query, StringComparison.InvariantCultureIgnoreCase))
                {
                    Games.Add(item);
                }
            }
        }
    }

    [RelayCommand]
    private void ToggleGameTitle()
    {
        ShowGameTitle = !ShowGameTitle;
    }

    [RelayCommand]
    private void ToggleCompatibilityRating()
    {
        ShowCompatibilityRating = !ShowCompatibilityRating;
    }

    [RelayCommand]
    private async Task ScanDirectory()
    {
        Logger.Info<LibraryPageViewModel>("ScanDirectory command invoked");

        // Initialize variables
        XeniaVersion xeniaVersion;
        IStorageProvider? storageProvider;

        // Check if StorageProvider is available
        try
        {
            storageProvider = App.MainWindow?.StorageProvider;
            if (storageProvider == null)
            {
                Logger.Warning<LibraryPageViewModel>("Storage provider is not available");
                throw new Exception("Storage provider is not available");
            }
        }
        catch (Exception ex)
        {
            Logger.Error<LibraryPageViewModel>("Storage provider is not available");
            Logger.LogExceptionDetails<LibraryPageViewModel>(ex);
            await _messageBoxService.ShowErrorAsync(
                LocalizationHelper.GetText("LibraryPage.Options.ScanDirectory.MissingStorageProvider.Title"),
                string.Format(LocalizationHelper.GetText("LibraryPage.Options.ScanDirectory.MissingStorageProvider.Message"), ex));
            return;
        }

        // Select the correct Xenia version
        try
        {
            List<XeniaVersion> installedVersions = _settings.GetInstalledVersions(_settings);
            switch (installedVersions.Count)
            {
                case 0:
                    Logger.Error<LibraryPageViewModel>("No Xenia installations found");
                    await _messageBoxService.ShowErrorAsync(
                        LocalizationHelper.GetText("LibraryPage.Options.ScanDirectory.NoXeniaInstalled.Title"),
                        LocalizationHelper.GetText("LibraryPage.Options.ScanDirectory.NoXeniaInstalled.Message"));
                    return;
                case 1:
                    Logger.Info<LibraryPageViewModel>($"Only Xenia {installedVersions[0]} is installed");
                    xeniaVersion = installedVersions[0];
                    break;
                default:
                    XeniaVersion? chosen = await XeniaSelectionDialog.ShowAsync(installedVersions);
                    if (chosen is { } version)
                    {
                        Logger.Info<LibraryPageViewModel>($"User selected Xenia {chosen}, proceeding with scan");
                        xeniaVersion = version;
                    }
                    else
                    {
                        Logger.Info<LibraryPageViewModel>("Xenia version selection was cancelled by user");
                        return;
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            Logger.Error<LibraryPageViewModel>("Failed to select Xenia version");
            Logger.LogExceptionDetails<LibraryPageViewModel>(ex);
            await _messageBoxService.ShowErrorAsync(
                LocalizationHelper.GetText("LibraryPage.Options.ScanDirectory.XeniaSelectionError.Title"),
                string.Format(LocalizationHelper.GetText("LibraryPage.Options.ScanDirectory.XeniaSelectionError.Message"), ex));
            return;
        }

        // Open folder picker dialog
        try
        {
            FolderPickerOpenOptions options = new FolderPickerOpenOptions
            {
                Title = LocalizationHelper.GetText("LibraryPage.Options.ScanDirectory.FolderPicker.Title")
            };

            IReadOnlyList<IStorageFolder> selectedFolder = await storageProvider.OpenFolderPickerAsync(options);
            if (selectedFolder.Count == 0)
            {
                Logger.Info<LibraryPageViewModel>("User cancelled folder selection");
                return;
            }

            string folderPath = selectedFolder[0].Path.LocalPath;
            Logger.Info<LibraryPageViewModel>($"User selected folder: {folderPath}");

            // Scan the directory with the progress dialog Disable
            // .zar file scanning if ParseGameDetailsWithXenia is disabled
            bool scanZarFiles = _settings.Settings.General.ParseGameDetailsWithXenia;

            List<string> discoveredGameFiles;
            bool scanCancelled;

            try
            {
                // Show the progress dialog while scanning
                (discoveredGameFiles, scanCancelled) = await FolderScanProgressDialog.ShowAsync(async (cancellationToken, progressReporter) =>
                {
                    return await Task.Run(() =>
                        GameManager.DiscoverGameFiles(
                            folderPath,
                            scanZarFiles,
                            cancellationToken,
                            progressReporter), cancellationToken);
                });
            }
            catch (Exception ex)
            {
                Logger.Error<LibraryPageViewModel>("Failed to scan directory");
                Logger.LogExceptionDetails<LibraryPageViewModel>(ex);
                await _messageBoxService.ShowErrorAsync(LocalizationHelper.GetText("LibraryPage.Options.ScanDirectory.ScanError.Title"),
                    ex.Message);
                return;
            }

            // Check if the scan was canceled
            if (scanCancelled)
            {
                Logger.Info<LibraryPageViewModel>("Scan was cancelled by user");
                return;
            }

            Logger.Info<LibraryPageViewModel>($"Found {discoveredGameFiles.Count} potential game files");

            // Show a progress dialog while adding games
            (int gamesAdded, int gamesSkipped, int gamesFailed) = await AddGamesProgressDialog.ShowAsync(async (progressReporter) =>
            {
                int added = 0;
                int skipped = 0;
                int failed = 0;
                int totalGames = discoveredGameFiles.Count;
                int processed = 0;

                foreach (string gameFile in discoveredGameFiles)
                {
                    processed++;
                    int progress = (processed * 100) / totalGames;

                    // Check for duplicates
                    if (GameManager.IsDuplicateGame(gameFile))
                    {
                        Logger.Warning<LibraryPageViewModel>($"Skipping duplicate game: {gameFile}");
                        skipped++;
                        progressReporter($"Skipping duplicate: {Path.GetFileName(gameFile)}", gameFile,
                            processed, totalGames, added, skipped, failed, progress);
                        continue;
                    }

                    // Report progress - getting game details
                    progressReporter($"Processing: {Path.GetFileName(gameFile)}", gameFile,
                        processed, totalGames, added, skipped, failed, progress);

                    // Get game details
                    Logger.Info<LibraryPageViewModel>($"Retrieving game details for: {gameFile}");
                    ParsedGameDetails details = GameManager.GetGameDetails(gameFile);

                    if (!details.IsValid && _settings.Settings.General.ParseGameDetailsWithXenia)
                    {
                        // Fetching details using Xenia
                        details = await GameManager.GetGameDetailsWithXenia(gameFile, xeniaVersion);
                    }

                    // Try to add the game to the library
                    try
                    {
                        await XboxDatabase.LoadAsync();
                        Logger.Info<LibraryPageViewModel>($"Searching database by title_id {details.TitleId}");
                        await Task.WhenAll(XboxDatabase.SearchDatabase(details.TitleId));
                        if (XboxDatabase.FilteredDatabase.Count == 1)
                        {
                            // Add the game using fetched GameInfo
                            GameInfo gameInfo = XboxDatabase.FilteredDatabase[0];
                            await GameManager.AddGame(xeniaVersion, gameInfo, gameFile, details);
                        }
                        else
                        {
                            // TODO: Open GameDatabaseWindow to allow the user to select the game
                            // Currently disabled
                            await GameManager.AddUnknownGame(xeniaVersion, details, gameFile);
                        }

                        added++;
                        Logger.Info<LibraryPageViewModel>($"Successfully added game: {details.Title} ({details.TitleId})");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error<LibraryPageViewModel>($"Failed to add game {gameFile}: {ex.Message}");
                        Logger.LogExceptionDetails<LibraryPageViewModel>(ex);
                        failed++;
                    }

                    // Update progress after processing
                    progressReporter($"Processed: {Path.GetFileName(gameFile)}", gameFile,
                        processed, totalGames, added, skipped, failed, progress);
                }

                Logger.Info<LibraryPageViewModel>($"Directory scan completed. Games added: {added}, Skipped (duplicates): {skipped}, Failed: {failed}, Total games in library: {GameManager.Games.Count}");
                return (added, skipped, failed);
            });

            Logger.Info<LibraryPageViewModel>($"Directory scan completed. Games added: {gamesAdded}, Skipped (duplicates): {gamesSkipped}, Failed: {gamesFailed}, Total games in library: {GameManager.Games.Count}");

            // Refresh the library to update the UI with newly added games
            RefreshLibrary();

            // Show results
            if (gamesAdded > 0)
            {
                Logger.Info<LibraryPageViewModel>($"Successfully added {gamesAdded} games from directory scan");
                await _messageBoxService.ShowInfoAsync(
                    LocalizationHelper.GetText("LibraryPage.Options.ScanDirectory.Success.Title"),
                    string.Format(LocalizationHelper.GetText("LibraryPage.Options.ScanDirectory.Success.Message"), gamesAdded));
            }
            else
            {
                Logger.Warning<LibraryPageViewModel>("No games were added during directory scan");
                await _messageBoxService.ShowWarningAsync(
                    LocalizationHelper.GetText("LibraryPage.Options.ScanDirectory.NoGamesFound.Title"),
                    LocalizationHelper.GetText("LibraryPage.Options.ScanDirectory.NoGamesFound.Message"));
            }
        }
        catch (Exception ex)
        {
            Logger.Error<LibraryPageViewModel>($"Error during directory scan");
            Logger.LogExceptionDetails<LibraryPageViewModel>(ex);
            await _messageBoxService.ShowErrorAsync(
                LocalizationHelper.GetText("LibraryPage.Options.ScanDirectory.Error.Title"),
                string.Format(LocalizationHelper.GetText("LibraryPage.Options.ScanDirectory.Error.Message"), ex.Message));
        }
    }

    [RelayCommand]
    private async Task AddGame()
    {
        // Initialize required variables
        XeniaVersion xeniaVersion;
        IStorageProvider? storageProvider;

        // Build file type filter - exclude .zar if ParseGameDetailsWithXenia is disabled
        List<FilePickerFileType> fileTypeFilters = new List<FilePickerFileType>
        {
            new FilePickerFileType("Supported Files")
            {
                Patterns = _settings.Settings.General.ParseGameDetailsWithXenia
                    ? ["*.iso", "*.xex", "*.zar"]
                    : ["*.iso", "*.xex"]
            },
            new FilePickerFileType("All Files")
            {
                Patterns = ["*"]
            }
        };

        // Create a file picker
        FilePickerOpenOptions options = new FilePickerOpenOptions
        {
            Title = LocalizationHelper.GetText("LibraryPage.Options.AddGame.FilePicker.Title"),
            AllowMultiple = true,
            FileTypeFilter = fileTypeFilters
        };

        // Check if StorageProvider is available
        try
        {
            // Check if we have StorageProvider
            storageProvider = App.MainWindow?.StorageProvider;
            if (storageProvider == null)
            {
                Logger.Warning<LibraryPageViewModel>("Storage provider is not available");
                // TODO: Custom Exception
                throw new Exception();
            }
        }
        catch (Exception ex)
        {
            Logger.Error<LibraryPageViewModel>("Storage provider is not available");
            Logger.LogExceptionDetails<LibraryPageViewModel>(ex);
            await _messageBoxService.ShowErrorAsync(LocalizationHelper.GetText("LibraryPage.Options.AddGame.MissingStorageProvider.Title"),
                string.Format(LocalizationHelper.GetText("LibraryPage.Options.AddGame.MissingStorageProvider.Message"), ex));
            return;
        }

        // Select the correct Xenia version
        try
        {
            List<XeniaVersion> installedVersions = _settings.GetInstalledVersions(_settings);
            switch (installedVersions.Count)
            {
                case 0:
                    Logger.Error<LibraryPageViewModel>("No Xenia installations found, throwing exception");
                    // TODO: Custom exception
                    throw new Exception();
                case 1:
                    Logger.Info<LibraryPageViewModel>($"Only Xenia {installedVersions[0]} is installed");
                    xeniaVersion = installedVersions[0];
                    break;
                default:
                    XeniaVersion? chosen = await XeniaSelectionDialog.ShowAsync(installedVersions);
                    if (chosen is { } version)
                    {
                        // User selected a version – proceed
                        Logger.Info<LibraryPageViewModel>($"User selected Xenia {chosen}, proceeding with launch");
                        xeniaVersion = version;
                    }
                    else
                    {
                        // User canceled the selection
                        Logger.Info<LibraryPageViewModel>("Xenia version selection was cancelled by user");
                        return;
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            Logger.Error<LibraryPageViewModel>("No version of Xenia is currently installed");
            Logger.LogExceptionDetails<LibraryPageViewModel>(ex);
            await _messageBoxService.ShowErrorAsync(LocalizationHelper.GetText("LibraryPage.Options.AddGame.MissingXenia.Title"),
                LocalizationHelper.GetText("LibraryPage.Options.AddGame.MissingXenia.Message"));
            return;
        }

        // Open file picker
        IReadOnlyList<IStorageFile> files = await storageProvider.OpenFilePickerAsync(options);
        EventManager.Instance.DisableWindow();

        // Add all files
        foreach (IStorageFile file in files)
        {
            try
            {
                // Skip .zar files if ParseGameDetailsWithXenia is disabled
                if (!_settings.Settings.General.ParseGameDetailsWithXenia && file.Path.LocalPath.EndsWith(".zar", StringComparison.OrdinalIgnoreCase))
                {
                    Logger.Warning<LibraryPageViewModel>($"Skipping .zar file (ParseGameDetailsWithXenia disabled): {file.Path.LocalPath}");
                    continue;
                }

                Logger.Info<LibraryPageViewModel>($"Selected File: {file.Path.LocalPath}");
                ParsedGameDetails details = GameManager.GetGameDetails(file.Path.LocalPath);

                if (!details.IsValid && _settings.Settings.General.ParseGameDetailsWithXenia)
                {
                    // Fetching details using Xenia
                    details = await GameManager.GetGameDetailsWithXenia(file.Path.LocalPath, xeniaVersion);
                }
                Logger.Info<LibraryPageViewModel>($"Title: {details.Title}, Game ID: {details.TitleId}, Media ID: {details.MediaId}");
                try
                {
                    await XboxDatabase.LoadAsync();
                    Logger.Info<LibraryPageViewModel>($"Searching database by title_id {details.TitleId}");
                    await Task.WhenAll(XboxDatabase.SearchDatabase(details.TitleId));
                    if (XboxDatabase.FilteredDatabase.Count == 1)
                    {
                        // Add the game using fetched GameInfo
                        GameInfo gameInfo = XboxDatabase.FilteredDatabase[0];
                        await GameManager.AddGame(xeniaVersion, gameInfo, file.Path.LocalPath, details);
                    }
                    else
                    {
                        // TODO: Open GameDatabaseWindow to allow the user to select the game
                        // Currently disabled
                        await GameManager.AddUnknownGame(xeniaVersion, details, file.Path.LocalPath);
                    }
                }
                catch (HttpRequestException)
                {
                    // TODO: Log it and add it as unknown game
                    await GameManager.AddUnknownGame(xeniaVersion, details, file.Path.LocalPath);
                }
                catch (TaskCanceledException)
                {
                    // TODO: Log it and add it as unknown game
                    await GameManager.AddUnknownGame(xeniaVersion, details, file.Path.LocalPath);
                }
            }
            catch (Exception ex)
            {
                Logger.Error<LibraryPageViewModel>($"Failed to add game: {file.Path.LocalPath}");
                Logger.LogExceptionDetails<LibraryPageViewModel>(ex);
                EventManager.Instance.EnableWindow();
                await _messageBoxService.ShowErrorAsync(LocalizationHelper.GetText("LibraryPage.Options.AddGame.Failed.Title"),
                    string.Format(LocalizationHelper.GetText("LibraryPage.Options.AddGame.Failed.Message"), file.Path.LocalPath, ex));
            }
        }
        EventManager.Instance.EnableWindow();
        RefreshLibrary();
    }

    [RelayCommand]
    private async Task ExportGameShortcuts()
    {
        try
        {
            IStorageProvider? storageProvider = App.MainWindow?.StorageProvider;
            if (storageProvider == null)
            {
                Logger.Warning<LibraryPageViewModel>("Storage provider is not available");
                await _messageBoxService.ShowErrorAsync(
                    LocalizationHelper.GetText("LibraryPage.Options.ExportShortcuts.MissingStorageProvider.Title"),
                    LocalizationHelper.GetText("LibraryPage.Options.ExportShortcuts.MissingStorageProvider.Message"));
                return;
            }

            // Open folder picker dialog
            FolderPickerOpenOptions options = new FolderPickerOpenOptions
            {
                Title = LocalizationHelper.GetText("LibraryPage.Options.ExportShortcuts.FolderPicker.Title"),
                AllowMultiple = false
            };

            IReadOnlyList<IStorageFolder> folders = await storageProvider.OpenFolderPickerAsync(options);

            if (folders.Count == 0)
            {
                // User canceled the folder picker
                Logger.Info<LibraryPageViewModel>("Export game shortcuts canceled by user");
                return;
            }

            string exportDirectory = folders[0].Path.LocalPath;
            Logger.Info<LibraryPageViewModel>($"Exporting game shortcuts to: '{exportDirectory}'");

            List<string> successList = [];
            List<string> failList = [];

            foreach (GameItemViewModel game in Games)
            {
                try
                {
                    ShortcutManager.CreateShortcut(game.Game, exportDirectory);
                    successList.Add(game.Title);
                }
                catch (Exception ex)
                {
                    Logger.Error<LibraryPageViewModel>($"Failed to create shortcut for: '{game.Title}'");
                    Logger.LogExceptionDetails<LibraryPageViewModel>(ex);
                    failList.Add(game.Title);
                }
            }

            Logger.Info<LibraryPageViewModel>($"Successfully exported {successList.Count} game shortcut(s) to: '{exportDirectory}'");

            string message = string.Format(
                LocalizationHelper.GetText("LibraryPage.Options.ExportShortcuts.Success.Message"),
                successList.Count, failList.Count,
                successList.Count > 0 ? string.Join("\n", successList.Select(t => $"• {t}")) : "None",
                failList.Count > 0 ? string.Join("\n", failList.Select(t => $"• {t}")) : "None");

            await _messageBoxService.ShowInfoAsync(
                LocalizationHelper.GetText("LibraryPage.Options.ExportShortcuts.Success.Title"),
                message);
        }
        catch (Exception ex)
        {
            Logger.Error<LibraryPageViewModel>("Failed to export game shortcuts");
            Logger.LogExceptionDetails<LibraryPageViewModel>(ex);
            await _messageBoxService.ShowErrorAsync(
                LocalizationHelper.GetText("LibraryPage.Options.ExportShortcuts.Error.Title"),
                string.Format(LocalizationHelper.GetText("LibraryPage.Options.ExportShortcuts.Error.Message"), ex.Message));
        }
    }

    [RelayCommand]
    private async Task UpdateCompatibilityRatings()
    {
        try
        {
            Logger.Info<LibraryPageViewModel>("Starting compatibility rating update for all games");

            // Force reload the compatibility database to get fresh data
            Logger.Info<LibraryPageViewModel>("Force reloading game compatibility database");
            await GameCompatibilityDatabase.ForceReloadAsync();

            int updatedCount = 0;
            int failedCount = 0;
            int unchangedCount = 0;
            List<string> updatedGames = [];
            List<string> failedGames = [];
            List<string> unchangedGames = [];

            foreach (GameItemViewModel gameItem in Games)
            {
                try
                {
                    Game game = gameItem.Game;
                    CompatibilityRating oldRating = game.Compatibility.Rating;
                    Logger.Debug<LibraryPageViewModel>($"Updating compatibility rating for: '{game.Title}' (ID: {game.GameId})");

                    await GameCompatibilityDatabase.SetCompatibilityRating(game);

                    if (oldRating != game.Compatibility.Rating)
                    {
                        updatedCount++;
                        updatedGames.Add($"{game.Title} ({oldRating} → {game.Compatibility.Rating})");
                        Logger.Info<LibraryPageViewModel>($"Updated compatibility rating for: '{game.Title}' from {oldRating} to {game.Compatibility.Rating}");
                    }
                    else
                    {
                        unchangedCount++;
                        unchangedGames.Add(game.Title);
                        Logger.Debug<LibraryPageViewModel>($"Compatibility rating unchanged for: '{game.Title}' ({oldRating})");
                    }
                }
                catch (Exception ex)
                {
                    failedCount++;
                    failedGames.Add(gameItem.Title);
                    Logger.Error<LibraryPageViewModel>($"Failed to update compatibility rating for: '{gameItem.Title}'");
                    Logger.LogExceptionDetails<LibraryPageViewModel>(ex);
                }
            }

            Logger.Info<LibraryPageViewModel>($"Compatibility rating update completed. Updated: {updatedCount}, Unchanged: {unchangedCount}, Failed: {failedCount}");

            // Refresh the library to update the UI
            RefreshLibrary();

            // Show results
            string message = string.Format(
                LocalizationHelper.GetText("LibraryPage.Options.UpdateCompatibilityRatings.Success.Message"),
                updatedCount, unchangedCount, failedCount,
                updatedCount > 0 ? string.Join("\n", updatedGames.Select(t => $"• {t}")) : "None",
                unchangedCount > 0 ? string.Join("\n", unchangedGames.Select(t => $"• {t}")) : "None",
                failedCount > 0 ? string.Join("\n", failedGames.Select(t => $"• {t}")) : "None");

            await _messageBoxService.ShowInfoAsync(
                LocalizationHelper.GetText("LibraryPage.Options.UpdateCompatibilityRatings.Success.Title"),
                message);
        }
        catch (Exception ex)
        {
            Logger.Error<LibraryPageViewModel>("Failed to update compatibility ratings");
            Logger.LogExceptionDetails<LibraryPageViewModel>(ex);
            await _messageBoxService.ShowErrorAsync(
                LocalizationHelper.GetText("LibraryPage.Options.UpdateCompatibilityRatings.Error.Title"),
                string.Format(LocalizationHelper.GetText("LibraryPage.Options.UpdateCompatibilityRatings.Error.Message"), ex.Message));
        }
        finally
        {
            GameManager.SaveLibrary();
        }
    }

    [RelayCommand]
    private async Task UpdateOptimizedSettings()
    {
        try
        {
            Logger.Info<LibraryPageViewModel>("Starting optimized settings update for all games");

            // Disable the window to prevent user interaction during the update
            EventManager.Instance.DisableWindow();

            // Force reload the optimized settings database to get fresh data
            Logger.Info<LibraryPageViewModel>("Force reloading optimized settings database");
            await OptimizedSettingsDatabase.ForceReloadAsync();

            int updatedCount = 0;
            int failedCount = 0;
            int notFoundCount = 0;
            int unchangedCount = 0;
            List<string> updatedGames = [];
            List<string> failedGames = [];
            List<string> notFoundGames = [];
            List<string> unchangedGames = [];

            foreach (GameItemViewModel gameItem in Games)
            {
                try
                {
                    Game game = gameItem.Game;
                    Logger.Debug<LibraryPageViewModel>($"Updating optimized settings for: '{game.Title}' (ID: {game.GameId})");

                    // Fetch optimized settings from the database
                    ConfigFile? optimizedConfigFile = await OptimizedSettingsDatabase.GetOptimizedSettings(game);

                    if (optimizedConfigFile == null)
                    {
                        notFoundCount++;
                        notFoundGames.Add(game.Title);
                        Logger.Debug<LibraryPageViewModel>($"No optimized settings found for: '{game.Title}'");
                        continue;
                    }

                    Logger.Info<LibraryPageViewModel>($"Found optimized settings for game: '{game.Title}'");

                    // Get the game's config file path
                    string configPath = AppPathResolver.GetFullPath(game.FileLocations.Config);

                    // Check if the config file exists, create if not
                    if (!File.Exists(configPath))
                    {
                        Logger.Debug<LibraryPageViewModel>($"Config file not found for '{game.Title}', creating from default");
                        ConfigManager.CreateConfigurationFile(configPath, game.XeniaVersion);
                    }

                    // Load the current config file
                    ConfigFile currentConfigFile = ConfigFile.Load(configPath);

                    // Apply optimized settings to the current config (mimicking XeniaSettingsPageViewModel behavior)
                    bool hasChanges = false;
                    foreach (ConfigSection optimizedSection in optimizedConfigFile.Sections)
                    {
                        ConfigSection? currentSection = currentConfigFile.GetSection(optimizedSection.Name);
                        if (currentSection == null)
                        {
                            // Skip the section if it doesn't exist in the current config
                            Logger.Debug<LibraryPageViewModel>($"Skipping section '{optimizedSection.Name}' - not present in current config");
                            continue;
                        }

                        foreach (ConfigOption optimizedOption in optimizedSection.Options)
                        {
                            ConfigOption? currentOption = currentSection.GetOption(optimizedOption.Name);
                            if (currentOption == null)
                            {
                                // Skip option if it doesn't exist in the current config
                                Logger.Debug<LibraryPageViewModel>($"Skipping option '{optimizedSection.Name}.{optimizedOption.Name}' - not present in current config");
                                continue;
                            }

                            // Only apply if the types match
                            if (currentOption.Type != optimizedOption.Type)
                            {
                                Logger.Debug<LibraryPageViewModel>($"Skipping option '{optimizedSection.Name}.{optimizedOption.Name}' - type mismatch (current: {currentOption.Type}, optimized: {optimizedOption.Type})");
                                continue;
                            }

                            // Compare values properly (handle null and use Equals for value comparison)
                            bool valuesAreEqual = currentOption.Value?.Equals(optimizedOption.Value) ?? optimizedOption.Value == null;

                            if (!valuesAreEqual)
                            {
                                // Store old value for logging before updating
                                object? oldValue = currentOption.Value;

                                // Update the value if different
                                currentOption.Value = optimizedOption.Value;
                                hasChanges = true;
                                Logger.Debug<LibraryPageViewModel>($"Updated option '{optimizedSection.Name}.{optimizedOption.Name}' from '{oldValue}' to '{currentOption.Value}'");
                            }
                            else
                            {
                                Logger.Debug<LibraryPageViewModel>($"Option '{optimizedSection.Name}.{optimizedOption.Name}' unchanged (current: {currentOption.Value}, optimized: {optimizedOption.Value})");
                            }
                        }
                    }

                    if (hasChanges)
                    {
                        // Save the changes
                        currentConfigFile.Save(configPath);
                        updatedCount++;
                        updatedGames.Add(game.Title);
                        Logger.Info<LibraryPageViewModel>($"Saved optimized settings for game: '{game.Title}'");
                    }
                    else
                    {
                        unchangedCount++;
                        unchangedGames.Add(game.Title);
                        Logger.Debug<LibraryPageViewModel>($"Optimized settings unchanged for: '{game.Title}'");
                    }
                }
                catch (Exception ex)
                {
                    failedCount++;
                    failedGames.Add(gameItem.Title);
                    Logger.Error<LibraryPageViewModel>($"Failed to update optimized settings for: '{gameItem.Title}'");
                    Logger.LogExceptionDetails<LibraryPageViewModel>(ex);
                }
            }

            Logger.Info<LibraryPageViewModel>($"Optimized settings update completed. Updated: {updatedCount}, Unchanged: {unchangedCount}, Not Found: {notFoundCount}, Failed: {failedCount}");

            // Refresh the library to update the UI
            RefreshLibrary();

            // Show results
            string message = string.Format(
                LocalizationHelper.GetText("LibraryPage.Options.OptimizeGameSettings.Success.Message"),
                updatedCount, unchangedCount, notFoundCount, failedCount,
                updatedCount > 0 ? string.Join("\n", updatedGames.Select(t => $"• {t}")) : "None",
                unchangedCount > 0 ? string.Join("\n", unchangedGames.Select(t => $"• {t}")) : "None",
                notFoundCount > 0 ? string.Join("\n", notFoundGames.Select(t => $"• {t}")) : "None",
                failedCount > 0 ? string.Join("\n", failedGames.Select(t => $"• {t}")) : "None");

            // Re-enable the window before showing the message box
            EventManager.Instance.EnableWindow();

            await _messageBoxService.ShowInfoAsync(
                LocalizationHelper.GetText("LibraryPage.Options.OptimizeGameSettings.Success.Title"),
                message);
        }
        catch (Exception ex)
        {
            Logger.Error<LibraryPageViewModel>("Failed to update optimized settings");
            Logger.LogExceptionDetails<LibraryPageViewModel>(ex);

            // Re-enable the window before showing the error message box
            EventManager.Instance.EnableWindow();

            await _messageBoxService.ShowErrorAsync(
                LocalizationHelper.GetText("LibraryPage.Options.OptimizeGameSettings.Error.Title"),
                string.Format(LocalizationHelper.GetText("LibraryPage.Options.OptimizeGameSettings.Error.Message"), ex.Message));
        }
        finally
        {
            GameManager.SaveLibrary();
        }
    }
}