using XeniaManager.BigScreen.Models;
using XeniaManager.Core.Models.Game;

namespace XeniaManager.BigScreen.Services;

/// <summary>
/// Loads the active Canary profile and resolves per-game achievement/gamerscore stats.
/// </summary>
public interface IProfileService
{
    /// <summary>
    /// Gamertag of the active Canary profile, or "Guest" when none exists.
    /// </summary>
    string Gamertag { get; }

    /// <summary>
    /// Total gamerscore of the active profile, or "0" when none exists.
    /// </summary>
    string Gamerscore { get; }

    /// <summary>
    /// Loads the first available Canary profile and its gamerscore from the profile GPD.
    /// </summary>
    void Load();

    /// <summary>
    /// Resolves achievement/gamerscore counters for the given game.
    /// </summary>
    GameStatInfo? GetGameStats(Game game);
}