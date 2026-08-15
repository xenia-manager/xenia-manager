using System;
using System.Collections.ObjectModel;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using XeniaManager.BigScreen.Constants;
using XeniaManager.BigScreen.Models;
using XeniaManager.BigScreen.Models.Settings;
using XeniaManager.BigScreen.Services;
using XeniaManager.BigScreen.ViewModels.Items;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Tweening;

namespace XeniaManager.BigScreen.ViewModels.Dashboard;

/// <summary>
/// Dashboard state: recent games, option cards, the static background brush and
/// the fading dynamic artwork layer.
/// </summary>
public partial class DashboardViewModel : ViewModelBase
{
    private readonly IBackgroundService _backgroundService;

    /// <summary>
    /// The static base brush (solid/gradient/image) always applied to the window
    /// background; dynamic artwork fades on its own layer above it.
    /// </summary>
    [ObservableProperty]
    public partial IBrush? Background { get; set; }

    /// <summary>
    /// The artwork currently shown on the fading image layer (Dynamic mode only).
    /// </summary>
    [ObservableProperty]
    public partial Bitmap? Artwork { get; set; }

    /// <summary>
    /// Opacity of the artwork layer (0 = hidden, 1 = fully visible).
    /// </summary>
    [ObservableProperty]
    public partial double ArtOpacity { get; set; }

    /// <summary>
    /// Whether the vignette overlay should be shown. Only for image-based backgrounds
    /// (Image mode, or Dynamic with artwork) - it ruins flat color/gradient backgrounds.
    /// </summary>
    [ObservableProperty]
    public partial bool VignetteVisible { get; set; }

    /// <summary>
    /// The in-flight artwork fade; stopped and replaced on every art swap so
    /// only one fade ever plays (latest request wins).
    /// </summary>
    private Tween _artFade;

    /// <summary>
    /// Cached tween writer for <see cref="ArtOpacity"/> (avoids a closure
    /// allocation on every fade start).
    /// </summary>
    private readonly Action<double> _setArtOpacity;

    /// <summary>
    /// The first 6 games, shown on the dashboard.
    /// </summary>
    public ObservableCollection<GameCardViewModel> RecentGames { get; } = [];

    /// <summary>
    /// Whether the dashboard shows the disc stub (no games in the library).
    /// </summary>
    public bool ShowEmptyStub => RecentGames.Count == 0;

    public ObservableCollection<OptionsCardViewModel> Options { get; } =
    [
        new("Library", "Games", OverlayScreen.Library),
        new("Gallery", "Library", OverlayScreen.Gallery),
        new("Settings", "Settings", OverlayScreen.Settings),
        new("Quit", "Power", OverlayScreen.None)
    ];

    public DashboardViewModel(IBackgroundService backgroundService)
    {
        _backgroundService = backgroundService;
        _setArtOpacity = value => ArtOpacity = value;
        RecentGames.CollectionChanged += (_, _) => OnPropertyChanged(nameof(ShowEmptyStub));
    }

    /// <summary>
    /// Fades the artwork layer opacity to <paramref name="to"/>, starting from its
    /// current value.
    /// </summary>
    private Tween FadeArtOpacity(double to) =>
        Tween.Custom(ArtOpacity, to, TimingConstants.ArtFadeDuration, _setArtOpacity, Tween.DefaultEasing);

    /// <summary>
    /// Recomputes the background from the current settings and selection. In Dynamic
    /// mode the artwork crossfades on its own layer above the static base (no black);
    /// all other modes and settings changes swap instantly.
    /// </summary>
    public void UpdateBackground(Bitmap? selectedArt, bool fade = false)
    {
        BackgroundMode mode = _backgroundService.Settings.Mode;
        IBrush? brush = _backgroundService.GetBackground(null);
        if (brush == null)
        {
            _backgroundService.Settings.Mode = BackgroundMode.LinearGradient;
            brush = _backgroundService.GetBackground(null);
        }

        Background = brush;

        // Vignette only belongs on image-based backgrounds
        VignetteVisible = mode == BackgroundMode.Image
                          || (mode == BackgroundMode.Dynamic && selectedArt != null);

        Bitmap? newArt = mode == BackgroundMode.Dynamic ? selectedArt : null;
        if (newArt == null)
        {
            // Nothing to show on the artwork layer - hide it instantly
            _artFade.Stop();
            Artwork = null;
            ArtOpacity = 0;
        }
        else if (ReferenceEquals(newArt, Artwork))
        {
            // Already displayed - cancel any lingering fade and show it fully
            _artFade.Stop();
            ArtOpacity = 1;
        }
        else if (fade)
        {
            // Supersede any in-flight fade, then fade the old artwork out (the
            // static base shows through), swap, and fade the new artwork in.
            _artFade.Stop();
            _artFade = FadeArtOpacity(0).OnComplete(() =>
            {
                Artwork = newArt;
                ArtOpacity = 0;
                _artFade = FadeArtOpacity(1);
            });
        }
        else
        {
            // Instant swap (settings changes)
            _artFade.Stop();
            Artwork = newArt;
            ArtOpacity = 1;
        }

        Logger.Debug<DashboardViewModel>(
            $"Background updated: mode={_backgroundService.Settings.Mode}, art={(newArt != null ? "game art" : "none")}");
    }
}