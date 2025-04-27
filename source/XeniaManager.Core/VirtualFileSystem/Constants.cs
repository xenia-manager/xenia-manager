namespace XeniaManager.Core.VirtualFileSystem;

/// <summary>
/// A static class that contains constant values used across the virtual file system
/// implementation within the XeniaManager.Core.VirtualFileSystem namespace.
/// </summary>
public static class Constants
{
    /// <summary>
    /// Represents the offset value for the header section within a file.
    /// </summary>
    /// <remarks>
    /// This constant is used as a base address or starting point to identify the beginning of the header.
    /// </remarks>
    public const long HEADER_OFFSET = 0x0;

    /// <summary>
    /// Specifies the length of the header in bytes within the virtual file system.
    /// </summary>
    public const long HEADER_LENGTH = 0x4;
    
    // STFS
    /// <summary>
    /// Specifies the offset in bytes for the Title ID field within an STFS (Secure Transacted File System) structure.
    /// This constant is used to locate and extract the Title ID from the file.
    /// </summary>
    public const long STFS_TITLEID_OFFSET = 0x0360;

    /// <summary>
    /// Defines the length, in bytes, of the Title ID field in an STFS (Secure Transacted File System) file.
    /// This constant is used to determine how many bytes to read when extracting the Title ID metadata from the file.
    /// </summary>
    public const int STFS_TITLEID_LENGTH = 0x4;

    /// <summary>
    /// Represents the byte offset within an STFS (Secure Transacted File System) file where the Media ID is stored.
    /// This offset is used to locate and extract the Media ID value, which is a unique identifier
    /// for the media associated with the file.
    /// </summary>
    public const long STFS_MEDIAID_OFFSET = 0x0354;

    /// <summary>
    /// Represents the length, in bytes, of the Media ID field within an STFS (Secure Transacted File System) file.
    /// This value is used to determine the exact size of the Media ID data when reading from the file.
    /// </summary>
    public const int STFS_MEDIAID_LENGTH = 0x4;

    /// <summary>
    /// Represents the file offset where the STFS (Secure Transacted File System) title is stored.
    /// This constant is used to locate and extract the title from the binary data
    /// of STFS container files in Xbox 360 systems.
    /// </summary>
    public const long STFS_TITLE_OFFSET = 0x1691;

    /// <summary>
    /// Specifies the byte offset in the STFS (Secure Transacted File System) file header where
    /// the display name of the content is stored.
    /// </summary>
    /// <remarks>
    /// The display name represents a human-readable name for the content, such as
    /// a game or application title. This constant is used when parsing STFS files
    /// to locate the specific section containing the display name data.
    /// </remarks>
    public const long STFS_DISPLAYNAME_OFFSET = 0x411;

    /// <summary>
    /// Represents the maximum length of the title in an STFS (Secure Transacted File System) file.
    /// This constant defines the number of bytes allocated for the title field
    /// when extracting or writing title information within an STFS file structure.
    /// </summary>
    public const int STFS_TITLE_LENGTH = 0x80;
    
    // Xgd
    /// <summary>
    /// Represents the default filename for an XEX file.
    /// </summary>
    /// <remarks>
    /// This constant is used as a reference in file handling operations to identify and process files
    /// associated with the XEX format, such as during binary extraction or container analysis.
    /// </remarks>
    public const string XEX_FILE_NAME = "default.xex";

    /// <summary>
    /// The default file name for Xbox Executable (XBE) files within the Virtual File System.
    /// </summary>
    /// <remarks>
    /// Used as a constant to identify or compare file names when processing Xbox Executable files in the system.
    /// </remarks>
    public const string XBE_FILE_NAME = "default.xbe";

    /// <summary>
    /// Represents the unique identifier or "magic" string for XGD (Xbox Game Disc) image files.
    /// </summary>
    /// <remarks>
    /// This constant is used as a validation marker to verify the authenticity and format of Xbox image files
    /// by comparing it with the "Magic" and "MagicTail" headers within the file.
    /// </remarks>
    public const string XGD_IMAGE_MAGIC = "MICROSOFT*XBOX*MEDIA";

    /// <summary>
    /// Defines the size of a sector in an Xbox Game Disc (XGD) format.
    /// </summary>
    /// <remarks>
    /// This constant specifies the sector size, which is used in operations such as reading,
    /// decoding, and processing sectors within an XGD-format file or virtual file system.
    /// </remarks>
    public const uint XGD_SECTOR_SIZE = 0x800;

    /// <summary>
    /// Represents the base sector offset of an Xbox Game Disc (XGD) ISO image.
    /// </summary>
    /// <remarks>
    /// This constant is used as a reference point for calculating sector-based offsets
    /// within various disc reading and decoding operations. It serves as the foundational
    /// sector from which further sector positions are derived in the context of parsing
    /// Xbox Game Disc ISO structures.
    /// </remarks>
    public const uint XGD_ISO_BASE_SECTOR = 0x20;

    /// <summary>
    /// Represents the magic sector offset specifically for the XDK (Xbox Development Kit) used in XGD ISO structures.
    /// </summary>
    /// <remarks>
    /// This constant is a reference offset within the ISO to locate specific sectors corresponding to XDK files
    /// or data structures. It aids in decoding and processing XGD-formatted files during initialization of the sector decoder.
    /// </remarks>
    public const uint XGD_MAGIC_SECTOR_XDKI = XGD_ISO_BASE_SECTOR;

    /// <summary>
    /// Represents the sector position used to identify the magic header for XGD1 disc format.
    /// </summary>
    /// <remarks>
    /// This constant is used during disc structure analysis to verify and locate XGD1 data.
    /// It serves as a reference point for specific operations involved in disc sector decoding.
    /// </remarks>
    public const uint XGD_MAGIC_SECTOR_XGD1 = 0x30620;

    /// <summary>
    /// Defines the specific sector offset for the XGD2 disc format magic value.
    /// </summary>
    /// <remarks>
    /// This constant is used to identify and locate the sector containing the magic value
    /// for XGD2 formatted discs during decoding or validation processes.
    /// It serves as a reference point in operations involving Xbox 360 Game Disc version 2 (XGD2).
    /// </remarks>
    public const uint XGD_MAGIC_SECTOR_XGD2 = 0x1FB40;

    /// <summary>
    /// Represents the magic sector value specific to the XGD3 disc format.
    /// </summary>
    /// <remarks>
    /// This constant is used in the process of identifying and accessing the base sector
    /// for XGD3 disc images. It serves as a key marker for sector decoding operations
    /// in the virtual file system.
    /// </remarks>
    public const uint XGD_MAGIC_SECTOR_XGD3 = 0x4120;
}