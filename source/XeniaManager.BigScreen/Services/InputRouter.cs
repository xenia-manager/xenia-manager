using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using XeniaManager.BigScreen.Models;
using XeniaManager.BigScreen.ViewModels.Items;
using XeniaManager.BigScreen.Views.Screens;
using XeniaManager.BigScreen.ViewModels.Shell;
using XeniaManager.Core.Models;

namespace XeniaManager.BigScreen.Services;

/// <summary>
/// Translates keyboard and gamepad input into dashboard navigation commands,
/// routing them through the navigation controller. One command set serves
/// both input sources; the active screen or modal decides what each command
/// does (settings takes its keyboard input on the native controls and its
/// gamepad input on the row navigation).
/// </summary>
public class InputRouter(DashboardNavigationController navigation, IModalService modalService)
{
    /// <summary>
    /// Closes the open overlay and restores the option-row focus.
    /// </summary>
    private void CloseOverlay(MainWindowViewModel vm)
    {
        vm.CloseOverlay();
        navigation.RestoreOptionFocus(vm.Dashboard);
    }

    /// <summary>
    /// Moves the active dashboard row by the given step. Right from the profile
    /// row drops into the game row on the next card (wrapping after the last).
    /// </summary>
    private void MoveDashboard(MainWindowViewModel vm, int delta)
    {
        if (navigation.IsOnOptionsRow)
        {
            DashboardNavigationController.MoveOptionSelection(vm.Dashboard, delta);
        }
        else if (navigation.IsOnProfileRow)
        {
            if (delta > 0)
            {
                navigation.AdvanceFromProfileRow(vm.Dashboard);
            }
        }
        else
        {
            DashboardNavigationController.MoveRecentGameSelection(vm.Dashboard, delta);
        }
    }

    /// <summary>
    /// Activates the focused/selected element: the profile row opens the profile
    /// picker, the option row activates its screen, game cards launch.
    /// </summary>
    private void Activate(MainWindowViewModel vm, GameCardViewModel? gameCard)
    {
        if (navigation.IsOnProfileRow)
        {
            DashboardNavigationController.ActivateProfileRow(vm);
        }
        else if (navigation.IsOnOptionsRow)
        {
            navigation.ActivateSelectedOption(vm, vm.Dashboard);
        }
        else if (gameCard != null)
        {
            navigation.ActivateGame(vm, gameCard);
        }
    }

    /// <summary>
    /// Moves up a dashboard row: game row → profile row; option row → game row,
    /// or straight to the profile row when the library is empty.
    /// </summary>
    private void MoveUp(MainWindowViewModel vm)
    {
        if (!navigation.IsOnOptionsRow || vm.Dashboard.ShowEmptyStub)
        {
            navigation.SelectProfileRow(vm.Dashboard);
            return;
        }

        navigation.SelectGameRow(vm.Dashboard);
    }

    /// <summary>
    /// Moves down a dashboard row: profile row → game row (option row when the
    /// library is empty); game/option rows → option row.
    /// </summary>
    private void MoveDown(MainWindowViewModel vm)
    {
        if (navigation.IsOnProfileRow && !vm.Dashboard.ShowEmptyStub)
        {
            navigation.SelectGameRow(vm.Dashboard);
            return;
        }

        navigation.SelectOptionRow(vm.Dashboard);
    }

    /// <summary>
    /// Commands while the settings screen is open. Back closes the screen
    /// after any open row editor; the remaining commands reach here from the
    /// gamepad only and drive the row navigation.
    /// </summary>
    private void HandleSettings(MainWindowViewModel vm, NavigationCommand command, bool fromGamepad)
    {
        if (command == NavigationCommand.Back)
        {
            if (!vm.Settings.HandleBack())
            {
                CloseOverlay(vm);
            }

            return;
        }

        if (fromGamepad)
        {
            vm.Settings.HandleInput(command);
        }
    }

    /// <summary>
    /// Opens the game modal for the game currently selected in the library.
    /// </summary>
    private static void OpenGameDetailsForSelected(MainWindowViewModel vm)
    {
        GameCardViewModel? card = vm.Library.Games.FirstOrDefault(g => g.IsSelected);
        if (card != null)
        {
            vm.OpenGameModal(card.Game);
        }
    }

    /// <summary>
    /// Commands while the library screen is open.
    /// </summary>
    private void HandleLibrary(MainWindowViewModel vm, NavigationCommand command)
    {
        switch (command)
        {
            case NavigationCommand.MoveLeft:
            case NavigationCommand.MoveUp:
                navigation.MoveGameSelection(vm.Library, -1);
                break;
            case NavigationCommand.MoveRight:
            case NavigationCommand.MoveDown:
                navigation.MoveGameSelection(vm.Library, 1);
                break;
            case NavigationCommand.CycleSort:
                vm.Library.CycleSort();
                break;
            case NavigationCommand.Activate:
                DashboardNavigationController.LaunchSelectedGame(vm);
                break;
            case NavigationCommand.Back:
                CloseOverlay(vm);
                break;
            case NavigationCommand.ToggleView:
                vm.Library.ToggleView();
                break;
            case NavigationCommand.Details:
                OpenGameDetailsForSelected(vm);
                break;
        }
    }

    /// <summary>
    /// Opens the game modal for the given card, or the currently selected
    /// dashboard card when none was passed.
    /// </summary>
    private static void OpenGameDetails(MainWindowViewModel vm, GameCardViewModel? gameCard)
    {
        gameCard ??= vm.Dashboard.RecentGames.FirstOrDefault(g => g.IsSelected);
        if (gameCard != null)
        {
            vm.OpenGameModal(gameCard.Game);
        }
    }

    /// <summary>
    /// Commands while the Gallery screen is open.
    /// </summary>
    private void HandleGallery(MainWindowViewModel vm, NavigationCommand command)
    {
        switch (command)
        {
            case NavigationCommand.MoveLeft:
                navigation.MoveScreenshotSelection(vm.Gallery, -1);
                break;
            case NavigationCommand.MoveRight:
                navigation.MoveScreenshotSelection(vm.Gallery, 1);
                break;
            case NavigationCommand.MoveUp:
                navigation.MoveScreenshotSelection(vm.Gallery, -GalleryView.CardsPerRow);
                break;
            case NavigationCommand.MoveDown:
                navigation.MoveScreenshotSelection(vm.Gallery, GalleryView.CardsPerRow);
                break;
            case NavigationCommand.CycleSort:
                vm.Gallery.CycleGallerySort();
                break;
            case NavigationCommand.Activate:
                DashboardNavigationController.OpenSelectedScreenshot(vm.Gallery);
                break;
            case NavigationCommand.Back:
                CloseOverlay(vm);
                break;
        }
    }

    /// <summary>
    /// Commands while an overlay screen is open. Library and Gallery respond to
    /// both input sources; Settings only takes Back from the keyboard - its
    /// native controls keep the keyboard interaction, while the gamepad drives
    /// the row navigation.
    /// </summary>
    private void HandleOverlay(MainWindowViewModel vm, NavigationCommand command, bool fromGamepad)
    {
        if (vm.IsLibraryScreen)
        {
            HandleLibrary(vm, command);
        }
        else if (vm.IsGalleryScreen)
        {
            HandleGallery(vm, command);
        }
        else if (vm.IsSettingsScreen)
        {
            HandleSettings(vm, command, fromGamepad);
        }
    }

    /// <summary>
    /// Commands while the dashboard is showing: row movement, row switching
    /// and activation.
    /// </summary>
    private void HandleDashboard(MainWindowViewModel vm, NavigationCommand command, GameCardViewModel? gameCard)
    {
        switch (command)
        {
            case NavigationCommand.MoveLeft:
                MoveDashboard(vm, -1);
                break;
            case NavigationCommand.MoveRight:
                MoveDashboard(vm, 1);
                break;
            case NavigationCommand.MoveUp:
                MoveUp(vm);
                break;
            case NavigationCommand.MoveDown:
                MoveDown(vm);
                break;
            case NavigationCommand.Activate:
                Activate(vm, gameCard);
                break;
            case NavigationCommand.Details:
                OpenGameDetails(vm, gameCard);
                break;
        }
    }

    /// <summary>
    /// Routes a command to the handler for the active layer: the modal stack
    /// (top modal), an overlay screen, or the dashboard. The source only matters
    /// for the settings screen, where keyboard and gamepad diverge.
    /// </summary>
    private void Dispatch(MainWindowViewModel vm, NavigationCommand command, GameCardViewModel? gameCard,
        bool fromGamepad)
    {
        if (modalService.Top is { } modal)
        {
            modal.HandleInput(command);
        }
        else if (vm.IsOverlayOpen)
        {
            HandleOverlay(vm, command, fromGamepad);
        }
        else
        {
            HandleDashboard(vm, command, gameCard);
        }
    }

    /// <summary>
    /// Maps a keyboard key to a command, or null when it isn't navigation-relevant.
    /// </summary>
    private static NavigationCommand? ToCommand(Key key)
    {
        return key switch
        {
            Key.Left => NavigationCommand.MoveLeft,
            Key.Right => NavigationCommand.MoveRight,
            Key.Up => NavigationCommand.MoveUp,
            Key.Down => NavigationCommand.MoveDown,
            Key.Enter or Key.Space => NavigationCommand.Activate,
            Key.B or Key.Escape => NavigationCommand.Back,
            Key.X => NavigationCommand.CycleSort,
            Key.Y => NavigationCommand.Details,
            Key.V => NavigationCommand.ToggleView,
            _ => null
        };
    }

    /// <summary>
    /// Maps a gamepad button to a command, or null when it isn't navigation-relevant.
    /// </summary>
    private static NavigationCommand? ToCommand(GamepadButton button)
    {
        return button switch
        {
            GamepadButton.DpadLeft or GamepadButton.LeftShoulder => NavigationCommand.MoveLeft,
            GamepadButton.DpadRight or GamepadButton.RightShoulder => NavigationCommand.MoveRight,
            GamepadButton.DpadUp => NavigationCommand.MoveUp,
            GamepadButton.DpadDown => NavigationCommand.MoveDown,
            GamepadButton.A => NavigationCommand.Activate,
            GamepadButton.B => NavigationCommand.Back,
            GamepadButton.X => NavigationCommand.CycleSort,
            GamepadButton.Y => NavigationCommand.Details,
            GamepadButton.View => NavigationCommand.ToggleView,
            GamepadButton.Start => NavigationCommand.Start,
            _ => null
        };
    }

    /// <summary>
    /// Whether the focused element is a text-entry control (a <see cref="TextBox"/>
    /// or anything containing one, e.g. AutoCompleteBox's inner field). While it is,
    /// typed keys must reach the field, so navigation routing is skipped.
    /// </summary>
    private static bool IsTypingInTextEntry(IFocusManager? focusManager)
    {
        return focusManager?.GetFocusedElement() is Control focus
               && focus.GetSelfAndVisualAncestors().OfType<TextBox>().Any();
    }

    /// <summary>
    /// Routes a keyboard key to the matching navigation command.
    /// </summary>
    public void HandleKey(MainWindowViewModel vm, KeyEventArgs e, IFocusManager? focusManager)
    {
        if (IsTypingInTextEntry(focusManager) && e.Key != Key.Escape)
        {
            return;
        }

        NavigationCommand? command = ToCommand(e.Key);
        if (command == null)
        {
            return;
        }

        GameCardViewModel? gameCard =
            (focusManager?.GetFocusedElement() as Control)?.DataContext is GameCardViewModel card ? card : null;
        Dispatch(vm, command.Value, gameCard, false);
        e.Handled = true;
    }

    /// <summary>
    /// Routes a gamepad button press to the matching navigation command.
    /// The caller gates input while the window is disabled (game running).
    /// </summary>
    public void HandleGamepad(MainWindowViewModel vm, GamepadButton button)
    {
        NavigationCommand? command = ToCommand(button);
        if (command == null)
        {
            return;
        }

        GameCardViewModel? gameCard = !vm.IsOverlayOpen && !navigation.IsOnOptionsRow
            ? vm.Dashboard.RecentGames.FirstOrDefault(g => g.IsSelected)
            : null;
        Dispatch(vm, command.Value, gameCard, true);
    }
}