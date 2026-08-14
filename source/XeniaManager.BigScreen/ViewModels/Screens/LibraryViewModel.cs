using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using XeniaManager.BigScreen.Constants;
using XeniaManager.BigScreen.Models;
using XeniaManager.BigScreen.Models.Settings;
using XeniaManager.BigScreen.Services;
using XeniaManager.BigScreen.Utilities;
using XeniaManager.BigScreen.ViewModels.Items;
using XeniaManager.Core.Database;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Models.Database.Xbox;
using XeniaManager.Core.Utilities;

namespace XeniaManager.BigScreen.ViewModels.Screens;

/// <summary>
/// Library screen state: the full game carousel/list, its sort mode, and the
/// details pane of the list view.
/// </summary>
public partial class LibraryViewModel : ScreenViewModel
{
    private readonly SettingsViewModel _settings;

    /// <summary>
    /// All games in the library (carousel / list).
    /// </summary>
    public ObservableCollection<GameCardViewModel> Games { get; } = [];

    /// <summary>
    /// Whether the library screen shows the "no games" stub.
    /// </summary>
    public bool ShowEmptyStub => Games.Count == 0;

    /// <summary>
    /// The current library sort mode (cycled with Y).
    /// </summary>
    [ObservableProperty] private LibrarySort _sort = LibrarySort.Alphabetical;

    /// <summary>
    /// Display name of the current library sort mode.
    /// </summary>
    public string SortText => Sort switch
    {
        LibrarySort.TimePlayed => LocalizationHelper.GetText("Library.Sort.TimePlayed"),
        LibrarySort.LastPlayed => LocalizationHelper.GetText("Library.Sort.LastPlayed"),
        _ => LocalizationHelper.GetText("Library.Sort.Alphabetical"),
    };

    /// <summary>
    /// Whether the library is shown as a vertical list with a details pane
    /// (vs. the horizontal card carousel). Follows the settings dropdown.
    /// </summary>
    [ObservableProperty] private bool _isListView;

    /// <summary>
    /// The currently selected game card (drives the details pane).
    /// </summary>
    [ObservableProperty] private GameCardViewModel? _selectedCard;

    /// <summary>
    /// The details pane content for the selected game, or null when nothing is selected.
    /// </summary>
    [ObservableProperty] private GameDetailsViewModel? _details;

    /// <summary>
    /// In-memory cache of fetched database info keyed by game ID (null values are
    /// cached too so games missing from the marketplace DB aren't re-fetched).
    /// </summary>
    private readonly Dictionary<string, GameDetailedInfo?> _detailsCache = [];

    /// <summary>
    /// Monotonic generation counter so a slow fetch can't overwrite the pane for a
    /// game the user already navigated away from.
    /// </summary>
    private int _detailsLoadGeneration;

    public LibraryViewModel(SettingsViewModel settings, IModalService modalService) : base(settings, modalService)
    {
        _settings = settings;
        Games.CollectionChanged += OnGamesCollectionChanged;
        settings.LibraryViewModeChanged += OnLibraryViewModeChanged;
        IsListView = settings.LibraryViewMode == LibraryViewMode.List;
    }

    /// <summary>
    /// Re-sorts the game collection. The selection follows the list position,
    /// not the element, so the selected card stays in the same spot on screen.
    /// </summary>
    public void ApplySort()
    {
        if (Games.Count == 0)
        {
            return;
        }

        List<GameCardViewModel> sorted = Sort switch
        {
            LibrarySort.TimePlayed => Games.OrderByDescending(g => g.Game.Playtime).ToList(),
            LibrarySort.LastPlayed => Games.OrderByDescending(g => g.Game.LastPlayed).ToList(),
            _ => Games.OrderBy(g => g.Game.Title, StringComparer.OrdinalIgnoreCase).ToList(),
        };

        SelectionHelper.ResortPreservingSelection(Games, sorted);
    }

    partial void OnSortChanged(LibrarySort value)
    {
        ApplySort();
        OnPropertyChanged(nameof(SortText));
        Logger.Debug<LibraryViewModel>($"Library sort changed to {value}");
    }

    /// <summary>
    /// Cycles the library sort mode: Alphabetical → Time Played → Last Played.
    /// </summary>
    public void CycleSort() => Sort = EnumCycleHelper.Next(Sort, 1);

    /// <summary>
    /// Swaps between the carousel and list views, persisting the choice through
    /// the settings dropdown (which the library follows live).
    /// </summary>
    public void ToggleView() =>
        _settings.LibraryViewMode = _settings.LibraryViewMode == LibraryViewMode.List
            ? LibraryViewMode.Carousel
            : LibraryViewMode.List;

    /// <summary>
    /// Tracks card selection changes (via PropertyChanged) whenever cards are
    /// added/removed, so the details pane follows the selected game.
    /// </summary>
    private void OnGamesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (GameCardViewModel card in e.NewItems.Cast<GameCardViewModel>())
            {
                card.PropertyChanged += OnCardPropertyChanged;
                if (card.IsSelected)
                {
                    SelectedCard = card;
                }
            }
        }

        if (e.OldItems != null)
        {
            foreach (GameCardViewModel card in e.OldItems.Cast<GameCardViewModel>())
            {
                card.PropertyChanged -= OnCardPropertyChanged;
            }
        }

        OnPropertyChanged(nameof(ShowEmptyStub));
    }

    /// <summary>
    /// Updates the selected card when it gains selection.
    /// </summary>
    private void OnCardPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(GameCardViewModel.IsSelected) &&
            sender is GameCardViewModel { IsSelected: true } card)
        {
            SelectedCard = card;
        }
    }

    partial void OnSelectedCardChanged(GameCardViewModel? value)
    {
        Details = value != null ? new GameDetailsViewModel(value) : null;
        if (value != null)
        {
            _ = LoadDetailsAsync(value);
        }
    }

    /// <summary>
    /// Fetches the marketplace database info for the selected game (off the UI thread,
    /// disk-cached for a day by Core) and applies it to the details pane. Stale results
    /// are discarded if the selection moved on while the fetch was in flight.
    /// </summary>
    private async Task LoadDetailsAsync(GameCardViewModel card)
    {
        string gameId = card.Game.GameId;
        if (_detailsCache.TryGetValue(gameId, out GameDetailedInfo? cached))
        {
            ApplyDetails(cached);
            return;
        }

        int generation = ++_detailsLoadGeneration;
        if (Details != null)
        {
            Details.IsLoading = true;
        }

        try
        {
            GameDetailedInfo? info = await XboxDatabase.GetFullGameInfo(gameId);
            _detailsCache[gameId] = info;
            if (generation == _detailsLoadGeneration && ReferenceEquals(SelectedCard, card))
            {
                ApplyDetails(info);
            }
        }
        catch (Exception ex)
        {
            Logger.Warning<LibraryViewModel>($"Failed to fetch database info for '{card.Game.Title}'");
            Logger.LogExceptionDetails<LibraryViewModel>(ex);
            _detailsCache[gameId] = null;
            if (generation == _detailsLoadGeneration && ReferenceEquals(SelectedCard, card))
            {
                ApplyDetails(null);
            }
        }
        finally
        {
            if (generation == _detailsLoadGeneration && Details != null)
            {
                Details.IsLoading = false;
            }
        }
    }

    /// <summary>
    /// Applies the fetched database info to the current details pane.
    /// </summary>
    private void ApplyDetails(GameDetailedInfo? info)
    {
        if (Details != null)
        {
            Details.Info = info;
        }
    }

    /// <summary>
    /// Preloads the marketplace DB info for every game into the details cache
    /// (runs inside the game-data boot stage). Failures are negative-cached so
    /// the details pane never re-fetches them. Logs how long each game took.
    /// </summary>
    public async Task PreloadDetailsAsync(CancellationToken cancellationToken)
    {
        int total = Games.Count;
        foreach (GameCardViewModel card in Games)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!_detailsCache.ContainsKey(card.Game.GameId))
            {
                try
                {
                    Stopwatch sw = Stopwatch.StartNew();
                    GameDetailedInfo? info = await XboxDatabase.GetFullGameInfo(card.Game.GameId, cancellationToken);
                    sw.Stop();
                    _detailsCache[card.Game.GameId] = info;
                    Logger.Info<LibraryViewModel>(
                        $"Details for '{card.Game.Title}' in {sw.ElapsedMilliseconds}ms");
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    Logger.Warning<LibraryViewModel>($"Failed to preload details for '{card.Game.Title}'");
                    Logger.LogExceptionDetails<LibraryViewModel>(ex);
                    _detailsCache[card.Game.GameId] = null;
                }
            }
        }

        Logger.Info<LibraryViewModel>($"Details preloaded for {total} games");
    }

    /// <summary>
    /// Follows the settings dropdown so the library swaps layouts live.
    /// </summary>
    private void OnLibraryViewModeChanged()
    {
        IsListView = _settings.LibraryViewMode == LibraryViewMode.List;
    }
}