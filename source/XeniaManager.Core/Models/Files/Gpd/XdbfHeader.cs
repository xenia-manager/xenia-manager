using System.Buffers.Binary;
using XeniaManager.Core.Logging;

namespace XeniaManager.Core.Models.Files.Gpd;

/// <summary>
/// Represents the XDBF file header structure.
/// Total length: 24 bytes (0x18)
/// Byte ordering depends on magic - little endian indicates GFWL, big endian indicates Xbox.
/// </summary>
public struct XdbfHeader
{
    /// <summary>
    /// Magic identifier (0x58444246 = "XDBF").
    /// Offset: 0x0, Length: 4 bytes
    /// </summary>
    public uint Magic;

    /// <summary>
    /// File version (typically 0x10000).
    /// Offset: 0x4, Length: 4 bytes
    /// </summary>
    public uint Version;

    /// <summary>
    /// Entry table length in number of entries (multiple of 512).
    /// Offset: 0x8, Length: 4 bytes
    /// </summary>
    public uint EntryTableLength;

    /// <summary>
    /// Number of valid entries in the entry table.
    /// Offset: 0xC, Length: 4 bytes
    /// </summary>
    public uint EntryCount;

    /// <summary>
    /// Free space table length in number of entries (multiple of 512).
    /// Offset: 0x10, Length: 4 bytes
    /// </summary>
    public uint FreeSpaceTableLength;

    /// <summary>
    /// Number of valid entries in the free space table.
    /// Offset: 0x14, Length: 4 bytes
    /// </summary>
    public uint FreeSpaceTableEntryCount;

    /// <summary>
    /// Gets whether the file is in big-endian (Xbox) format.
    /// </summary>
    public bool IsBigEndian => Magic == 0x58444246;

    /// <summary>
    /// Gets whether the file is in little-endian (GFWL) format.
    /// </summary>
    public bool IsLittleEndian => Magic == 0x46424458;

    /// <summary>
    /// Parses an XDBF header from raw bytes.
    /// </summary>
    /// <param name="data">The raw byte data containing the header.</param>
    /// <param name="offset">The offset to start reading from.</param>
    /// <returns>The parsed XdbfHeader structure.</returns>
    /// <exception cref="ArgumentException">Thrown when the magic is invalid.</exception>
    public static XdbfHeader FromBytes(byte[] data, int offset = 0)
    {
        Logger.Trace<XdbfHeader>($"Parsing XDBF header from bytes at offset {offset}");

        if (data.Length < offset + 24)
        {
            Logger.Error<XdbfHeader>($"Data too short for XDBF header (expected at least {offset + 24} bytes, got {data.Length})");
            throw new ArgumentException("Data too short for XDBF header");
        }

        uint magic = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset));
        bool isBigEndian = magic == 0x58444246;

        Logger.Trace<XdbfHeader>($"Magic: 0x{magic:X8}, Endianness: {(isBigEndian ? "Big (Xbox)" : "Little (GFWL)")}");

        XdbfHeader header = new XdbfHeader
        {
            Magic = magic,
            Version = isBigEndian
                ? BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset + 0x4))
                : BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(offset + 0x4)),
            EntryTableLength = isBigEndian
                ? BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset + 0x8))
                : BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(offset + 0x8)),
            EntryCount = isBigEndian
                ? BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset + 0xC))
                : BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(offset + 0xC)),
            FreeSpaceTableLength = isBigEndian
                ? BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset + 0x10))
                : BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(offset + 0x10)),
            FreeSpaceTableEntryCount = isBigEndian
                ? BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset + 0x14))
                : BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(offset + 0x14))
        };

        Logger.Debug<XdbfHeader>($"Version: 0x{header.Version:X8}, Entry Table: {header.EntryCount}/{header.EntryTableLength}, Free Space: {header.FreeSpaceTableEntryCount}/{header.FreeSpaceTableLength}");

        if (header is { IsBigEndian: false, IsLittleEndian: false })
        {
            Logger.Error<XdbfHeader>($"Invalid XDBF magic: 0x{magic:X8}");
            throw new ArgumentException($"Invalid XDBF magic: 0x{magic:X8}");
        }

        Logger.Debug<XdbfHeader>($"Successfully parsed XDBF header");
        return header;
    }

    /// <summary>
    /// Converts the header to a byte array.
    /// </summary>
    /// <returns>The header as a byte array.</returns>
    public byte[] ToBytes()
    {
        Logger.Trace<XdbfHeader>("Converting XDBF header to bytes");

        byte[] data = new byte[24];
        bool isBigEndian = IsBigEndian;

        if (isBigEndian)
        {
            BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(0x0), Magic);
            BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(0x4), Version);
            BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(0x8), EntryTableLength);
            BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(0xC), EntryCount);
            BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(0x10), FreeSpaceTableLength);
            BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(0x14), FreeSpaceTableEntryCount);
        }
        else
        {
            BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(0x0), Magic);
            BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(0x4), Version);
            BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(0x8), EntryTableLength);
            BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(0xC), EntryCount);
            BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(0x10), FreeSpaceTableLength);
            BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(0x14), FreeSpaceTableEntryCount);
        }

        Logger.Trace<XdbfHeader>($"Header converted to bytes (24 bytes, Magic: 0x{Magic:X8})");
        return data;
    }
}