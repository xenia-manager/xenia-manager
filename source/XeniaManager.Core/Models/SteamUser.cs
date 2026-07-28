namespace XeniaManager.Core.Models;

/// <summary>
/// Represents a Steam user account parsed from loginusers.vdf
/// </summary>
public sealed record SteamUser
{
    /// <summary>
    /// The user's SteamID64 (64-bit identifier)
    /// </summary>
    public required string SteamId64 { get; init; }

    /// <summary>
    /// The user's SteamID32 (32-bit identifier, converted from SteamID64)
    /// </summary>
    public string? SteamId32 { get; init; }

    /// <summary>
    /// The user's display name (PersonaName in Steam)
    /// </summary>
    public required string PersonaName { get; init; }

    /// <summary>
    /// The user's account name (AccountName in Steam)
    /// </summary>
    public string? AccountName { get; init; }
}