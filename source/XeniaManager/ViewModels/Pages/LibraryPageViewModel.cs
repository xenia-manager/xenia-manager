using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using XeniaManager.Core.Logging;
using XeniaManager.Core.Manage;
using XeniaManager.Core.Models;
using XeniaManager.Core.Models.Database.Xbox;
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
    [ObservableProperty] private string _viewToggleText = "List View";
    [ObservableProperty] private Symbol _viewToggleIcon = Symbol.List;
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
    private void ToggleView()
    {
        IsGridView = !IsGridView;

        if (IsGridView)
        {
            ViewToggleText = "List View";
            ViewToggleIcon = Symbol.List;
        }
        else
        {
            ViewToggleText = "Grid View";
            ViewToggleIcon = Symbol.Grid;
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
        // TODO: Open Folder Dialog, scan for compatible files (.xex, .iso & GOD) and add it to Xenia Manager
        // TODO: Improve it to support more folder layouts
        await _messageBoxService.ShowErrorAsync("Not implemented", "This feature is not implemented yet.");
    }

    [RelayCommand]
    private async Task AddGame()
    {
        // Initialize required variables
        XeniaVersion xeniaVersion;
        IStorageProvider? storageProvider;
        // Create a file picker
        FilePickerOpenOptions options = new FilePickerOpenOptions
        {
            Title = "Select Game",
            AllowMultiple = true,
            FileTypeFilter = new List<FilePickerFileType>
            {
                new FilePickerFileType("Supported Files")
                {
                    Patterns = ["*.iso", "*.xex", "*.zar"]
                },
                new FilePickerFileType("All Files")
                {
                    Patterns = ["*"]
                }
            }
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
                Logger.Info<LibraryPageViewModel>($"Selected File: {file.Path.LocalPath}");
                (string gameTitle, string gameId, string mediaId) = ("Not found", "00000000", "00000000");
                // TODO: Get details without Xenia

                // Fetching details using Xenia
                if (gameId == "00000000" || mediaId == "00000000")
                {
                    (gameTitle, gameId, mediaId) = await GameManager.GetGameDetailsWithXenia(file.Path.LocalPath, xeniaVersion);
                }
                Logger.Info<LibraryPageViewModel>($"Title: {gameTitle}, Game ID: {gameId}, Media ID: {mediaId}");
                try
                {
                    await XboxDatabase.LoadAsync();
                    Logger.Info<LibraryPageViewModel>($"Searching database by title_id {gameId}");
                    await Task.WhenAll(XboxDatabase.SearchDatabase(gameId));
                    if (XboxDatabase.FilteredDatabase.Count == 1)
                    {
                        // Add the game using fetched GameInfo
                        GameInfo gameInfo = XboxDatabase.FilteredDatabase[0];
                        await GameManager.AddGame(xeniaVersion, gameInfo, file.Path.LocalPath, gameTitle, gameId, mediaId);
                    }
                    else
                    {
                        // TODO: Open GameDatabaseWindow to allow the user to select the game
                        // Currently disabled
                        await GameManager.AddUnknownGame(xeniaVersion, gameTitle, file.Path.LocalPath, gameId, mediaId);
                    }
                }
                catch (HttpRequestException)
                {
                    // TODO: Log it and add it as unknown game
                    await GameManager.AddUnknownGame(xeniaVersion, gameTitle, file.Path.LocalPath, gameId, mediaId);
                }
                catch (TaskCanceledException)
                {
                    // TODO: Log it and add it as unknown game
                    await GameManager.AddUnknownGame(xeniaVersion, gameTitle, file.Path.LocalPath, gameId, mediaId);
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
        // TODO: Open Folder Dialog, user selects a folder and export all game shortcuts to that location
        await _messageBoxService.ShowErrorAsync("Not implemented", "This feature is not implemented yet.");
    }
}