namespace XeniaManager.Core.Models.Database.Patches;

/// <summary>
/// Specifies which patch database variant to use (Canary or Netplay).
/// </summary>
public enum PatchDatabaseType
{
    /// <summary>
    /// Canary build patches (latest development version)
    /// </summary>
    Canary,

    /// <summary>
    /// Netplay build patches (multiplayer-focused version)
    /// </summary>
    Netplay
}