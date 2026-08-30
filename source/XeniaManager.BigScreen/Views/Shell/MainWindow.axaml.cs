using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FluentAvalonia.UI.Windowing;
using Microsoft.Extensions.DependencyInjection;
using XeniaManager.BigScreen.Constants;
using XeniaManager.BigScreen.Controls.Cards;
using XeniaManager.BigScreen.Controls.Profiles;
using XeniaManager.BigScreen.Controls.Splash;
using XeniaManager.BigScreen.Services;
using XeniaManager.BigScreen.Utilities;
using XeniaManager.BigScreen.ViewModels.Items;
using XeniaManager.BigScreen.ViewModels.Shell;
using XeniaManager.BigScreen.Views.Dashboard;
using XeniaManager.BigScreen.Views.Screens;
using XeniaManager.Logging;
using XeniaManager.Core.Models;
using XeniaManager.Core.Services;
using TweenAvalonia;

namespace XeniaManager.BigScreen.Views.Shell;

public partial class MainWindow : FAAppWindow
{
    private readonly DashboardNavigationController _navigation;
    private readonly InputRouter _router;
    private readonly IGamepadInputService? _gamepadService;

    /// <summary>
    /// The in-flight header reveal fade; always completes to full opacity.
    /// </summary>
    private Tween _headerFade;

    /// <summary>
    /// Whether the window can route input to the dashboard: enabled and the
    /// boot pipeline completed, so a stray key or button can't activate
    /// anything during the splash or while a game is running.
    /// </summary>
    private bool CanHandleInput
    {
        get
        {
            return IsEnabled && DataContext is MainWindowViewModel { IsInitialized: true };
        }
    }

    /// <summary>
    /// Whether dashboard cards may update their row selection on focus.
    /// Overlay cards update their own screen's selection instead.
    /// </summary>
    private bool CanRouteCardFocus
    {
        get
        {
            return IsEnabled && DataContext is MainWindowViewModel { IsOverlayOpen: false };
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
    /// Shuts the app down through the desktop lifetime when the view model
    /// requests a quit, so the view model's exit code survives: a bare
    /// <see cref="Environment.ExitCode"/> assignment (or a plain <see cref="Window.Close"/>)
    /// is overwritten with 0 by <see cref="ClassicDesktopStyleApplicationLifetime.StartCore"/>.
    /// </summary>
    private void OnQuitRequested(object? sender, EventArgs e)
    {
        int exitCode = DataContext is MainWindowViewModel vm ? vm.ExitCode : 0;
        Logger.Info<MainWindow>($"Quit requested, shutting down with exit code {exitCode}");
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lifetime)
        {
            lifetime.Shutdown(exitCode);
        }
        else
        {
            Close();
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
            _navigation.SelectOptionRow(vm.Dashboard);
        }
    }

    /// <summary>
    /// Fades the dashboard elements in (header + card rows) from 0 to full
    /// opacity. Raised by the view model at the moment the splash is about to
    /// close, so the tween is visible from the first revealed frame.
    /// </summary>
    private void StartDashboardReveal()
    {
        _headerFade.Stop();
        HeaderRow.Opacity = 0;
        _headerFade = Tween.Opacity(HeaderRow, 1, TimingConstants.LaunchFadeDuration);
        Find<DashboardView>()?.BeginReveal();
    }

    /// <summary>
    /// Routes keyboard input to the input router. Input is ignored until the
    /// boot pipeline completes (the splash is showing) so a stray key can't
    /// activate anything before the dashboard is ready.
    /// </summary>
    private void OnWindowKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm || !CanHandleInput)
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
        if (DataContext is not MainWindowViewModel vm || !CanHandleInput)
        {
            return;
        }

        _router.HandleGamepad(vm, button);
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
                    _navigation.SelectOptionRow(vm.Dashboard);
                }
            }

            Find<LibraryView>()?.ScrollToSelected();
        });
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
    private void OnScrollLibraryRequested() => Dispatcher.UIThread.Post(() => Find<LibraryView>()?.ScrollToSelected());

    /// <summary>
    /// Scrolls the Gallery grid to its selected card (posted so a just-opened
    /// overlay has been laid out before centering).
    /// </summary>
    private void OnScrollGalleryRequested() => Dispatcher.UIThread.Post(() => Find<GalleryView>()?.ScrollToSelected());

    /// <summary>
    /// Moves focus into the open overlay (first focusable element).
    /// </summary>
    private void OnOverlayFocusRequested()
    {
        if (DataContext is MainWindowViewModel { IsSettingsScreen: true })
        {
            if (Find<SettingsView>() is { } settingsView)
            {
                settingsView.FocusFirst();
                return;
            }
        }

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
        if (DataContext is not MainWindowViewModel vm || !CanRouteCardFocus)
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
        if (DataContext is not MainWindowViewModel vm || !CanHandleInput)
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

    /// <summary>
    /// Opens the game modal on a right-click of a dashboard, carousel or list
    /// card, mirroring the controller path (Y = Details). The card is selected
    /// first so the dialog's game matches the pointer target.
    /// </summary>
    private void OnGameCardRightPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm || !CanHandleInput)
        {
            return;
        }

        if (!e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
        {
            return;
        }

        if (e.Source is not Control control)
        {
            return;
        }

        if (control.GetSelfAndVisualAncestors().FirstOrDefault(c => c is GameCard or LibraryCard or LibraryListItem)
            is not Control card)
        {
            return;
        }

        if (card.DataContext is not GameCardViewModel gameCard)
        {
            return;
        }

        if (card is GameCard)
        {
            _navigation.OnGameCardFocused(vm.Dashboard, gameCard);
        }
        else
        {
            SelectionHelper.SelectOnly(vm.Library.Games, gameCard);
            Dispatcher.UIThread.Post(() => Find<LibraryView>()?.ScrollToSelected());
        }

        vm.OpenGameModal(gameCard.Game);
    }

    /// <summary>
    /// Subscribes the window to the navigation controller's focus and scroll
    /// requests.
    /// </summary>
    private void RegisterNavigationHandlers()
    {
        _navigation.OptionFocusRequested += OnOptionFocusRequested;
        _navigation.GameFocusRequested += OnGameFocusRequested;
        _navigation.ScrollLibraryRequested += OnScrollLibraryRequested;
        _navigation.ScrollGalleryRequested += OnScrollGalleryRequested;
        _navigation.OverlayFocusRequested += OnOverlayFocusRequested;
        _navigation.ProfileFocusRequested += OnProfileFocusRequested;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        EventManager.Instance.WindowDisabled += OnWindowDisabled;
        Logger.Debug<MainWindow>("Main window loaded");

        if (DataContext is MainWindowViewModel vm)
        {
            vm.QuitRequested += OnQuitRequested;
            vm.LibraryRefreshed += OnLibraryRefreshed;
            vm.DashboardRevealRequested += StartDashboardReveal;

            if (_gamepadService is { IsActive: true })
            {
                vm.ApplyGamepadState(_gamepadService.IsConnected, _gamepadService.BatteryPercent,
                    _gamepadService.IsCharging);
            }

            if (vm.IsInitialized)
            {
                InitializeDashboardSelection(vm);
                Dispatcher.UIThread.Post(StartDashboardReveal);
            }
            else
            {
                vm.InitializationCompleted += (_, _) => InitializeDashboardSelection(vm);
            }
        }
    }

    public MainWindow()
    {
        DataContext = App.Services.GetRequiredService<MainWindowViewModel>();
        _navigation = App.Services.GetRequiredService<DashboardNavigationController>();
        _router = App.Services.GetRequiredService<InputRouter>();
        _gamepadService = App.Services.GetRequiredService<IGamepadInputService>();

        WindowState = WindowState.FullScreen;

        InitializeComponent();

        if (TitleBar != null)
        {
            TitleBar.ExtendsContentIntoTitleBar = true;
            TitleBar.Height = 0;
        }

        SplashScreen = new AppSplashScreen();

        Loaded += OnLoaded;
        KeyDown += OnWindowKeyDown;
        AddHandler(GotFocusEvent, OnCardGotFocus, RoutingStrategies.Bubble, true);
        AddHandler(PointerPressedEvent, OnOptionCardPressed, RoutingStrategies.Bubble, true);
        AddHandler(PointerPressedEvent, OnGameCardRightPressed, RoutingStrategies.Bubble, true);

        RegisterNavigationHandlers();

        if (_gamepadService.IsActive)
        {
            _gamepadService.ButtonPressed += OnGamepadButtonPressed;
            _gamepadService.StateChanged += OnGamepadStateChanged;
        }
    }
}