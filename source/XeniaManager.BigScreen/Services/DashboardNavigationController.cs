using System;
using System.Linq;
using XeniaManager.BigScreen.Models;
using XeniaManager.BigScreen.Utilities;
using XeniaManager.BigScreen.ViewModels;
using XeniaManager.BigScreen.ViewModels.Items;

namespace XeniaManager.BigScreen.Services;

/// <summary>
/// Drives the dashboard's controller-style navigation: the active row, card
/// selection movement and option activation. Raises focus/scroll requests
/// that the view fulfills.
/// </summary>
public class DashboardNavigationController
{
    /// <summary>
    /// Whether the dashboard's active row is the option row (vs the game row).
    /// Tracked explicitly instead of relying on keyboard focus, since a game
    /// card is always focused regardless of the active row.
    /// </summary>
    public bool IsOnOptionsRow { get; set; }

    /// <summary>
    /// Whether the last option activation came from a mouse click, which skips
    /// focus restoration when the overlay closes.
    /// </summary>
    private bool _lastActivationWasMouse;

    /// <summary>
    /// Raised to request focus on an option card.
    /// </summary>
    public event Action<OptionsCardViewModel>? OptionFocusRequested;

    /// <summary>
    /// Raised to request focus on a game card.
    /// </summary>
    public event Action<GameCardViewModel>? GameFocusRequested;

    /// <summary>
    /// Raised when the library carousel should scroll to its selected card.
    /// </summary>
    public event Action? ScrollLibraryRequested;

    /// <summary>
    /// Raised when the media grid should scroll to its selected card.
    /// </summary>
    public event Action? ScrollMediaRequested;

    /// <summary>
    /// Raised when an overlay just opened and focus should move into it.
    /// </summary>
    public event Action? OverlayFocusRequested;

    /// <summary>
    /// Column mapping from a game card index to the option card underneath it
    /// (game 1 → option 1, games 2-3 → option 2, games 4-5 → option 3, game 6 → option 4).
    /// </summary>
    private static readonly int[] GameToOptionColumn = [0, 1, 1, 2, 2, 3];

    /// <summary>
    /// Column mapping from an option card index to the first game card of its
    /// group (option 1 → game 1, option 2 → game 2, option 3 → game 4, option 4 → game 6).
    /// </summary>
    private static readonly int[] OptionToGameColumn = [0, 1, 3, 5];

    /// <summary>
    /// Switches the dashboard to the option row, selecting the option card in the
    /// column underneath the current game selection (clamped to the option count).
    /// </summary>
    public void SelectOptionRow(DashboardViewModel dashboard)
    {
        if (dashboard.Options.Count == 0)
        {
            return;
        }

        IsOnOptionsRow = true;

        int gameIndex = SelectionHelper.IndexOfSelected(dashboard.RecentGames);
        int mapped = GameToOptionColumn[Math.Clamp(gameIndex, 0, GameToOptionColumn.Length - 1)];
        int target = Math.Clamp(mapped, 0, dashboard.Options.Count - 1);
        SelectionHelper.SelectOnlyAt(dashboard.Options, target);

        OptionFocusRequested?.Invoke(dashboard.Options[target]);
    }

    /// <summary>
    /// Switches the dashboard to the game row, selecting the first game card of
    /// the current option's column group (clamped to the game count). When the
    /// library is empty there is no game row, so the option row stays active.
    /// </summary>
    public void SelectGameRow(DashboardViewModel dashboard)
    {
        // No games - the game row doesn't exist, stay on the option row
        if (dashboard.RecentGames.Count == 0)
        {
            IsOnOptionsRow = true;
            return;
        }

        IsOnOptionsRow = false;

        int optionIndex = SelectionHelper.IndexOfSelected(dashboard.Options);
        int mapped = OptionToGameColumn[Math.Clamp(optionIndex, 0, OptionToGameColumn.Length - 1)];
        int target = Math.Clamp(mapped, 0, dashboard.RecentGames.Count - 1);
        SelectionHelper.SelectOnlyAt(dashboard.RecentGames, target);
        SelectionHelper.ClearSelection(dashboard.Options);

        GameFocusRequested?.Invoke(dashboard.RecentGames[target]);
    }

    /// <summary>
    /// Moves the option row selection by the given step, clamped at both ends.
    /// </summary>
    public void MoveOptionSelection(DashboardViewModel dashboard, int delta)
    {
        if (dashboard.Options.Count == 0)
        {
            return;
        }

        SelectionHelper.MoveSelection(dashboard.Options, delta);
    }

    /// <summary>
    /// Moves the dashboard game selection by the given step, clamped at both ends.
    /// </summary>
    public void MoveRecentGameSelection(DashboardViewModel dashboard, int delta)
    {
        if (dashboard.RecentGames.Count == 0)
        {
            return;
        }

        SelectionHelper.MoveSelection(dashboard.RecentGames, delta);
    }

    /// <summary>
    /// Moves the library carousel selection by the given step, clamped at both ends.
    /// </summary>
    public void MoveGameSelection(LibraryViewModel library, int delta)
    {
        if (library.Games.Count == 0)
        {
            return;
        }

        SelectionHelper.MoveSelection(library.Games, delta);
        ScrollLibraryRequested?.Invoke();
    }

    /// <summary>
    /// Moves the screenshot selection by the given step (1 per column, a full row
    /// for Up/Down), clamped at both ends of the grid - no wrap-around.
    /// </summary>
    public void MoveScreenshotSelection(MediaViewModel media, int delta)
    {
        if (media.Screenshots.Count == 0)
        {
            return;
        }

        SelectionHelper.MoveSelection(media.Screenshots, delta);
        ScrollMediaRequested?.Invoke();
    }

    /// <summary>
    /// Launches the game currently selected in the library carousel, or the given card.
    /// </summary>
    public void LaunchSelectedGame(MainWindowViewModel vm, GameCardViewModel? explicitCard = null)
    {
        GameCardViewModel? card = explicitCard ?? vm.Library.Games.FirstOrDefault(g => g.IsSelected);
        if (card != null)
        {
            _ = vm.LaunchGame(card);
        }
    }

    /// <summary>
    /// Opens the screen for the given option card, or quits for the Quit card.
    /// </summary>
    public void ActivateOption(MainWindowViewModel vm, OptionsCardViewModel option)
    {
        if (option.TargetScreen == OverlayScreen.None)
        {
            vm.Quit();
            return;
        }

        vm.OpenScreen(option.TargetScreen);
        OverlayFocusRequested?.Invoke();
        if (option.TargetScreen == OverlayScreen.Library)
        {
            ScrollLibraryRequested?.Invoke();
        }
        else if (option.TargetScreen == OverlayScreen.Media)
        {
            ScrollMediaRequested?.Invoke();
        }
    }

    /// <summary>
    /// Activates the currently selected option card (A on the option row).
    /// </summary>
    public void ActivateSelectedOption(MainWindowViewModel vm, DashboardViewModel dashboard)
    {
        OptionsCardViewModel? option = dashboard.Options.FirstOrDefault(o => o.IsSelected);
        if (option != null)
        {
            _lastActivationWasMouse = false;
            ActivateOption(vm, option);
        }
    }

    /// <summary>
    /// Launches the given game card, marking the activation as keyboard-driven
    /// so focus is restored when its overlay closes.
    /// </summary>
    public void ActivateGame(MainWindowViewModel vm, GameCardViewModel game)
    {
        _lastActivationWasMouse = false;
        LaunchSelectedGame(vm, game);
    }

    /// <summary>
    /// Opens the modal viewer for the currently selected screenshot (Enter in the gallery).
    /// </summary>
    public void OpenSelectedScreenshot(MediaViewModel media)
    {
        ScreenshotItemViewModel? selected = media.Screenshots.FirstOrDefault(s => s.IsSelected);
        if (selected != null)
        {
            media.OpenScreenshot(selected);
        }
    }

    /// <summary>
    /// Handles a mouse click on an option card. A mouse click must not leave the
    /// card focused/selected - only the controller (IsSelected via keyboard focus)
    /// or hover should show it.
    /// </summary>
    public void HandleOptionCardPressed(MainWindowViewModel vm, DashboardViewModel dashboard, OptionsCardViewModel option)
    {
        _lastActivationWasMouse = true;
        ActivateOption(vm, option);
        SelectionHelper.ClearSelection(dashboard.Options);
    }

    /// <summary>
    /// Restores focus to the previously selected option card after closing an overlay.
    /// Skipped when the overlay was opened with a mouse click - the card stays unfocused.
    /// </summary>
    public void RestoreOptionFocus(DashboardViewModel dashboard)
    {
        if (_lastActivationWasMouse)
        {
            return;
        }

        OptionsCardViewModel? selected = dashboard.Options.FirstOrDefault(o => o.IsSelected);
        if (selected == null)
        {
            return;
        }

        OptionFocusRequested?.Invoke(selected);
    }

    /// <summary>
    /// Updates the selection when a game card gains focus (controller/keyboard/mouse).
    /// </summary>
    public void OnGameCardFocused(DashboardViewModel dashboard, GameCardViewModel game)
    {
        IsOnOptionsRow = false;
        SelectionHelper.SelectOnly(dashboard.RecentGames, game);
    }

    /// <summary>
    /// Updates the selection when an option card gains focus (controller/keyboard/mouse).
    /// </summary>
    public void OnOptionCardFocused(DashboardViewModel dashboard, OptionsCardViewModel option)
    {
        IsOnOptionsRow = true;
        SelectionHelper.SelectOnly(dashboard.Options, option);
    }
}
