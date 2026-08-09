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
using XeniaManager.BigScreen.ViewModels;
using XeniaManager.BigScreen.ViewModels.Items;
using XeniaManager.Core.Services;

namespace XeniaManager.BigScreen.Views;

public partial class MainWindow : Window
{
    private SettingsOverlay? _settingsOverlay;
    private LibraryOverlay? _libraryOverlay;
    private MediaOverlay? _mediaOverlay;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        KeyDown += OnWindowKeyDown;
        GamesRow.AddHandler(GotFocusEvent, OnCardGotFocus, RoutingStrategies.Bubble, true);
        OptionsRow.AddHandler(GotFocusEvent, OnCardGotFocus, RoutingStrategies.Bubble, true);
        OptionsRow.AddHandler(PointerPressedEvent, OnOptionCardPressed, RoutingStrategies.Bubble, true);
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        EventManager.Instance.WindowDisabled += OnWindowDisabled;

        // Start with the first card selected
        if (DataContext is MainWindowViewModel vm)
        {
            vm.QuitRequested += OnQuitRequested;
            vm.LibraryRefreshed += OnLibraryRefreshed;
            if (vm.RecentGames.Count > 0)
            {
                vm.RecentGames[0].IsSelected = true;
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
        Dispatcher.UIThread.Post(() => _libraryOverlay?.ScrollToSelected());
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
                vm.CycleSort();
                Dispatcher.UIThread.Post(() => _libraryOverlay?.ScrollToSelected());
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
                vm.CycleMediaSort();
                Dispatcher.UIThread.Post(() => _mediaOverlay?.ScrollToSelected());
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
            if (FocusManager?.GetFocusedElement() is Control { DataContext: OptionsCardViewModel option })
            {
                _lastActivationWasMouse = false;
                ActivateOption(option);
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
            MoveRecentGameSelection(e.Key == Key.Right ? 1 : -1);
            e.Handled = true;
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
        // never clears the selection of the other
        switch (control.DataContext)
        {
            case GameCardViewModel focusedGame:
                foreach (GameCardViewModel game in vm.RecentGames)
                {
                    game.IsSelected = ReferenceEquals(game, focusedGame);
                }
                break;
            case OptionsCardViewModel focusedOption:
                foreach (OptionsCardViewModel option in vm.Options)
                {
                    option.IsSelected = ReferenceEquals(option, focusedOption);
                }
                break;
        }
    }
}
