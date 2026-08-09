using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using XeniaManager.Core.Models.Files.Gpd;
using XeniaManager.Core.Models.Game;
using XeniaManager.Core.Utilities;

namespace XeniaManager.BigScreen.ViewModels.Items;

/// <summary>
/// ViewModel for a single game tile on the dashboard or library carousel.
/// </summary>
public partial class GameCardViewModel : ObservableObject
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
    public Bitmap? Boxart => !string.IsNullOrEmpty(Game.Artwork.Boxart) ? Game.Artwork.CachedBoxart : null;

    /// <summary>
    /// Whether box art is available to show.
    /// </summary>
    public bool HasBoxart => Boxart != null;

    /// <summary>
    /// Achievements unlocked / total, from the profile's GPD. Defaults to 0 / 0 when no profile data exists.
    /// </summary>
    public string AchievementsText { get; } = "0 / 0";

    /// <summary>
    /// Gamerscore earned / total, from the profile's GPD. Defaults to 0 / 0 when no profile data exists.
    /// </summary>
    public string GamerscoreText { get; } = "0 / 0";

    /// <summary>
    /// Total time played, formatted via <see cref="PlaytimeFormatter"/>.
    /// </summary>
    public string PlaytimeText => PlaytimeFormatter.Format(Game.Playtime);

    public GameCardViewModel(Game game, TitleEntry? titleEntry = null)
    {
        Game = game;
        _title = game.Title;

        if (titleEntry != null)
        {
            AchievementsText = $"{titleEntry.AchievementUnlockedCount} / {titleEntry.AchievementCount}";
            GamerscoreText = $"{titleEntry.GamerscoreUnlocked} / {titleEntry.GamerscoreTotal}";
        }
    }

    /// <summary>
    /// Activates the card (launching the game). Stub for future wiring.
    /// </summary>
    [RelayCommand]
    private void Select()
    {
    }
}
