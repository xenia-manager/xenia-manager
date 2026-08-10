using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
    private readonly BackgroundService _backgroundService = new();
    private readonly ProfileService _profileService = new();
    private readonly ScreenshotLibraryService _screenshotLibraryService = new();

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

    public MainWindowViewModel()
    {
        Header = new HeaderViewModel(_profileService);
        Settings = new SettingsViewModel(_backgroundService);
        Library = new LibraryViewModel(Settings);
        Media = new MediaViewModel(Settings, _screenshotLibraryService);
        Dashboard = new DashboardViewModel(_backgroundService);
        Settings.AppearanceChanged += () => Dashboard.UpdateBackground(_lastSelectedGame?.BackgroundArt);
        LoadLibrary();

        // Pre-warm the first library game's background art so the first library
        // open doesn't pay a synchronous image decode (the splash screen will
        // hide this boot-time work)
        Library.Games.FirstOrDefault()?.EnsureBackgroundLoaded();

        Dashboard.UpdateBackground(null);

        foreach (GameCardViewModel game in Library.Games.Concat(Dashboard.RecentGames))
        {
            game.PropertyChanged += OnGameCardPropertyChanged;
        }
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
        CurrentScreen = null;
        Dashboard.UpdateBackground(_lastSelectedGame?.BackgroundArt, fade: true);
    }

    /// <summary>
    /// Requests the app to quit. When returning to Xenia Manager is enabled and the
    /// base app isn't running, it is launched first; BigScreen then closes.
    /// </summary>
    public void Quit()
    {
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
    private GameCardViewModel CreateGameCard(Game game)
    {
        GameCardViewModel card = new(game, _profileService.GetGameStats(game));
        card.PropertyChanged += OnGameCardPropertyChanged;
        return card;
    }

    /// <summary>
    /// Creates a dashboard game card, pre-loading its background art so the
    /// dynamic background has something to show immediately.
    /// </summary>
    private GameCardViewModel CreateRecentGameCard(Game game)
    {
        GameCardViewModel card = CreateGameCard(game);
        card.EnsureBackgroundLoaded();
        return card;
    }

    /// <summary>
    /// Loads the game library from Core and populates the library and dashboard
    /// game cards, attaching each game's achievement stats from the loaded profile GPD.
    /// </summary>
    private void LoadLibrary()
    {
        GameLibraryService.Load();
        foreach (Game game in GameLibraryService.Games)
        {
            Library.Games.Add(CreateGameCard(game));
        }

        foreach (Game game in GameLibraryService.GetRecentGames(AppConstants.RecentGamesLimit))
        {
            Dashboard.RecentGames.Add(CreateRecentGameCard(game));
        }
    }

    /// <summary>
    /// Reloads the game library from disk and rebuilds the dashboard/library card
    /// collections so playtime and last-played values reflect the finished session.
    /// Selection is preserved per row, falling back to the first card so a card is
    /// always selected after a refresh.
    /// </summary>
    private void RefreshLibrary()
    {
        string? librarySelectedId = Library.Games.FirstOrDefault(g => g.IsSelected)?.Game.GameId;
        string? recentSelectedId = Dashboard.RecentGames.FirstOrDefault(g => g.IsSelected)?.Game.GameId;

        GameLibraryService.Load();
        Library.Games.Clear();
        Dashboard.RecentGames.Clear();

        foreach (Game game in GameLibraryService.Games)
        {
            Library.Games.Add(CreateGameCard(game));
        }

        foreach (Game game in GameLibraryService.GetRecentGames(AppConstants.RecentGamesLimit))
        {
            Dashboard.RecentGames.Add(CreateRecentGameCard(game));
        }

        (Library.Games.FirstOrDefault(g => g.Game.GameId == librarySelectedId) ?? Library.Games.FirstOrDefault())?.IsSelected = true;
        (Dashboard.RecentGames.FirstOrDefault(g => g.Game.GameId == recentSelectedId) ?? Dashboard.RecentGames.FirstOrDefault())?.IsSelected = true;

        LibraryRefreshed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Launches the given game via Core's Launcher. Disables the window while the
    /// game runs, then re-enables it and refreshes the library (playtime, last played).
    /// </summary>
    public async Task LaunchGame(GameCardViewModel card)
    {
        try
        {
            EventManager.Instance.DisableWindow();
            Settings settings = new();
            await Launcher.LaunchGameASync(card.Game, settings, discNumber: card.Game.LastPlayedDisc);
        }
        catch (Exception ex)
        {
            Logger.Error<MainWindowViewModel>($"Failed to launch '{card.Game.Title}'");
            Logger.LogExceptionDetails<MainWindowViewModel>(ex);
        }
        finally
        {
            EventManager.Instance.EnableWindow();
            RefreshLibrary();
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
