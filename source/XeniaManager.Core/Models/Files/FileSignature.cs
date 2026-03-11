namespace XeniaManager.Core.Models.Files;

/// <summary>
/// Represents the type of Xbox file detected.
/// </summary>
public enum FileSignature
{
    /// <summary>
    /// Unknown or unsupported file type.
    /// </summary>
    Unknown,

    /// <summary>
    /// STFS CON package (console signed).
    /// </summary>
    CON,

    /// <summary>
    /// STFS LIVE package (Microsoft signed).
    /// </summary>
    LIVE,

    /// <summary>
    /// STFS PIRS package (Microsoft signed).
    /// </summary>
    PIRS,

    /// <summary>
    /// XEX1 executable file (original Xbox format).
    /// </summary>
    XEX1,

    /// <summary>
    /// XEX2 executable file (Xbox 360 format).
    /// </summary>
    XEX2,

    /// <summary>
    /// XISO disc image (Xbox ISO).
    /// </summary>
    XISO,

    /// <summary>
    /// ZAR archive (Disc Archive).
    /// </summary>
    ZAR,

    /// <summary>
    /// ISO disc image.
    /// </summary>
    ISO
}