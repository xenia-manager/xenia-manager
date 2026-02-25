using System.Runtime.InteropServices;

namespace XeniaManager.Core.Models.Xex;

/// <summary>
/// XEX header structure.
/// <para>
/// Total length: 24 bytes. Byte ordering: Big Endian.
/// </para>
/// <para>
/// The XEX header is the first structure in every Xbox 360 executable file.
/// It contains the magic bytes ("XEX2"), module flags, offsets to other structures,
/// and the count of optional headers in the directory.
/// </para>
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct XexHeader
{
    /// <summary>
    /// Magic bytes identifying the file as a XEX2 executable.
    /// Should contain "XEX2" (0x58455832 in big-endian) for valid Xbox 360 executables.
    /// Offset: 0x0 (4 bytes)
    /// </summary>
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public byte[] Magic;

    /// <summary>
    /// Module flags indicating the type and properties of the executable.
    /// This is a bitfield where each bit represents a different module attribute.
    /// Offset: 0x4 (4 bytes)
    /// <para>
    /// Module Flags (Bitfield):
    /// </para>
    /// - Bit 0: Title Module
    /// <para>
    /// - Bit 1: Exports To Title
    /// </para>
    /// - Bit 2: System Debugger
    /// <para>
    /// - Bit 3: DLL Module
    /// </para>
    /// - Bit 4: Module Patch
    /// <para>
    /// - Bit 5: Patch Full
    /// </para>
    /// - Bit 6: Patch Delta
    /// <para>
    /// - Bit 7: User Mode
    /// </para>
    /// </summary>
    public uint ModuleFlags;

    /// <summary>
    /// Offset to the PE data (program/section content) from the beginning of the file.
    /// The PE data is typically found at offset 0x2000 and contains the encrypted/compressed executable code.
    /// Offset: 0x8 (4 bytes)
    /// </summary>
    public uint SizeOfHeaders;

    /// <summary>
    /// Reserved field.
    /// Offset: 0xC (4 bytes)
    /// </summary>
    public uint SizeOfDiscardableHeaders;

    /// <summary>
    /// Offset to the security info structure from the beginning of the file.
    /// The security info contains RSA signatures, AES keys, image hashes, and region locks.
    /// Offset: 0x10 (4 bytes)
    /// </summary>
    public uint SecurityInfo;

    /// <summary>
    /// Number of entries in the optional header directory table.
    /// Each directory entry is 8 bytes (4 bytes ID + 4 bytes data/offset).
    /// The directory follows immediately after the XEX header at offset 0x18.
    /// Offset: 0x14 (4 bytes)
    /// </summary>
    public uint HeaderDirectoryEntryCount;
}