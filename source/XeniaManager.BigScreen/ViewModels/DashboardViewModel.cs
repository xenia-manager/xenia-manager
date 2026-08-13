using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using XeniaManager.BigScreen.Constants;
using XeniaManager.BigScreen.Models;
using XeniaManager.BigScreen.Services;
using XeniaManager.BigScreen.ViewModels.Items;
using XeniaManager.Core.Logging;

namespace XeniaManager.BigScreen.ViewModels;

/// <summary>
/// Dashboard state: recent games, option cards and the background brush
/// (including the fade-through-black transition).
/// </summary>
public partial class DashboardViewModel : ViewModelBase
{
    private readonly IBackgroundService _backgroundService;

    /// <summary>
    /// The brush currently applied to the dashboard background.
    /// </summary>
    [ObservableProperty] private IBrush? _background;

    /// <summary>
    /// Whether the vignette overlay should be shown. Only for image-based backgrounds
    /// (Image mode, or Dynamic with artwork) - it ruins flat color/gradient backgrounds.
    /// </summary>
    [ObservableProperty] private bool _vignetteVisible;

    /// <summary>
    /// Opacity of the black fade overlay (0 = transparent, 1 = black). Used to
    /// fade the background out to black and back in when the selected game changes.
    /// </summary>
    [ObservableProperty] private double _fadeOpacity;

    /// <summary>
    /// Cancels in-flight background fades; only the newest request completes.
    /// </summary>
    private int _fadeGeneration;

    /// <summary>
    /// The artwork of the currently displayed background, used to skip pointless
    /// fades when the selection didn't actually change the image.
    /// </summary>
    private Bitmap? _currentBackgroundArt;

    /// <summary>
    /// The artwork of the background update in flight (set before the fade check).
    /// </summary>
    private Bitmap? _pendingArt;

    /// <summary>
    /// Whether the in-flight update requested a fade-through-black.
    /// </summary>
    private bool _fadeRequested;

    /// <summary>
    /// Whether the in-flight update should animate through black: a fade was
    /// requested, the mode is Dynamic and the artwork actually changed.
    /// </summary>
    private bool ShouldFadeBackground => _fadeRequested
        && _backgroundService.Settings.Mode == BackgroundMode.Dynamic
        && !ReferenceEquals(_pendingArt, _currentBackgroundArt);

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
        new("Media", "Library", OverlayScreen.Media),
        new("Settings", "Settings", OverlayScreen.Settings),
        new("Quit", "Power", OverlayScreen.None),
    ];

    public DashboardViewModel(IBackgroundService backgroundService)
    {
        _backgroundService = backgroundService;
        RecentGames.CollectionChanged += (_, _) => OnPropertyChanged(nameof(ShowEmptyStub));
    }

    /// <summary>
    /// Fades the background out to black, swaps the brush, then fades back in.
    /// Superseded requests (rapid selection changes) abort without swapping.
    /// </summary>
    private async Task FadeBackgroundAsync(IBrush brush)
    {
        int generation = ++_fadeGeneration;
        FadeOpacity = 1;
        await Task.Delay(TimingConstants.FadeDuration);
        if (generation != _fadeGeneration)
        {
            return;
        }

        Background = brush;
        FadeOpacity = 0;
    }

    /// <summary>
    /// Recomputes the background brush from the current settings and selection.
    /// Falls back to the linear gradient when the requested brush can't be built.
    /// When <paramref name="fade"/> is set, the swap animates through black
    /// (used for selection-driven Dynamic changes; settings changes stay instant).
    /// </summary>
    public void UpdateBackground(Bitmap? selectedArt, bool fade = false)
    {
        _pendingArt = selectedArt;
        _fadeRequested = fade;

        BackgroundMode mode = _backgroundService.Settings.Mode;
        IBrush? brush = _backgroundService.GetBackground(selectedArt);
        if (brush == null)
        {
            _backgroundService.Settings.Mode = BackgroundMode.LinearGradient;
            brush = _backgroundService.GetBackground(null);
        }

        // Vignette only belongs on image-based backgrounds
        VignetteVisible = mode == BackgroundMode.Image
                          || (mode == BackgroundMode.Dynamic && selectedArt != null);

        if (ShouldFadeBackground)
        {
            // The fallback above always produces a brush (linear gradient)
            _ = FadeBackgroundAsync(brush!);
        }
        else
        {
            Background = brush;
        }

        _currentBackgroundArt = selectedArt;
        Logger.Debug<DashboardViewModel>(
            $"Background updated: mode={_backgroundService.Settings.Mode}, art={(selectedArt != null ? "game art" : "none")}");
    }
}