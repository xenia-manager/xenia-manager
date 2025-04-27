namespace XeniaManager.Core.VirtualFileSystem.Models;

/// <summary>
/// Represents information about an XGD (Xbox Game Disc) file system structure.
/// </summary>
/// <remarks>
/// Contains details about the structure of the XGD file system such as
/// the base sector, root directory sector, root directory size, and the
/// creation timestamp.
/// </remarks>
public class XgdInfo
{
    /// <summary>
    /// Represents the base sector of a disk or container in the virtual file system.
    /// </summary>
    /// <remarks>
    /// The <c>BaseSector</c> defines the starting sector for relative addressing
    /// within the container or file system. It is often used in conjunction with
    /// other properties, such as <c>RootDirSector</c> and <c>RootDirSize</c>, to
    /// determine the logical layout and structure of the virtual storage.
    /// </remarks>
    /// <example>
    /// The <c>BaseSector</c> is used during sector decoding and offset calculations
    /// for file access or directory traversal within a virtual container.
    /// </example>
    public uint BaseSector;

    /// <summary>
    /// Represents the sector offset within the file system, relative to the base sector,
    /// where the root directory begins. This value determines the starting location
    /// of the root directory in relation to the base sector of the XGD structure.
    /// </summary>
    /// <remarks>
    /// This variable is used to calculate the physical sector location for accessing
    /// data corresponding to the root directory. It works in conjunction with other
    /// properties such as <see cref="XgdInfo.BaseSector"/> and <see cref="XgdInfo.RootDirSize"/>
    /// for directory data processing.
    /// </remarks>
    public uint RootDirSector;

    /// Represents the size of the root directory in bytes within the XGD file system.
    /// This value indicates the total size allocated for the root directory's data
    /// in the XGD structure. It is used to calculate the amount of space required
    /// to read and process the root directory's content.
    /// The size is expressed as an unsigned 32-bit integer (uint), and its
    /// interpretation depends on the context of the XGD file system being handled.
    /// Used in operations such as decoding, reading, and interpreting the
    /// root directory structure to ensure proper allocation and processing of
    /// directory data.
    public uint RootDirSize;

    /// <summary>
    /// Represents the date and time when the XGD file system was created.
    /// It is derived from the CreationFileTime value in the XgdHeader and is
    /// converted to a <see cref="DateTime"/> instance using UTC.
    /// </summary>
    public DateTime CreationDateTime;
}