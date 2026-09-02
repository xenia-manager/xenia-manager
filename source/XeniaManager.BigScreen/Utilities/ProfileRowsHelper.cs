using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using XeniaManager.BigScreen.Services;
using XeniaManager.BigScreen.ViewModels.Items;
using XeniaManager.Core.Models;
using XeniaManager.Files.Models.Account;

namespace XeniaManager.BigScreen.Utilities;

/// <summary>
/// Shared helpers for the profile row lists (profile picker and manage profiles).
/// </summary>
public static class ProfileRowsHelper
{
    /// <summary>
    /// Builds profile rows ordered active-first, then alphabetically.
    /// </summary>
    public static List<ProfileItemViewModel> BuildRows(IReadOnlyList<AccountInfo> profiles, AccountInfo? active)
    {
        return profiles
            .OrderByDescending(p => ReferenceEquals(p, active))
            .ThenBy(p => p.Gamertag, StringComparer.OrdinalIgnoreCase)
            .Select(p => new ProfileItemViewModel(p, ReferenceEquals(p, active)))
            .ToList();
    }

    /// <summary>
    /// Resolves every row's gamerscore off the UI thread (GPD file I/O),
    /// updating each row as it lands.
    /// </summary>
    public static async Task LoadGamerscoresAsync(
        IEnumerable<ProfileItemViewModel> rows, IProfileService profileService, XeniaVersion version)
    {
        foreach (ProfileItemViewModel item in rows)
        {
            int score = await Task.Run(() => profileService.GetGamerscore(version, item.Profile));
            item.GamerscoreText = score.ToString();
        }
    }
}