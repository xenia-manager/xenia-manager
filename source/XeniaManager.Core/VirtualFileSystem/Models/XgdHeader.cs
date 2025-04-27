using System.Runtime.InteropServices;

namespace XeniaManager.Core.VirtualFileSystem.Models;

/// <summary>
/// Represents the XGD file system header structure.
/// This class provides information about the root directory sector, size, creation time,
/// and other metadata relevant to the XGD format.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public class XgdHeader
{
    /// <summary>
    /// An array of bytes representing the "Magic" value used to identify and validate the integrity of the XGD file header.
    /// </summary>
    /// <remarks>
    /// This field is part of the <see cref="XgdHeader"/> structure and plays a critical role in determining
    /// whether a sector contains valid XGD image data. The field typically holds a predefined string,
    /// such as <see cref="Constants.XGD_IMAGE_MAGIC"/>, encoded in bytes.
    /// </remarks>
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
    public byte[] Magic = Array.Empty<byte>();

    /// <summary>
    /// Represents the starting sector of the root directory in an XGD (Xbox Game Disc) structure.
    /// This value indicates the sector position where the root directory is located
    /// within the disc's filesystem, enabling navigation and access to its contents.
    /// </summary>
    public uint RootDirSector;

    /// <summary>
    /// Specifies the size of the root directory in bytes for the XgdHeader structure.
    /// </summary>
    /// <remarks>
    /// This field is used to determine the total size allocated to the root directory
    /// of the file system in the context of the XgdHeader. It is represented as an unsigned
    /// 32-bit integer.
    /// </remarks>
    public uint RootDirSize;

    /// <summary>
    /// Represents the file time of creation for the XGD header, stored as a 64-bit signed integer.
    /// This value is typically in the Windows file time format, which represents the number
    /// of 100-nanosecond intervals that have elapsed since January 1, 1601 (UTC).
    /// </summary>
    public long CreationFileTime;

    /// <summary>
    /// Represents a reserved byte array primarily used for padding within the structure to align data
    /// or fill unused space based on specified size constraints.
    /// It is defined with a fixed size of 0x7c8 to ensure the correct memory layout in the serialized data.
    /// </summary>
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x7c8)]
    public byte[] Padding = Array.Empty<byte>();

    /// <summary>
    /// Represents the trailing magic bytes used in the <see cref="XgdHeader"/> structure
    /// for verifying and identifying a valid XGD (Xbox Game Disc) image structure.
    /// </summary>
    /// <remarks>
    /// This byte array is expected to match a predefined magic string, typically
    /// defined by <see cref="Constants.XGD_IMAGE_MAGIC"/>, to validate the integrity
    /// and format of the disc image.
    /// </remarks>
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
    public byte[] MagicTail = Array.Empty<byte>();
}