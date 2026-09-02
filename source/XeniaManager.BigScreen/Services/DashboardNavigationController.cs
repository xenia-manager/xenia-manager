using System;
using System.Linq;
using XeniaManager.BigScreen.Models;
using XeniaManager.BigScreen.Utilities;
using XeniaManager.BigScreen.ViewModels.Items;
using XeniaManager.BigScreen.ViewModels.Dashboard;
using XeniaManager.BigScreen.ViewModels.Screens;
using XeniaManager.BigScreen.ViewModels.Shell;
using XeniaManager.Logging;

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
    /// Whether the dashboard's active row is the header profile row. It holds a
    /// single element (the avatar chip), so movement is only vertical.
    /// </summary>
    public bool IsOnProfileRow { get; set; }

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
    /// Raised when the gallery grid should scroll to its selected card.
    /// </summary>
    public event Action? ScrollGalleryRequested;

    /// <summary>
    /// Raised when an overlay just opened and focus should move into it.
    /// </summary>
    public event Action? OverlayFocusRequested;

    /// <summary>
    /// Raised to request focus on the header profile button.
    /// </summary>
    public event Action? ProfileFocusRequested;

    /// <summary>
    /// Column mapping from a game card index to the option card underneath it
    /// (games 1-2 → option 1, games 3-4 → option 2, games 5-6 → option 3, games 7-8 → option 4).
    /// </summary>
    private static readonly int[] GameToOptionColumn = [0, 0, 1, 1, 2, 2, 3, 3];

    /// <summary>
    /// Column mapping from an option card index to the first game card of its
    /// group (option 1 → game 1, option 2 → game 3, option 3 → game 5, option 4 → game 7).
    /// </summary>
    private static readonly int[] OptionToGameColumn = [0, 2, 4, 6];

    private int _lastSelectedGameIndex = 0;

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
        IsOnProfileRow = false;

        int gameIndex = SelectionHelper.IndexOfSelected(dashboard.RecentGames);
        int mapped = GameToOptionColumn[Math.Clamp(gameIndex, 0, GameToOptionColumn.Length - 1)];
        int target = Math.Clamp(mapped, 0, dashboard.Options.Count - 1);
        _lastSelectedGameIndex = gameIndex;
        SelectionHelper.SelectOnlyAt(dashboard.Options, target);
        SelectionHelper.ClearSelection(dashboard.RecentGames);
        dashboard.IsGameRowFocused = false;

        OptionFocusRequested?.Invoke(dashboard.Options[target]);
        Logger.Debug<DashboardNavigationController>("Switched to option row");
    }

    /// <summary>
    /// Switches the dashboard to the game row. Coming from the profile row it
    /// returns to the card that was selected before the header took focus (the
    /// game row keeps its selection while the avatar row is active); coming
    /// from the option row it selects the first game card of the current
    /// option's column group. When the library is empty there is no game row,
    /// so the option row stays active.
    /// </summary>
    public void SelectGameRow(DashboardViewModel dashboard)
    {
        if (dashboard.RecentGames.Count == 0)
        {
            IsOnOptionsRow = true;
            IsOnProfileRow = false;
            return;
        }

        bool fromProfileRow = IsOnProfileRow;
        IsOnOptionsRow = false;
        IsOnProfileRow = false;

        int optionIndex = SelectionHelper.IndexOfSelected(dashboard.Options);
        int mapped = fromProfileRow
            ? _lastSelectedGameIndex
            : OptionToGameColumn[Math.Clamp(optionIndex, 0, OptionToGameColumn.Length - 1)];
        int target = Math.Clamp(mapped, 0, dashboard.RecentGames.Count - 1);
        SelectionHelper.SelectOnlyAt(dashboard.RecentGames, target);
        SelectionHelper.ClearSelection(dashboard.Options);
        dashboard.IsGameRowFocused = true;

        GameFocusRequested?.Invoke(dashboard.RecentGames[target]);
        Logger.Debug<DashboardNavigationController>("Switched to game row");
    }

    /// <summary>
    /// Switches the dashboard to the header profile row (a single element).
    /// The game row keeps its selection - a game card must always stay selected
    /// on the dashboard, so only the header's own state flips.
    /// </summary>
    public void SelectProfileRow(DashboardViewModel dashboard)
    {
        IsOnProfileRow = true;
        IsOnOptionsRow = false;
        dashboard.IsGameRowFocused = false;

        int gameIndex = SelectionHelper.IndexOfSelected(dashboard.RecentGames);
        if (gameIndex >= 0)
        {
            _lastSelectedGameIndex = gameIndex;
        }

        SelectionHelper.ClearSelection(dashboard.RecentGames);

        ProfileFocusRequested?.Invoke();
        Logger.Debug<DashboardNavigationController>("Switched to profile row");
    }

    /// <summary>
    /// Moves the option row selection by the given step, clamped at both ends.
    /// </summary>
    public static void MoveOptionSelection(DashboardViewModel dashboard, int delta)
    {
        if (dashboard.Options.Count == 0)
        {
            return;
        }

        SelectionHelper.MoveSelection(dashboard.Options, delta);
        Logger.Trace<DashboardNavigationController>($"Moved option selection by {delta}");
    }

    /// <summary>
    /// Moves the dashboard game selection by the given step, clamped at both ends.
    /// </summary>
    public static void MoveRecentGameSelection(DashboardViewModel dashboard, int delta)
    {
        if (dashboard.RecentGames.Count == 0)
        {
            return;
        }

        SelectionHelper.MoveSelection(dashboard.RecentGames, delta);
        Logger.Trace<DashboardNavigationController>($"Moved recent game selection by {delta}");
    }

    /// <summary>
    /// Coming from the profile row with Right: drops into the game row on the
    /// card after the currently selected one, wrapping from the last card back
    /// to the first. With no games the option row stays active.
    /// </summary>
    public void AdvanceFromProfileRow(DashboardViewModel dashboard)
    {
        if (dashboard.RecentGames.Count == 0)
        {
            SelectOptionRow(dashboard);
            return;
        }

        IsOnProfileRow = false;
        IsOnOptionsRow = false;

        int target = (_lastSelectedGameIndex + 1) % dashboard.RecentGames.Count;
        SelectionHelper.SelectOnlyAt(dashboard.RecentGames, target);
        SelectionHelper.ClearSelection(dashboard.Options);
        dashboard.IsGameRowFocused = true;

        GameFocusRequested?.Invoke(dashboard.RecentGames[target]);
        Logger.Debug<DashboardNavigationController>($"Advanced from profile row to game card {target + 1}");
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
        Logger.Trace<DashboardNavigationController>($"Moved library selection by {delta}");
    }

    /// <summary>
    /// Moves the screenshot selection by the given step (1 per column, a full row
    /// for Up/Down), clamped at both ends of the grid - no wrap-around.
    /// </summary>
    public void MoveScreenshotSelection(GalleryViewModel gallery, int delta)
    {
        if (gallery.Screenshots.Count == 0)
        {
            return;
        }

        SelectionHelper.MoveSelection(gallery.Screenshots, delta);
        ScrollGalleryRequested?.Invoke();
        Logger.Trace<DashboardNavigationController>($"Moved gallery selection by {delta}");
    }

    /// <summary>
    /// Launches the game currently selected in the library carousel, or the given card.
    /// </summary>
    public static void LaunchSelectedGame(MainWindowViewModel vm, GameCardViewModel? explicitCard = null)
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
        Logger.Info<DashboardNavigationController>($"Activating option '{option.Title}'");
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
        else if (option.TargetScreen == OverlayScreen.Gallery)
        {
            ScrollGalleryRequested?.Invoke();
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
    public static void OpenSelectedScreenshot(GalleryViewModel gallery)
    {
        ScreenshotItemViewModel? selected = gallery.Screenshots.FirstOrDefault(s => s.IsSelected);
        if (selected != null)
        {
            gallery.OpenScreenshot(selected);
        }
    }

    /// <summary>
    /// Activates the header profile row (A on the profile row): opens the profile picker.
    /// </summary>
    public static void ActivateProfileRow(MainWindowViewModel vm)
    {
        Logger.Info<DashboardNavigationController>("Activating profile row");
        vm.OpenProfilePicker();
    }

    /// <summary>
    /// Handles a mouse click on an option card. A mouse click must not leave the
    /// card focused/selected - only the controller (IsSelected via keyboard focus)
    /// or hover should show it.
    /// </summary>
    public void HandleOptionCardPressed(MainWindowViewModel vm, DashboardViewModel dashboard,
        OptionsCardViewModel option)
    {
        _lastActivationWasMouse = true;
        ActivateOption(vm, option);
        SelectionHelper.ClearSelection(dashboard.Options);
        dashboard.IsGameRowFocused = false;
        Logger.Debug<DashboardNavigationController>($"Option card clicked: '{option.Title}'");
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
        IsOnProfileRow = false;
        dashboard.IsGameRowFocused = true;
        SelectionHelper.SelectOnly(dashboard.RecentGames, game);
    }

    /// <summary>
    /// Updates the selection when an option card gains focus (controller/keyboard/mouse).
    /// </summary>
    public void OnOptionCardFocused(DashboardViewModel dashboard, OptionsCardViewModel option)
    {
        IsOnOptionsRow = true;
        IsOnProfileRow = false;
        dashboard.IsGameRowFocused = false;
        SelectionHelper.SelectOnly(dashboard.Options, option);
    }
}