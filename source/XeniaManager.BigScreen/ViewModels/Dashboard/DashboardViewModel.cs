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
using TweenAvalonia;

namespace XeniaManager.BigScreen.ViewModels.Dashboard;

/// <summary>
/// Dashboard state: recent games, option cards, the static background brush and
/// the fading dynamic artwork layer. The artwork crossfade runs here on the
/// bound <see cref="ArtOpacity"/> value; the view just binds it to the layer.
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
    /// (Image mode, or Dynamic with artwork) - it ruins flat colour/gradient backgrounds.
    /// </summary>
    [ObservableProperty]
    public partial bool VignetteVisible { get; set; }

    [ObservableProperty]
    public partial bool IsGameRowFocused { get; set; } = true;
    
    [ObservableProperty]
    public partial double CardSpacing { get; set; } = LayoutConstants.DashboardCardSpacing;

    /// <summary>
    /// The in-flight artwork fade; stopped and replaced on every art swap so
    /// only one fade ever plays (latest request wins).
    /// </summary>
    private Tween _artFade;

    /// <summary>
    /// The artwork queued for the fade-in leg of the crossfade; committed by
    /// <see cref="CommitArtwork"/> once the layer has faded out.
    /// </summary>
    private Bitmap? _pendingArtwork;

    /// <summary>
    /// The first 8 games, shown on the dashboard.
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
        RecentGames.CollectionChanged += (_, _) => OnPropertyChanged(nameof(ShowEmptyStub));
    }
    
    /// <summary>
    /// Fades the artwork layer opacity to <paramref name="to"/>, starting from its
    /// current value. Target-based on this view model, so the callback is cached
    /// and allocation-free.
    /// </summary>
    private Tween FadeArtOpacity(double to) =>
        Tween.Custom(this, ArtOpacity, to, static (vm, v) => vm.ArtOpacity = v, TimingConstants.ArtFadeDuration);

    private void UpdateCardSpacing(bool focus)
    {
        CardSpacing = focus ? LayoutConstants.DashboardCardSpacing : LayoutConstants.DashboardCardSpacingUnfocused;
        Logger.Debug<DashboardViewModel>($"Card spacing: {CardSpacing}");
    }
    
    partial void OnIsGameRowFocusedChanged(bool value)
    {
        UpdateCardSpacing(value);
    }
    
    /// <summary>
    /// Swaps in the queued artwork and fades the layer back in. Runs when the
    /// fade-out leg completes naturally.
    /// </summary>
    private void CommitArtwork()
    {
        Artwork = _pendingArtwork;
        ArtOpacity = 0;
        _artFade = FadeArtOpacity(1);
    }

    /// <summary>
    /// Resolves the static base brush, falling back to a linear gradient when
    /// the configured background can't be built.
    /// </summary>
    private IBrush ResolveBackgroundBrush()
    {
        IBrush? brush = _backgroundService.GetBackground(null);
        if (brush != null)
        {
            return brush;
        }

        _backgroundService.Settings.Mode = BackgroundMode.LinearGradient;
        return _backgroundService.GetBackground(null)!;
    }

    /// <summary>
    /// Whether the vignette overlay belongs on the given background: image
    /// backgrounds always, dynamic backgrounds only while artwork is shown.
    /// </summary>
    private static bool ShouldShowVignette(BackgroundMode mode, bool hasArtwork) =>
        mode == BackgroundMode.Image || (mode == BackgroundMode.Dynamic && hasArtwork);

    /// <summary>
    /// Updates the fading artwork layer: hidden without artwork, fully visible
    /// when the same artwork is already shown, crossfaded when fading is
    /// requested and swapped instantly otherwise.
    /// </summary>
    private void UpdateArtworkLayer(Bitmap? newArt, bool hasArtwork, bool fade)
    {
        if (!hasArtwork)
        {
            _artFade.Stop();
            Artwork = null;
            ArtOpacity = 0;
            return;
        }

        if (ReferenceEquals(newArt, Artwork))
        {
            _artFade.Stop();
            ArtOpacity = 1;
            return;
        }

        if (fade)
        {
            _artFade.Stop();
            _pendingArtwork = newArt;
            _artFade = FadeArtOpacity(0).OnComplete(target: this, static t => t.CommitArtwork());
            return;
        }

        _artFade.Stop();
        Artwork = newArt;
        ArtOpacity = 1;
    }

    /// <summary>
    /// Recomputes the background from the current settings and selection. In Dynamic
    /// mode the artwork crossfades on its own layer above the static base (no black);
    /// all other modes and settings changes swap instantly.
    /// </summary>
    public void UpdateBackground(Bitmap? selectedArt, bool fade = false)
    {
        BackgroundMode mode = _backgroundService.Settings.Mode;
        bool hasArtwork = mode == BackgroundMode.Dynamic && selectedArt != null;

        Background = ResolveBackgroundBrush();
        VignetteVisible = ShouldShowVignette(mode, hasArtwork);
        UpdateArtworkLayer(selectedArt, hasArtwork, fade);

        Logger.Debug<DashboardViewModel>(
            $"Background updated: mode={_backgroundService.Settings.Mode}, art={(hasArtwork ? "game art" : "none")}");
    }
}