using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using XeniaManager.BigScreen.Factories;
using XeniaManager.BigScreen.Models;
using XeniaManager.BigScreen.Services;
using XeniaManager.BigScreen.Utilities;
using XeniaManager.BigScreen.ViewModels.Items;
using XeniaManager.Files;
using XeniaManager.Files.Models.Account;
using XeniaManager.Files.Models.XConfig;
using XeniaManager.Files.Utilities;
using XeniaManager.Logging;
using XeniaManager.Core.Models.Game;
using XeniaManager.Core.Utilities;

namespace XeniaManager.BigScreen.ViewModels.Modals;

/// <summary>
/// The game modal's achievements pane: stats header, an X-cycled sort
/// (Achieved / Gamerscore Awarded / Alphabetical) and a scrollable flat list
/// of rows from the active profile's per-game achievement GPD. When there are
/// no achievements, A creates them from the game disc; Y fetches the
/// achievement images from the disc.
/// </summary>
public partial class AchievementsPaneViewModel : ViewModelBase, IGameModalPane
{
    private readonly Game _game;
    private readonly IModalService _modalService;
    private readonly IProfileService _profileService;
    private List<AchievementItemViewModel> _allAchievements = [];
    private GpdFile? _gpdFile;

    /// <summary>
    /// The achievements currently shown, sorted by <see cref="Sort"/>.
    /// </summary>
    public ObservableCollection<AchievementItemViewModel> Rows { get; } = [];

    /// <summary>
    /// Whether the pane shows the empty state (no GPD or no achievements).
    /// </summary>
    public bool ShowEmpty
    {
        get
        {
            return Rows.Count == 0;
        }
    }

    /// <summary>
    /// The empty-state text, hinting that A creates the achievements from the disc.
    /// </summary>
    public string EmptyStateText
    {
        get
        {
            return LocalizationHelper.GetText("GameModal.Achievements.EmptyCreate");
        }
    }

    /// <summary>
    /// Whether an achievement GPD build from disc is currently running.
    /// </summary>
    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    /// <summary>
    /// The active sort order; X cycles through the options.
    /// </summary>
    [ObservableProperty]
    public partial AchievementSort Sort { get; set; } = AchievementSort.Achieved;

    /// <summary>
    /// Unlocked / total achievement counters for the active profile.
    /// </summary>
    public string AchievementText { get; private set; } = "0 / 0";

    /// <summary>
    /// Unlocked / total gamerscore for the active profile.
    /// </summary>
    public string GamerscoreText { get; private set; } = "0 / 0";

    /// <summary>
    /// The sort order's display text.
    /// </summary>
    public string SortText
    {
        get
        {
            return Sort switch
            {
                AchievementSort.GamerscoreAwarded =>
                    LocalizationHelper.GetText("GameModal.Achievements.Sort.GamerscoreAwarded"),
                AchievementSort.Alphabetical => LocalizationHelper.GetText("GameModal.Achievements.Sort.Alphabetical"),
                _ => LocalizationHelper.GetText("GameModal.Achievements.Sort.Achieved")
            };
        }
    }

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
    private void ApplySort() => SelectionHelper.ResortPreservingSelection(Rows, SortAchievements(_allAchievements));

    partial void OnSortChanged(AchievementSort value)
    {
        ApplySort();
        OnPropertyChanged(nameof(SortText));
    }

    /// <summary>
    /// Selects the first achievement row when the pane becomes active.
    /// </summary>
    public void OnPaneEntered() => SelectionHelper.SelectOnlyAt(Rows, 0);

    /// <summary>
    /// Clears the achievement selection when the pane loses focus.
    /// </summary>
    public void OnPaneExited() => SelectionHelper.ClearSelection(Rows);

    /// <summary>
    /// Handles pane input: Up/Down moves the rows (scrolling into view), X
    /// cycles the sort, A creates the achievements from the disc when empty,
    /// Y fetches the achievement images from the disc (or creates the
    /// achievements when empty).
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
            case NavigationCommand.Activate:
                if (ShowEmpty && !IsBusy)
                {
                    TaskUtilities.RunSafely<AchievementsPaneViewModel>(CreateMissingAchievementsAsync, "Creating achievements");
                    return true;
                }

                return false;
            case NavigationCommand.Details:
                if (IsBusy)
                {
                    return false;
                }

                if (ShowEmpty)
                {
                    TaskUtilities.RunSafely<AchievementsPaneViewModel>(CreateMissingAchievementsAsync, "Creating achievements");
                    return true;
                }

                TaskUtilities.RunSafely<AchievementsPaneViewModel>(FetchAchievementImagesAsync, "Fetching achievement images");
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Creates or fills the achievement GPD for the version's active profile
    /// from the game disc. For multi-disc games the user picks which disc to
    /// read via the disc selection modal. Called from the pane (A/Y when empty)
    /// and from the game modal (Y on the achievements option).
    /// </summary>
    public async Task CreateMissingAchievementsAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            int? disc = _game.FileLocations.IsMultiDisc
                ? await _modalService.ShowAsync<int?>(new DiscSelectionViewModel(_game))
                : _game.LastPlayedDisc;
            if (disc == null || disc < 1)
            {
                Logger.Info<AchievementsPaneViewModel>("Disc selection cancelled, aborting achievement creation");
                return;
            }

            string? discPath = _game.FileLocations.GetDiscPath(disc.Value);
            if (string.IsNullOrEmpty(discPath) || (!File.Exists(discPath) && !Directory.Exists(discPath)))
            {
                throw new FileNotFoundException($"Disc file not found: {discPath}", discPath);
            }

            string? titleGpdPath = _profileService.GetGameAchievementGpdPath(_game.XeniaVersion, _game.GameId);
            string? profileGpdPath = _profileService.GetProfileGpdPath(_game.XeniaVersion);
            AccountInfo? profile = _profileService.ActiveProfileFor(_game.XeniaVersion);
            if (titleGpdPath == null || profileGpdPath == null || profile == null)
            {
                throw new InvalidOperationException("No active profile found");
            }

            XLanguage language = AchievementGpdBuilder.FromConsoleLanguage(profile.Language);
            AchievementGpdBuildResult result = await Task.Run(() =>
                AchievementGpdBuilder.EnsureAchievements(discPath, titleGpdPath, profileGpdPath, language));

            Logger.Info<AchievementsPaneViewModel>(
                $"Created achievements from disc: {result.AchievementsAdded} added, {result.AchievementsTotal} total");
            GameDataCache.ClearAchievementGpds();
            ReloadAchievements();
            _profileService.Refresh();
        }
        catch (Exception ex)
        {
            Logger.Error<AchievementsPaneViewModel>("Failed to create achievements from disc");
            Logger.LogExceptionDetails<AchievementsPaneViewModel>(ex);
            await ModalFactory.ConfirmAsync(_modalService,
                LocalizationHelper.GetText("GameModal.Achievements.Create.Failed.Title"),
                string.Format(LocalizationHelper.GetText("GameModal.Achievements.Create.Failed.Message"), ex.Message),
                LocalizationHelper.GetText("Modal.Confirm"),
                LocalizationHelper.GetText("Modal.Cancel"));
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Fetches achievement images from the game disc into the active profile's
    /// GPD. The user chooses between filling only missing images or
    /// overwriting all of them; multi-disc games ask which disc to read.
    /// </summary>
    private async Task FetchAchievementImagesAsync()
    {
        if (IsBusy || _gpdFile == null)
        {
            return;
        }

        IsBusy = true;
        try
        {
            bool? missingOnly = await ModalFactory.ConfirmAsync(_modalService,
                LocalizationHelper.GetText("GameModal.Achievements.Fetch.Mode.Title"),
                LocalizationHelper.GetText("GameModal.Achievements.Fetch.Mode.Message"),
                LocalizationHelper.GetText("GameModal.Achievements.Fetch.Mode.MissingOnly"),
                LocalizationHelper.GetText("GameModal.Achievements.Fetch.Mode.OverwriteAll"));
            if (missingOnly == null)
            {
                return;
            }

            int? disc = _game.FileLocations.IsMultiDisc
                ? await _modalService.ShowAsync<int?>(new DiscSelectionViewModel(_game))
                : _game.LastPlayedDisc;
            if (disc == null || disc < 1)
            {
                Logger.Info<AchievementsPaneViewModel>("Disc selection cancelled, aborting image fetch");
                return;
            }

            string? discPath = _game.FileLocations.GetDiscPath(disc.Value);
            if (string.IsNullOrEmpty(discPath) || (!File.Exists(discPath) && !Directory.Exists(discPath)))
            {
                throw new FileNotFoundException($"Disc file not found: {discPath}", discPath);
            }

            string? titleGpdPath = _profileService.GetGameAchievementGpdPath(_game.XeniaVersion, _game.GameId);
            if (titleGpdPath == null)
            {
                throw new InvalidOperationException("No active profile found");
            }

            bool overwriteAll = missingOnly == false;
            int written = await Task.Run(() => AchievementGpdBuilder.FetchImages(discPath, titleGpdPath, overwriteAll));

            Logger.Info<AchievementsPaneViewModel>($"Fetched {written} achievement images from disc (overwrite: {overwriteAll})");
            GameDataCache.ClearAchievementGpds();
            ReloadAchievements();
        }
        catch (Exception ex)
        {
            Logger.Error<AchievementsPaneViewModel>("Failed to fetch achievement images from disc");
            Logger.LogExceptionDetails<AchievementsPaneViewModel>(ex);
            await ModalFactory.ConfirmAsync(_modalService,
                LocalizationHelper.GetText("GameModal.Achievements.Fetch.Failed.Title"),
                string.Format(LocalizationHelper.GetText("GameModal.Achievements.Fetch.Failed.Message"), ex.Message),
                LocalizationHelper.GetText("Modal.Confirm"),
                LocalizationHelper.GetText("Modal.Cancel"));
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Loads the achievement GPD for the active profile (from the boot preload
    /// cache; the cache owns its lifetime) and rebuilds the rows.
    /// </summary>
    private void ReloadAchievements()
    {
        Rows.Clear();
        _allAchievements = [];
        _gpdFile = GameDataCache.GetAchievementGpd(_game);
        if (_gpdFile == null)
        {
            AchievementText = "0 / 0";
            GamerscoreText = "0 / 0";
            OnPropertyChanged(nameof(ShowEmpty));
            OnPropertyChanged(nameof(EmptyStateText));
            OnPropertyChanged(nameof(AchievementText));
            OnPropertyChanged(nameof(GamerscoreText));
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

        OnPropertyChanged(nameof(ShowEmpty));
        OnPropertyChanged(nameof(EmptyStateText));
        OnPropertyChanged(nameof(AchievementText));
        OnPropertyChanged(nameof(GamerscoreText));
        SelectionHelper.SelectOnlyAt(Rows, 0);

        Logger.Debug<AchievementsPaneViewModel>(
            $"Achievements pane: {Rows.Count} achievements ({_allAchievements.Count(a => a.IsUnlocked)} unlocked)");
    }

    /// <summary>
    /// Loads the achievement GPD for the active profile (from the boot preload
    /// cache; the cache owns its lifetime) and builds the rows.
    /// </summary>
    public AchievementsPaneViewModel(Game game)
    {
        _game = game;
        _modalService = App.Services.GetRequiredService<IModalService>();
        _profileService = App.Services.GetRequiredService<IProfileService>();
        ReloadAchievements();
    }
}