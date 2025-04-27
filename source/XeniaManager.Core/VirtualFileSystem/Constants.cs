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
}