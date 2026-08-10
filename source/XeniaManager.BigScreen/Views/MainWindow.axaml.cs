using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using XeniaManager.BigScreen.Controls;
using XeniaManager.BigScreen.Models;
using XeniaManager.BigScreen.Services;
using XeniaManager.BigScreen.ViewModels;
using XeniaManager.BigScreen.ViewModels.Items;
using XeniaManager.Core.Services;

namespace XeniaManager.BigScreen.Views;

public partial class MainWindow : Window
{
    private SettingsOverlay? _settingsOverlay;
    private LibraryOverlay? _libraryOverlay;
    private MediaOverlay? _mediaOverlay;
    private GamepadService? _gamepadService;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        KeyDown += OnWindowKeyDown;
        GamesRow.AddHandler(GotFocusEvent, OnCardGotFocus, RoutingStrategies.Bubble, true);
        OptionsRow.AddHandler(GotFocusEvent, OnCardGotFocus, RoutingStrategies.Bubble, true);
        OptionsRow.AddHandler(PointerPressedEvent, OnOptionCardPressed, RoutingStrategies.Bubble, true);

        _gamepadService = new GamepadService();
        if (_gamepadService.IsActive)
        {
            _gamepadService.ButtonPressed += OnGamepadButtonPressed;
            _gamepadService.StateChanged += OnGamepadStateChanged;
        }
    }

    /// <summary>
    /// Forwards the live gamepad connection/battery state to the view model.
    /// </summary>
    private void OnGamepadStateChanged()
    {
        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        vm.ApplyGamepadState(_gamepadService!.IsConnected, _gamepadService.BatteryPercent, _gamepadService.IsCharging);
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        EventManager.Instance.WindowDisabled += OnWindowDisabled;

        // Start with the first card selected
        if (DataContext is MainWindowViewModel vm)
        {
            vm.QuitRequested += OnQuitRequested;
            vm.LibraryRefreshed += OnLibraryRefreshed;

            // Push the gamepad state captured during construction (DataContext wasn't set yet)
            if (_gamepadService is { IsActive: true })
            {
                vm.ApplyGamepadState(_gamepadService.IsConnected, _gamepadService.BatteryPercent, _gamepadService.IsCharging);
            }

            if (vm.RecentGames.Count > 0)
            {
                _onOptionsRow = false;
                vm.RecentGames[0].IsSelected = true;
            }
            else
            {
                // No games - the game row isn't available, start on the option row
                SelectOptionRow();
            }

            _settingsOverlay = this.GetVisualDescendants().OfType<SettingsOverlay>().FirstOrDefault();
            if (_settingsOverlay != null)
            {
                _settingsOverlay.PickImageRequested += async (_, _) => await PickBackgroundImageAsync();
            }

            _libraryOverlay = this.GetVisualDescendants().OfType<LibraryOverlay>().FirstOrDefault();
            _mediaOverlay = this.GetVisualDescendants().OfType<MediaOverlay>().FirstOrDefault();
        }
    }

    /// <summary>
    /// Disables/enables the window while a game is running (Core's EventManager).
    /// </summary>
    private void OnWindowDisabled(bool isDisabled)
    {
        IsEnabled = !isDisabled;
    }

    /// <summary>
    /// Re-centers the library carousel after the library is refreshed (post-layout).
    /// </summary>
    private void OnLibraryRefreshed(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (DataContext is MainWindowViewModel vm && vm.RecentGames.Count == 0 && !vm.IsOverlayOpen)
            {
                // Library became empty - the game row is gone, fall back to options
                SelectOptionRow();
            }

            _libraryOverlay?.ScrollToSelected();
        });
    }

    private void OnQuitRequested(object? sender, System.EventArgs e)
    {
        Close();
    }

    /// <summary>
    /// Handles Enter to activate an option card and B/Escape to close overlays.
    /// </summary>
    private void OnWindowKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        if (vm.IsOverlayOpen)
        {
            if (vm.IsMediaScreen && vm.IsMediaViewerOpen)
            {
                if (e.Key is Key.Left or Key.Right)
                {
                    vm.StepScreenshot(e.Key == Key.Right ? 1 : -1);
                    e.Handled = true;
                }
                else if (e.Key is Key.B or Key.Escape)
                {
                    vm.CloseMediaViewer();
                    e.Handled = true;
                }

                return;
            }

            if (e.Key == Key.B || e.Key == Key.Escape)
            {
                vm.CloseOverlay();
                RestoreOptionFocus();
                e.Handled = true;
            }
            else if (vm.IsLibraryScreen && e.Key is Key.Left or Key.Right)
            {
                MoveGameSelection(e.Key == Key.Right ? 1 : -1);
                e.Handled = true;
            }
            else if (vm.IsLibraryScreen && e.Key == Key.Y)
            {
                // Sort keeps the selection on the same card, but the viewport stays put
                vm.CycleSort();
                e.Handled = true;
            }
            else if (vm.IsLibraryScreen && e.Key is Key.A or Key.Enter or Key.Space)
            {
                LaunchSelectedGame(vm);
                e.Handled = true;
            }
            else if (vm.IsMediaScreen && e.Key is Key.Left or Key.Right)
            {
                MoveScreenshotSelection(e.Key == Key.Right ? 1 : -1);
                e.Handled = true;
            }
            else if (vm.IsMediaScreen && e.Key is Key.Up or Key.Down)
            {
                MoveScreenshotSelection(e.Key == Key.Down ? MediaOverlay.CardsPerRow : -MediaOverlay.CardsPerRow);
                e.Handled = true;
            }
            else if (vm.IsMediaScreen && e.Key == Key.Y)
            {
                // Sort keeps the selection on the same card, but the viewport stays put
                vm.CycleMediaSort();
                e.Handled = true;
            }
            else if (vm.IsMediaScreen && e.Key is Key.Enter or Key.Space)
            {
                OpenSelectedScreenshot(vm);
                e.Handled = true;
            }

            return;
        }

        if (e.Key is Key.Enter or Key.Space)
        {
            if (_onOptionsRow)
            {
                ActivateSelectedOption(vm);
                e.Handled = true;
            }
            else if (FocusManager?.GetFocusedElement() is Control { DataContext: GameCardViewModel game })
            {
                _lastActivationWasMouse = false;
                LaunchSelectedGame(vm, game);
                e.Handled = true;
            }
        }
        else if (e.Key is Key.Left or Key.Right)
        {
            if (_onOptionsRow)
            {
                MoveOptionSelection(e.Key == Key.Right ? 1 : -1);
            }
            else
            {
                MoveRecentGameSelection(e.Key == Key.Right ? 1 : -1);
            }

            e.Handled = true;
        }
        else if (e.Key is Key.Up or Key.Down)
        {
            if (e.Key == Key.Down)
            {
                SelectOptionRow();
            }
            else
            {
                SelectGameRow();
            }

            e.Handled = true;
        }
    }

    /// <summary>
    /// Whether the dashboard's active row is the option row (vs the game row).
    /// The controller model tracks this explicitly instead of relying on keyboard focus,
    /// since a game card is always focused regardless of the active row.
    /// </summary>
    private bool _onOptionsRow;

    /// <summary>
    /// Routes gamepad input to the same actions as the keyboard. Settings is
    /// keyboard-only; input is ignored while the window is disabled (game running).
    /// </summary>
    private void OnGamepadButtonPressed(GamepadButton button)
    {
        if (!IsEnabled || DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        // Modal viewer takes priority within the media screen
        if (vm.IsMediaScreen && vm.IsMediaViewerOpen)
        {
            switch (button)
            {
                case GamepadButton.DpadLeft:
                case GamepadButton.LeftShoulder:
                    vm.StepScreenshot(-1);
                    break;
                case GamepadButton.DpadRight:
                case GamepadButton.RightShoulder:
                    vm.StepScreenshot(1);
                    break;
                case GamepadButton.B:
                    vm.CloseMediaViewer();
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
                        MoveGameSelection(-1);
                    }
                    else if (vm.IsMediaScreen)
                    {
                        MoveScreenshotSelection(-1);
                    }

                    break;
                case GamepadButton.DpadRight:
                case GamepadButton.RightShoulder:
                    if (vm.IsLibraryScreen)
                    {
                        MoveGameSelection(1);
                    }
                    else if (vm.IsMediaScreen)
                    {
                        MoveScreenshotSelection(1);
                    }

                    break;
                case GamepadButton.DpadUp:
                    if (vm.IsMediaScreen)
                    {
                        MoveScreenshotSelection(-MediaOverlay.CardsPerRow);
                    }

                    break;
                case GamepadButton.DpadDown:
                    if (vm.IsMediaScreen)
                    {
                        MoveScreenshotSelection(MediaOverlay.CardsPerRow);
                    }

                    break;
                case GamepadButton.Y:
                    if (vm.IsLibraryScreen)
                    {
                        vm.CycleSort();
                    }
                    else if (vm.IsMediaScreen)
                    {
                        vm.CycleMediaSort();
                    }

                    break;
                case GamepadButton.A:
                    if (vm.IsLibraryScreen)
                    {
                        LaunchSelectedGame(vm);
                    }
                    else if (vm.IsMediaScreen)
                    {
                        OpenSelectedScreenshot(vm);
                    }

                    break;
                case GamepadButton.B:
                    vm.CloseOverlay();
                    RestoreOptionFocus();
                    break;
            }

            return;
        }

        switch (button)
        {
            case GamepadButton.DpadLeft:
            case GamepadButton.LeftShoulder:
                if (_onOptionsRow)
                {
                    MoveOptionSelection(-1);
                }
                else
                {
                    MoveRecentGameSelection(-1);
                }

                break;
            case GamepadButton.DpadRight:
            case GamepadButton.RightShoulder:
                if (_onOptionsRow)
                {
                    MoveOptionSelection(1);
                }
                else
                {
                    MoveRecentGameSelection(1);
                }

                break;
            case GamepadButton.DpadDown:
                SelectOptionRow();
                break;
            case GamepadButton.DpadUp:
                SelectGameRow();
                break;
            case GamepadButton.A:
                if (_onOptionsRow)
                {
                    ActivateSelectedOption(vm);
                }
                else
                {
                    LaunchSelectedGame(vm, vm.RecentGames.FirstOrDefault(g => g.IsSelected));
                }

                break;
        }
    }

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
    private void SelectOptionRow()
    {
        if (DataContext is not MainWindowViewModel vm || vm.Options.Count == 0)
        {
            return;
        }

        _onOptionsRow = true;

        int gameIndex = 0;
        for (int i = 0; i < vm.RecentGames.Count; i++)
        {
            if (vm.RecentGames[i].IsSelected)
            {
                gameIndex = i;
                break;
            }
        }

        int mapped = GameToOptionColumn[Math.Clamp(gameIndex, 0, GameToOptionColumn.Length - 1)];
        int target = Math.Clamp(mapped, 0, vm.Options.Count - 1);
        foreach (OptionsCardViewModel option in vm.Options)
        {
            option.IsSelected = option == vm.Options[target];
        }

        OptionsCard? card = OptionsRow.GetVisualDescendants().OfType<OptionsCard>()
            .FirstOrDefault(c => ReferenceEquals(c.DataContext, vm.Options[target]));
        card?.Focus();
    }

    /// <summary>
    /// Switches the dashboard to the game row, selecting the first game card of
    /// the current option's column group (clamped to the game count). When the
    /// library is empty there is no game row, so the option row stays active.
    /// </summary>
    private void SelectGameRow()
    {
        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        // No games - the game row doesn't exist, stay on the option row
        if (vm.RecentGames.Count == 0)
        {
            _onOptionsRow = true;
            return;
        }

        _onOptionsRow = false;

        int optionIndex = 0;
        for (int i = 0; i < vm.Options.Count; i++)
        {
            if (vm.Options[i].IsSelected)
            {
                optionIndex = i;
                break;
            }
        }

        int mapped = OptionToGameColumn[Math.Clamp(optionIndex, 0, OptionToGameColumn.Length - 1)];
        int target = Math.Clamp(mapped, 0, vm.RecentGames.Count - 1);
        foreach (GameCardViewModel game in vm.RecentGames)
        {
            game.IsSelected = game == vm.RecentGames[target];
        }

        foreach (OptionsCardViewModel option in vm.Options)
        {
            option.IsSelected = false;
        }

        GameCard? card = GamesRow.GetVisualDescendants().OfType<GameCard>()
            .FirstOrDefault(c => ReferenceEquals(c.DataContext, vm.RecentGames[target]));
        card?.Focus();
    }

    /// <summary>
    /// Moves the option row selection by the given step, clamped at both ends.
    /// </summary>
    private void MoveOptionSelection(int delta)
    {
        if (DataContext is not MainWindowViewModel vm || vm.Options.Count == 0)
        {
            return;
        }

        int index = 0;
        for (int i = 0; i < vm.Options.Count; i++)
        {
            if (vm.Options[i].IsSelected)
            {
                index = i;
                break;
            }
        }

        int target = Math.Clamp(index + delta, 0, vm.Options.Count - 1);
        if (target == index)
        {
            return;
        }

        OptionsCardViewModel next = vm.Options[target];
        foreach (OptionsCardViewModel option in vm.Options)
        {
            option.IsSelected = ReferenceEquals(option, next);
        }
    }

    /// <summary>
    /// Activates the currently selected option card (A on the option row).
    /// </summary>
    private void ActivateSelectedOption(MainWindowViewModel vm)
    {
        OptionsCardViewModel? option = vm.Options.FirstOrDefault(o => o.IsSelected);
        if (option != null)
        {
            _lastActivationWasMouse = false;
            ActivateOption(option);
        }
    }

    /// <summary>
    /// Moves the dashboard game selection by the given step, clamped at both ends.
    /// </summary>
    private void MoveRecentGameSelection(int delta)
    {
        if (DataContext is not MainWindowViewModel vm || vm.RecentGames.Count == 0)
        {
            return;
        }

        int index = 0;
        for (int i = 0; i < vm.RecentGames.Count; i++)
        {
            if (vm.RecentGames[i].IsSelected)
            {
                index = i;
                break;
            }
        }

        int target = Math.Clamp(index + delta, 0, vm.RecentGames.Count - 1);
        if (target == index)
        {
            return;
        }

        GameCardViewModel next = vm.RecentGames[target];
        foreach (GameCardViewModel game in vm.RecentGames)
        {
            game.IsSelected = ReferenceEquals(game, next);
        }
    }

    /// <summary>
    /// Launches the game currently selected in the library carousel.
    /// </summary>
    private void LaunchSelectedGame(MainWindowViewModel vm, GameCardViewModel? explicitCard = null)
    {
        GameCardViewModel? card = explicitCard ?? vm.Games.FirstOrDefault(g => g.IsSelected);
        if (card != null)
        {
            _ = vm.LaunchGame(card);
        }
    }

    /// <summary>
    /// Moves the game selection by the given step, clamped at both ends of the library.
    /// </summary>
    private void MoveGameSelection(int delta)
    {
        if (DataContext is not MainWindowViewModel vm || vm.Games.Count == 0)
        {
            return;
        }

        int index = 0;
        for (int i = 0; i < vm.Games.Count; i++)
        {
            if (vm.Games[i].IsSelected)
            {
                index = i;
                break;
            }
        }

        int target = Math.Clamp(index + delta, 0, vm.Games.Count - 1);
        if (target == index)
        {
            return;
        }

        GameCardViewModel next = vm.Games[target];
        foreach (GameCardViewModel game in vm.Games)
        {
            game.IsSelected = ReferenceEquals(game, next);
        }

        _libraryOverlay?.ScrollToSelected();
    }

    /// <summary>
    /// Moves the screenshot selection by the given step (1 per column, a full row for
    /// Up/Down), clamped at both ends of the grid - no wrap-around.
    /// </summary>
    private void MoveScreenshotSelection(int delta)
    {
        if (DataContext is not MainWindowViewModel vm || vm.Screenshots.Count == 0)
        {
            return;
        }

        int index = -1;
        for (int i = 0; i < vm.Screenshots.Count; i++)
        {
            if (vm.Screenshots[i].IsSelected)
            {
                index = i;
                break;
            }
        }

        if (index < 0)
        {
            index = 0;
        }

        int target = Math.Clamp(index + delta, 0, vm.Screenshots.Count - 1);
        if (target == index && index == 0)
        {
            vm.Screenshots[0].IsSelected = true;
            return;
        }

        ScreenshotItemViewModel next = vm.Screenshots[target];
        foreach (ScreenshotItemViewModel screenshot in vm.Screenshots)
        {
            screenshot.IsSelected = ReferenceEquals(screenshot, next);
        }

        _mediaOverlay?.ScrollToSelected();
    }

    /// <summary>
    /// Opens the modal viewer for the currently selected screenshot (Enter in the gallery).
    /// </summary>
    private void OpenSelectedScreenshot(MainWindowViewModel vm)
    {
        ScreenshotItemViewModel? selected = vm.Screenshots.FirstOrDefault(s => s.IsSelected);
        if (selected != null)
        {
            vm.OpenScreenshot(selected);
        }
    }

    /// <summary>
    /// Opens the screen for the given option card, or quits for the Quit card.
    /// </summary>
    private void ActivateOption(OptionsCardViewModel option)
    {
        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        if (option.TargetScreen == OverlayScreen.None)
        {
            vm.Quit();
            return;
        }

        vm.OpenScreen(option.TargetScreen);
        FocusOverlay();
        if (option.TargetScreen == OverlayScreen.Library)
        {
            // Post so the overlay has been laid out before centering
            Dispatcher.UIThread.Post(() => _libraryOverlay?.ScrollToSelected());
        }
        else if (option.TargetScreen == OverlayScreen.Media)
        {
            Dispatcher.UIThread.Post(() => _mediaOverlay?.ScrollToSelected());
        }
    }

    private bool _lastActivationWasMouse;

    private void OnOptionCardPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Source is not Control control)
        {
            return;
        }

        if (control.GetSelfAndVisualAncestors().OfType<OptionsCard>().FirstOrDefault()
            is { DataContext: OptionsCardViewModel option })
        {
            _lastActivationWasMouse = true;
            ActivateOption(option);

            // A mouse click must not leave the card focused/selected - only the
            // controller (IsSelected via keyboard focus) or hover should show it
            if (DataContext is MainWindowViewModel vm)
            {
                foreach (OptionsCardViewModel o in vm.Options)
                {
                    o.IsSelected = false;
                }
            }
        }
    }

    /// <summary>
    /// Moves focus into the open overlay (first focusable element).
    /// </summary>
    private void FocusOverlay()
    {
        if (DataContext is MainWindowViewModel vm && vm.IsSettingsScreen && _settingsOverlay != null)
        {
            _settingsOverlay.FocusFirst();
        }
        else
        {
            // Focus the overlay panel itself so keys route through the window handler
            Focus();
        }
    }

    /// <summary>
    /// Restores focus to the previously selected option card after closing an overlay.
    /// Skipped when the overlay was opened with a mouse click - the card stays unfocused.
    /// </summary>
    private void RestoreOptionFocus()
    {
        if (_lastActivationWasMouse)
        {
            return;
        }

        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        OptionsCardViewModel? selected = vm.Options.FirstOrDefault(o => o.IsSelected);
        if (selected == null)
        {
            return;
        }

        OptionsCard? card = OptionsRow.GetVisualDescendants().OfType<OptionsCard>()
            .FirstOrDefault(c => ReferenceEquals(c.DataContext, selected));
        card?.Focus();
    }

    /// <summary>
    /// Opens a file picker and applies the chosen image as the dashboard background.
    /// </summary>
    public async Task PickBackgroundImageAsync()
    {
        FilePickerOpenOptions options = new()
        {
            Title = "Select Background Image",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Images")
                {
                    Patterns = ["*.png", "*.jpg", "*.jpeg", "*.bmp", "*.gif", "*.webp"],
                },
            ],
        };

        IReadOnlyList<IStorageFile> files = await StorageProvider.OpenFilePickerAsync(options);
        if (files.Count == 0 || DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        vm.SetBackgroundImage(files[0].Path.LocalPath);
    }

    private void OnCardGotFocus(object? sender, FocusChangedEventArgs e)
    {
        if (e.Source is not Control { DataContext: GameCardViewModel or OptionsCardViewModel } control)
        {
            return;
        }

        if (DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        // Each row keeps its own independent selection; focus/click on one row
        // never clears the selection of the other. The active row also tracks the
        // controller model so A/L-R act on the right row.
        switch (control.DataContext)
        {
            case GameCardViewModel focusedGame:
                _onOptionsRow = false;
                foreach (GameCardViewModel game in vm.RecentGames)
                {
                    game.IsSelected = ReferenceEquals(game, focusedGame);
                }
                break;
            case OptionsCardViewModel focusedOption:
                _onOptionsRow = true;
                foreach (OptionsCardViewModel option in vm.Options)
                {
                    option.IsSelected = ReferenceEquals(option, focusedOption);
                }
                break;
        }
    }
}
