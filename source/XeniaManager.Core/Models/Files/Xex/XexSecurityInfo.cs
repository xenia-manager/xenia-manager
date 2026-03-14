using System.Runtime.InteropServices;

namespace XeniaManager.Core.Models.Files.Xex;

/// <summary>
/// XEX security info structure.
/// <para>
/// Contains cryptographic signatures, hashes, image information, and media type restrictions
/// used to verify the integrity and authenticity of Xbox 360 executables.
/// </para>
/// <para>
/// The security info structure is located at the offset specified in the XEX header.
/// It contains a 256-byte RSA signature, SHA hashes, AES encryption keys, and region codes.
/// The cryptography is used to prevent unauthorized executables from running on the console.
/// </para>
/// <para>
/// Structure layout (offset from security info start):
/// - 0x00: Size (4 bytes)
/// - 0x04: Image Size (4 bytes)
/// - 0x08: HV Image Info (0x170 bytes) - contains signature, hashes, keys
/// - 0x178: Allowed Media Types (4 bytes)
/// - 0x17C: Page Descriptor Count (4 bytes)
/// </para>
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct XexSecurityInfo
{
    /// <summary>
    /// Size of the security info structure in bytes.
    /// Offset: 0x0 (4 bytes)
    /// </summary>
    public uint Size;

    /// <summary>
    /// Size of the loaded image in memory.
    /// Offset: 0x4 (4 bytes)
    /// </summary>
    public uint ImageSize;

    /// <summary>
    /// Hypervisor image information containing signatures, hashes, and load parameters.
    /// This structure contains the RSA signature, image hash, AES key, and other cryptographic data.
    /// Offset: 0x8 (0x170 bytes)
    /// </summary>
    public HvImageInfo ImageInfo;

    /// <summary>
    /// Bitmask of allowed media types (e.g., HDD, DVD, XBLA, USB).
    /// Restricts where the executable can be loaded from.
    /// Common values include:
    /// - 0x00000001: Hard Disk
    /// - 0x00000002: DVD-ROM
    /// - 0x00000004: Digital Download (XBLA)
    /// - 0x00000010: USB Memory Unit
    /// Offset: 0x178 (4 bytes)
    /// </summary>
    public uint AllowedMediaTypes;

    /// <summary>
    /// Number of page descriptors in the security info.
    /// Page descriptors define memory protection and encryption settings for each section.
    /// Each page descriptor is 24 bytes and follows the security info structure.
    /// Offset: 0x17C (4 bytes)
    /// </summary>
    public uint PageDescriptorCount;
}