using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using XeniaManager.BigScreen.Models;
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
    [ObservableProperty] private string _title;

    /// <summary>
    /// Whether this card currently has focus/selection on the dashboard.
    /// </summary>
    [ObservableProperty] private bool _isSelected;

    /// <summary>
    /// The game's artwork, used by the dynamic background. Null until real art is loaded.
    /// </summary>
    [ObservableProperty] private Bitmap? _backgroundArt;

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

    public GameCardViewModel(Game game, GameStatInfo? stats = null)
    {
        Game = game;
        _title = game.Title;

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
    }
}
