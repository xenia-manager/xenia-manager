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
using XeniaManager.BigScreen.ViewModels.Items;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Manage;
using XeniaManager.Core.Models.Game;
using XeniaManager.Core.Services;
using XeniaManager.Core.Settings;
using XeniaManager.Core.Utilities;

namespace XeniaManager.BigScreen.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IBackgroundService _backgroundService;
    private readonly IProfileService _profileService;
    private readonly IGameLibraryService _gameLibraryService;
    private readonly IScreenshotLibraryService _screenshotLibraryService;

    /// <summary>
    /// The most recently selected game card (dashboard or library row). Drives the
    /// dynamic background, so selection in either row changes the artwork.
    /// </summary>
    private GameCardViewModel? _lastSelectedGame;

    /// <summary>
    /// The currently open overlay screen, or null when the dashboard is showing.
    /// </summary>
    [ObservableProperty] private ViewModelBase? _currentScreen;

    /// <summary>
    /// Whether any overlay is currently open.
    /// </summary>
    public bool IsOverlayOpen => CurrentScreen != null;

    /// <summary>
    /// Whether the library overlay is open.
    /// </summary>
    public bool IsLibraryScreen => CurrentScreen == Library;

    /// <summary>
    /// Whether the media overlay is open.
    /// </summary>
    public bool IsMediaScreen => CurrentScreen == Media;

    /// <summary>
    /// Whether the settings overlay is open.
    /// </summary>
    public bool IsSettingsScreen => CurrentScreen == Settings;

    /// <summary>
    /// Whether the media screenshot viewer is open.
    /// </summary>
    public bool IsMediaViewerOpen => CurrentScreen is MediaViewModel { IsViewerOpen: true };

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
    /// Media screen state (screenshot gallery, sort and viewer).
    /// </summary>
    public MediaViewModel Media { get; }

    partial void OnCurrentScreenChanged(ViewModelBase? value)
    {
        OnPropertyChanged(nameof(IsOverlayOpen));
        OnPropertyChanged(nameof(IsLibraryScreen));
        OnPropertyChanged(nameof(IsMediaScreen));
        OnPropertyChanged(nameof(IsSettingsScreen));

        // Start the library on the first game (keeps the previous selection on re-open)
        if (value == Library && Library.Games.Count > 0 && !Library.Games.Any(g => g.IsSelected))
        {
            Library.Games[0].IsSelected = true;
        }
    }

    public MainWindowViewModel(
        IBackgroundService backgroundService,
        IProfileService profileService,
        IGameLibraryService gameLibraryService,
        IScreenshotLibraryService screenshotLibraryService)
    {
        _backgroundService = backgroundService;
        _profileService = profileService;
        _gameLibraryService = gameLibraryService;
        _screenshotLibraryService = screenshotLibraryService;

        // The constructor stays cheap: profile, library and screenshot loading
        // happen in InitializeAsync, behind the splash screen
        Header = new HeaderViewModel();
        Settings = new SettingsViewModel(backgroundService);
        Library = new LibraryViewModel(Settings);
        Media = new MediaViewModel(Settings, screenshotLibraryService);
        Dashboard = new DashboardViewModel(backgroundService);
        Settings.AppearanceChanged += () => Dashboard.UpdateBackground(_lastSelectedGame?.BackgroundArt);
        Settings.CardImageChanged += () =>
        {
            foreach (GameCardViewModel card in Dashboard.RecentGames)
            {
                card.CardImageMode = Settings.CardImageMode;
            }
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
    /// dashboard, library and media loading with live status/progress,
    /// cancellable between steps.
    /// </summary>
    public async Task InitializeAsync(
        IProgress<(string Status, double Progress)>? progress = null,
        CancellationToken cancellationToken = default)
    {
        // Yield so the splash screen paints its first frame before any work runs
        await Task.Yield();

        await StageAsync(progress, LocalizationHelper.GetText("Splash.LoadingProfile"), 0.10, cancellationToken, () =>
        {
            _profileService.Load();
            Header.ApplyProfile(_profileService);
        });

        // Loads the persisted settings and builds the background from them
        // (Image mode decodes the configured image - too slow for the constructor)
        await StageAsync(progress, LocalizationHelper.GetText("Splash.LoadingSettings"), 0.25, cancellationToken, () =>
        {
            Settings.Load();
            Dashboard.UpdateBackground(null);
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
            if (loadedGames % 10 == 0)
            {
                progress?.Report((LocalizationHelper.GetText("Splash.LoadingLibrary"),
                    0.45 + 0.30 * (double)loadedGames / Math.Max(1, totalGames)));
            }
        }

        await Task.Delay(TimingConstants.StageDwell, cancellationToken);
        await Media.LoadScreenshotsAsync(progress, cancellationToken);
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
            OverlayScreen.Media => Media,
            OverlayScreen.Settings => Settings,
            _ => null,
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
        Logger.Info<MainWindowViewModel>($"Quitting BigScreen (return to Xenia Manager: {Settings.ReturnToXeniaOnQuit})");
        if (Settings.ReturnToXeniaOnQuit)
        {
            string baseExe = Path.Combine(AppPathResolver.BaseDirectory(), AppConstants.BaseAppExecutable);
            if (File.Exists(baseExe) &&
                Process.GetProcessesByName(Path.GetFileNameWithoutExtension(AppConstants.BaseAppExecutable)).Length == 0)
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
    /// Reloads the game library from disk and rebuilds the dashboard/library card
    /// collections so playtime and last-played values reflect the finished session.
    /// Selection is preserved per row, falling back to the first card so a card is
    /// always selected after a refresh. Stats are resolved off the UI thread.
    /// </summary>
    private async Task RefreshLibrary()
    {
        string? librarySelectedId = Library.Games.FirstOrDefault(g => g.IsSelected)?.Game.GameId;
        string? recentSelectedId = Dashboard.RecentGames.FirstOrDefault(g => g.IsSelected)?.Game.GameId;

        _gameLibraryService.Load();
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

        (Library.Games.FirstOrDefault(g => g.Game.GameId == librarySelectedId) ?? Library.Games.FirstOrDefault())?.IsSelected = true;
        (Dashboard.RecentGames.FirstOrDefault(g => g.Game.GameId == recentSelectedId) ?? Dashboard.RecentGames.FirstOrDefault())?.IsSelected = true;

        LibraryRefreshed?.Invoke(this, EventArgs.Empty);
        Logger.Debug<MainWindowViewModel>(
            $"Library refreshed: {Library.Games.Count} games, {Dashboard.RecentGames.Count} recent");
    }

    /// <summary>
    /// Launches the given game via Core's Launcher. Disables the window while the
    /// game runs, then re-enables it and refreshes the library (playtime, last played).
    /// </summary>
    public async Task LaunchGame(GameCardViewModel card)
    {
        Logger.Info<MainWindowViewModel>($"Launching '{card.Game.Title}'");
        try
        {
            EventManager.Instance.DisableWindow();
            Settings settings = new();
            await Launcher.LaunchGameASync(card.Game, settings, discNumber: card.Game.LastPlayedDisc);
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

    private void OnGameCardPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(GameCardViewModel.IsSelected))
        {
            if (sender is GameCardViewModel { IsSelected: true } card)
            {
                _lastSelectedGame = card;
            }
        }

        // Swap behind closed overlays; the library/media screens cover the
        // dashboard, so the fade only matters when the dashboard is visible
        if (e.PropertyName is nameof(GameCardViewModel.IsSelected) or nameof(GameCardViewModel.BackgroundArt)
            && !IsOverlayOpen)
        {
            Dashboard.UpdateBackground(_lastSelectedGame?.BackgroundArt, fade: true);
        }
    }
}
