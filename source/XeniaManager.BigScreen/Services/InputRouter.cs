using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using XeniaManager.BigScreen.ViewModels;
using XeniaManager.BigScreen.ViewModels.Items;
using XeniaManager.BigScreen.Views;
using XeniaManager.Core.Models;

namespace XeniaManager.BigScreen.Services;

/// <summary>
/// Translates keyboard and gamepad input into dashboard navigation commands,
/// routing them through the navigation controller. One command set serves
/// both input sources; the active screen decides what each command does
/// (settings stays keyboard-only for its controls).
/// </summary>
public class InputRouter(DashboardNavigationController navigation)
{
    /// <summary>
    /// The navigation-relevant actions produced by either input source.
    /// </summary>
    private enum Command
    {
        MoveLeft,
        MoveRight,
        MoveUp,
        MoveDown,
        Activate,
        Back,
        CycleSort,
    }

    /// <summary>
    /// Closes the open overlay and restores the option-row focus.
    /// </summary>
    private void CloseOverlay(MainWindowViewModel vm)
    {
        vm.CloseOverlay();
        navigation.RestoreOptionFocus(vm.Dashboard);
    }

    /// <summary>
    /// Moves the active dashboard row by the given step.
    /// </summary>
    private void MoveDashboard(MainWindowViewModel vm, int delta)
    {
        if (navigation.IsOnOptionsRow)
        {
            navigation.MoveOptionSelection(vm.Dashboard, delta);
        }
        else
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
    private static void HandleViewer(MainWindowViewModel vm, Command command)
    {
        switch (command)
        {
            case Command.MoveLeft:
                vm.Media.Viewer!.Step(-1);
                break;
            case Command.MoveRight:
                vm.Media.Viewer!.Step(1);
                break;
            case Command.Back:
                vm.Media.CloseMediaViewer();
                break;
        }
    }

    /// <summary>
    /// Commands while an overlay screen is open. Settings only responds to Back
    /// (its controls stay keyboard/mouse-driven).
    /// </summary>
    private void HandleOverlay(MainWindowViewModel vm, Command command)
    {
        if (vm.IsLibraryScreen)
        {
            switch (command)
            {
                case Command.MoveLeft:
                    navigation.MoveGameSelection(vm.Library, -1);
                    break;
                case Command.MoveRight:
                    navigation.MoveGameSelection(vm.Library, 1);
                    break;
                case Command.CycleSort:
                    // Sort keeps the selection on the same card, but the viewport stays put
                    vm.Library.CycleSort();
                    break;
                case Command.Activate:
                    navigation.LaunchSelectedGame(vm);
                    break;
                case Command.Back:
                    CloseOverlay(vm);
                    break;
            }
        }
        else if (vm.IsMediaScreen)
        {
            switch (command)
            {
                case Command.MoveLeft:
                    navigation.MoveScreenshotSelection(vm.Media, -1);
                    break;
                case Command.MoveRight:
                    navigation.MoveScreenshotSelection(vm.Media, 1);
                    break;
                case Command.MoveUp:
                    navigation.MoveScreenshotSelection(vm.Media, -MediaView.CardsPerRow);
                    break;
                case Command.MoveDown:
                    navigation.MoveScreenshotSelection(vm.Media, MediaView.CardsPerRow);
                    break;
                case Command.CycleSort:
                    // Sort keeps the selection on the same card, but the viewport stays put
                    vm.Media.CycleMediaSort();
                    break;
                case Command.Activate:
                    navigation.OpenSelectedScreenshot(vm.Media);
                    break;
                case Command.Back:
                    CloseOverlay(vm);
                    break;
            }
        }
        else if (vm.IsSettingsScreen && command == Command.Back)
        {
            CloseOverlay(vm);
        }
    }

    /// <summary>
    /// Commands while the dashboard is showing: row movement, row switching
    /// and activation.
    /// </summary>
    private void HandleDashboard(MainWindowViewModel vm, Command command, GameCardViewModel? gameCard)
    {
        switch (command)
        {
            case Command.MoveLeft:
                MoveDashboard(vm, -1);
                break;
            case Command.MoveRight:
                MoveDashboard(vm, 1);
                break;
            case Command.MoveUp:
                navigation.SelectGameRow(vm.Dashboard);
                break;
            case Command.MoveDown:
                navigation.SelectOptionRow(vm.Dashboard);
                break;
            case Command.Activate:
                Activate(vm, gameCard);
                break;
        }
    }

    /// <summary>
    /// Routes a command to the handler for the active screen
    /// (screenshot viewer, overlay screen, dashboard).
    /// </summary>
    private void Dispatch(MainWindowViewModel vm, Command command, GameCardViewModel? gameCard = null)
    {
        if (vm is { IsMediaScreen: true, IsMediaViewerOpen: true })
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
    private static Command? ToCommand(Key key)
    {
        return key switch
        {
            Key.Left => Command.MoveLeft,
            Key.Right => Command.MoveRight,
            Key.Up => Command.MoveUp,
            Key.Down => Command.MoveDown,
            Key.Enter or Key.Space => Command.Activate,
            Key.B or Key.Escape => Command.Back,
            Key.Y => Command.CycleSort,
            _ => null,
        };
    }

    /// <summary>
    /// Maps a gamepad button to a command, or null when it isn't navigation-relevant.
    /// </summary>
    private static Command? ToCommand(GamepadButton button)
    {
        return button switch
        {
            GamepadButton.DpadLeft or GamepadButton.LeftShoulder => Command.MoveLeft,
            GamepadButton.DpadRight or GamepadButton.RightShoulder => Command.MoveRight,
            GamepadButton.DpadUp => Command.MoveUp,
            GamepadButton.DpadDown => Command.MoveDown,
            GamepadButton.A => Command.Activate,
            GamepadButton.B => Command.Back,
            GamepadButton.Y => Command.CycleSort,
            _ => null,
        };
    }

    /// <summary>
    /// Routes a keyboard key to the matching navigation command.
    /// </summary>
    public void HandleKey(MainWindowViewModel vm, KeyEventArgs e, IFocusManager? focusManager)
    {
        Command? command = ToCommand(e.Key);
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
        Command? command = ToCommand(button);
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
