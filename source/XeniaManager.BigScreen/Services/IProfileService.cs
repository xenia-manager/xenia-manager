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
    /// Safety net: makes sure the active profile matches the persisted
    /// <c>profile_xuid</c>, reloading the profile list when needed. Called at
    /// game launch so sessions always run under the right profile even when
    /// the boot-time restore fell back to the first profile.
    /// </summary>
    void EnsureActiveProfile();

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

    /// <summary>
    /// Writes the active profile's XUID, language and country into the Canary
    /// XConfig (the emulator's default profile). Skipped when no XConfig exists.
    /// </summary>
    void SyncXConfigDefaultProfile();
}