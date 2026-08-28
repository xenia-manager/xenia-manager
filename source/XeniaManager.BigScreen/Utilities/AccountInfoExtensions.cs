using XeniaManager.BigScreen.Constants;
using XeniaManager.Files.Models.Account;

namespace XeniaManager.BigScreen.Utilities;

/// <summary>
/// Extension helpers for account profiles.
/// </summary>
public static class AccountInfoExtensions
{
    /// <summary>
    /// The profile's path XUID as a 16-digit hex string, or null when unset.
    /// </summary>
    public static string? PathXuidText(this AccountInfo? profile) =>
        profile?.PathXuid?.Value.ToString(FormatConstants.XuidFormat);
}