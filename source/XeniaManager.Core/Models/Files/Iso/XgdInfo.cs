namespace XeniaManager.Core.Models.Files.Iso;

/// <summary>
/// Contains information about an XGD (Xbox Disc Format) image.
/// </summary>
public class XgdInfo
{
    /// <summary>
    /// Gets or sets the base sector offset for the disc image.
    /// </summary>
    public uint BaseSector { get; set; }

    /// <summary>
    /// Gets or sets the sector offset of the root directory.
    /// </summary>
    public uint RootDirSector { get; set; }

    /// <summary>
    /// Gets or sets the size of the root directory in bytes.
    /// </summary>
    public uint RootDirSize { get; set; }

    /// <summary>
    /// Gets or sets the creation date and time of the disc image.
    /// </summary>
    public DateTime CreationDateTime { get; set; }
}