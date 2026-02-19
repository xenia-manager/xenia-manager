using System.Buffers.Binary;
using XeniaManager.Core.Logging;

namespace XeniaManager.Core.Models.Gpd;

/// <summary>
/// Represents an entry in the XDBF free space table.
/// Used to map out unused space within the file for efficient allocation.
/// Total length: 8 bytes
/// </summary>
public record struct FreeSpaceEntry
{
    /// <summary>
    /// Offset specifier for the free space block.
    /// For the final entry, this represents the length (file length - header and tables).
    /// Offset: 0x0, Length: 4 bytes
    /// </summary>
    public uint OffsetSpecifier;

    /// <summary>
    /// Length of the free space block.
    /// For the final entry, this is -1 - OffsetSpecifier.
    /// Offset: 0x4, Length: 4 bytes
    /// </summary>
    public uint Length;

    /// <summary>
    /// Gets whether this entry represents actual free space (vs. end-of-file marker).
    /// </summary>
    public bool IsFreeSpace => Length != 0xFFFFFFFF;

    /// <summary>
    /// Parses a free space entry from raw bytes.
    /// </summary>
    /// <param name="data">The raw byte data containing the entry.</param>
    /// <param name="offset">The offset to start reading from.</param>
    /// <param name="isBigEndian">Whether the data is in big-endian format.</param>
    /// <returns>The parsed FreeSpaceEntry structure.</returns>
    public static FreeSpaceEntry FromBytes(byte[] data, int offset, bool isBigEndian = true)
    {
        Logger.Trace<FreeSpaceEntry>($"Parsing free space entry from bytes at offset {offset}");

        if (data.Length < offset + 8)
        {
            Logger.Error<FreeSpaceEntry>($"Data too short for free space entry (expected 8, got {data.Length - offset})");
            throw new ArgumentException("Data too short for free space entry");
        }

        FreeSpaceEntry entry = new FreeSpaceEntry
        {
            OffsetSpecifier = isBigEndian
                ? BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset))
                : BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(offset)),
            Length = isBigEndian
                ? BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset + 0x4))
                : BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(offset + 0x4))
        };

        Logger.Trace<FreeSpaceEntry>($"Free space entry parsed - Offset: 0x{entry.OffsetSpecifier:X8}, " +
                                     $"Length: {entry.Length}, IsFreeSpace: {entry.IsFreeSpace}");
        return entry;
    }

    /// <summary>
    /// Converts the entry to a byte array.
    /// </summary>
    /// <param name="isBigEndian">Whether to write in big-endian format.</param>
    /// <returns>The entry as a byte array.</returns>
    public byte[] ToBytes(bool isBigEndian = true)
    {
        Logger.Trace<FreeSpaceEntry>($"Converting free space entry to bytes (Offset: 0x{OffsetSpecifier:X8}, Length: {Length})");

        byte[] data = new byte[8];

        if (isBigEndian)
        {
            BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(0x0), OffsetSpecifier);
            BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(0x4), Length);
        }
        else
        {
            BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(0x0), OffsetSpecifier);
            BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(0x4), Length);
        }

        Logger.Trace<FreeSpaceEntry>($"Free space entry converted to bytes (8 bytes)");
        return data;
    }
}