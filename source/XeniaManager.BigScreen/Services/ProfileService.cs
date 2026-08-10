using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using XeniaManager.BigScreen.Constants;
using XeniaManager.BigScreen.Models;
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
/// Loads the active Canary profile and resolves per-game achievement/gamerscore
/// stats from the profile GPD, falling back to each game's achievement GPD.
/// </summary>
public class ProfileService : IProfileService
{
    /// <summary>
    /// Gamertag of the active Canary profile, or "Guest" when none exists.
    /// </summary>
    public string Gamertag { get; private set; } = "Guest";

    /// <summary>
    /// Total gamerscore of the active profile, or "0" when none exists.
    /// </summary>
    public string Gamerscore { get; private set; } = "0";

    /// <summary>
    /// The profile GPD of the active Canary profile, used for per-game achievement stats.
    /// </summary>
    private GpdFile? _profileGpd;

    /// <summary>
    /// The active Canary profile, used to locate per-game achievement GPDs.
    /// </summary>
    private AccountInfo? _profileAccount;

    /// <summary>
    /// Loads the first available Canary profile and its gamerscore from the profile GPD.
    /// </summary>
    public void Load()
    {
        try
        {
            List<AccountInfo> profiles = ProfileManager.LoadProfiles(XeniaVersion.Canary);
            AccountInfo? profile = profiles.FirstOrDefault();
            if (profile == null)
            {
                return;
            }

            Gamertag = profile.Gamertag;
            _profileAccount = profile;

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
                Logger.Warning<ProfileService>("Failed to load the profile GPD, gamerscore kept at 0");
                Logger.LogExceptionDetails<ProfileService>(ex);
            }
        }
        catch (Exception ex)
        {
            // No profiles found - keep defaults
            Logger.Warning<ProfileService>("Failed to load Canary profiles, keeping defaults");
            Logger.LogExceptionDetails<ProfileService>(ex);
        }
    }

    /// <summary>
    /// Loads the per-game achievement GPD ({titleId}.gpd next to the profile GPD) if it exists.
    /// </summary>
    private GpdFile? LoadGameAchievementGpd(string gameId)
    {
        if (_profileAccount == null || string.IsNullOrEmpty(gameId))
        {
            return null;
        }

        try
        {
            string xuid = (_profileAccount.PathXuid?.Value ?? _profileAccount.Xuid.Value).ToString(FormatConstants.XuidFormat);
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
