using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using XeniaManager.BigScreen.Models;
using XeniaManager.BigScreen.ViewModels;
using XeniaManager.BigScreen.ViewModels.Items;
using XeniaManager.BigScreen.Views;

namespace XeniaManager.BigScreen.Services;

/// <summary>
/// Translates keyboard and gamepad input into dashboard navigation actions,
/// routing them through the navigation controller. One state machine serves
/// both input sources (settings stays keyboard-only).
/// </summary>
public class InputRouter
{
    private readonly DashboardNavigationController _navigation;

    public InputRouter(DashboardNavigationController navigation)
    {
        _navigation = navigation;
    }

    /// <summary>
    /// Routes a keyboard key to the matching navigation action.
    /// </summary>
    public void HandleKey(MainWindowViewModel vm, KeyEventArgs e, IFocusManager? focusManager)
    {
        if (vm.IsOverlayOpen)
        {
            if (vm.IsMediaScreen && vm.IsMediaViewerOpen)
            {
                if (e.Key is Key.Left or Key.Right)
                {
                    vm.Media.Viewer!.Step(e.Key == Key.Right ? 1 : -1);
                    e.Handled = true;
                }
                else if (e.Key is Key.B or Key.Escape)
                {
                    vm.Media.CloseMediaViewer();
                    e.Handled = true;
                }

                return;
            }

            if (e.Key == Key.B || e.Key == Key.Escape)
            {
                vm.CloseOverlay();
                _navigation.RestoreOptionFocus(vm.Dashboard);
                e.Handled = true;
            }
            else if (vm.IsLibraryScreen && e.Key is Key.Left or Key.Right)
            {
                _navigation.MoveGameSelection(vm.Library, e.Key == Key.Right ? 1 : -1);
                e.Handled = true;
            }
            else if (vm.IsLibraryScreen && e.Key == Key.Y)
            {
                // Sort keeps the selection on the same card, but the viewport stays put
                vm.Library.CycleSort();
                e.Handled = true;
            }
            else if (vm.IsLibraryScreen && e.Key is Key.A or Key.Enter or Key.Space)
            {
                _navigation.LaunchSelectedGame(vm);
                e.Handled = true;
            }
            else if (vm.IsMediaScreen && e.Key is Key.Left or Key.Right)
            {
                _navigation.MoveScreenshotSelection(vm.Media, e.Key == Key.Right ? 1 : -1);
                e.Handled = true;
            }
            else if (vm.IsMediaScreen && e.Key is Key.Up or Key.Down)
            {
                _navigation.MoveScreenshotSelection(vm.Media, e.Key == Key.Down ? MediaView.CardsPerRow : -MediaView.CardsPerRow);
                e.Handled = true;
            }
            else if (vm.IsMediaScreen && e.Key == Key.Y)
            {
                // Sort keeps the selection on the same card, but the viewport stays put
                vm.Media.CycleMediaSort();
                e.Handled = true;
            }
            else if (vm.IsMediaScreen && e.Key is Key.Enter or Key.Space)
            {
                _navigation.OpenSelectedScreenshot(vm.Media);
                e.Handled = true;
            }

            return;
        }

        if (e.Key is Key.Enter or Key.Space)
        {
            if (_navigation.IsOnOptionsRow)
            {
                _navigation.ActivateSelectedOption(vm, vm.Dashboard);
                e.Handled = true;
            }
            else if (focusManager?.GetFocusedElement() is Control { DataContext: GameCardViewModel game })
            {
                _navigation.ActivateGame(vm, game);
                e.Handled = true;
            }
        }
        else if (e.Key is Key.Left or Key.Right)
        {
            if (_navigation.IsOnOptionsRow)
            {
                _navigation.MoveOptionSelection(vm.Dashboard, e.Key == Key.Right ? 1 : -1);
            }
            else
            {
                _navigation.MoveRecentGameSelection(vm.Dashboard, e.Key == Key.Right ? 1 : -1);
            }

            e.Handled = true;
        }
        else if (e.Key is Key.Up or Key.Down)
        {
            if (e.Key == Key.Down)
            {
                _navigation.SelectOptionRow(vm.Dashboard);
            }
            else
            {
                _navigation.SelectGameRow(vm.Dashboard);
            }

            e.Handled = true;
        }
    }

    /// <summary>
    /// Routes a gamepad button press to the matching navigation action. Settings
    /// is keyboard-only; the caller gates input while the window is disabled.
    /// </summary>
    public void HandleGamepad(MainWindowViewModel vm, GamepadButton button)
    {
        // Modal viewer takes priority within the media screen
        if (vm.IsMediaScreen && vm.IsMediaViewerOpen)
        {
            switch (button)
            {
                case GamepadButton.DpadLeft:
                case GamepadButton.LeftShoulder:
                    vm.Media.Viewer!.Step(-1);
                    break;
                case GamepadButton.DpadRight:
                case GamepadButton.RightShoulder:
                    vm.Media.Viewer!.Step(1);
                    break;
                case GamepadButton.B:
                    vm.Media.CloseMediaViewer();
                    break;
            }

            return;
        }

        if (vm.IsOverlayOpen)
        {
            switch (button)
            {
                case GamepadButton.DpadLeft:
                case GamepadButton.LeftShoulder:
                    if (vm.IsLibraryScreen)
                    {
                        _navigation.MoveGameSelection(vm.Library, -1);
                    }
                    else if (vm.IsMediaScreen)
                    {
                        _navigation.MoveScreenshotSelection(vm.Media, -1);
                    }

                    break;
                case GamepadButton.DpadRight:
                case GamepadButton.RightShoulder:
                    if (vm.IsLibraryScreen)
                    {
                        _navigation.MoveGameSelection(vm.Library, 1);
                    }
                    else if (vm.IsMediaScreen)
                    {
                        _navigation.MoveScreenshotSelection(vm.Media, 1);
                    }

                    break;
                case GamepadButton.DpadUp:
                    if (vm.IsMediaScreen)
                    {
                        _navigation.MoveScreenshotSelection(vm.Media, -MediaView.CardsPerRow);
                    }

                    break;
                case GamepadButton.DpadDown:
                    if (vm.IsMediaScreen)
                    {
                        _navigation.MoveScreenshotSelection(vm.Media, MediaView.CardsPerRow);
                    }

                    break;
                case GamepadButton.Y:
                    if (vm.IsLibraryScreen)
                    {
                        vm.Library.CycleSort();
                    }
                    else if (vm.IsMediaScreen)
                    {
                        vm.Media.CycleMediaSort();
                    }

                    break;
                case GamepadButton.A:
                    if (vm.IsLibraryScreen)
                    {
                        _navigation.LaunchSelectedGame(vm);
                    }
                    else if (vm.IsMediaScreen)
                    {
                        _navigation.OpenSelectedScreenshot(vm.Media);
                    }

                    break;
                case GamepadButton.B:
                    vm.CloseOverlay();
                    _navigation.RestoreOptionFocus(vm.Dashboard);
                    break;
            }

            return;
        }

        switch (button)
        {
            case GamepadButton.DpadLeft:
            case GamepadButton.LeftShoulder:
                if (_navigation.IsOnOptionsRow)
                {
                    _navigation.MoveOptionSelection(vm.Dashboard, -1);
                }
                else
                {
                    _navigation.MoveRecentGameSelection(vm.Dashboard, -1);
                }

                break;
            case GamepadButton.DpadRight:
            case GamepadButton.RightShoulder:
                if (_navigation.IsOnOptionsRow)
                {
                    _navigation.MoveOptionSelection(vm.Dashboard, 1);
                }
                else
                {
                    _navigation.MoveRecentGameSelection(vm.Dashboard, 1);
                }

                break;
            case GamepadButton.DpadDown:
                _navigation.SelectOptionRow(vm.Dashboard);
                break;
            case GamepadButton.DpadUp:
                _navigation.SelectGameRow(vm.Dashboard);
                break;
            case GamepadButton.A:
                if (_navigation.IsOnOptionsRow)
                {
                    _navigation.ActivateSelectedOption(vm, vm.Dashboard);
                }
                else
                {
                    _navigation.LaunchSelectedGame(vm, vm.Dashboard.RecentGames.FirstOrDefault(g => g.IsSelected));
                }

                break;
        }
    }
}
