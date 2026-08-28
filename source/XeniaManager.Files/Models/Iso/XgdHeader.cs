using System.Runtime.InteropServices;

namespace XeniaManager.Core.Models.Files.Iso;

/// <summary>
/// Internal structure representing the XGD (Xbox Disc Format) header.
/// This header is found at a specific sector in Xbox ISO files.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct XgdHeader
{
    /// <summary>
    /// Magic string identifying the disc format ("MICROSOFT*XBOX*MEDIA").
    /// </summary>
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
    public byte[] Magic;

    /// <summary>
    /// The sector offset of the root directory.
    /// </summary>
    public uint RootDirSector;

    /// <summary>
    /// The size of the root directory in bytes.
    /// </summary>
    public uint RootDirSize;

    /// <summary>
    /// The creation time of the disc image as a Windows file time.
    /// </summary>
    public long CreationFileTime;

    /// <summary>
    /// Padding to align the structure.
    /// </summary>
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x7C8)]
    public byte[] Padding;

    /// <summary>
    /// Duplicate magic string at the end of the header for validation.
    /// </summary>
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
    public byte[] MagicTail;
}