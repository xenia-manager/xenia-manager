using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using XeniaManager.BigScreen.Models;
using XeniaManager.BigScreen.ViewModels;
using XeniaManager.BigScreen.ViewModels.Items;
using XeniaManager.BigScreen.Views;
using XeniaManager.Core.Models;

namespace XeniaManager.BigScreen.Services;

/// <summary>
/// Translates keyboard and gamepad input into dashboard navigation commands,
/// routing them through the navigation controller. One command set serves
/// both input sources; the active screen or modal decides what each command
/// does (settings stays keyboard-only for its controls).
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
    /// Moves the active dashboard row by the given step. The profile row has a
    /// single element, so horizontal movement there is a no-op.
    /// </summary>
    private void MoveDashboard(MainWindowViewModel vm, int delta)
    {
        if (navigation.IsOnOptionsRow)
        {
            navigation.MoveOptionSelection(vm.Dashboard, delta);
        }
        else if (!navigation.IsOnProfileRow)
        {
            navigation.MoveRecentGameSelection(vm.Dashboard, delta);
        }
    }

    /// <summary>
    /// Activates the selected option card, or launches the given game card.
    /// </summary>
    private void Activate(MainWindowViewModel vm, GameCardViewModel? gameCard)
    {
        if (navigation.IsOnOptionsRow)
        {
            navigation.ActivateSelectedOption(vm, vm.Dashboard);
        }
        else if (gameCard != null)
        {
            navigation.ActivateGame(vm, gameCard);
        }
    }

    /// <summary>
    /// Commands while the screenshot viewer is open: step or close.
    /// </summary>
    private static void HandleViewer(MainWindowViewModel vm, NavigationCommand command)
    {
        switch (command)
        {
            case NavigationCommand.MoveLeft:
                vm.Media.Viewer!.Step(-1);
                break;
            case NavigationCommand.MoveRight:
                vm.Media.Viewer!.Step(1);
                break;
            case NavigationCommand.Back:
                vm.Media.CloseMediaViewer();
                break;
        }
    }

    /// <summary>
    /// Commands while an overlay screen is open. Settings only responds to Back
    /// (its controls stay keyboard/mouse-driven).
    /// </summary>
    private void HandleOverlay(MainWindowViewModel vm, NavigationCommand command)
    {
        if (vm.IsLibraryScreen)
        {
            switch (command)
            {
                case NavigationCommand.MoveLeft:
                    navigation.MoveGameSelection(vm.Library, -1);
                    break;
                case NavigationCommand.MoveRight:
                    navigation.MoveGameSelection(vm.Library, 1);
                    break;
                case NavigationCommand.MoveUp:
                    navigation.MoveGameSelection(vm.Library, -1);
                    break;
                case NavigationCommand.MoveDown:
                    navigation.MoveGameSelection(vm.Library, 1);
                    break;
                case NavigationCommand.CycleSort:
                    // Sort keeps the selection on the same card, but the viewport stays put
                    vm.Library.CycleSort();
                    break;
                case NavigationCommand.Activate:
                    navigation.LaunchSelectedGame(vm);
                    break;
                case NavigationCommand.Back:
                    CloseOverlay(vm);
                    break;
                case NavigationCommand.ToggleView:
                    vm.Library.ToggleView();
                    break;
            }
        }
        else if (vm.IsMediaScreen)
        {
            switch (command)
            {
                case NavigationCommand.MoveLeft:
                    navigation.MoveScreenshotSelection(vm.Media, -1);
                    break;
                case NavigationCommand.MoveRight:
                    navigation.MoveScreenshotSelection(vm.Media, 1);
                    break;
                case NavigationCommand.MoveUp:
                    navigation.MoveScreenshotSelection(vm.Media, -MediaView.CardsPerRow);
                    break;
                case NavigationCommand.MoveDown:
                    navigation.MoveScreenshotSelection(vm.Media, MediaView.CardsPerRow);
                    break;
                case NavigationCommand.CycleSort:
                    // Sort keeps the selection on the same card, but the viewport stays put
                    vm.Media.CycleMediaSort();
                    break;
                case NavigationCommand.Activate:
                    navigation.OpenSelectedScreenshot(vm.Media);
                    break;
                case NavigationCommand.Back:
                    CloseOverlay(vm);
                    break;
            }
        }
        else if (vm.IsSettingsScreen && command == NavigationCommand.Back)
        {
            CloseOverlay(vm);
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
                // With no games there is no game row - hop straight to the profile row
                if (navigation.IsOnOptionsRow)
                {
                    if (vm.Dashboard.RecentGames.Count > 0)
                    {
                        navigation.SelectGameRow(vm.Dashboard);
                    }
                    else
                    {
                        navigation.SelectProfileRow(vm.Dashboard);
                    }
                }
                else
                {
                    navigation.SelectProfileRow(vm.Dashboard);
                }

                break;
            case NavigationCommand.MoveDown:
                if (navigation.IsOnProfileRow)
                {
                    if (vm.Dashboard.RecentGames.Count > 0)
                    {
                        navigation.SelectGameRow(vm.Dashboard);
                    }
                    else
                    {
                        navigation.SelectOptionRow(vm.Dashboard);
                    }
                }
                else
                {
                    navigation.SelectOptionRow(vm.Dashboard);
                }

                break;
            case NavigationCommand.Activate:
                if (navigation.IsOnProfileRow)
                {
                    navigation.ActivateProfileRow(vm);
                }
                else
                {
                    Activate(vm, gameCard);
                }

                break;
        }
    }

    /// <summary>
    /// Routes a command to the handler for the active layer: the modal stack
    /// (top modal), the screenshot viewer, an overlay screen, or the dashboard.
    /// </summary>
    private void Dispatch(MainWindowViewModel vm, NavigationCommand command, GameCardViewModel? gameCard = null)
    {
        if (modalService.Top is { } modal)
        {
            modal.HandleInput(command);
        }
        else if (vm is { IsMediaScreen: true, IsMediaViewerOpen: true })
        {
            HandleViewer(vm, command);
        }
        else if (vm.IsOverlayOpen)
        {
            HandleOverlay(vm, command);
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
            _ => null,
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
            _ => null,
        };
    }

    /// <summary>
    /// Routes a keyboard key to the matching navigation command.
    /// </summary>
    public void HandleKey(MainWindowViewModel vm, KeyEventArgs e, IFocusManager? focusManager)
    {
        NavigationCommand? command = ToCommand(e.Key);
        if (command == null)
        {
            return;
        }

        // Keyboard activation acts on the focused game card
        GameCardViewModel? gameCard =
            (focusManager?.GetFocusedElement() as Control)?.DataContext is GameCardViewModel card ? card : null;
        Dispatch(vm, command.Value, gameCard);
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

        // Gamepad activation acts on the selected dashboard game card
        GameCardViewModel? gameCard = !vm.IsOverlayOpen && !navigation.IsOnOptionsRow
            ? vm.Dashboard.RecentGames.FirstOrDefault(g => g.IsSelected)
            : null;
        Dispatch(vm, command.Value, gameCard);
    }
}