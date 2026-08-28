using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using XeniaManager.BigScreen.Models;
using XeniaManager.BigScreen.Services;
using XeniaManager.BigScreen.Utilities;
using XeniaManager.BigScreen.ViewModels.Items;
using XeniaManager.Files;
using XeniaManager.Logging;
using XeniaManager.Core.Models.Game;
using XeniaManager.Core.Utilities;

namespace XeniaManager.BigScreen.ViewModels.Modals;

/// <summary>
/// The game modal's achievements pane: stats header, an X-cycled sort
/// (Achieved / Gamerscore Awarded / Alphabetical) and a scrollable flat list
/// of rows from the active profile's per-game achievement GPD.
/// </summary>
public partial class AchievementsPaneViewModel : ViewModelBase, IGameModalPane
{
    private readonly List<AchievementItemViewModel> _allAchievements;
    private readonly GpdFile? _gpdFile;

    /// <summary>
    /// The achievements currently shown, sorted by <see cref="Sort"/>.
    /// </summary>
    public ObservableCollection<AchievementItemViewModel> Rows { get; } = [];

    /// <summary>
    /// Whether the pane shows the empty state (no GPD or no achievements).
    /// </summary>
    public bool ShowEmpty => Rows.Count == 0;

    /// <summary>
    /// The active sort order; X cycles through the options.
    /// </summary>
    [ObservableProperty]
    public partial AchievementSort Sort { get; set; } = AchievementSort.Achieved;

    /// <summary>
    /// Unlocked / total achievement counters for the active profile.
    /// </summary>
    public string AchievementText { get; }

    /// <summary>
    /// Unlocked / total gamerscore for the active profile.
    /// </summary>
    public string GamerscoreText { get; }

    /// <summary>
    /// The sort order's display text.
    /// </summary>
    public string SortText => Sort switch
    {
        AchievementSort.GamerscoreAwarded =>
            LocalizationHelper.GetText("GameModal.Achievements.Sort.GamerscoreAwarded"),
        AchievementSort.Alphabetical => LocalizationHelper.GetText("GameModal.Achievements.Sort.Alphabetical"),
        _ => LocalizationHelper.GetText("GameModal.Achievements.Sort.Achieved")
    };

    /// <summary>
    /// Raised after the selection moves, so the view can scroll the selected
    /// row into view.
    /// </summary>
    public event Action? ScrollRequested;

    /// <summary>
    /// Steps the sort to the next option, keeping the selection on the same row.
    /// </summary>
    private void CycleSort()
    {
        Sort = EnumCycleHelper.Next(Sort, 1);
        Logger.Trace<AchievementsPaneViewModel>($"Achievements sort: {Sort}");
    }

    /// <summary>
    /// Sorts an achievement set by the current sort order (Achieved keeps the
    /// GPD order within each group).
    /// </summary>
    private List<AchievementItemViewModel> SortAchievements(IEnumerable<AchievementItemViewModel> items)
    {
        return Sort switch
        {
            AchievementSort.GamerscoreAwarded =>
                items.OrderByDescending(item => item.Gamerscore).ToList(),
            AchievementSort.Alphabetical =>
                items.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase).ToList(),
            _ => items.OrderBy(item => item.IsUnlocked ? 0 : 1).ToList()
        };
    }

    /// <summary>
    /// Rebuilds <see cref="Rows"/> from the sort order, keeping the selection on
    /// the same row index so the viewport stays put.
    /// </summary>
    private void ApplySort()
    {
        SelectionHelper.ResortPreservingSelection(Rows, SortAchievements(_allAchievements));
    }

    partial void OnSortChanged(AchievementSort value)
    {
        ApplySort();
        OnPropertyChanged(nameof(SortText));
    }

    /// <summary>
    /// Selects the first achievement row when the pane becomes active.
    /// </summary>
    public void OnPaneEntered()
    {
        SelectionHelper.SelectOnlyAt(Rows, 0);
    }

    /// <summary>
    /// Clears the achievement selection when the pane loses focus.
    /// </summary>
    public void OnPaneExited()
    {
        SelectionHelper.ClearSelection(Rows);
    }

    /// <summary>
    /// Handles pane input: Up/Down moves the rows (scrolling into view), X
    /// cycles the sort.
    /// </summary>
    public bool HandleInput(NavigationCommand command)
    {
        switch (command)
        {
            case NavigationCommand.MoveUp:
                SelectionHelper.MoveSelection(Rows, -1);
                ScrollRequested?.Invoke();
                return true;
            case NavigationCommand.MoveDown:
                SelectionHelper.MoveSelection(Rows, 1);
                ScrollRequested?.Invoke();
                return true;
            case NavigationCommand.CycleSort:
                CycleSort();
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Loads the achievement GPD for the active profile (from the boot preload
    /// cache; the cache owns its lifetime) and builds the rows.
    /// </summary>
    public AchievementsPaneViewModel(Game game)
    {
        _gpdFile = GameDataCache.GetAchievementGpd(game);
        if (_gpdFile == null)
        {
            AchievementText = "0 / 0";
            GamerscoreText = "0 / 0";
            _allAchievements = [];
            return;
        }

        _allAchievements = _gpdFile.Achievements
            .Select(achievement => new AchievementItemViewModel(achievement, _gpdFile))
            .ToList();
        int unlockedCount = 0;
        int unlockedGamerscore = 0;
        int totalGamerscore = 0;
        foreach (AchievementItemViewModel achievement in _allAchievements)
        {
            totalGamerscore += achievement.Gamerscore;
            if (achievement.IsUnlocked)
            {
                unlockedCount++;
                unlockedGamerscore += achievement.Gamerscore;
            }
        }

        AchievementText = $"{unlockedCount} / {_allAchievements.Count}";
        GamerscoreText = $"{unlockedGamerscore} / {totalGamerscore}";
        foreach (AchievementItemViewModel achievement in SortAchievements(_allAchievements))
        {
            Rows.Add(achievement);
        }

        Logger.Debug<AchievementsPaneViewModel>(
            $"Achievements pane: {Rows.Count} achievements ({_allAchievements.Count(a => a.IsUnlocked)} unlocked)");
    }
}