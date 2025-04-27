using System.Runtime.InteropServices;

namespace XeniaManager.Core.VirtualFileSystem.Models;

/// <summary>
/// Represents the header structure of an XEX (Xbox Executable) file.
/// </summary>
/// <remarks>
/// The XEX header contains important metadata and pointers to other sections of the executable,
/// including the security information, module flags, and header directory entries.
/// This structure is typically read and interpreted when working with XEX files in the Xbox file system.
/// </remarks>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct XexHeader
{
    /// <summary>
    /// Represents the magic number sequence used in the header structure of XEX files.
    /// </summary>
    /// <remarks>
    /// - This field is a byte array with a fixed size of 4.
    /// - It is typically used to identify or validate the file format.
    /// </remarks>
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public byte[] Magic;

    /// Represents the flag settings of a module in the XexHeader structure.
    /// This field defines various characteristics and settings for the module.
    public uint ModuleFlags;

    /// <summary>
    /// Represents the size of the headers within the XEX image structure.
    /// This value indicates the total size of the headers in bytes
    /// as defined in the XexHeader structure.
    /// </summary>
    public uint SizeOfHeaders;

    /// <summary>
    /// Represents the size of discardable headers in the XexHeader structure.
    /// </summary>
    /// <remarks>
    /// The value of this field specifies the size, in bytes, of the portions of the headers
    /// that can be discarded after the initial processing. These portions are typically
    /// used during the loading or initialization phase and are not required to remain in memory
    /// afterward, optimizing resource usage.
    /// </remarks>
    public uint SizeOfDiscardableHeaders;

    /// <summary>
    /// Represents the security information offset within the XexHeader structure.
    /// This value, when interpreted, points to the location of the SecurityInfo
    /// structure in the binary data of the XEX file. It is utilized during the
    /// parsing and extraction of the XEX header contents to validate and read
    /// security metadata associated with the file.
    /// </summary>
    public uint SecurityInfo;

    /// <summary>
    /// Represents the count of header directory entries in the XEX file header structure.
    /// </summary>
    /// <remarks>
    /// This value indicates the number of entries in the header directory,
    /// which can be used to iterate through or process the directory entries
    /// when parsing or analyzing the XEX file.
    /// </remarks>
    public uint HeaderDirectoryEntryCount;
}

/// <summary>
/// Represents the Hypervisor Image Information (HvImageInfo) structure that contains
/// metadata about an image loaded in an Xbox 360 environment. This structure provides
/// the details necessary for verification, integrity checks, and resource management.
/// </summary>
/// <remarks>
/// The HvImageInfo structure is critical in the context of Xbox 360's Hypervisor
/// and its secure environment. It encapsulates cryptographic and operational data,
/// including image signatures, hash values, flags, and load addresses, which are essential
/// for executing and validating applications or games in a secure manner.
/// </remarks>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct HvImageInfo
{
    /// <summary>
    /// Represents a cryptographic signature used within
    /// the <see cref="HvImageInfo"/> structure.
    /// </summary>
    /// <remarks>
    /// This field is a fixed-size array of bytes (256 bytes)
    /// used for security and verification.
    /// The content of the signature ensures the authenticity
    /// and integrity of image data.
    /// </remarks>
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x100)]
    public byte[] Signature;

    /// <summary>
    /// Represents the size of information within the <see cref="HvImageInfo"/> structure.
    /// </summary>
    /// <remarks>
    /// This value typically defines the total size of the data being described
    /// within the <see cref="HvImageInfo"/> structure. It contributes to the organization
    /// and parsing of data related to image headers and associated metadata.
    /// </remarks>
    public uint InfoSize;

    /// <summary>
    /// Represents flags related to image loading and configuration within the HvImageInfo structure.
    /// </summary>
    /// <remarks>
    /// The <c>ImageFlags</c> field is typically used to store specific attributes or
    /// properties associated with an executable image in the context of the XEX (Xbox Executable) format.
    /// These attributes may indicate particular loading or security-related characteristics
    /// of the image, such as compatibility or additional metadata.
    /// </remarks>
    public uint ImageFlags;

    /// <summary>
    /// Represents the memory load address of the executable image in the system's memory layout.
    /// This address is used to define where the image is mapped and executed in memory.
    /// </summary>
    public uint LoadAddress;

    /// <summary>
    /// Represents the cryptographic hash of the image within the <see cref="HvImageInfo"/> structure.
    /// </summary>
    /// <remarks>
    /// The <c>ImageHash</c> is a 20-byte array that typically stores the hash value used for
    /// verifying the integrity of the executable image.
    /// </remarks>
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x14)]
    public byte[] ImageHash;

    /// <summary>
    /// Represents the number of entries in the import table within the HvImageInfo structure.
    /// </summary>
    /// <remarks>
    /// This variable is part of the HvImageInfo structure, used in defining header information
    /// for a HV (Hypervisor) image. It is typically used to store the count of import table entries,
    /// facilitating lookups for imported functions or data in a binary.
    /// </remarks>
    public uint ImportTableCoun;

    /// <summary>
    /// Represents a cryptographic hash (SHA-1) used to verify
    /// the integrity of imported table data within the hypervisor image information.
    /// </summary>
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x14)]
    public byte[] ImportDigest;

    /// <summary>
    /// Represents the unique identifier associated with the media content.
    /// </summary>
    /// <remarks>
    /// This 16-byte array is used to uniquely identify a specific piece of media
    /// within the system. It is typically stored within the <see cref="HvImageInfo"/>
    /// structure, encapsulating media-related metadata.
    /// </remarks>
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x10)]
    public byte[] MediaID;

    /// <summary>
    /// Represents the cryptographic key used for securing the image contents within the
    /// HvImageInfo structure. It is a 16-byte key that is crucial for validation and integrity
    /// checks of the associated image data.
    /// </summary>
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x10)]
    public byte[] ImageKey;

    /// Represents the memory address of the export table within the image header structure of an XEX file.
    /// This field is part of the `HvImageInfo` structure and provides a reference to the export table,
    /// which is used to access or query functions or symbols that are exposed by the module during execution.
    /// The export table address is stored as a 32-bit unsigned integer and is an integral part of the
    /// image header required for resolving external dependencies or function calls.
    /// Typically used in the context of reverse engineering or low-level manipulation of executable files
    /// in environments using XEX format.
    public uint ExportTableAddress;

    /// <summary>
    /// Represents the hash of the header in the image information structure.
    /// This field is typically used to verify the integrity and authenticity of the header data.
    /// It is a 20-byte array containing the SHA-1 hash of the header.
    /// </summary>
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x14)]
    public byte[] HeaderHash;

    /// <summary>
    /// Represents the region information for a game, typically used to define
    /// the geographical or compatibility region where the game is intended to be played.
    /// This value can determine whether the game is executable on a particular console
    /// depending on the region-locking system or compatibility settings.
    /// </summary>
    public uint GameRegion;
}

/// <summary>
/// Represents the security information structure for an XEX file.
/// This structure is designed to encapsulate and interpret critical
/// security-related metadata, such as the size of the XEX image,
/// allowed media types, page descriptor count, and associated image
/// information.
/// The fields of this structure are essential for the internal
/// context when parsing and validating XEX files within a virtual
/// file system or as part of an XEX utility library.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct XexSecurityInfo
{
    /// <summary>
    /// Represents the size of a specific object or region within the context
    /// of the XexSecurityInfo structure. This variable is typically used to
    /// denote the size in bytes and serves as a key attribute to describe
    /// memory allocation or resource management.
    /// </summary>
    public uint Size;

    /// <summary>
    /// Represents the size of the executable image in bytes.
    /// This field provides the total size of the image as it appears
    /// in memory, including all sections and related data.
    /// </summary>
    public uint ImageSize;

    /// Represents the image information of a Hypervisor (Hv) in the context of the XexSecurityInfo structure.
    /// This variable is part of the XexSecurityInfo structure and contains detailed information
    /// about the image, including its signature, hash values, size, flags, and other related metadata.
    /// The ImageInfo structure is used to verify and work with the security data of XEX (Xbox Executable) files
    /// in Xbox 360 emulation or related systems.
    public HvImageInfo ImageInfo;

    /// <summary>
    /// Represents the types of media that are allowed for execution within
    /// the context of the XexSecurityInfo structure. This variable is used
    /// to identify and restrict the media formats that a particular title
    /// or executable can be run on, such as optical discs, hard drives, or
    /// other storage media.
    /// </summary>
    public uint AllowedMediaTypes;

    /// <summary>
    /// Represents the total count of page descriptors associated with the XEX security information.
    /// Page descriptors are typically used to define memory regions or sections within an executable.
    /// </summary>
    public uint PageDescriptorCount;
}

/// <summary>
/// Represents the execution details of an Xbox executable (XEX) file.
/// This structure encapsulates the metadata and binary details that define
/// the environment and execution parameters of the XEX file.
/// The supported fields include information about the game's media ID, version,
/// base version, title ID, publisher ID, game ID, platform, executable type, disc number,
/// total discs, and save game ID.
/// The struct is specifically laid out in memory with explicit offsets,
/// ensuring compatibility with the binary representation of XEX execution data.
/// </summary>
[StructLayout(LayoutKind.Explicit, Pack = 1)]
internal struct XexExecution
{
    /// <summary>
    /// Represents the Media ID associated with a specific Xex execution structure.
    /// This value is typically used to uniquely identify media-related information
    /// for a given executable on the Xbox 360 system.
    /// </summary>
    [FieldOffset(0)]
    public uint MediaId;

    /// Represents the version of the executable in the XexExecution structure.
    /// This field defines the version of the software or executable binary
    /// being described. It is stored as a 32-bit unsigned integer in the
    /// specified structure.
    /// The version can be used for identifying compatibility or runtime
    /// requirements for the associated executable.
    /// This is stored at an offset of 4 bytes within the XexExecution structure.
    [FieldOffset(4)]
    public uint Version;

    /// <summary>
    /// Represents the base version of the executable in the Xex file structure.
    /// This field is part of the <see cref="XexExecution"/> structure and is used to indicate
    /// the starting version of a title or component.
    /// </summary>
    [FieldOffset(8)]
    public uint BaseVersion;

    /// <summary>
    /// Represents the unique identifier (ID) for a title in the context of XEX execution structures.
    /// </summary>
    /// <remarks>
    /// TitleId is a 32-bit unsigned integer and is used to uniquely identify a specific game or application.
    /// It forms part of the XexExecution structure, along with other fields such as MediaId, Version, and BaseVersion.
    /// </remarks>
    [FieldOffset(12)]
    public uint TitleId;

    /// <summary>
    /// Represents the unique identifier for the publisher of the game or application.
    /// </summary>
    /// <remarks>
    /// This field is part of the <c>XexExecution</c> structure and is used to associate a game or application
    /// with its respective publisher. It is stored as an unsigned 16-bit integer.
    /// </remarks>
    [FieldOffset(12)]
    public ushort PublisherId;

    /// <summary>
    /// Represents the unique identifier for a game within the execution metadata of the XEX (Xbox Executable) file structure.
    /// </summary>
    /// <remarks>
    /// The <c>GameId</c> is a 16-bit unsigned integer field residing within the <see cref="XexExecution"/> structure.
    /// It typically identifies a specific game and may be used to associate data or settings tailored to that game.
    /// </remarks>
    [FieldOffset(14)]
    public ushort GameId;

    /// <summary>
    /// Represents the platform type identifier for an application or game in the XexExecution structure.
    /// </summary>
    /// <remarks>
    /// The <c>Platform</c> field indicates the target platform for the corresponding executable.
    /// It is stored as a single byte and occupies a fixed position within the XexExecution structure,
    /// ensuring alignment with the other fields. The value of this field can be used to determine
    /// the platform compatibility or origin of the executable.
    /// </remarks>
    [FieldOffset(16)]
    public byte Platform;

    /// <summary>
    /// Represents the type of executable in the XEX (Xbox Executable) structure.
    /// Used to specify the nature or format of the executable, such as retail, debug, or test executable.
    /// </summary>
    /// <remarks>
    /// This variable is a part of the <see cref="XexExecution"/> struct and is stored as a single byte.
    /// The value likely corresponds to predefined categories or formats of Xbox executables as determined by the XEX format specifications.
    /// </remarks>
    [FieldOffset(17)]
    public byte ExecutableType;

    /// <summary>
    /// Represents the disc number in a multi-disc set for the associated executable.
    /// </summary>
    /// <remarks>
    /// This field indicates which disc, within a multi-disc game or application, is associated with the executable.
    /// It is a byte-sized value, part of the XexExecution structure, stored at a fixed offset within the structure.
    /// </remarks>
    [FieldOffset(18)]
    public byte DiscNum;

    /// <summary>
    /// Represents the total number of discs in a multi-disc game.
    /// </summary>
    /// <remarks>
    /// This field is part of the struct XexExecution and indicates the total number of discs involved in the execution.
    /// It is commonly used to determine the complete set of discs belonging to a title.
    /// </remarks>
    [FieldOffset(19)]
    public byte DiscTotal;

    /// <summary>
    /// Represents the unique identifier for the saved game data associated with a specific game or title.
    /// This field is used to distinguish individual save data and enables proper handling
    /// of game states during the saving and loading processes.
    /// </summary>
    [FieldOffset(20)]
    public uint SaveGameID;
}