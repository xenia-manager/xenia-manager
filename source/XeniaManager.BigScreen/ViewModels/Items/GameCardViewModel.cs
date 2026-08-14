using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using XeniaManager.BigScreen.Constants;
using XeniaManager.BigScreen.Models;
using XeniaManager.BigScreen.Models.Settings;
using XeniaManager.BigScreen.Utilities;
using XeniaManager.Core.Models.Game;
using XeniaManager.Core.Utilities;

namespace XeniaManager.BigScreen.ViewModels.Items;

/// <summary>
/// ViewModel for a single game tile on the dashboard or library carousel.
/// </summary>
public partial class GameCardViewModel : ObservableObject, ISelectable
{
    /// <summary>
    /// The Core game model this card represents.
    /// </summary>
    public Game Game { get; }

    /// <summary>
    /// The game's display title.
    /// </summary>
    [ObservableProperty]
    public partial string Title { get; set; }

    /// <summary>
    /// Whether this card currently has focus/selection on the dashboard.
    /// </summary>
    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    /// <summary>
    /// The game's artwork, used by the dynamic background. Null until real art is loaded.
    /// </summary>
    [ObservableProperty]
    public partial Bitmap? BackgroundArt { get; set; }

    /// <summary>
    /// The image shown on this card (box art or disc icon).
    /// </summary>
    [ObservableProperty]
    public partial CardImageMode CardImageMode { get; set; } = CardImageMode.Icon;

    /// <summary>
    /// The game's box art, or null when missing/unreadable.
    /// </summary>
    public Bitmap? BoxArt => !string.IsNullOrEmpty(Game.Artwork.Boxart) ? Game.Artwork.CachedBoxart : null;

    /// <summary>
    /// Whether box art is available to show.
    /// </summary>
    public bool HasBoxArt => BoxArt != null;

    /// <summary>
    /// The game's disc art (icon), or null when missing/unreadable.
    /// </summary>
    public Bitmap? DiscArt => Game.Artwork.CachedIcon;

    /// <summary>
    /// Whether disc art is available to show.
    /// </summary>
    public bool HasDiscArt => DiscArt != null;

    /// <summary>
    /// Whether the box art layer is shown (Box Art mode only).
    /// </summary>
    public bool ShowBoxArt => CardImageMode == CardImageMode.BoxArt && HasBoxArt;

    /// <summary>
    /// Whether the disc icon layer is shown (always in Icon mode, fallback in Box Art mode).
    /// </summary>
    public bool ShowDiscArt => CardImageMode == CardImageMode.Icon || !HasBoxArt;

    /// <summary>
    /// Achievements unlocked / total, from the profile's GPD or per-game achievement GPD.
    /// Defaults to 0 / 0 when no data exists.
    /// </summary>
    public string AchievementsText { get; }

    /// <summary>
    /// Gamerscore earned / total, from the profile's GPD or per-game achievement GPD.
    /// Defaults to 0 / 0 when no data exists.
    /// </summary>
    public string GamerscoreText { get; }

    /// <summary>
    /// Total time played, formatted via <see cref="PlaytimeFormatter"/>.
    /// </summary>
    public string PlaytimeText => PlaytimeFormatter.Format(Game.Playtime);

    /// <summary>
    /// The tile's width; grows on selection.
    /// </summary>
    public double CardWidth => IsSelected
        ? LayoutConstants.DashboardCardSelectedWidth
        : LayoutConstants.DashboardCardWidth;

    /// <summary>
    /// The tile's height. Box Art mode uses a portrait tile sized so the art fills
    /// it bottom-anchored with the top ~12% cropped (Icon mode stays square);
    /// selection grows both.
    /// </summary>
    public double CardHeight => CardImageMode == CardImageMode.BoxArt
        ? IsSelected
            ? LayoutConstants.DashboardCardBoxArtSelectedHeight
            : LayoutConstants.DashboardCardBoxArtHeight
        : IsSelected
            ? LayoutConstants.DashboardCardIconSelectedHeight
            : LayoutConstants.DashboardCardIconHeight;

    /// <summary>
    /// Full height of the box art at the current tile width; the bottom-anchored
    /// crop container, so only the art's top ~12% is clipped. 0 without art.
    /// </summary>
    public double BoxArtFullHeight => BoxArt != null
        ? (CardWidth - LayoutConstants.DashboardCardArtMargin) * BoxArt.PixelSize.Height / BoxArt.PixelSize.Width
        : 0;

    public GameCardViewModel(Game game, GameStatInfo? stats = null)
    {
        Game = game;
        Title = game.Title;

        if (stats != null)
        {
            AchievementsText = $"{stats.AchievementsUnlocked} / {stats.AchievementsTotal}";
            GamerscoreText = $"{stats.GamerscoreUnlocked} / {stats.GamerscoreTotal}";
        }
        else
        {
            AchievementsText = "0 / 0";
            GamerscoreText = "0 / 0";
        }
    }

    /// <summary>
    /// Loads the card's background art from the cached artwork (no-op once loaded).
    /// </summary>
    public void EnsureBackgroundLoaded()
    {
        if (BackgroundArt == null)
        {
            BackgroundArt = Game.Artwork.CachedBackground;
        }
    }

    partial void OnIsSelectedChanged(bool value)
    {
        if (value)
        {
            EnsureBackgroundLoaded();
        }

        OnPropertyChanged(nameof(CardWidth));
        OnPropertyChanged(nameof(CardHeight));
        OnPropertyChanged(nameof(BoxArtFullHeight));
    }

    partial void OnCardImageModeChanged(CardImageMode value)
    {
        OnPropertyChanged(nameof(ShowBoxArt));
        OnPropertyChanged(nameof(ShowDiscArt));
        OnPropertyChanged(nameof(CardHeight));
    }
}