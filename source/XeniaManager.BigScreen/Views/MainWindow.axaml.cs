using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
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
    private readonly DashboardNavigationController _navigation = new();
    private readonly InputRouter _router;
    private IGamepadService? _gamepadService;

    public MainWindow()
    {
        InitializeComponent();
        _router = new InputRouter(_navigation);
        Loaded += OnLoaded;
        KeyDown += OnWindowKeyDown;

        // Window-wide: any card gaining focus updates its row's selection
        // (dashboard rows only - overlay cards are handled by their own views)
        AddHandler(GotFocusEvent, OnCardGotFocus, RoutingStrategies.Bubble, true);
        AddHandler(PointerPressedEvent, OnOptionCardPressed, RoutingStrategies.Bubble, true);

        _navigation.OptionFocusRequested += OnOptionFocusRequested;
        _navigation.GameFocusRequested += OnGameFocusRequested;
        _navigation.ScrollLibraryRequested += OnScrollLibraryRequested;
        _navigation.ScrollMediaRequested += OnScrollMediaRequested;
        _navigation.OverlayFocusRequested += OnOverlayFocusRequested;

        _gamepadService = new GamepadService();
        if (_gamepadService.IsActive)
        {
            _gamepadService.ButtonPressed += OnGamepadButtonPressed;
            _gamepadService.StateChanged += OnGamepadStateChanged;
        }
    }

    /// <summary>
    /// Finds the first descendant of the given type in the visual tree,
    /// optionally matching a predicate.
    /// </summary>
    private T? Find<T>(Func<T, bool>? predicate = null) where T : Control
    {
        return predicate == null
            ? this.GetVisualDescendants().OfType<T>().FirstOrDefault()
            : this.GetVisualDescendants().OfType<T>().FirstOrDefault(predicate);
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

            if (vm.Dashboard.RecentGames.Count > 0)
            {
                _navigation.IsOnOptionsRow = false;
                vm.Dashboard.RecentGames[0].IsSelected = true;
            }
            else
            {
                // No games - the game row isn't available, start on the option row
                _navigation.SelectOptionRow(vm.Dashboard);
            }
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
            if (DataContext is MainWindowViewModel vm && vm.Dashboard.RecentGames.Count == 0 && !vm.IsOverlayOpen)
            {
                // Library became empty - the game row is gone, fall back to options
                _navigation.SelectOptionRow(vm.Dashboard);
            }

            Find<LibraryView>()?.ScrollToSelected();
        });
    }

    private void OnQuitRequested(object? sender, System.EventArgs e)
    {
        Close();
    }

    /// <summary>
    /// Routes keyboard input to the input router.
    /// </summary>
    private void OnWindowKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            _router.HandleKey(vm, e, FocusManager);
        }
    }

    /// <summary>
    /// Routes gamepad input to the same actions as the keyboard. Input is ignored
    /// while the window is disabled (game running).
    /// </summary>
    private void OnGamepadButtonPressed(GamepadButton button)
    {
        if (!IsEnabled || DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        _router.HandleGamepad(vm, button);
    }

    /// <summary>
    /// Fulfills a focus request on an option card.
    /// </summary>
    private void OnOptionFocusRequested(OptionsCardViewModel option)
    {
        Find<OptionsCard>(c => ReferenceEquals(c.DataContext, option))?.Focus();
    }

    /// <summary>
    /// Fulfills a focus request on a game card.
    /// </summary>
    private void OnGameFocusRequested(GameCardViewModel game)
    {
        Find<GameCard>(c => ReferenceEquals(c.DataContext, game))?.Focus();
    }

    /// <summary>
    /// Scrolls the library carousel to its selected card (posted so a just-opened
    /// overlay has been laid out before centering).
    /// </summary>
    private void OnScrollLibraryRequested()
    {
        Dispatcher.UIThread.Post(() => Find<LibraryView>()?.ScrollToSelected());
    }

    /// <summary>
    /// Scrolls the media grid to its selected card (posted so a just-opened
    /// overlay has been laid out before centering).
    /// </summary>
    private void OnScrollMediaRequested()
    {
        Dispatcher.UIThread.Post(() => Find<MediaView>()?.ScrollToSelected());
    }

    /// <summary>
    /// Moves focus into the open overlay (first focusable element).
    /// </summary>
    private void OnOverlayFocusRequested()
    {
        if (DataContext is MainWindowViewModel vm && vm.IsSettingsScreen
            && Find<SettingsView>() is { } settingsView)
        {
            settingsView.FocusFirst();
        }
        else
        {
            // Focus the overlay panel itself so keys route through the window handler
            Focus();
        }
    }

    /// <summary>
    /// Updates the row selection when a card gains focus (controller/keyboard/mouse).
    /// Each row keeps its own independent selection; focus/click on one row never
    /// clears the selection of the other. Only dashboard cards are handled here -
    /// overlay cards update their own screen's selection.
    /// </summary>
    private void OnCardGotFocus(object? sender, FocusChangedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm || vm.IsOverlayOpen)
        {
            return;
        }

        if (e.Source is not Control { DataContext: GameCardViewModel or OptionsCardViewModel } control)
        {
            return;
        }

        switch (control.DataContext)
        {
            case GameCardViewModel focusedGame:
                _navigation.OnGameCardFocused(vm.Dashboard, focusedGame);
                break;
            case OptionsCardViewModel focusedOption:
                _navigation.OnOptionCardFocused(vm.Dashboard, focusedOption);
                break;
        }
    }

    /// <summary>
    /// Activates an option card on mouse click.
    /// </summary>
    private void OnOptionCardPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Source is not Control control)
        {
            return;
        }

        if (control.GetSelfAndVisualAncestors().OfType<OptionsCard>().FirstOrDefault()
            is { DataContext: OptionsCardViewModel option })
        {
            if (DataContext is MainWindowViewModel vm)
            {
                _navigation.HandleOptionCardPressed(vm, vm.Dashboard, option);
            }
        }
    }
}
