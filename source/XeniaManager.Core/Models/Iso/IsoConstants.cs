namespace XeniaManager.Core.Models.Iso;

/// <summary>
/// Constants used for Xbox ISO (XGD) format parsing.
/// </summary>
internal static class IsoConstants
{
    /// <summary>
    /// The default executable name in Xbox 360 discs.
    /// </summary>
    public const string DEFAULT_EXECUTABLE_NAME = "default.xex";

    /// <summary>
    /// The magic string found in valid XGD headers.
    /// </summary>
    public const string XGD_IMAGE_MAGIC = "MICROSOFT*XBOX*MEDIA";

    /// <summary>
    /// The size of each sector in bytes (2048 bytes).
    /// </summary>
    public const uint SECTOR_SIZE = 0x800;

    /// <summary>
    /// The base sector offset for ISO data.
    /// </summary>
    public const uint ISO_BASE_SECTOR = 0x20;

    /// <summary>
    /// The sector containing the XGD1 magic header.
    /// </summary>
    public const uint MAGIC_SECTOR_XGD1 = 0x30620;

    /// <summary>
    /// The sector containing the XGD2 magic header.
    /// </summary>
    public const uint MAGIC_SECTOR_XGD2 = 0x1FB40;

    /// <summary>
    /// The sector containing the XGD3 magic header.
    /// </summary>
    public const uint MAGIC_SECTOR_XGD3 = 0x4120;

    /// <summary>
    /// The sector containing the XDKI magic header.
    /// </summary>
    public const uint MAGIC_SECTOR_XDKI = ISO_BASE_SECTOR;
}