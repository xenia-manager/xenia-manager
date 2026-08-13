using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FluentAvalonia.UI.Windowing;
using Microsoft.Extensions.DependencyInjection;
using XeniaManager.BigScreen.Controls;
using XeniaManager.BigScreen.Services;
using XeniaManager.BigScreen.ViewModels;
using XeniaManager.BigScreen.ViewModels.Items;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Models;
using XeniaManager.Core.Services;

namespace XeniaManager.BigScreen.Views;

public partial class MainWindow : FAAppWindow
{
    private readonly DashboardNavigationController _navigation;
    private readonly InputRouter _router;
    private IGamepadInputService? _gamepadService;

    public MainWindow()
    {
        // Resolve the injected services from the container (the XAML loader
        // requires a public parameterless constructor)
        DataContext = App.Services.GetRequiredService<MainWindowViewModel>();
        _navigation = App.Services.GetRequiredService<DashboardNavigationController>();
        _router = App.Services.GetRequiredService<InputRouter>();
        _gamepadService = App.Services.GetRequiredService<IGamepadInputService>();

        WindowState = Avalonia.Controls.WindowState.FullScreen;

        InitializeComponent();

        // FAAppWindow's managed title bar reserves a top strip and insets the
        // window content below it; extend the content into it so overlays and
        // the modal layer cover the entire window (like the window background)
        if (TitleBar != null)
        {
            TitleBar.ExtendsContentIntoTitleBar = true;
            TitleBar.Height = 0;
        }

        // FluentAvalonia's built-in splash: shows the splash, runs the boot
        // pipeline, then reveals this window
        SplashScreen = new AppSplashScreen();

        Loaded += OnLoaded;
        KeyDown += OnWindowKeyDown;

        // Window-wide: any card gaining focus updates its row's selection
        // (dashboard rows only - overlay cards are handled by their own views)
        AddHandler(GotFocusEvent, OnCardGotFocus, RoutingStrategies.Bubble, true);
        AddHandler(PointerPressedEvent, OnOptionCardPressed, RoutingStrategies.Bubble, true);

        _navigation.OptionFocusRequested += OnOptionFocusRequested;
        _navigation.GameFocusRequested += OnGameFocusRequested;
        _navigation.ScrollLibraryRequested += OnScrollLibraryRequested;
        _navigation.ScrollGalleryRequested += OnScrollGalleryRequested;
        _navigation.OverlayFocusRequested += OnOverlayFocusRequested;
        _navigation.ProfileFocusRequested += OnProfileFocusRequested;

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
        Logger.Debug<MainWindow>("Main window loaded");

        // Start with the first card selected (after the boot pipeline, which
        // runs behind the splash screen - the collections may still be empty)
        if (DataContext is MainWindowViewModel vm)
        {
            vm.QuitRequested += OnQuitRequested;
            vm.LibraryRefreshed += OnLibraryRefreshed;

            // Push the gamepad state captured during construction (DataContext wasn't set yet)
            if (_gamepadService is { IsActive: true })
            {
                vm.ApplyGamepadState(_gamepadService.IsConnected, _gamepadService.BatteryPercent,
                    _gamepadService.IsCharging);
            }

            if (vm.IsInitialized)
            {
                InitializeDashboardSelection(vm);
            }
            else
            {
                vm.InitializationCompleted += (_, _) => InitializeDashboardSelection(vm);
            }
        }
    }

    /// <summary>
    /// Selects the first dashboard card, or falls back to the option row when
    /// the library is empty.
    /// </summary>
    private void InitializeDashboardSelection(MainWindowViewModel vm)
    {
        if (!vm.Dashboard.ShowEmptyStub)
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

    /// <summary>
    /// Disables/enables the window while a game is running (Core's EventManager).
    /// </summary>
    private void OnWindowDisabled(bool isDisabled)
    {
        IsEnabled = !isDisabled;
        Logger.Debug<MainWindow>($"Window {(isDisabled ? "disabled" : "enabled")} (game running)");
    }

    /// <summary>
    /// Re-centers the library carousel after the library is refreshed (post-layout).
    /// </summary>
    private void OnLibraryRefreshed(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (DataContext is MainWindowViewModel vm)
            {
                if (vm.Dashboard.ShowEmptyStub && !vm.IsOverlayOpen)
                {
                    // Library became empty - the game row is gone, fall back to options
                    _navigation.SelectOptionRow(vm.Dashboard);
                }
            }

            Find<LibraryView>()?.ScrollToSelected();
        });
    }

    private void OnQuitRequested(object? sender, System.EventArgs e)
    {
        Logger.Info<MainWindow>("Quit requested, closing window");
        Close();
    }

    /// <summary>
    /// Routes keyboard input to the input router. Input is ignored until the
    /// boot pipeline completes (the splash is showing) so a stray key can't
    /// activate anything before the dashboard is ready.
    /// </summary>
    private void OnWindowKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm || !vm.IsInitialized)
        {
            return;
        }

        _router.HandleKey(vm, e, FocusManager);
    }

    /// <summary>
    /// Routes gamepad input to the same actions as the keyboard. Input is ignored
    /// while the window is disabled (game running) or before the boot pipeline
    /// completes.
    /// </summary>
    private void OnGamepadButtonPressed(GamepadButton button)
    {
        if (!IsEnabled || DataContext is not MainWindowViewModel vm)
        {
            return;
        }

        if (!vm.IsInitialized)
        {
            return;
        }

        _router.HandleGamepad(vm, button);
    }

    /// <summary>
    /// Fulfills a focus request on an option card, deselecting the profile row.
    /// </summary>
    private void OnOptionFocusRequested(OptionsCardViewModel option)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.Header.IsSelected = false;
        }

        Find<OptionsCard>(c => ReferenceEquals(c.DataContext, option))?.Focus();
    }

    /// <summary>
    /// Fulfills a focus request on a game card, deselecting the profile row.
    /// </summary>
    private void OnGameFocusRequested(GameCardViewModel game)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.Header.IsSelected = false;
        }

        Find<GameCard>(c => ReferenceEquals(c.DataContext, game))?.Focus();
    }

    /// <summary>
    /// Fulfills a focus request on the header profile button, marking its row
    /// as selected so the accent outline shows.
    /// </summary>
    private void OnProfileFocusRequested()
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.Header.IsSelected = true;
        }

        Find<ProfileButton>()?.Focus();
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
    /// Scrolls the Gallery grid to its selected card (posted so a just-opened
    /// overlay has been laid out before centering).
    /// </summary>
    private void OnScrollGalleryRequested()
    {
        Dispatcher.UIThread.Post(() => Find<GalleryView>()?.ScrollToSelected());
    }

    /// <summary>
    /// Moves focus into the open overlay (first focusable element).
    /// </summary>
    private void OnOverlayFocusRequested()
    {
        if (DataContext is MainWindowViewModel vm && vm.IsSettingsScreen)
        {
            if (Find<SettingsView>() is { } settingsView)
            {
                settingsView.FocusFirst();
                return;
            }
        }

        // Focus the overlay panel itself so keys route through the window handler
        Focus();
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
                vm.Header.IsSelected = false;
                _navigation.OnGameCardFocused(vm.Dashboard, focusedGame);
                break;
            case OptionsCardViewModel focusedOption:
                vm.Header.IsSelected = false;
                _navigation.OnOptionCardFocused(vm.Dashboard, focusedOption);
                break;
        }
    }

    /// <summary>
    /// Activates an option card on mouse click. Ignored until the boot pipeline
    /// completes so a click can't activate anything during the splash.
    /// </summary>
    private void OnOptionCardPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm || !vm.IsInitialized)
        {
            return;
        }

        if (e.Source is not Control control)
        {
            return;
        }

        if (control.GetSelfAndVisualAncestors().OfType<OptionsCard>().FirstOrDefault()
            is { DataContext: OptionsCardViewModel option })
        {
            _navigation.HandleOptionCardPressed(vm, vm.Dashboard, option);
        }
    }
}