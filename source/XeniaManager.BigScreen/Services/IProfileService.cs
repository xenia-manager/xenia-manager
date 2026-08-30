using System;
using System.Collections.Generic;
using XeniaManager.BigScreen.Models;
using XeniaManager.Core.Models;
using XeniaManager.Files.Models.Account;
using XeniaManager.Core.Models.Game;

namespace XeniaManager.BigScreen.Services;

/// <summary>
/// Snapshot of one emulator version's profile state (profiles, active profile,
/// gamertag and gamerscore).
/// </summary>
public readonly record struct VersionProfileState(
    IReadOnlyList<AccountInfo> Profiles,
    AccountInfo? ActiveProfile,
    string Gamertag,
    string Gamerscore);

/// <summary>
/// Loads the profiles of every installed emulator version, tracks the active
/// profile per version and resolves per-game achievement/gamerscore stats. The
/// active profiles are persisted and restored at boot; switching them refreshes
/// the header and per-game stats.
/// </summary>
public interface IProfileService
{
    /// <summary>
    /// The version whose profile drives the header and default pickers.
    /// </summary>
    XeniaVersion? ActiveVersion { get; }

    /// <summary>
    /// Gamertag of the active version's profile, or "Guest" when none exists.
    /// </summary>
    string Gamertag { get; }

    /// <summary>
    /// Total gamerscore of the active version's profile, or "0" when none exists.
    /// </summary>
    string Gamerscore { get; }

    /// <summary>
    /// All profiles of the active version found on disk.
    /// </summary>
    IReadOnlyList<AccountInfo> Profiles { get; }

    /// <summary>
    /// The active version's active profile, or null when no profiles exist.
    /// </summary>
    AccountInfo? ActiveProfile { get; }

    /// <summary>
    /// Installed versions (in installation order) that have at least one profile.
    /// </summary>
    IReadOnlyList<XeniaVersion> VersionsWithProfiles { get; }

    /// <summary>
    /// All installed emulator versions (in installation order).
    /// </summary>
    IReadOnlyList<XeniaVersion> InstalledVersions { get; }

    /// <summary>
    /// Raised after the active profile changes, so the header and game stats refresh.
    /// </summary>
    event Action? ProfileChanged;

    /// <summary>
    /// Loads the profiles of every installed version and restores each version's
    /// persisted active profile.
    /// </summary>
    void Load();

    /// <summary>
    /// Re-scans the profile lists after external changes (create/delete/import/rename),
    /// keeping each version's active profile when it still exists and refreshing
    /// the header/stats.
    /// </summary>
    void Refresh();

    /// <summary>
    /// Safety net: makes sure the given version's active profile matches its
    /// persisted XUID, loading the version's profile list when needed. Called at
    /// game launch so sessions always run under the right profile even when the
    /// boot-time restore fell back to the first profile.
    /// </summary>
    void EnsureActiveProfile(XeniaVersion version);

    /// <summary>
    /// Activates the given profile for the given version, persists the selection
    /// and raises <see cref="ProfileChanged"/>.
    /// </summary>
    void SwitchProfile(XeniaVersion version, AccountInfo profile);

    /// <summary>
    /// Loads the per-game achievement GPD of the given version's active profile,
    /// or null when no profile or GPD exists.
    /// </summary>
    Files.GpdFile? LoadGameAchievementGpd(XeniaVersion version, string gameId);

    /// <summary>
    /// Resolves achievement/gamerscore counters for the given game.
    /// </summary>
    GameStatInfo? GetGameStats(Game game);

    /// <summary>
    /// Resolves the given profile's total gamerscore from its profile GPD
    /// (reusing the loaded GPD when the profile is the version's active one).
    /// </summary>
    int GetGamerscore(XeniaVersion version, AccountInfo profile);

    /// <summary>
    /// Writes the given version's active profile XUID, language and country into
    /// its XConfig (the emulator's default profile). Skipped when no XConfig exists.
    /// </summary>
    void SyncXConfigDefaultProfile(XeniaVersion version);

    /// <summary>
    /// Returns the profile state of the given version, loading it on demand when
    /// it hasn't been loaded yet.
    /// </summary>
    VersionProfileState StateFor(XeniaVersion version);

    /// <summary>
    /// All profiles of the given version.
    /// </summary>
    IReadOnlyList<AccountInfo> ProfilesFor(XeniaVersion version);

    /// <summary>
    /// The active profile of the given version, or null when none exists.
    /// </summary>
    AccountInfo? ActiveProfileFor(XeniaVersion version);

    /// <summary>
    /// Adds a newly created account to the given version's in-memory profile list,
    /// so subsequent saves (<see cref="ProfilesFor"/>) include it. The caller owns
    /// creating the account on disk.
    /// </summary>
    void AddProfile(XeniaVersion version, AccountInfo profile);
}