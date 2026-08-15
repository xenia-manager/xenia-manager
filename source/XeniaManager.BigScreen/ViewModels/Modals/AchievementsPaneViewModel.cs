using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using XeniaManager.BigScreen.Models;
using XeniaManager.BigScreen.Services;
using XeniaManager.BigScreen.Utilities;
using XeniaManager.BigScreen.ViewModels.Items;
using XeniaManager.Core.Files;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Models.Game;
using XeniaManager.Core.Utilities;

namespace XeniaManager.BigScreen.ViewModels.Modals;

/// <summary>
/// The game modal's achievements pane: stats header, an X-cycled sort
/// (Achieved / Gamerscore Awarded / Alphabetical) and scrollable rows from
/// the active profile's per-game achievement GPD.
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
    /// The unlocked achievements, shown in their own section.
    /// </summary>
    public ObservableCollection<AchievementItemViewModel> UnlockedRows { get; } = [];

    /// <summary>
    /// The locked achievements, shown in their own (spoiler-gated) section.
    /// </summary>
    public ObservableCollection<AchievementItemViewModel> LockedRows { get; } = [];

    /// <summary>
    /// "Unlocked (N)" header text for the unlocked section.
    /// </summary>
    public string UnlockedCountText =>
        $"{LocalizationHelper.GetText("GameModal.Achievements.Section.Unlocked")} ({UnlockedRows.Count})";

    /// <summary>
    /// "Locked (N)" header text for the locked section.
    /// </summary>
    public string LockedCountText =>
        $"{LocalizationHelper.GetText("GameModal.Achievements.Section.Locked")} ({LockedRows.Count})";

    /// <summary>
    /// Whether the unlocked section is shown.
    /// </summary>
    public bool HasUnlocked => UnlockedRows.Count > 0;

    /// <summary>
    /// Whether the locked section is shown.
    /// </summary>
    public bool HasLocked => LockedRows.Count > 0;

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
    /// Whether the pane shows the empty state (no GPD or no achievements).
    /// </summary>
    public bool ShowEmpty => Rows.Count == 0;

    /// <summary>
    /// Raised after the selection moves, so the view can scroll the selected
    /// row into view.
    /// </summary>
    public event Action? ScrollRequested;

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
        AchievementText = $"{_allAchievements.Count(achievement => achievement.IsUnlocked)} / {_allAchievements.Count}";
        GamerscoreText =
            $"{_allAchievements.Where(achievement => achievement.IsUnlocked).Sum(achievement => achievement.Gamerscore)} / {_allAchievements.Sum(achievement => achievement.Gamerscore)}";
        foreach (AchievementItemViewModel achievement in _allAchievements)
        {
            Rows.Add(achievement);
        }

        RebuildSections();
        Logger.Debug<AchievementsPaneViewModel>(
            $"Achievements pane: {Rows.Count} achievements ({UnlockedRows.Count} unlocked, {LockedRows.Count} locked)");
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
    /// Steps the sort to the next option, keeping the selection on the same row.
    /// </summary>
    private void CycleSort()
    {
        Sort = EnumCycleHelper.Next(Sort, 1);
        Logger.Trace<AchievementsPaneViewModel>($"Achievements sort: {Sort}");
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

    partial void OnSortChanged(AchievementSort value)
    {
        ApplySort();
        OnPropertyChanged(nameof(SortText));
    }

    /// <summary>
    /// Rebuilds <see cref="Rows"/> from the sort order, keeping the selection on
    /// the same row index so the viewport stays put, and rebuilds the
    /// unlocked/locked display sections.
    /// </summary>
    private void ApplySort()
    {
        List<AchievementItemViewModel> unlocked = SortAchievements(_allAchievements.Where(item => item.IsUnlocked));
        List<AchievementItemViewModel> locked = SortAchievements(_allAchievements.Where(item => !item.IsUnlocked));

        SelectionHelper.ResortPreservingSelection(Rows, unlocked.Concat(locked).ToList());
        RebuildSections(unlocked, locked);
    }

    /// <summary>
    /// Sorts an achievement set by the current sort order (Achieved keeps the
    /// GPD order within each section).
    /// </summary>
    private List<AchievementItemViewModel> SortAchievements(IEnumerable<AchievementItemViewModel> items)
    {
        return Sort switch
        {
            AchievementSort.GamerscoreAwarded =>
                items.OrderByDescending(item => item.Gamerscore).ToList(),
            AchievementSort.Alphabetical =>
                items.OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase).ToList(),
            _ => items.ToList()
        };
    }

    /// <summary>
    /// Rebuilds the unlocked/locked display sections (with the given sorted
    /// sets when provided, otherwise from the current sort order).
    /// </summary>
    private void RebuildSections(List<AchievementItemViewModel>? unlocked = null,
        List<AchievementItemViewModel>? locked = null)
    {
        unlocked ??= SortAchievements(_allAchievements.Where(item => item.IsUnlocked));
        locked ??= SortAchievements(_allAchievements.Where(item => !item.IsUnlocked));

        UnlockedRows.Clear();
        foreach (AchievementItemViewModel item in unlocked)
        {
            UnlockedRows.Add(item);
        }

        LockedRows.Clear();
        foreach (AchievementItemViewModel item in locked)
        {
            LockedRows.Add(item);
        }

        OnPropertyChanged(nameof(ShowEmpty));
        OnPropertyChanged(nameof(UnlockedCountText));
        OnPropertyChanged(nameof(LockedCountText));
        OnPropertyChanged(nameof(HasUnlocked));
        OnPropertyChanged(nameof(HasLocked));
    }
}