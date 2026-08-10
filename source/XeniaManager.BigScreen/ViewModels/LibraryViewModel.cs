using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using XeniaManager.BigScreen.Models;
using XeniaManager.BigScreen.Utilities;
using XeniaManager.BigScreen.ViewModels.Items;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Utilities;

namespace XeniaManager.BigScreen.ViewModels;

/// <summary>
/// Library screen state: the full game carousel and its sort mode.
/// </summary>
public partial class LibraryViewModel : ScreenViewModel
{
    /// <summary>
    /// All games in the library (library carousel).
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

    public LibraryViewModel(SettingsViewModel settings) : base(settings)
    {
        Games.CollectionChanged += (_, _) => OnPropertyChanged(nameof(ShowEmptyStub));
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
}
