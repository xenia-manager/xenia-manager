using XeniaManager.BigScreen.Models;
using XeniaManager.BigScreen.Models.Settings;
using XeniaManager.BigScreen.ViewModels.Items;
using XeniaManager.Core.Models.Game;

namespace XeniaManager.BigScreen.Factories;

/// <summary>
/// Creates game cards for the dashboard and library rows. Selection wiring
/// (background art swap) stays in the caller.
/// </summary>
public static class GameCardFactory
{
    /// <summary>
    /// Creates a game card with the given stats (or zeroed defaults when null).
    /// </summary>
    public static GameCardViewModel Create(Game game, GameStatInfo? stats = null)
    {
        return new GameCardViewModel(game, stats);
    }

    /// <summary>
    /// Creates a dashboard game card, applying the card image mode and
    /// pre-loading its background art so the dynamic background has
    /// something to show immediately.
    /// </summary>
    public static GameCardViewModel CreateRecent(Game game, GameStatInfo? stats, CardImageMode cardImageMode)
    {
        GameCardViewModel card = Create(game, stats);
        card.CardImageMode = cardImageMode;
        card.EnsureBackgroundLoaded();
        return card;
    }
}