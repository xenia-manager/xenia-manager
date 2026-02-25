namespace XeniaManager.Core.Models.Files.Stfs;

/// <summary>
/// Represents the signature type of STFS package.
/// </summary>
public enum SignatureType
{
    /// <summary>
    /// Signed by a console. Found on cache files, profiles, saved games.
    /// </summary>
    CON,

    /// <summary>
    /// Signed by Microsoft. Found on files delivered by Microsoft through non-Xbox Live means.
    /// </summary>
    PIRS,

    /// <summary>
    /// Signed by Microsoft. Found on files delivered over Xbox Live.
    /// </summary>
    LIVE
}
