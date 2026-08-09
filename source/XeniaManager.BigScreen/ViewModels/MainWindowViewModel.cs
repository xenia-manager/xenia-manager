using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using XeniaManager.Core.Files;
using XeniaManager.Core.Manage;
using XeniaManager.Core.Models;
using XeniaManager.Core.Models.Files.Account;
using XeniaManager.Core.Models.Files.Gpd;
using XeniaManager.Core.Models.Game;
using XeniaManager.Core.Models.Items;
using XeniaManager.Core.Utilities;
using XeniaManager.BigScreen.Models;
using XeniaManager.BigScreen.Services;
using XeniaManager.BigScreen.ViewModels.Items;

namespace XeniaManager.BigScreen.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly BackgroundService _backgroundService = new();

    /// <summary>
    /// The brush currently applied to the dashboard background.
    /// </summary>
    [ObservableProperty] private IBrush? _background;

    /// <summary>
    /// The active background mode.
    /// </summary>
    [ObservableProperty] private BackgroundMode _mode = BackgroundMode.LinearGradient;

    /// <summary>
    /// Options shown in the settings background-type dropdown.
    /// </summary>
    public ObservableCollection<BackgroundModeOption> BackgroundModeOptions { get; } =
    [
        new(BackgroundMode.Image, "Image"),
        new(BackgroundMode.Solid, "Solid Colour"),
        new(BackgroundMode.LinearGradient, "Linear Gradient"),
        new(BackgroundMode.RadialGradient, "Radial Gradient"),
        new(BackgroundMode.Dynamic, "Dynamic (Selected Game)"),
    ];

    /// <summary>
    /// The selected option in the background-type dropdown.
    /// </summary>
    [ObservableProperty] private BackgroundModeOption? _selectedBackgroundMode;

    /// <summary>
    /// The primary color; gradients are derived from it.
    /// </summary>
    [ObservableProperty] private Color _primaryColor;

    /// <summary>
    /// The dashboard's accent color (selected card border).
    /// </summary>
    [ObservableProperty] private Color _accentColor;

    /// <summary>
    /// Vignette edge opacity (0-1).
    /// </summary>
    [ObservableProperty] private double _vignetteOpacity;

    /// <summary>
    /// Whether the vignette overlay should be shown. Only for image-based backgrounds
    /// (Image mode, or Dynamic with artwork) - it ruins flat color/gradient backgrounds.
    /// </summary>
    [ObservableProperty] private bool _vignetteVisible;

    /// <summary>
    /// The currently open overlay screen.
    /// </summary>
    [ObservableProperty] private OverlayScreen _currentScreen = OverlayScreen.None;

    /// <summary>
    /// The current library sort mode (cycled with Y).
    /// </summary>
    [ObservableProperty] private LibrarySort _sort = LibrarySort.Alphabetical;

    /// <summary>
    /// Display name of the current library sort mode.
    /// </summary>
    public string SortText => Sort switch
    {
        LibrarySort.TimePlayed => "Time Played",
        LibrarySort.LastPlayed => "Last Played",
        _ => "Alphabetical",
    };

    partial void OnSortChanged(LibrarySort value)
    {
        ApplySort();
        OnPropertyChanged(nameof(SortText));
    }

    /// <summary>
    /// Cycles the library sort mode: Alphabetical → Time Played → Last Played.
    /// </summary>
    public void CycleSort()
    {
        LibrarySort[] modes = Enum.GetValues<LibrarySort>();
        int index = Array.IndexOf(modes, Sort);
        Sort = modes[(index + 1) % modes.Length];
    }

    /// <summary>
    /// Re-sorts the game collection, keeping the currently selected game selected.
    /// </summary>
    private void ApplySort()
    {
        if (Games.Count == 0)
        {
            return;
        }

        GameCardViewModel? selected = Games.FirstOrDefault(g => g.IsSelected);
        List<GameCardViewModel> sorted = Sort switch
        {
            LibrarySort.TimePlayed => Games.OrderByDescending(g => g.Game.Playtime).ToList(),
            LibrarySort.LastPlayed => Games.OrderByDescending(g => g.Game.LastPlayed).ToList(),
            _ => Games.OrderBy(g => g.Game.Title, StringComparer.OrdinalIgnoreCase).ToList(),
        };

        Games.Clear();
        foreach (GameCardViewModel game in sorted)
        {
            Games.Add(game);
        }

        if (selected != null)
        {
            selected.IsSelected = true;
        }
    }

    /// <summary>
    /// Whether any overlay is currently open.
    /// </summary>
    public bool IsOverlayOpen => CurrentScreen != OverlayScreen.None;

    /// <summary>
    /// Whether the library overlay is open.
    /// </summary>
    public bool IsLibraryScreen => CurrentScreen == OverlayScreen.Library;

    /// <summary>
    /// Whether the media overlay is open.
    /// </summary>
    public bool IsMediaScreen => CurrentScreen == OverlayScreen.Media;

    /// <summary>
    /// Whether the settings overlay is open.
    /// </summary>
    public bool IsSettingsScreen => CurrentScreen == OverlayScreen.Settings;

    /// <summary>
    /// The current media sort mode (cycled with Y).
    /// </summary>
    [ObservableProperty] private MediaSort _mediaSort = MediaSort.NewestFirst;

    /// <summary>
    /// Display name of the current media sort mode.
    /// </summary>
    public string MediaSortText => MediaSort switch
    {
        MediaSort.OldestFirst => "Oldest First",
        MediaSort.ByGame => "By Game",
        _ => "Newest First",
    };

    partial void OnMediaSortChanged(MediaSort value)
    {
        ApplyMediaSort();
        OnPropertyChanged(nameof(MediaSortText));
    }

    /// <summary>
    /// Cycles the media sort mode: Newest First → Oldest First → By Game.
    /// </summary>
    public void CycleMediaSort()
    {
        MediaSort[] modes = Enum.GetValues<MediaSort>();
        int index = Array.IndexOf(modes, MediaSort);
        MediaSort = modes[(index + 1) % modes.Length];
    }

    /// <summary>
    /// Re-sorts the screenshot collection, keeping the selected screenshot selected.
    /// </summary>
    private void ApplyMediaSort()
    {
        if (Screenshots.Count == 0)
        {
            return;
        }

        ScreenshotItemViewModel? selected = Screenshots.FirstOrDefault(s => s.IsSelected);
        List<ScreenshotItemViewModel> sorted = MediaSort switch
        {
            MediaSort.OldestFirst => Screenshots.OrderBy(s => s.CapturedAt).ToList(),
            MediaSort.ByGame => Screenshots.OrderBy(s => s.GameTitle).ThenByDescending(s => s.CapturedAt).ToList(),
            _ => Screenshots.OrderByDescending(s => s.CapturedAt).ToList(),
        };

        Screenshots.Clear();
        foreach (ScreenshotItemViewModel screenshot in sorted)
        {
            Screenshots.Add(screenshot);
        }

        if (selected != null)
        {
            selected.IsSelected = true;
        }
    }

    /// <summary>
    /// Raised when the user chooses to quit BigScreen.
    /// </summary>
    public event EventHandler? QuitRequested;

    partial void OnCurrentScreenChanged(OverlayScreen value)
    {
        OnPropertyChanged(nameof(IsOverlayOpen));
        OnPropertyChanged(nameof(IsLibraryScreen));
        OnPropertyChanged(nameof(IsMediaScreen));
        OnPropertyChanged(nameof(IsSettingsScreen));
    }

    /// <summary>
    /// Gamertag of the active profile (Canary)
    /// </summary>
    [ObservableProperty] private string _gamertag = "Guest";

    /// <summary>
    /// Total gamerscore of the active profile
    /// </summary>
    [ObservableProperty] private string _gamerscore = "0";

    /// <summary>
    /// Whether a controller is connected
    /// </summary>
    [ObservableProperty] private bool _controllerConnected = true;

    /// <summary>
    /// Controller battery level in percent
    /// </summary>
    [ObservableProperty] private int _batteryLevel = 100;

    /// <summary>
    /// Whether the controller battery is charging
    /// </summary>
    [ObservableProperty] private bool _isCharging;

    /// <summary>
    /// Whether wifi is connected
    /// </summary>
    [ObservableProperty] private bool _isWifiConnected = true;

    /// <summary>
    /// Current time string
    /// </summary>
    [ObservableProperty] private string _time = DateTime.Now.ToString("hh:mm tt");

    /// <summary>
    /// Fluent icon name for the current wifi state
    /// </summary>
    public string WifiIcon => IsWifiConnected ? "WiFi3" : "WiFiOff";

    /// <summary>
    /// Fluent icon name for the current controller battery state
    /// </summary>
    public string BatteryIcon => IsCharging
        ? "BatteryCharge"
        : BatteryLevel switch
        {
            <= 0 => "Battery0",
            <= 20 => "Battery1",
            <= 40 => "Battery3",
            <= 60 => "Battery5",
            <= 80 => "Battery7",
            _ => "Battery10",
        };

    partial void OnIsWifiConnectedChanged(bool value) => OnPropertyChanged(nameof(WifiIcon));

    partial void OnBatteryLevelChanged(int value) => OnPropertyChanged(nameof(BatteryIcon));

    partial void OnIsChargingChanged(bool value) => OnPropertyChanged(nameof(BatteryIcon));

    partial void OnModeChanged(BackgroundMode value)
    {
        _backgroundService.Settings.Mode = value;
        _backgroundService.Save();
        UpdateBackground();
        SelectedBackgroundMode = BackgroundModeOptions.FirstOrDefault(o => o.Mode == value);
    }

    partial void OnSelectedBackgroundModeChanged(BackgroundModeOption? value)
    {
        if (value != null)
        {
            Mode = value.Mode;
        }
    }

    partial void OnPrimaryColorChanged(Color value)
    {
        _backgroundService.Settings.PrimaryColor = value;
        _backgroundService.Save();
        UpdateBackground();
        OnPropertyChanged(nameof(ScreenBackground));
    }

    partial void OnAccentColorChanged(Color value)
    {
        _backgroundService.Settings.AccentColor = value;
        _backgroundService.Save();
    }

    partial void OnVignetteOpacityChanged(double value)
    {
        _backgroundService.Settings.VignetteOpacity = value;
        _backgroundService.Save();
        OnPropertyChanged(nameof(VignetteText));
    }

    /// <summary>
    /// Brush used as the overlay/menu background, derived from the primary colour
    /// so menus match the dashboard instead of being pitch black.
    /// </summary>
    public IBrush ScreenBackground => new SolidColorBrush(PrimaryColor);

    private readonly DispatcherTimer _clockTimer;

    /// <summary>
    /// All games in the library (library carousel).
    /// </summary>
    public ObservableCollection<GameCardViewModel> Games { get; } = [];

    /// <summary>
    /// The first 6 games, shown on the dashboard.
    /// </summary>
    public ObservableCollection<GameCardViewModel> RecentGames { get; } = [];

    /// <summary>
    /// Whether the dashboard shows the disc stub (no games in the library).
    /// </summary>
    public bool ShowEmptyStub => RecentGames.Count == 0;

    /// <summary>
    /// All screenshots in the media gallery.
    /// </summary>
    public ObservableCollection<ScreenshotItemViewModel> Screenshots { get; } = [];

    /// <summary>
    /// Whether the media overlay shows the "no screenshots" stub.
    /// </summary>
    public bool ShowEmptyScreenshots => Screenshots.Count == 0;

    /// <summary>
    /// The screenshot currently shown in the modal viewer, or null when it is closed.
    /// </summary>
    [ObservableProperty] private ScreenshotItemViewModel? _selectedScreenshot;

    /// <summary>
    /// Whether the full-screen screenshot viewer is open.
    /// </summary>
    public bool IsMediaViewerOpen => SelectedScreenshot != null;

    /// <summary>
    /// Opens the modal viewer for the given screenshot.
    /// </summary>
    public void OpenScreenshot(ScreenshotItemViewModel screenshot)
    {
        SelectedScreenshot = screenshot;
    }

    /// <summary>
    /// Closes the modal screenshot viewer.
    /// </summary>
    public void CloseMediaViewer()
    {
        SelectedScreenshot = null;
    }

    /// <summary>
    /// Moves the modal viewer to the neighbouring screenshot, clamped at both ends.
    /// </summary>
    public void StepScreenshot(int delta)
    {
        if (SelectedScreenshot == null || Screenshots.Count == 0)
        {
            return;
        }

        int index = Screenshots.IndexOf(SelectedScreenshot);
        if (index < 0)
        {
            return;
        }

        int target = Math.Clamp(index + delta, 0, Screenshots.Count - 1);
        if (target != index)
        {
            SelectedScreenshot = Screenshots[target];
        }
    }

    partial void OnSelectedScreenshotChanged(ScreenshotItemViewModel? value)
    {
        OnPropertyChanged(nameof(IsMediaViewerOpen));
        OnPropertyChanged(nameof(HasPrevious));
        OnPropertyChanged(nameof(HasNext));
    }

    /// <summary>
    /// Whether the modal viewer can step to the previous screenshot.
    /// </summary>
    public bool HasPrevious => SelectedScreenshot != null
                               && Screenshots.IndexOf(SelectedScreenshot) > 0;

    /// <summary>
    /// Whether the modal viewer can step to the next screenshot.
    /// </summary>
    public bool HasNext => SelectedScreenshot != null
                           && Screenshots.IndexOf(SelectedScreenshot) < Screenshots.Count - 1;

    public ObservableCollection<OptionsCardViewModel> Options { get; } =
    [
        new("Library", "Games", OverlayScreen.Library),
        new("Media", "Library", OverlayScreen.Media),
        new("Settings", "Settings", OverlayScreen.Settings),
        new("Quit", "Power", OverlayScreen.None),
    ];

    public MainWindowViewModel()
    {
        LoadProfile();
        LoadLibrary();
        Games.CollectionChanged += (_, _) => OnPropertyChanged(nameof(ShowEmptyStub));
        Screenshots.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(ShowEmptyScreenshots));
            OnPropertyChanged(nameof(HasPrevious));
            OnPropertyChanged(nameof(HasNext));
        };

        _backgroundService.Load();
        Mode = _backgroundService.Settings.Mode;
        SelectedBackgroundMode = BackgroundModeOptions.FirstOrDefault(o => o.Mode == Mode);
        PrimaryColor = _backgroundService.Settings.PrimaryColor;
        AccentColor = _backgroundService.Settings.AccentColor;
        VignetteOpacity = _backgroundService.Settings.VignetteOpacity;
        UpdateBackground();

        foreach (GameCardViewModel game in Games.Concat(RecentGames))
        {
            game.PropertyChanged += OnGameCardPropertyChanged;
        }

        LoadProfile();
        _clockTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1),
        };
        _clockTimer.Tick += (_, _) => Time = DateTime.Now.ToString("hh:mm tt");
        _clockTimer.Start();
    }

    /// <summary>
    /// Display name of the current background mode.
    /// </summary>
    public string ModeText => Mode switch
    {
        BackgroundMode.Image => "Image",
        BackgroundMode.Solid => "Solid Colour",
        BackgroundMode.LinearGradient => "Linear Gradient",
        BackgroundMode.RadialGradient => "Radial Gradient",
        BackgroundMode.Dynamic => "Dynamic (Selected Game)",
        _ => "Linear Gradient",
    };

    /// <summary>
    /// Display text for the vignette opacity as a percentage.
    /// </summary>
    public string VignetteText => $"{Math.Round(VignetteOpacity * 100)}%";

    /// <summary>
    /// Display text for the currently configured background image.
    /// </summary>
    public string ImageDisplayText => string.IsNullOrEmpty(_backgroundService.Settings.ImagePath)
        ? "None"
        : System.IO.Path.GetFileName(_backgroundService.Settings.ImagePath);

    /// <summary>
    /// Cycles the background mode by the given step.
    /// </summary>
    public void CycleMode(int delta)
    {
        BackgroundMode[] modes = Enum.GetValues<BackgroundMode>();
        int index = Array.IndexOf(modes, Mode);
        Mode = modes[(index + delta + modes.Length) % modes.Length];
    }

    /// <summary>
    /// Cycles the primary color through the given palette by the given step.
    /// </summary>
    public void CyclePrimaryColor(int delta, Color[] palette)
    {
        int index = Array.IndexOf(palette, PrimaryColor);
        if (index < 0)
        {
            index = 0;
        }
        PrimaryColor = palette[(index + delta + palette.Length) % palette.Length];
    }

    /// <summary>
    /// Cycles the accent color through the given palette by the given step.
    /// </summary>
    public void CycleAccentColor(int delta, Color[] palette)
    {
        int index = Array.IndexOf(palette, AccentColor);
        if (index < 0)
        {
            index = 1;
        }
        AccentColor = palette[(index + delta + palette.Length) % palette.Length];
    }

    /// <summary>
    /// Steps the vignette opacity by the given direction (0.05 per step), clamped to 0-1.
    /// </summary>
    public void AdjustVignette(int delta)
    {
        VignetteOpacity = Math.Clamp(VignetteOpacity + delta * 0.05, 0, 1);
    }

    /// <summary>
    /// Opens the given overlay screen.
    /// </summary>
    public void OpenScreen(OverlayScreen screen)
    {
        CurrentScreen = screen;
    }

    /// <summary>
    /// Closes the currently open overlay screen.
    /// </summary>
    public void CloseOverlay()
    {
        CurrentScreen = OverlayScreen.None;
    }

    /// <summary>
    /// Requests the app to quit (returns to regular Xenia Manager).
    /// </summary>
    public void Quit()
    {
        QuitRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Sets a custom image path and switches to image background mode.
    /// </summary>
    public void SetBackgroundImage(string path)
    {
        _backgroundService.Settings.ImagePath = path;
        _backgroundService.Settings.Mode = BackgroundMode.Image;
        _backgroundService.Save();
        Mode = BackgroundMode.Image;
        UpdateBackground();
        OnPropertyChanged(nameof(ImageDisplayText));
    }

    /// <summary>
    /// Recomputes the background brush from the current settings and selection.
    /// Falls back to the linear gradient when the requested brush can't be built.
    /// </summary>
    private void UpdateBackground()
    {
        Bitmap? art = RecentGames.FirstOrDefault(g => g.IsSelected)?.BackgroundArt;
        BackgroundMode mode = _backgroundService.Settings.Mode;
        IBrush? brush = _backgroundService.GetBackground(art);
        if (brush == null)
        {
            _backgroundService.Settings.Mode = BackgroundMode.LinearGradient;
            brush = _backgroundService.GetBackground(null);
        }

        // Vignette only belongs on image-based backgrounds
        VignetteVisible = mode == BackgroundMode.Image
                          || (mode == BackgroundMode.Dynamic && art != null);
        Background = brush;
    }

    private void OnGameCardPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(GameCardViewModel.IsSelected) or nameof(GameCardViewModel.BackgroundArt))
        {
            UpdateBackground();
        }
    }

    /// <summary>
    /// Loads the game library from Core and populates the dashboard game cards,
    /// attaching each game's achievement stats from the loaded profile GPD.
    /// </summary>
    private void LoadLibrary()
    {
        GameManager.LoadLibrary();
        foreach (Game game in GameManager.Games)
        {
            Games.Add(new GameCardViewModel(game, FindTitleEntry(game)));
        }

        foreach (Game game in GameManager.Games.Take(6))
        {
            RecentGames.Add(new GameCardViewModel(game, FindTitleEntry(game)));
        }
    }

    /// <summary>
    /// Image extensions recognized as screenshots.
    /// </summary>
    private static readonly string[] ImageExtensions = [".png", ".jpg", ".jpeg", ".bmp", ".webp", ".gif"];

    private bool _screenshotsLoaded;

    /// <summary>
    /// Scans the Canary screenshots folder once (recursively, per-game subfolders)
    /// and fills the media gallery, applying the current sort.
    /// </summary>
    public void EnsureScreenshotsLoaded()
    {
        if (_screenshotsLoaded)
        {
            return;
        }

        _screenshotsLoaded = true;
        string screenshotsFolder = AppPathResolver.GetFullPath(
            XeniaVersionInfo.GetXeniaVersionInfo(XeniaVersion.Canary).ScreenshotsFolderLocation);

        if (!Directory.Exists(screenshotsFolder))
        {
            return;
        }

        List<ScreenshotItemViewModel> screenshots = [];
        foreach (string file in Directory.EnumerateFiles(screenshotsFolder, "*", SearchOption.AllDirectories)
                     .Where(f => ImageExtensions.Contains(Path.GetExtension(f).ToLowerInvariant())))
        {
            try
            {
                screenshots.Add(new ScreenshotItemViewModel(
                    file,
                    Path.GetFileName(file),
                    File.GetLastWriteTime(file),
                    ResolveGameTitle(Path.GetFileName(Path.GetDirectoryName(file) ?? string.Empty)),
                    new Bitmap(file)));
            }
            catch (Exception)
            {
                // Skip unreadable images
            }
        }

        Screenshots.Clear();
        foreach (ScreenshotItemViewModel screenshot in screenshots)
        {
            Screenshots.Add(screenshot);
        }

        ApplyMediaSort();
    }

    /// <summary>
    /// Matches a screenshot's parent folder name (a game ID) to a library game title.
    /// </summary>
    private string ResolveGameTitle(string gameId)
    {
        if (string.IsNullOrEmpty(gameId))
        {
            return "Unknown Game";
        }

        return GameManager.Games
            .FirstOrDefault(g => g.GameId.Equals(gameId, StringComparison.OrdinalIgnoreCase))?.Title
            ?? gameId;
    }

    /// <summary>
    /// Finds the profile GPD title entry (achievements/gamerscore) for the given game.
    /// </summary>
    private TitleEntry? FindTitleEntry(Game game)
    {
        if (_profileGpd == null ||
            !uint.TryParse(game.GameId, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint titleId))
        {
            return null;
        }

        return _profileGpd.Titles.FirstOrDefault(t => t.TitleId == titleId);
    }

    /// <summary>
    /// The profile GPD of the active Canary profile, used for per-game achievement stats.
    /// </summary>
    private GpdFile? _profileGpd;

    /// <summary>
    /// Loads the first available Canary profile and its gamerscore from the profile GPD
    /// </summary>
    private void LoadProfile()
    {
        try
        {
            System.Collections.Generic.List<AccountInfo> profiles = ProfileManager.LoadProfiles(XeniaVersion.Canary);
            AccountInfo? profile = profiles.FirstOrDefault();
            if (profile == null)
            {
                return;
            }

            Gamertag = profile.Gamertag;

            try
            {
                AccountContent content = new(profile, XeniaVersion.Canary, "FFFE07D1");
                if (content.ProfileGpd != null)
                {
                    _profileGpd = content.ProfileGpd;
                    Gamerscore = content.ProfileGpd.Titles.Sum(t => t.GamerscoreUnlocked).ToString();
                }
            }
            catch (Exception)
            {
                // Profile GPD missing or unreadable - keep gamerscore at 0
            }
        }
        catch (Exception)
        {
            // No profiles found - keep defaults
        }
    }
}
