using System;
using System.Collections.Generic;
using XeniaManager.BigScreen.Models;
using XeniaManager.Core.Models.Files.Account;
using XeniaManager.Core.Models.Game;

namespace XeniaManager.BigScreen.Services;

/// <summary>
/// Loads the Canary profiles, tracks the active one and resolves per-game
/// achievement/gamerscore stats. The active profile is persisted and restored
/// at boot; switching it refreshes the header and per-game stats.
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
    /// All Canary profiles found on disk.
    /// </summary>
    IReadOnlyList<AccountInfo> Profiles { get; }

    /// <summary>
    /// The active profile, or null when no profiles exist.
    /// </summary>
    AccountInfo? ActiveProfile { get; }

    /// <summary>
    /// Raised after the active profile changes, so the header and game stats refresh.
    /// </summary>
    event Action? ProfileChanged;

    /// <summary>
    /// Loads all Canary profiles and activates the persisted one (or the first).
    /// </summary>
    void Load();

    /// <summary>
    /// Re-scans the profile list after external changes (create/delete/import/rename),
    /// keeping the active profile when it still exists and refreshing the header/stats.
    /// </summary>
    void Refresh();

    /// <summary>
    /// Activates the given profile, persists the selection and raises
    /// <see cref="ProfileChanged"/>.
    /// </summary>
    void SwitchProfile(AccountInfo profile);

    /// <summary>
    /// Loads the per-game achievement GPD for the active profile, or null when
    /// no profile or GPD exists.
    /// </summary>
    Core.Files.GpdFile? LoadGameAchievementGpd(string gameId);

    /// <summary>
    /// Resolves achievement/gamerscore counters for the given game.
    /// </summary>
    GameStatInfo? GetGameStats(Game game);

    /// <summary>
    /// Resolves the given profile's total gamerscore from its profile GPD
    /// (reusing the loaded GPD when the profile is the active one).
    /// </summary>
    int GetGamerscore(AccountInfo profile);
}