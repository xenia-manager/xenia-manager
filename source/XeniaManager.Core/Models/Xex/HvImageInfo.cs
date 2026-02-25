using System.Runtime.InteropServices;

namespace XeniaManager.Core.Models.Xex;

/// <summary>
/// Hypervisor image information structure.
/// <para>
/// Contains cryptographic signatures, hashes, and load parameters for secure boot.
/// This structure is part of the security info and is used by the hypervisor to verify
/// the authenticity and integrity of the executable before loading it.
/// </para>
/// <para>
/// The RSA signature (256 bytes) is used to verify the image was signed by Microsoft.
/// The SHA hashes are used to verify specific sections haven't been tampered with.
/// The ImageKey is a seed for deriving the AES-CBC decryption key.
/// </para>
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct HvImageInfo
{
    /// <summary>
    /// RSA-2048 signature for the image (256 bytes).
    /// Used to verify the authenticity of the executable was signed by Microsoft.
    /// Offset: 0x04 (0x100 bytes)
    /// </summary>
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x100)]
    public byte[] Signature;

    /// <summary>
    /// Size of the image info structure.
    /// Offset: 0x104 (4 bytes)
    /// </summary>
    public uint InfoSize;

    /// <summary>
    /// Image flags controlling load behavior and permissions.
    /// Offset: 0x108 (4 bytes)
    /// </summary>
    public uint ImageFlags;

    /// <summary>
    /// Memory address where the image should be loaded.
    /// Offset: 0x10C (4 bytes)
    /// </summary>
    public uint LoadAddress;

    /// <summary>
    /// SHA-1 hash of the image data (20 bytes).
    /// Used for integrity verification of the decrypted/decompressed image.
    /// Offset: 0x110 (0x14 bytes)
    /// </summary>
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x14)]
    public byte[] ImageHash;

    /// <summary>
    /// Number of import table entries.
    /// Offset: 0x124 (4 bytes)
    /// </summary>
    public uint ImportTableCount;

    /// <summary>
    /// SHA-1 hash of the import table (20 bytes).
    /// Used to verify the integrity of imported libraries.
    /// Offset: 0x128 (0x14 bytes)
    /// </summary>
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x14)]
    public byte[] ImportDigest;

    /// <summary>
    /// Media ID (16 bytes).
    /// Identifies the specific disc or media type for multi-disc games.
    /// Offset: 0x13C (0x10 bytes)
    /// </summary>
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x10)]
    public byte[] MediaId;

    /// <summary>
    /// AES key seed for image decryption (16 bytes).
    /// This is combined with console-specific keys to derive the actual AES-CBC decryption key.
    /// Offset: 0x14C (0x10 bytes)
    /// </summary>
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x10)]
    public byte[] ImageKey;

    /// <summary>
    /// Memory address of the export table.
    /// Offset: 0x15C (4 bytes)
    /// </summary>
    public uint ExportTableAddress;

    /// <summary>
    /// SHA-1 hash of the header (20 bytes).
    /// Used to verify the integrity of the XEX header.
    /// Offset: 0x160 (0x14 bytes)
    /// </summary>
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x14)]
    public byte[] HeaderHash;

    /// <summary>
    /// Game region code indicating geographic restrictions.
    /// Region codes:
    /// - 0x00000001: North America
    /// - 0x00000002: Japan
    /// - 0x00000004: Europe/Australia
    /// Offset: 0x174 (4 bytes)
    /// </summary>
    public uint GameRegion;
}