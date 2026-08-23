using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using XeniaManager.BigScreen.Models;
using XeniaManager.BigScreen.Utilities;
using XeniaManager.BigScreen.ViewModels.Items;
using XeniaManager.Core.Database;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Manage;
using XeniaManager.Core.Models.Database.Patches;
using XeniaManager.Core.Models.Game;
using XeniaManager.Core.Utilities;

namespace XeniaManager.BigScreen.ViewModels.Modals;

/// <summary>
/// The patch download modal: searches both patch databases (pre-filled with
/// the game ID), lists the results with their source, and downloads the
/// selected patch on activation.
/// </summary>
public partial class PatchDownloadViewModel : ModalViewModelBase
{
    private readonly Game _game;
    private int _searchGeneration;

    /// <summary>
    /// The download search text, pre-filled with the game ID.
    /// </summary>
    [ObservableProperty]
    public partial string SearchText { get; set; }

    /// <summary>
    /// Whether the search is running (drives the "Searching…" state).
    /// </summary>
    [ObservableProperty]
    public partial bool IsSearching { get; set; }

    /// <summary>
    /// The search results.
    /// </summary>
    public ObservableCollection<PatchDownloadItemViewModel> Results { get; } = [];

    /// <summary>
    /// The failure status text, or empty when everything succeeded.
    /// </summary>
    [ObservableProperty]
    public partial string StatusText { get; set; } = string.Empty;

    /// <summary>
    /// Whether the status line is shown.
    /// </summary>
    public bool ShowStatus => StatusText.Length > 0;

    /// <summary>
    /// Whether the results area shows the empty state ("Searching…" while a
    /// search is running, "No patches found" afterwards).
    /// </summary>
    public bool ShowEmpty => !IsSearching && Results.Count == 0;

    /// <summary>
    /// Whether the results area shows the searching state.
    /// </summary>
    public bool ShowSearching => IsSearching && Results.Count == 0;

    /// <summary>
    /// The modal header: the download title plus the game's name.
    /// </summary>
    public string HeaderText => string.Format(
        LocalizationHelper.GetText("GameModal.Patches.Download.TitleWithGame"), _game.Title);

    /// <summary>
    /// Creates the modal, pre-fills the search with the game ID and runs the
    /// initial search.
    /// </summary>
    public PatchDownloadViewModel(Game game)
    {
        _game = game;
        SearchText = game.GameId;
        Results.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(ShowEmpty));
            OnPropertyChanged(nameof(ShowSearching));
        };
        TaskUtilities.RunSafely<PatchDownloadViewModel>(SearchAsync, "Searching patches database");
    }

    partial void OnIsSearchingChanged(bool value)
    {
        OnPropertyChanged(nameof(ShowEmpty));
        OnPropertyChanged(nameof(ShowSearching));
    }

    partial void OnStatusTextChanged(string value)
    {
        OnPropertyChanged(nameof(ShowStatus));
    }

    partial void OnSearchTextChanged(string value)
    {
        TaskUtilities.RunSafely<PatchDownloadViewModel>(SearchAsync, "Searching patches database");
    }

    /// <summary>
    /// Handles modal input: Up/Down moves the results, A downloads the selected
    /// patch (closing on success), B closes.
    /// </summary>
    public override bool HandleInput(NavigationCommand command)
    {
        switch (command)
        {
            case NavigationCommand.MoveUp:
                SelectionHelper.MoveSelection(Results, -1);
                return true;
            case NavigationCommand.MoveDown:
                SelectionHelper.MoveSelection(Results, 1);
                return true;
            case NavigationCommand.Activate:
                TaskUtilities.RunSafely<PatchDownloadViewModel>(
                    DownloadSelectedAsync, "Downloading selected patch");
                return true;
            case NavigationCommand.Back:
                Close();
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Selects and downloads the given result (mouse path).
    /// </summary>
    public void SelectResult(PatchDownloadItemViewModel item)
    {
        SelectionHelper.SelectOnly(Results, item);
        TaskUtilities.RunSafely<PatchDownloadViewModel>(
            DownloadSelectedAsync, "Downloading selected patch");
    }

    /// <summary>
    /// Searches both patch databases for the current search text. Stale searches
    /// (superseded by a newer keystroke) are dropped via a generation counter.
    /// </summary>
    private async Task SearchAsync()
    {
        int generation = ++_searchGeneration;
        try
        {
            IsSearching = true;
            await PatchesDatabase.LoadCanaryAsync();
            await PatchesDatabase.LoadNetplayAsync();
            await PatchesDatabase.SearchCanaryDatabase(SearchText);
            await PatchesDatabase.SearchNetplayDatabase(SearchText);
            if (generation != _searchGeneration)
            {
                return;
            }

            Results.Clear();
            foreach (PatchInfo patch in PatchesDatabase.CanaryFilteredDatabase)
            {
                Results.Add(new PatchDownloadItemViewModel(patch, "Canary"));
            }

            foreach (PatchInfo patch in PatchesDatabase.NetplayFilteredDatabase)
            {
                Results.Add(new PatchDownloadItemViewModel(patch, "Netplay"));
            }

            StatusText = string.Empty;
        }
        catch (Exception ex)
        {
            Logger.Error<PatchDownloadViewModel>("Failed to search the patches database");
            Logger.LogExceptionDetails<PatchDownloadViewModel>(ex);
            StatusText = LocalizationHelper.GetText("GameModal.Patches.Download.Failed");
        }
        finally
        {
            IsSearching = false;
        }
    }

    /// <summary>
    /// Downloads the selected patch for this game, closing the modal on success.
    /// </summary>
    private async Task DownloadSelectedAsync()
    {
        PatchDownloadItemViewModel? item = Results.FirstOrDefault(r => r.IsSelected);
        if (item == null)
        {
            return;
        }

        try
        {
            await PatchManager.DownloadPatchAsync(_game, item.PatchInfo);
            Logger.Info<PatchDownloadViewModel>($"Downloaded patch '{item.Name}'");
            Close();
        }
        catch (Exception ex)
        {
            Logger.Error<PatchDownloadViewModel>($"Failed to download patch '{item.Name}'");
            Logger.LogExceptionDetails<PatchDownloadViewModel>(ex);
            StatusText = LocalizationHelper.GetText("GameModal.Patches.Download.Failed");
        }
    }
}