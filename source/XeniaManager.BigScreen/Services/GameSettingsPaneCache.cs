using System.Runtime.CompilerServices;
using XeniaManager.BigScreen.ViewModels.Modals;
using XeniaManager.Core.Models.Game;

namespace XeniaManager.BigScreen.Services;

/// <summary>
/// Keeps one game settings pane alive per game for the app session, so the
/// config load (parse + section/option VM build) happens only once instead of
/// on every game modal open.
/// </summary>
public static class GameSettingsPaneCache
{
    private static readonly ConditionalWeakTable<Game, GameSettingsPaneViewModel> Panes = new();

    /// <summary>
    /// Returns the cached settings pane for the given game, creating it on
    /// first use.
    /// </summary>
    public static GameSettingsPaneViewModel GetOrCreate(Game game)
    {
        return Panes.GetValue(game, static g => new GameSettingsPaneViewModel(g));
    }
}
