using System.Buffers.Binary;
using XeniaManager.Core.Logging;

namespace XeniaManager.Core.Models.Files.Gpd;

/// <summary>
/// Represents an entry in the XDBF entry table.
/// Each entry maps to a specific namespace and ID with associated data.
/// Total length: 18 bytes (0x12)
/// </summary>
public record struct EntryTableEntry
{
    /// <summary>
    /// Namespace identifier (Achievement, Image, Setting, Title, String, etc.).
    /// Offset: 0x0, Length: 2 bytes
    /// </summary>
    public EntryNamespace Namespace;

    /// <summary>
    /// Entry ID within the namespace.
    /// Special IDs: 0x100000000 = Sync List, 0x200000000 = Sync Data
    /// Offset: 0x2, Length: 8 bytes
    /// </summary>
    public ulong Id;

    /// <summary>
    /// Offset specifier - used to calculate the real data offset.
    /// Offset: 0xA, Length: 4 bytes
    /// </summary>
    public uint OffsetSpecifier;

    /// <summary>
    /// Length of the entry data.
    /// Offset: 0xE, Length: 4 bytes
    /// </summary>
    public uint Length;

    /// <summary>
    /// Gets whether this entry is a Sync List entry.
    /// </summary>
    public bool IsSyncList => Id == 0x100000000 || Id == 1;

    /// <summary>
    /// Gets whether this entry is a Sync Data entry.
    /// </summary>
    public bool IsSyncData => Id == 0x200000000 || Id == 2;

    /// <summary>
    /// Parses an entry table entry from raw bytes.
    /// </summary>
    /// <param name="data">The raw byte data containing the entry.</param>
    /// <param name="offset">The offset to start reading from.</param>
    /// <param name="isBigEndian">Whether the data is in big-endian format.</param>
    /// <returns>The parsed EntryTableEntry structure.</returns>
    public static EntryTableEntry FromBytes(byte[] data, int offset, bool isBigEndian = true)
    {
        Logger.Trace<EntryTableEntry>($"Parsing entry table entry from bytes at offset {offset}");

        if (data.Length < offset + 18)
        {
            Logger.Error<EntryTableEntry>($"Data too short for entry table entry (expected 18, got {data.Length - offset})");
            throw new ArgumentException("Data too short for entry table entry");
        }

        EntryTableEntry entry = new EntryTableEntry
        {
            Namespace = isBigEndian
                ? (EntryNamespace)BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(offset))
                : (EntryNamespace)BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(offset)),
            Id = isBigEndian
                ? BinaryPrimitives.ReadUInt64BigEndian(data.AsSpan(offset + 0x2))
                : BinaryPrimitives.ReadUInt64LittleEndian(data.AsSpan(offset + 0x2)),
            OffsetSpecifier = isBigEndian
                ? BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset + 0xA))
                : BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(offset + 0xA)),
            Length = isBigEndian
                ? BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset + 0xE))
                : BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(offset + 0xE))
        };

        Logger.Trace<EntryTableEntry>($"Entry parsed - Namespace: {entry.Namespace}, ID: 0x{entry.Id:X16}, " +
                                      $"Offset: 0x{entry.OffsetSpecifier:X8}, Length: {entry.Length}");
        return entry;
    }

    /// <summary>
    /// Converts the entry to a byte array.
    /// </summary>
    /// <param name="isBigEndian">Whether to write in big-endian format.</param>
    /// <returns>The entry as a byte array.</returns>
    public byte[] ToBytes(bool isBigEndian = true)
    {
        Logger.Trace<EntryTableEntry>($"Converting entry table entry to bytes (Namespace: {Namespace}, ID: 0x{Id:X16})");

        byte[] data = new byte[18];

        if (isBigEndian)
        {
            BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(0x0), (ushort)Namespace);
            BinaryPrimitives.WriteUInt64BigEndian(data.AsSpan(0x2), Id);
            BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(0xA), OffsetSpecifier);
            BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(0xE), Length);
        }
        else
        {
            BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(0x0), (ushort)Namespace);
            BinaryPrimitives.WriteUInt64LittleEndian(data.AsSpan(0x2), Id);
            BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(0xA), OffsetSpecifier);
            BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(0xE), Length);
        }

        Logger.Trace<EntryTableEntry>($"Entry table entry converted to bytes (18 bytes)");
        return data;
    }
}