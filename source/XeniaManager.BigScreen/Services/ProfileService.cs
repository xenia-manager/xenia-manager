using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using XeniaManager.BigScreen.Constants;
using XeniaManager.BigScreen.Models;
using XeniaManager.BigScreen.Utilities;
using XeniaManager.Core.Files;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Manage;
using XeniaManager.Core.Models;
using XeniaManager.Core.Models.Files.Account;
using XeniaManager.Core.Models.Files.Gpd;
using XeniaManager.Core.Models.Files.Stfs;
using XeniaManager.Core.Models.Game;
using XeniaManager.Core.Models.Items;
using XeniaManager.Core.Utilities;

namespace XeniaManager.BigScreen.Services;

/// <summary>
/// Loads all Canary profiles, tracks the active one (persisted as
/// <c>profile_xuid</c>) and resolves per-game achievement/gamerscore stats from
/// the active profile's GPD, falling back to each game's achievement GPD.
/// </summary>
public class ProfileService : IProfileService
{
    private readonly IBackgroundService _backgroundService;

    /// <summary>
    /// Gamertag of the active Canary profile, or "Guest" when none exists.
    /// </summary>
    public string Gamertag { get; private set; } = "Guest";

    /// <summary>
    /// Total gamerscore of the active profile, or "0" when none exists.
    /// </summary>
    public string Gamerscore { get; private set; } = "0";

    /// <summary>
    /// All Canary profiles found on disk.
    /// </summary>
    public IReadOnlyList<AccountInfo> Profiles { get; private set; } = [];

    /// <summary>
    /// The active profile, or null when no profiles exist.
    /// </summary>
    public AccountInfo? ActiveProfile { get; private set; }

    /// <summary>
    /// Raised after the active profile changes, so the header and game stats refresh.
    /// </summary>
    public event Action? ProfileChanged;

    /// <summary>
    /// The profile GPD of the active profile, used for per-game achievement stats.
    /// </summary>
    private GpdFile? _profileGpd;

    public ProfileService(IBackgroundService backgroundService)
    {
        _backgroundService = backgroundService;
    }

    /// <summary>
    /// Loads all Canary profiles and activates the persisted one (falling back
    /// to the first profile when the saved XUID no longer exists).
    /// </summary>
    public void Load()
    {
        try
        {
            Profiles = ProfileManager.LoadProfiles(XeniaVersion.Canary);
            if (Profiles.Count == 0)
            {
                return;
            }

            string? savedXuid = _backgroundService.Settings.ProfileXuid;
            AccountInfo? profile = Profiles.FirstOrDefault(p => MatchesXuid(p, savedXuid)) ?? Profiles[0];
            ActivateProfile(profile);
            Logger.Info<ProfileService>(
                $"Loaded {Profiles.Count} profiles, active: '{Gamertag}'");
        }
        catch (Exception ex)
        {
            // No profiles found - keep defaults
            Logger.Warning<ProfileService>("Failed to load Canary profiles, keeping defaults");
            Logger.LogExceptionDetails<ProfileService>(ex);
        }
    }

    /// <summary>
    /// Re-scans the profile list after external changes, keeping the active
    /// profile when it still exists (falling back to the first otherwise) and
    /// refreshing the header and per-game stats.
    /// </summary>
    public void Refresh()
    {
        try
        {
            IReadOnlyList<AccountInfo> loaded = ProfileManager.LoadProfiles(XeniaVersion.Canary);
            if (loaded.Count == 0)
            {
                Profiles = [];
                ActiveProfile = null;
                _profileGpd = null;
                Gamertag = "Guest";
                Gamerscore = "0";
                Logger.Warning<ProfileService>("No profiles remain after refresh");
                ProfileChanged?.Invoke();
                return;
            }

            string? activeXuid = ActiveProfile.PathXuidText();
            AccountInfo? profile = loaded.FirstOrDefault(p => MatchesXuid(p, activeXuid)) ?? loaded[0];
            Profiles = loaded;
            ActivateProfile(profile);

            // The active profile changed (deleted) - persist the new selection
            if (profile.PathXuidText() != activeXuid)
            {
                _backgroundService.Settings.ProfileXuid = profile.PathXuidText();
                _backgroundService.Save();
            }

            ProfileChanged?.Invoke();
            Logger.Info<ProfileService>($"Profiles refreshed: {Profiles.Count} loaded, active: '{Gamertag}'");
        }
        catch (Exception ex)
        {
            Logger.Warning<ProfileService>("Failed to refresh Canary profiles");
            Logger.LogExceptionDetails<ProfileService>(ex);
        }
    }

    /// <summary>
    /// Activates the given profile, persists the selection as <c>profile_xuid</c>
    /// and raises <see cref="ProfileChanged"/> so the header and game stats refresh.
    /// </summary>
    public void SwitchProfile(AccountInfo profile)
    {
        if (!Profiles.Contains(profile))
        {
            Logger.Warning<ProfileService>($"Cannot switch to unknown profile '{profile.Gamertag}'");
            return;
        }

        ActivateProfile(profile);
        _backgroundService.Settings.ProfileXuid = profile.PathXuidText();
        _backgroundService.Save();
        ProfileChanged?.Invoke();
        Logger.Info<ProfileService>($"Switched to profile '{Gamertag}' ({Gamerscore}G)");
    }

    /// <summary>
    /// Applies the given profile as active and loads its gamerscore from the profile GPD.
    /// </summary>
    private void ActivateProfile(AccountInfo profile)
    {
        ActiveProfile = profile;
        Gamertag = profile.Gamertag;
        _profileGpd = null;
        Gamerscore = "0";

        try
        {
            AccountContent content = new(profile, XeniaVersion.Canary, XboxConstants.ProfileContentTitleId);
            if (content.ProfileGpd != null)
            {
                _profileGpd = content.ProfileGpd;
                Gamerscore = content.ProfileGpd.Titles.Sum(t => t.GamerscoreUnlocked).ToString();
            }
        }
        catch (Exception ex)
        {
            // Profile GPD missing or unreadable - keep gamerscore at 0
            Logger.Warning<ProfileService>(
                $"Failed to load the profile GPD for '{profile.Gamertag}', gamerscore kept at 0");
            Logger.LogExceptionDetails<ProfileService>(ex);
        }
    }

    /// <summary>
    /// Whether the given profile's path XUID matches the persisted value.
    /// </summary>
    private static bool MatchesXuid(AccountInfo profile, string? xuid)
    {
        return !string.IsNullOrEmpty(xuid)
               && profile.PathXuidText()?.Equals(xuid, StringComparison.OrdinalIgnoreCase) == true;
    }

    /// <summary>
    /// Loads the per-game achievement GPD ({titleId}.gpd next to the profile GPD)
    /// for the active profile, or null when no profile or GPD exists.
    /// </summary>
    public GpdFile? LoadGameAchievementGpd(string gameId)
    {
        AccountInfo? profileAccount = ActiveProfile;
        if (profileAccount == null || string.IsNullOrEmpty(gameId))
        {
            return null;
        }

        try
        {
            string xuid =
                (profileAccount.PathXuid?.Value ?? profileAccount.Xuid.Value).ToString(FormatConstants.XuidFormat);
            string contentFolder = AppPathResolver.GetFullPath(
                XeniaVersionInfo.GetXeniaVersionInfo(XeniaVersion.Canary).ContentFolderLocation);
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
            Logger.Error<ProfileService>($"Failed to load achievement GPD for '{gameId}'");
            Logger.LogExceptionDetails<ProfileService>(ex);
            return null;
        }
    }

    /// <summary>
    /// Resolves the given profile's total gamerscore from its profile GPD,
    /// reusing the loaded GPD when the profile is the active one.
    /// </summary>
    public int GetGamerscore(AccountInfo profile)
    {
        if (ReferenceEquals(profile, ActiveProfile) && _profileGpd != null)
        {
            return _profileGpd.Titles.Sum(t => t.GamerscoreUnlocked);
        }

        try
        {
            AccountContent content = new(profile, XeniaVersion.Canary, XboxConstants.ProfileContentTitleId);
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
    /// Resolves achievement/gamerscore counters for the given game. Preferred source is the
    /// profile GPD TitleEntry; falls back to the per-game achievement GPD ({titleId}.gpd).
    /// </summary>
    public GameStatInfo? GetGameStats(Game game)
    {
        // Preferred: profile GPD TitleEntry
        if (_profileGpd != null &&
            uint.TryParse(game.GameId, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint titleId))
        {
            TitleEntry? entry = _profileGpd.Titles.FirstOrDefault(t => t.TitleId == titleId);
            if (entry != null)
            {
                return new GameStatInfo(
                    entry.AchievementUnlockedCount, entry.AchievementCount,
                    entry.GamerscoreUnlocked, entry.GamerscoreTotal);
            }
        }

        // Fallback: count from the per-game achievement GPD
        GpdFile? achievementGpd = LoadGameAchievementGpd(game.GameId);
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
}