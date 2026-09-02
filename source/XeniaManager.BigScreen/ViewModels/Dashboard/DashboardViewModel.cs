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
using XeniaManager.Logging;
using TweenAvalonia;

namespace XeniaManager.BigScreen.ViewModels.Dashboard;

/// <summary>
/// Dashboard state: recent games, options, background and focus animations.
/// </summary>
public partial class DashboardViewModel : ViewModelBase
{
    private readonly IBackgroundService _backgroundService;
    private const double FocusIndexEpsilon = 0.001;
    private const double MaxSmoothDistance = 1.0;

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

    [ObservableProperty] public partial bool IsGameRowFocused { get; set; } = true;

    /// <summary>
    /// Index of the focused card. Driven by selection and animated.
    /// </summary>
    [ObservableProperty]
    public partial double FocusedIndex { get; set; } = LayoutConstants.DashboardNoFocusIndex;

    /// <summary>
    /// Amount the row is focused. 1 when game row is active, 0 when not.
    /// </summary>
    [ObservableProperty]
    public partial double FocusAmount { get; set; } = 1.0;

    private Tween _focusedTween;
    private Tween _focusAmountTween;

    [ObservableProperty] public partial double CardSpacing { get; set; } = LayoutConstants.DashboardCardSpacing;

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
    public bool ShowEmptyStub
    {
        get
        {
            return RecentGames.Count == 0;
        }
    }

    public ObservableCollection<OptionsCardViewModel> Options { get; } =
    [
        new OptionsCardViewModel("Library", "Games", OverlayScreen.Library),
        new OptionsCardViewModel("Gallery", "Library", OverlayScreen.Gallery),
        new OptionsCardViewModel("Settings", "Settings", OverlayScreen.Settings),
        new OptionsCardViewModel("Quit", "Power", OverlayScreen.None)
    ];

    public DashboardViewModel(IBackgroundService backgroundService)
    {
        _backgroundService = backgroundService;
        RecentGames.CollectionChanged += (_, args) =>
        {
            OnPropertyChanged(nameof(ShowEmptyStub));
            if (args.NewItems != null)
            {
                foreach (GameCardViewModel card in args.NewItems)
                {
                    card.PropertyChanged += OnGameCardPropertyChanged;
                }
            }

            if (args.OldItems != null)
            {
                foreach (GameCardViewModel card in args.OldItems)
                {
                    card.PropertyChanged -= OnGameCardPropertyChanged;
                }
            }

            if (args.NewItems != null || args.OldItems != null)
            {
                SyncFocusedIndexToSelection();
            }
        };
    }

    private void OnGameCardPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(GameCardViewModel.IsSelected) && sender is GameCardViewModel vm && vm.IsSelected)
        {
            SyncFocusedIndexToSelection();
        }
        else if (e.PropertyName == nameof(GameCardViewModel.IsSelected) &&
                 Utilities.SelectionHelper.IndexOfSelected(RecentGames) < 0)
        {
            SyncFocusedIndexToSelection();
        }
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

    public void SyncFocusedIndexToSelection()
    {
        if (RecentGames.Count == 0)
        {
            AnimateFocusedIndex(LayoutConstants.DashboardNoFocusIndex);
            AnimateFocusAmount(0.0);
            return;
        }

        int idx = Utilities.SelectionHelper.IndexOfSelected(RecentGames);
        if (idx >= 0)
        {
            AnimateFocusedIndex(idx);
        }

        AnimateFocusAmount(IsGameRowFocused ? 1.0 : 0.0);
    }

    private void AnimateFocusedIndex(int targetIndex)
    {
        _focusedTween.Stop();
        double target = targetIndex < 0 ? LayoutConstants.DashboardNoFocusIndex : targetIndex;
        if (Math.Abs(FocusedIndex - target) < FocusIndexEpsilon)
        {
            FocusedIndex = target;
            return;
        }

        if (RecentGames.Count == 0)
        {
            FocusedIndex = target;
            return;
        }

        if (Math.Abs(FocusedIndex - target) > MaxSmoothDistance)
        {
            FocusedIndex = target;
            return;
        }

        _focusedTween = Tween.Custom(this, FocusedIndex, target, static (vm, v) => vm.FocusedIndex = v,
            TimingConstants.CardRowAnimationDuration);
    }

    private void AnimateFocusAmount(double target)
    {
        _focusAmountTween.Stop();
        if (Math.Abs(FocusAmount - target) < FocusIndexEpsilon)
        {
            FocusAmount = target;
            return;
        }

        _focusAmountTween = Tween.Custom(this, FocusAmount, target, static (vm, v) => vm.FocusAmount = v,
            TimingConstants.CardRowAnimationDuration);
    }

    partial void OnIsGameRowFocusedChanged(bool value)
    {
        UpdateCardSpacing(value);
        SyncFocusedIndexToSelection();
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
            _artFade = FadeArtOpacity(0).OnComplete(this, static t => t.CommitArtwork());
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