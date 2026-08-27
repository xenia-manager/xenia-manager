using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using XeniaManager.BigScreen.Constants;
using XeniaManager.BigScreen.Models;
using XeniaManager.BigScreen.Models.Settings;
using XeniaManager.BigScreen.Utilities;
using XeniaManager.Core.Files;
using XeniaManager.Logging;
using XeniaManager.Core.Manage;
using XeniaManager.Core.Models;
using XeniaManager.Core.Models.Files.Account;
using XeniaManager.Core.Models.Files.Gpd;
using XeniaManager.Core.Models.Files.Stfs;
using XeniaManager.Core.Models.Files.XConfig;
using XeniaManager.Core.Models.Game;
using XeniaManager.Core.Models.Items;
using XeniaManager.Core.Settings;
using XeniaManager.Core.Utilities;

namespace XeniaManager.BigScreen.Services;

/// <summary>
/// Loads the profiles of every installed emulator version, tracks the active
/// profile per version (persisted in <see cref="DashboardSettings.ActiveProfiles"/>)
/// and resolves per-game achievement/gamerscore stats from each version's active
/// profile GPD, falling back to each game's achievement GPD. When the unified
/// content folder is enabled all versions share the primary version's profile set.
/// </summary>
public class ProfileService : IProfileService
{
    /// <summary>
    /// Per-version profile state (profiles, active profile, gamerscore).
    /// </summary>
    private sealed class VersionState
    {
        public List<AccountInfo> Profiles { get; set; } = [];

        public AccountInfo? ActiveProfile { get; set; }

        /// <summary>
        /// The profile GPD of the active profile, used for per-game achievement stats.
        /// </summary>
        public GpdFile? ProfileGpd { get; set; }

        public string Gamertag { get; set; } = "Guest";

        public string Gamerscore { get; set; } = "0";
    }

    private static readonly VersionState EmptyState = new();

    private readonly IBackgroundService _backgroundService;
    private readonly ConcurrentDictionary<XeniaVersion, VersionState> _states = [];

    private List<XeniaVersion> _installedVersions = [];
    private bool _unifiedContentFolder;

    /// <summary>
    /// The version whose profile drives the header and default pickers.
    /// </summary>
    public XeniaVersion? ActiveVersion { get; private set; }

    /// <summary>
    /// Gamertag of the active version's profile, or "Guest" when none exists.
    /// </summary>
    public string Gamertag => StateForVersion(ActiveVersion).Gamertag;

    /// <summary>
    /// Total gamerscore of the active version's profile, or "0" when none exists.
    /// </summary>
    public string Gamerscore => StateForVersion(ActiveVersion).Gamerscore;

    /// <summary>
    /// All profiles of the active version found on disk.
    /// </summary>
    public IReadOnlyList<AccountInfo> Profiles => StateForVersion(ActiveVersion).Profiles;

    /// <summary>
    /// The active version's active profile, or null when no profiles exist.
    /// </summary>
    public AccountInfo? ActiveProfile => StateForVersion(ActiveVersion).ActiveProfile;

    /// <summary>
    /// Installed versions (in installation order) that have at least one profile.
    /// </summary>
    public IReadOnlyList<XeniaVersion> VersionsWithProfiles =>
        _installedVersions.Where(v => StateForVersion(v).Profiles.Count > 0).ToList();

    /// <summary>
    /// All installed emulator versions (in installation order).
    /// </summary>
    public IReadOnlyList<XeniaVersion> InstalledVersions => _installedVersions;

    /// <summary>
    /// Raised after the active profile changes, so the header and game stats refresh.
    /// </summary>
    public event Action? ProfileChanged;

    /// <summary>
    /// Whether the XConfig already carries the given default profile XUID,
    /// language and country.
    /// </summary>
    private static bool IsProfileSynced(XConfigFile xconfig, ulong xuid, XLanguage language, XOnlineCountry country) =>
        xconfig.DefaultProfile == xuid && xconfig.Language == language && xconfig.Country == country;

    /// <summary>
    /// Whether the given profile's path XUID matches the persisted value.
    /// </summary>
    private static bool MatchesXuid(AccountInfo profile, string? xuid)
    {
        return !string.IsNullOrEmpty(xuid)
               && profile.PathXuidText()?.Equals(xuid, StringComparison.OrdinalIgnoreCase) == true;
    }

    /// <summary>
    /// Returns the state of the given version. With a unified content folder all
    /// versions share the primary version's state; never-loaded versions report
    /// an empty state.
    /// </summary>
    private VersionState StateForVersion(XeniaVersion? version)
    {
        if (_unifiedContentFolder)
        {
            return _states.Count > 0 ? _states.Values.First() : EmptyState;
        }

        return version is { } v && _states.TryGetValue(v, out VersionState? state) ? state : EmptyState;
    }

    /// <summary>
    /// The versions whose profile sets are actually loaded (single version when
    /// the content folders are unified).
    /// </summary>
    private List<XeniaVersion> VersionsToLoad()
    {
        if (_unifiedContentFolder && _installedVersions.Count > 0)
        {
            return [_installedVersions[0]];
        }

        return _installedVersions;
    }

    /// <summary>
    /// The persisted XUID of the given version, or null when none was saved.
    /// </summary>
    private string? PersistedXuid(XeniaVersion version) =>
        _backgroundService.Settings.ActiveProfiles.GetValueOrDefault(version);

    /// <summary>
    /// The version whose profile drives the header: the persisted active version
    /// when it still has profiles, otherwise the first version with profiles.
    /// </summary>
    private XeniaVersion? ResolveActiveVersion()
    {
        if (_backgroundService.Settings.ActiveVersion is { } saved
            && _installedVersions.Contains(saved)
            && StateForVersion(saved).Profiles.Count > 0)
        {
            return saved;
        }

        return _installedVersions.FirstOrDefault(v => StateForVersion(v).Profiles.Count > 0);
    }

    /// <summary>
    /// Loads the given version's profiles and activates its persisted profile
    /// (falling back to the first when the saved XUID no longer exists).
    /// </summary>
    private void LoadVersion(XeniaVersion version)
    {
        if (version == XeniaVersion.Custom
            || !_installedVersions.Contains(version)
            || _states.ContainsKey(version))
        {
            return;
        }

        if (_unifiedContentFolder && _states.Count > 0)
        {
            return;
        }

        try
        {
            VersionState state = new VersionState { Profiles = ProfileManager.LoadProfiles(version) };
            _states[version] = state;
            ActivatePersisted(version, state);
        }
        catch (Exception ex)
        {
            Logger.Warning<ProfileService>($"Failed to load profiles for {version}, keeping empty state");
            Logger.LogExceptionDetails<ProfileService>(ex);
            _states[version] = new VersionState();
        }
    }

    /// <summary>
    /// Restores the persisted active profile of the given version, persisting a
    /// fallback selection when the saved XUID no longer exists.
    /// </summary>
    private void ActivatePersisted(XeniaVersion version, VersionState state)
    {
        if (state.Profiles.Count == 0)
        {
            return;
        }

        string? savedXuid = PersistedXuid(version);
        AccountInfo? profile = state.Profiles.FirstOrDefault(p => MatchesXuid(p, savedXuid)) ?? state.Profiles[0];
        ActivateProfile(version, profile);

        if (profile.PathXuidText() != savedXuid)
        {
            _backgroundService.Settings.ActiveProfiles[version] = profile.PathXuidText()!;
            _backgroundService.Save();
        }
    }

    /// <summary>
    /// Applies the given profile as the version's active profile and loads its
    /// gamerscore from the profile GPD.
    /// </summary>
    private void ActivateProfile(XeniaVersion version, AccountInfo profile)
    {
        VersionState state = StateForVersion(version);
        state.ActiveProfile = profile;
        state.Gamertag = profile.Gamertag;
        state.ProfileGpd = null;
        state.Gamerscore = "0";

        try
        {
            AccountContent content = new AccountContent(profile, version, XboxConstants.ProfileContentTitleId);
            if (content.ProfileGpd != null)
            {
                state.ProfileGpd = content.ProfileGpd;
                state.Gamerscore = content.ProfileGpd.Titles.Sum(t => t.GamerscoreUnlocked).ToString();
            }
        }
        catch (Exception ex)
        {
            Logger.Warning<ProfileService>(
                $"Failed to load the profile GPD for '{profile.Gamertag}' ({version}), gamerscore kept at 0");
            Logger.LogExceptionDetails<ProfileService>(ex);
        }

        SyncXConfigDefaultProfile(version);
    }

    /// <summary>
    /// Writes the active profile's XUID, language and country into the given
    /// version's XConfig (the emulator's default profile), so launched games run
    /// with the selected BigScreen profile. Skipped when no XConfig file exists.
    /// </summary>
    public void SyncXConfigDefaultProfile(XeniaVersion version)
    {
        if (version == XeniaVersion.Custom)
        {
            return;
        }

        try
        {
            XConfigFile? xconfig = XConfigManager.LoadXConfig(version);
            if (xconfig == null)
            {
                Logger.Debug<ProfileService>($"No {version} XConfig file - default profile sync skipped");
                return;
            }

            VersionState state = StateForVersion(version);
            ulong xuid = state.ActiveProfile?.PathXuid?.Value ?? 0;
            XLanguage language = state.ActiveProfile != null
                ? (XLanguage)state.ActiveProfile.Language
                : XLanguage.Invalid;
            XOnlineCountry country = state.ActiveProfile != null
                ? (XOnlineCountry)state.ActiveProfile.Country
                : (XOnlineCountry)0;

            if (IsProfileSynced(xconfig, xuid, language, country))
            {
                return;
            }

            xconfig.DefaultProfile = xuid;
            xconfig.Language = language;
            xconfig.Country = country;
            XConfigManager.SaveXConfig(xconfig, version);
            Logger.Info<ProfileService>(
                $"XConfig synced ({version}): default profile 0x{xuid:X16}, language {language}, country {country}");
        }
        catch (Exception ex)
        {
            Logger.Error<ProfileService>($"Failed to sync the {version} XConfig default profile");
            Logger.LogExceptionDetails<ProfileService>(ex);
        }
    }

    /// <summary>
    /// Loads the profiles of every installed version and restores each version's
    /// persisted active profile. The active version is the persisted one when it
    /// still has profiles, otherwise the first version with profiles.
    /// </summary>
    public void Load()
    {
        try
        {
            Settings desktopSettings = new Settings();
            _unifiedContentFolder = desktopSettings.Settings.Emulator.Settings.UnifiedContentFolder;
            _installedVersions = desktopSettings.GetInstalledVersions(desktopSettings);
            _states.Clear();

            foreach (XeniaVersion version in VersionsToLoad())
            {
                LoadVersion(version);
            }

            ActiveVersion = ResolveActiveVersion();
            if (ActiveVersion == null)
            {
                return;
            }

            ProfileChanged?.Invoke();
            Logger.Info<ProfileService>(
                $"Loaded profiles for {_states.Count} version(s), active version: {ActiveVersion}");
        }
        catch (Exception ex)
        {
            Logger.Warning<ProfileService>("Failed to load profiles, keeping defaults");
            Logger.LogExceptionDetails<ProfileService>(ex);
        }
    }

    /// <summary>
    /// Safety net: makes sure the given version's active profile matches its
    /// persisted XUID, loading the version's profile list when needed. Called at
    /// game launch so sessions always run under the right profile even when the
    /// boot-time restore fell back to the first profile.
    /// </summary>
    public void EnsureActiveProfile(XeniaVersion version)
    {
        if (version == XeniaVersion.Custom)
        {
            return;
        }

        try
        {
            if (!_states.ContainsKey(version) && !(_unifiedContentFolder && _states.Count > 0))
            {
                LoadVersion(version);
            }

            string? savedXuid = PersistedXuid(version);
            if (string.IsNullOrEmpty(savedXuid))
            {
                Logger.Debug<ProfileService>($"No persisted profile XUID for {version} - launch safety net skipped");
                return;
            }

            VersionState state = StateForVersion(version);
            if (state.ActiveProfile != null && MatchesXuid(state.ActiveProfile, savedXuid))
            {
                return;
            }

            AccountInfo? profile = state.Profiles.FirstOrDefault(p => MatchesXuid(p, savedXuid));
            if (profile == null)
            {
                Logger.Warning<ProfileService>(
                    $"Persisted profile XUID for {version} no longer exists - keeping the current active profile");
                return;
            }

            ActivateProfile(version, profile);
            ProfileChanged?.Invoke();
            Logger.Info<ProfileService>(
                $"Launch safety net restored the active profile for {version}: '{state.Gamertag}'");
        }
        catch (Exception ex)
        {
            Logger.Warning<ProfileService>("Failed to restore the persisted active profile at launch");
            Logger.LogExceptionDetails<ProfileService>(ex);
        }
    }

    /// <summary>
    /// Re-scans the profile lists after external changes, keeping each version's
    /// active profile when it still exists (falling back to the first otherwise)
    /// and refreshing the header and per-game stats.
    /// </summary>
    public void Refresh()
    {
        try
        {
            _states.Clear();
            foreach (XeniaVersion version in VersionsToLoad())
            {
                LoadVersion(version);
            }

            XeniaVersion? resolved = ResolveActiveVersion();
            if (resolved != null)
            {
                ActiveVersion = resolved;
            }

            ProfileChanged?.Invoke();
            Logger.Info<ProfileService>($"Profiles refreshed for {_states.Count} version(s)");
        }
        catch (Exception ex)
        {
            Logger.Warning<ProfileService>("Failed to refresh profiles");
            Logger.LogExceptionDetails<ProfileService>(ex);
        }
    }

    /// <summary>
    /// Activates the given profile for the given version, persists the selection
    /// (all versions when the content folders are unified) and raises
    /// <see cref="ProfileChanged"/> so the header and game stats refresh.
    /// </summary>
    public void SwitchProfile(XeniaVersion version, AccountInfo profile)
    {
        if (version == XeniaVersion.Custom)
        {
            return;
        }

        if (!StateForVersion(version).Profiles.Contains(profile))
        {
            Logger.Warning<ProfileService>($"Cannot switch to unknown profile '{profile.Gamertag}' for {version}");
            return;
        }

        ActivateProfile(version, profile);
        PersistActive(version, profile);
        ActiveVersion = version;
        ProfileChanged?.Invoke();
        Logger.Info<ProfileService>($"Switched to profile '{profile.Gamertag}' for {version}");
    }

    /// <summary>
    /// Persists the active profile selection: the given version only, or every
    /// installed version when the content folders are unified (they share the
    /// same profile set).
    /// </summary>
    private void PersistActive(XeniaVersion version, AccountInfo profile)
    {
        DashboardSettings settings = _backgroundService.Settings;
        List<XeniaVersion> targets = _unifiedContentFolder ? _installedVersions : [version];
        foreach (XeniaVersion target in targets)
        {
            settings.ActiveProfiles[target] = profile.PathXuidText()!;
        }

        settings.ActiveVersion = version;
        _backgroundService.Save();
    }

    /// <summary>
    /// Loads the per-game achievement GPD ({titleId}.gpd next to the profile GPD)
    /// of the given version's active profile, or null when no profile or GPD exists.
    /// </summary>
    public GpdFile? LoadGameAchievementGpd(XeniaVersion version, string gameId)
    {
        if (version == XeniaVersion.Custom)
        {
            return null;
        }

        AccountInfo? profileAccount = StateForVersion(version).ActiveProfile;
        if (profileAccount == null || string.IsNullOrEmpty(gameId))
        {
            return null;
        }

        try
        {
            string xuid =
                (profileAccount.PathXuid?.Value ?? profileAccount.Xuid.Value).ToString(FormatConstants.XuidFormat);
            string contentFolder = AppPathResolver.GetFullPath(
                XeniaVersionInfo.GetXeniaVersionInfo(version).ContentFolderLocation);
            string gpdPath = Path.Combine(contentFolder, xuid, XboxConstants.ProfileContentTitleId,
                ContentType.Profile.ToHexString(), xuid, $"{gameId.ToUpperInvariant()}.gpd");

            if (!File.Exists(gpdPath))
            {
                return null;
            }

            return GpdFile.Load(gpdPath);
        }
        catch (Exception ex)
        {
            Logger.Error<ProfileService>($"Failed to load achievement GPD for '{gameId}' ({version})");
            Logger.LogExceptionDetails<ProfileService>(ex);
            return null;
        }
    }

    /// <summary>
    /// Resolves the given profile's total gamerscore from its profile GPD,
    /// reusing the loaded GPD when the profile is the version's active one.
    /// </summary>
    public int GetGamerscore(XeniaVersion version, AccountInfo profile)
    {
        try
        {
            VersionState state = StateForVersion(version);
            if (ReferenceEquals(profile, state.ActiveProfile) && state.ProfileGpd != null)
            {
                return state.ProfileGpd.Titles.Sum(t => t.GamerscoreUnlocked);
            }

            AccountContent content = new AccountContent(profile, version, XboxConstants.ProfileContentTitleId);
            if (content.ProfileGpd == null)
            {
                return 0;
            }

            return content.ProfileGpd.Titles.Sum(t => t.GamerscoreUnlocked);
        }
        catch (Exception ex)
        {
            Logger.Warning<ProfileService>($"Failed to load the profile GPD for '{profile.Gamertag}'");
            Logger.LogExceptionDetails<ProfileService>(ex);
            return 0;
        }
    }

    /// <summary>
    /// Resolves achievement/gamerscore counters from the version's profile GPD
    /// TitleEntry matching the game's title id.
    /// </summary>
    private bool TryGetProfileGpdStats(VersionState state, Game game, out GameStatInfo? stats)
    {
        if (state.ProfileGpd == null ||
            !uint.TryParse(game.GameId, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint titleId))
        {
            stats = null;
            return false;
        }

        TitleEntry? entry = state.ProfileGpd.Titles.FirstOrDefault(t => t.TitleId == titleId);
        if (entry == null)
        {
            stats = null;
            return false;
        }

        stats = new GameStatInfo(
            entry.AchievementUnlockedCount, entry.AchievementCount,
            entry.GamerscoreUnlocked, entry.GamerscoreTotal);
        return true;
    }

    /// <summary>
    /// Resolves achievement/gamerscore counters for the given game. Preferred source is the
    /// version's profile GPD TitleEntry; falls back to the per-game achievement GPD
    /// ({titleId}.gpd).
    /// </summary>
    public GameStatInfo? GetGameStats(Game game)
    {
        VersionState state = StateForVersion(game.XeniaVersion);
        if (TryGetProfileGpdStats(state, game, out GameStatInfo? profileStats))
        {
            return profileStats;
        }

        GpdFile? achievementGpd = GameDataCache.GetAchievementGpd(game);
        if (achievementGpd != null)
        {
            return new GameStatInfo(
                achievementGpd.GetUnlockedAchievementCount(),
                achievementGpd.GetTotalAchievementCount(),
                achievementGpd.GetTotalGamerscore(),
                achievementGpd.GetTotalPossibleGamerscore());
        }

        return null;
    }

    /// <summary>
    /// Returns the profile state of the given version, loading it on demand when
    /// it hasn't been loaded yet.
    /// </summary>
    public VersionProfileState StateFor(XeniaVersion version)
    {
        if (version != XeniaVersion.Custom
            && !_states.ContainsKey(version)
            && !(_unifiedContentFolder && _states.Count > 0))
        {
            LoadVersion(version);
        }

        VersionState state = StateForVersion(version);
        return new VersionProfileState(state.Profiles, state.ActiveProfile, state.Gamertag, state.Gamerscore);
    }

    /// <summary>
    /// All profiles of the given version.
    /// </summary>
    public IReadOnlyList<AccountInfo> ProfilesFor(XeniaVersion version) => StateForVersion(version).Profiles;

    /// <summary>
    /// The active profile of the given version, or null when none exists.
    /// </summary>
    public AccountInfo? ActiveProfileFor(XeniaVersion version) => StateForVersion(version).ActiveProfile;

    /// <summary>
    /// Adds a newly created account to the given version's in-memory profile list,
    /// so subsequent saves include it. The caller owns creating the account on disk.
    /// </summary>
    public void AddProfile(XeniaVersion version, AccountInfo profile)
    {
        if (version == XeniaVersion.Custom)
        {
            return;
        }

        if (!_states.ContainsKey(version) && !(_unifiedContentFolder && _states.Count > 0))
        {
            LoadVersion(version);
        }

        VersionState state = StateForVersion(version);
        if (ReferenceEquals(state, EmptyState))
        {
            Logger.Warning<ProfileService>($"Cannot add profile '{profile.Gamertag}': no loaded state for {version}");
            return;
        }

        state.Profiles.Add(profile);
        Logger.Info<ProfileService>($"Added profile '{profile.Gamertag}' to {version}'s profile list");
    }

    public ProfileService(IBackgroundService backgroundService)
    {
        _backgroundService = backgroundService;
    }
}