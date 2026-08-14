using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using XeniaManager.BigScreen.Constants;
using XeniaManager.BigScreen.Models;
using XeniaManager.BigScreen.Services;
using XeniaManager.BigScreen.Utilities;
using XeniaManager.BigScreen.ViewModels.Items;
using XeniaManager.BigScreen.ViewModels.Dashboard;
using XeniaManager.BigScreen.ViewModels.Screens;
using XeniaManager.BigScreen.ViewModels.Modals;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Manage;
using XeniaManager.Core.Models.Game;
using XeniaManager.Core.Services;
using XeniaManager.Core.Settings;
using XeniaManager.Core.Utilities;

namespace XeniaManager.BigScreen.ViewModels.Shell;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IBackgroundService _backgroundService;
    private readonly IProfileService _profileService;
    private readonly IGameLibraryService _gameLibraryService;
    private readonly IScreenshotLibraryService _screenshotLibraryService;
    private readonly IGamepadInputService _gamepadService;
    private readonly IModalService _modalService;

    /// <summary>
    /// The most recently selected game card (dashboard or library row). Drives the
    /// dynamic background, so selection in either row changes the artwork.
    /// </summary>
    private GameCardViewModel? _lastSelectedGame;

    /// <summary>
    /// The screen that was open when the first modal was pushed, restored when
    /// the modal stack empties (e.g. a game modal opened from the library lands
    /// back in the library instead of the dashboard).
    /// </summary>
    private ViewModelBase? _screenBeforeModal;

    /// <summary>
    /// The currently open overlay screen, or null when the dashboard is showing.
    /// </summary>
    [ObservableProperty]
    public partial ViewModelBase? CurrentScreen { get; set; }

    /// <summary>
    /// Whether any overlay is currently open.
    /// </summary>
    public bool IsOverlayOpen => CurrentScreen != null;

    /// <summary>
    /// Whether the library overlay is open.
    /// </summary>
    public bool IsLibraryScreen => CurrentScreen == Library;

    /// <summary>
    /// Whether the Gallery overlay is open.
    /// </summary>
    public bool IsGalleryScreen => CurrentScreen == Gallery;

    /// <summary>
    /// Whether the settings overlay is open.
    /// </summary>
    public bool IsSettingsScreen => CurrentScreen == Settings;

    /// <summary>
    /// Whether any modal is on the modal stack (drives the full-window modal layer).
    /// </summary>
    [ObservableProperty]
    public partial bool IsModalOpen { get; set; }

    /// <summary>
    /// Whether the modal backdrop scrim shows. Hidden while the screenshot
    /// viewer is the top modal - its own opaque backdrop covers the window,
    /// so the extra scrim would double-darken it.
    /// </summary>
    [ObservableProperty]
    public partial bool ModalBackdropVisible { get; set; }

    /// <summary>
    /// Whether the library has games with nothing selected yet (first open).
    /// </summary>
    private bool LibraryHasUnselectedGames =>
        Library.Games.Count > 0 && !Library.Games.Any(g => g.IsSelected);

    /// <summary>
    /// Whether the base Xenia Manager app is currently running.
    /// </summary>
    private static bool IsBaseAppRunning =>
        Process.GetProcessesByName(Path.GetFileNameWithoutExtension(AppConstants.BaseAppExecutable)).Length > 0;

    /// <summary>
    /// Raised when the user chooses to quit BigScreen.
    /// </summary>
    public event EventHandler? QuitRequested;

    /// <summary>
    /// Raised after the library is reloaded (e.g. after a game session) so views can re-sync.
    /// </summary>
    public event EventHandler? LibraryRefreshed;

    /// <summary>
    /// Header state (profile, clock, wifi, controller battery).
    /// </summary>
    public HeaderViewModel Header { get; }

    /// <summary>
    /// Dashboard state (recent games, options, background).
    /// </summary>
    public DashboardViewModel Dashboard { get; }

    /// <summary>
    /// Settings screen state (appearance, quit behaviour).
    /// </summary>
    public SettingsViewModel Settings { get; }

    /// <summary>
    /// Library screen state (game carousel and sort).
    /// </summary>
    public LibraryViewModel Library { get; }

    /// <summary>
    /// Gallery screen state (screenshot gallery, sort and viewer).
    /// </summary>
    public GalleryViewModel Gallery { get; }

    partial void OnCurrentScreenChanged(ViewModelBase? value)
    {
        OnPropertyChanged(nameof(IsOverlayOpen));
        OnPropertyChanged(nameof(IsLibraryScreen));
        OnPropertyChanged(nameof(IsGalleryScreen));
        OnPropertyChanged(nameof(IsSettingsScreen));

        // Start the library on the first game (keeps the previous selection on re-open)
        if (value == Library && LibraryHasUnselectedGames)
        {
            Library.Games[0].IsSelected = true;
        }
    }

    public MainWindowViewModel(
        IBackgroundService backgroundService,
        IProfileService profileService,
        IGameLibraryService gameLibraryService,
        IScreenshotLibraryService screenshotLibraryService,
        IGamepadInputService gamepadService,
        IModalService modalService)
    {
        _backgroundService = backgroundService;
        _profileService = profileService;
        _gameLibraryService = gameLibraryService;
        _screenshotLibraryService = screenshotLibraryService;
        _gamepadService = gamepadService;
        _modalService = modalService;

        // The constructor stays cheap: profile, library and screenshot loading
        // happen in InitializeAsync, behind the splash screen
        Header = new HeaderViewModel();
        Settings = new SettingsViewModel(backgroundService, profileService, gamepadService, modalService);
        Library = new LibraryViewModel(Settings, modalService);
        Gallery = new GalleryViewModel(Settings, screenshotLibraryService, modalService);
        Dashboard = new DashboardViewModel(backgroundService);
        Settings.AppearanceChanged += () => Dashboard.UpdateBackground(_lastSelectedGame?.BackgroundArt);
        Settings.CardImageChanged += () =>
        {
            foreach (GameCardViewModel card in Dashboard.RecentGames)
            {
                card.CardImageMode = Settings.CardImageMode;
            }
        };
        Settings.TimeFormatChanged += () => Header.ApplyTimeFormat(Settings.TimeFormat);

        // A profile switch refreshes the header identity and rebuilds the cards;
        // the cached achievement GPDs belong to the old profile and are dropped.
        // Skipped during boot - the pipeline builds the header and cards itself.
        _profileService.ProfileChanged += () =>
        {
            if (!IsInitialized)
            {
                return;
            }

            GameDataCache.ClearAchievementGpds();
            Header.ApplyProfile(_profileService);
            TaskUtilities.RunSafely<MainWindowViewModel>(RebuildCards, "Rebuilding profile cards");
        };

        // The full-window modal layer follows the modal stack; when the stack
        // empties, the screen under the first modal is restored (the dashboard
        // shows when the modal was opened from it)
        _modalService.StackChanged += () =>
        {
            if (_modalService.IsOpen)
            {
                _screenBeforeModal ??= CurrentScreen;
            }
            else if (_screenBeforeModal != null)
            {
                CurrentScreen = _screenBeforeModal;
                _screenBeforeModal = null;
            }
            else
            {
                CloseOverlay();
            }

            IsModalOpen = _modalService.IsOpen;
            ModalBackdropVisible = _modalService is { IsOpen: true, Top: not ScreenshotViewerViewModel };
        };
    }

    /// <summary>
    /// Whether the boot pipeline (profile, library, screenshots) has completed.
    /// </summary>
    public bool IsInitialized { get; private set; }

    /// <summary>
    /// Raised when the boot pipeline completes, so views can run their initial
    /// selection logic (which needs the collections to be populated).
    /// </summary>
    public event EventHandler? InitializationCompleted;

    /// <summary>
    /// Runs the boot pipeline behind the splash screen: profile, library and
    /// screenshot loading with live status/progress, cancellable between steps.
    /// </summary>
    /// <summary>
    /// Reports a stage, runs its work and holds it for the stage dwell.
    /// </summary>
    private static async Task StageAsync(
        IProgress<(string Status, double Progress)>? progress,
        string status,
        double value,
        CancellationToken cancellationToken,
        Action work)
    {
        progress?.Report((status, value));
        cancellationToken.ThrowIfCancellationRequested();
        work();
        await Task.Delay(TimingConstants.StageDwell, cancellationToken);
    }

    /// <summary>
    /// Runs the boot pipeline behind the splash screen: profile, settings,
    /// dashboard, library and Gallery loading with live status/progress,
    /// cancellable between steps.
    /// </summary>
    public async Task InitializeAsync(
        IProgress<(string Status, double Progress)>? progress = null,
        CancellationToken cancellationToken = default)
    {
        // Yield so the splash screen paints its first frame before any work runs
        await Task.Yield();

        // Settings first: the persisted profile_xuid drives which profile activates
        await StageAsync(progress, LocalizationHelper.GetText("Splash.LoadingSettings"), 0.10, cancellationToken, () =>
        {
            Settings.Load();
            Dashboard.UpdateBackground(null);
            Header.ApplyTimeFormat(Settings.TimeFormat);

            // Restore the saved primary controller (falls back to the first pad)
            if (!string.IsNullOrEmpty(_backgroundService.Settings.PrimaryControllerGuid))
            {
                _gamepadService.SetPrimaryByGuid(_backgroundService.Settings.PrimaryControllerGuid);
            }
        });

        await StageAsync(progress, LocalizationHelper.GetText("Splash.LoadingProfile"), 0.25, cancellationToken, () =>
        {
            _profileService.Load();
            Header.ApplyProfile(_profileService);
        });

        await StageAsync(progress, LocalizationHelper.GetText("Splash.LoadingDashboard"), 0.35, cancellationToken, () =>
        {
            _gameLibraryService.Load();
            foreach (Game game in _gameLibraryService.GetRecentGames(AppConstants.RecentGamesLimit))
            {
                Dashboard.RecentGames.Add(CreateRecentGameCard(game));
            }
        });

        progress?.Report((LocalizationHelper.GetText("Splash.LoadingLibrary"), 0.45));
        cancellationToken.ThrowIfCancellationRequested();
        int totalGames = _gameLibraryService.Games.Count;

        // Per-game achievement stats are resolved off the UI thread (GPD file I/O)
        (Game Game, GameStatInfo? Stats)[] games = await Task.Run(() =>
            _gameLibraryService.Games
                .Select(game => (game, _profileService.GetGameStats(game)))
                .ToArray(), cancellationToken);
        int loadedGames = 0;
        foreach ((Game game, GameStatInfo? stats) in games)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Library.Games.Add(CreateGameCard(game, stats));
            loadedGames++;
            if (loadedGames % TimingConstants.ProgressReportInterval == 0)
            {
                progress?.Report((LocalizationHelper.GetText("Splash.LoadingLibrary"),
                    0.45 + 0.17 * (double)loadedGames / Math.Max(1, totalGames)));
            }
        }

        await Task.Delay(TimingConstants.StageDwell, cancellationToken);

        // Game data preload: parsed configs (lazy), content scans, patch files,
        // achievement GPDs and marketplace details for every game, so the game
        // modal's panes and the details pane open instantly
        progress?.Report((LocalizationHelper.GetText("Splash.LoadingGameData"), 0.66));
        cancellationToken.ThrowIfCancellationRequested();
        Stopwatch dataSw = Stopwatch.StartNew();
        await Task.Run(async () =>
        {
            int preloaded = 0;
            foreach (Game game in _gameLibraryService.Games)
            {
                cancellationToken.ThrowIfCancellationRequested();
                GameDataCache.PreloadGame(game);
                preloaded++;
                if (preloaded % TimingConstants.ProgressReportInterval == 0)
                {
                    progress?.Report((LocalizationHelper.GetText("Splash.LoadingGameData"),
                        0.66 + 0.12 * preloaded / Math.Max(1, totalGames)));
                }
            }

            await Library.PreloadDetailsAsync(cancellationToken);
        }, cancellationToken);
        dataSw.Stop();
        Logger.Info<MainWindowViewModel>(
            $"Game data preloaded for {_gameLibraryService.Games.Count} games in {dataSw.ElapsedMilliseconds}ms");
        await Task.Delay(TimingConstants.StageDwell, cancellationToken);

        await Gallery.LoadScreenshotsAsync(progress, cancellationToken);
        await Task.Delay(TimingConstants.StageDwell, cancellationToken);

        // Pre-warm the first library game's background art so the first library
        // open doesn't pay a synchronous image decode
        Library.Games.FirstOrDefault()?.EnsureBackgroundLoaded();

        IsInitialized = true;
        InitializationCompleted?.Invoke(this, EventArgs.Empty);
        Logger.Info<MainWindowViewModel>(
            $"Dashboard ready: {Library.Games.Count} games in library, {Dashboard.RecentGames.Count} recent");
        progress?.Report((LocalizationHelper.GetText("Splash.LoadingDone"), 1.0));
        await Task.Delay(TimingConstants.DoneDwell, cancellationToken);
    }

    /// <summary>
    /// Applies the live gamepad connection/battery state from the gamepad service.
    /// </summary>
    public void ApplyGamepadState(bool connected, int batteryPercent, bool charging) =>
        Header.ApplyGamepadState(connected, batteryPercent, charging);

    /// <summary>
    /// Opens the given overlay screen.
    /// </summary>
    public void OpenScreen(OverlayScreen screen)
    {
        Logger.Info<MainWindowViewModel>($"Opening {screen} screen");
        CurrentScreen = screen switch
        {
            OverlayScreen.Library => Library,
            OverlayScreen.Gallery => Gallery,
            OverlayScreen.Settings => Settings,
            _ => null
        };
    }

    /// <summary>
    /// Closes the currently open overlay screen.
    /// </summary>
    public void CloseOverlay()
    {
        Logger.Info<MainWindowViewModel>("Closing overlay");
        CurrentScreen = null;
        Dashboard.UpdateBackground(_lastSelectedGame?.BackgroundArt, fade: true);
    }

    /// <summary>
    /// Requests the app to quit. When returning to Xenia Manager is enabled and the
    /// base app isn't running, it is launched first; BigScreen then closes.
    /// </summary>
    public void Quit()
    {
        Logger.Info<MainWindowViewModel>(
            $"Quitting BigScreen (return to Xenia Manager: {Settings.ReturnToXeniaOnQuit})");
        if (Settings.ReturnToXeniaOnQuit)
        {
            string baseExe = Path.Combine(AppPathResolver.BaseDirectory(), AppConstants.BaseAppExecutable);
            if (File.Exists(baseExe) && !IsBaseAppRunning)
            {
                Process.Start(new ProcessStartInfo { FileName = baseExe, UseShellExecute = true });
            }
        }

        QuitRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Creates a game card wired to the shared selection handler.
    /// </summary>
    private GameCardViewModel CreateGameCard(Game game, GameStatInfo? stats)
    {
        GameCardViewModel card = new(game, stats);
        card.PropertyChanged += OnGameCardPropertyChanged;
        return card;
    }

    /// <summary>
    /// Creates a dashboard game card, pre-loading its background art so the
    /// dynamic background has something to show immediately.
    /// </summary>
    private GameCardViewModel CreateRecentGameCard(Game game)
    {
        GameCardViewModel card = CreateGameCard(game, _profileService.GetGameStats(game));
        card.CardImageMode = Settings.CardImageMode;
        card.EnsureBackgroundLoaded();
        return card;
    }

    /// <summary>
    /// Rebuilds the dashboard/library card collections with fresh stats, preserving
    /// the selection per row (falling back to the first card so a card is always
    /// selected). Stats are resolved off the UI thread.
    /// </summary>
    private async Task RebuildCards()
    {
        string? librarySelectedId = Library.Games.FirstOrDefault(g => g.IsSelected)?.Game.GameId;
        string? recentSelectedId = Dashboard.RecentGames.FirstOrDefault(g => g.IsSelected)?.Game.GameId;

        Library.Games.Clear();
        Dashboard.RecentGames.Clear();

        (Game Game, GameStatInfo? Stats)[] games = await Task.Run(() =>
            _gameLibraryService.Games
                .Select(game => (game, _profileService.GetGameStats(game)))
                .ToArray());

        foreach ((Game game, GameStatInfo? stats) in games)
        {
            Library.Games.Add(CreateGameCard(game, stats));
        }

        foreach (Game game in _gameLibraryService.GetRecentGames(AppConstants.RecentGamesLimit))
        {
            Dashboard.RecentGames.Add(CreateRecentGameCard(game));
        }

        (Library.Games.FirstOrDefault(g => g.Game.GameId == librarySelectedId) ?? Library.Games.FirstOrDefault())
            ?.IsSelected = true;
        (Dashboard.RecentGames.FirstOrDefault(g => g.Game.GameId == recentSelectedId) ??
         Dashboard.RecentGames.FirstOrDefault())?.IsSelected = true;

        LibraryRefreshed?.Invoke(this, EventArgs.Empty);
        Logger.Debug<MainWindowViewModel>(
            $"Cards rebuilt: {Library.Games.Count} games, {Dashboard.RecentGames.Count} recent");
    }

    /// <summary>
    /// Reloads the game library from disk and rebuilds the card collections so
    /// playtime and last-played values reflect the finished session.
    /// </summary>
    private async Task RefreshLibrary()
    {
        _gameLibraryService.Load();
        await RebuildCards();
        Logger.Debug<MainWindowViewModel>("Library refreshed from disk");
    }

    /// <summary>
    /// Opens the profile picker modal, where the active profile can be switched.
    /// Skipped when a modal is already open (e.g. the avatar keeps focus and a
    /// stray Enter would otherwise double-open the picker).
    /// </summary>
    public void OpenProfilePicker()
    {
        if (_modalService.IsOpen)
        {
            return;
        }

        Logger.Info<MainWindowViewModel>("Opening profile picker");
        TaskUtilities.RunSafely<MainWindowViewModel>(
            () => _modalService.ShowAsync(new ProfilePickerViewModel()), "Opening profile picker");
    }

    /// <summary>
    /// Opens the game modal modal for the given game (Y on a card, or right-click).
    /// Skipped when a modal is already open.
    /// </summary>
    public void OpenGameModal(Game game)
    {
        if (_modalService.IsOpen)
        {
            return;
        }

        Logger.Info<MainWindowViewModel>($"Opening game modal for '{game.Title}'");
        TaskUtilities.RunSafely<MainWindowViewModel>(
            () => _modalService.ShowAsync(new GameModalViewModel(game)), "Opening game modal");
    }

    /// <summary>
    /// Launches the given game via Core's Launcher. Multi-disc games show the
    /// disc selection modal first. Disables the window while the game runs,
    /// then re-enables it and refreshes the library (playtime, last played).
    /// </summary>
    public async Task LaunchGame(GameCardViewModel card)
    {
        Logger.Info<MainWindowViewModel>($"Launching '{card.Game.Title}'");

        int discNumber = await ResolveDiscNumber(card.Game);
        if (discNumber < 1)
        {
            Logger.Info<MainWindowViewModel>($"Disc selection cancelled for '{card.Game.Title}'");
            return;
        }

        try
        {
            EventManager.Instance.DisableWindow();
            Settings settings = new();
            await Launcher.LaunchGameASync(card.Game, settings, discNumber: discNumber);
            Logger.Info<MainWindowViewModel>($"Game session ended for '{card.Game.Title}'");
        }
        catch (Exception ex)
        {
            Logger.Error<MainWindowViewModel>($"Failed to launch '{card.Game.Title}'");
            Logger.LogExceptionDetails<MainWindowViewModel>(ex);
        }
        finally
        {
            EventManager.Instance.EnableWindow();
            await RefreshLibrary();
        }
    }

    /// <summary>
    /// Resolves the disc to launch: the game's last played disc for single-disc
    /// games, or the user's choice from the disc selection modal for multi-disc
    /// games. Returns 0 when the selection was cancelled.
    /// </summary>
    private async Task<int> ResolveDiscNumber(Game game)
    {
        if (!game.FileLocations.IsMultiDisc)
        {
            return game.LastPlayedDisc;
        }

        Logger.Info<MainWindowViewModel>(
            $"Showing disc selection for '{game.Title}' ({game.FileLocations.DiscCount} discs)");
        int? selectedDisc = await _modalService.ShowAsync<int?>(new DiscSelectionViewModel(game));
        return selectedDisc ?? 0;
    }

    private void OnGameCardPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(GameCardViewModel.IsSelected))
        {
            if (sender is GameCardViewModel { IsSelected: true } card)
            {
                _lastSelectedGame = card;
            }
        }

        // Swap behind closed overlays; the library/Gallery screens cover the
        // dashboard, so the fade only matters when the dashboard is visible
        if (e.PropertyName is nameof(GameCardViewModel.IsSelected) or nameof(GameCardViewModel.BackgroundArt))
        {
            if (!IsOverlayOpen)
            {
                Dashboard.UpdateBackground(_lastSelectedGame?.BackgroundArt, fade: true);
            }
        }
    }
}