using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using XeniaManager.Core.Files;
using XeniaManager.Core.Manage;
using XeniaManager.Core.Models;
using XeniaManager.Core.Models.Files.Account;
using XeniaManager.Core.Models.Items;
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
    }

    partial void OnPrimaryColorChanged(Color value)
    {
        _backgroundService.Settings.PrimaryColor = value;
        _backgroundService.Save();
        UpdateBackground();
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
    }

    private readonly DispatcherTimer _clockTimer;

    public ObservableCollection<GameCardViewModel> Games { get; } =
    [
        new("Halo 3"),
        new("Forza Motorsport 3"),
        new("Gears of War 2"),
        new("Mass Effect 2"),
        new("Red Dead Redemption"),
        new("Alan Wake"),
    ];

    public ObservableCollection<OptionsCardViewModel> Options { get; } =
    [
        new("Library", "Games"),
        new("Media", "Library"),
        new("Settings", "Settings"),
        new("Quit", "Power"),
    ];

    public MainWindowViewModel()
    {
        _backgroundService.Load();
        Mode = _backgroundService.Settings.Mode;
        PrimaryColor = _backgroundService.Settings.PrimaryColor;
        AccentColor = _backgroundService.Settings.AccentColor;
        VignetteOpacity = _backgroundService.Settings.VignetteOpacity;
        UpdateBackground();

        foreach (GameCardViewModel game in Games)
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
    /// Sets a custom image path and switches to image background mode.
    /// </summary>
    public void SetBackgroundImage(string path)
    {
        _backgroundService.Settings.ImagePath = path;
        _backgroundService.Settings.Mode = BackgroundMode.Image;
        _backgroundService.Save();
        Mode = BackgroundMode.Image;
        UpdateBackground();
    }

    /// <summary>
    /// Recomputes the background brush from the current settings and selection.
    /// Falls back to the linear gradient when the requested brush can't be built.
    /// </summary>
    private void UpdateBackground()
    {
        Bitmap? art = Games.FirstOrDefault(g => g.IsSelected)?.BackgroundArt;
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
