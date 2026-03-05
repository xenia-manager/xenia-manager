namespace XeniaManager.Core.Models.Files.Account;

/// <summary>
/// Represents a profile's display information for the ComboBox.
/// </summary>
/// <param name="gamertag">The profile's gamertag.</param>
/// <param name="pathXuid">The profile's PathXuid (preferred for display).</param>
/// <param name="xuid">The profile's XUID (fallback if PathXuid is null).</param>
public class ProfileDisplayInfo(string gamertag, AccountXuid? pathXuid, AccountXuid xuid)
{
    /// <summary>
    /// The profile's gamertag.
    /// </summary>
    public string Gamertag { get; } = gamertag;

    /// <summary>
    /// The profile's XUID for display (uses PathXuid if available, otherwise Xuid).
    /// </summary>
    public AccountXuid DisplayXuid { get; } = pathXuid ?? xuid;

    /// <summary>
    /// Returns a string representation of the profile in "Gamertag (Xuid)" format.
    /// </summary>
    /// <returns>The display string.</returns>
    public override string ToString() => $"{Gamertag} ({DisplayXuid})";
}
