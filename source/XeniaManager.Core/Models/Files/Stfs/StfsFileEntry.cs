using System.Buffers.Binary;
using System.Text;
using XeniaManager.Core.Logging;

namespace XeniaManager.Core.Models.Files.Stfs;

/// <summary>
/// Represents a file entry in the STFS file table.
/// Each entry describes a file or directory within the STFS package.
/// </summary>
public class StfsFileEntry
{
    /// <summary>
    /// Maximum length of a file name.
    /// </summary>
    public const int MaxNameLength = 40;

    /// <summary>
    /// Size of a file entry.
    /// </summary>
    public const int Size = 0x40;

    /// <summary>
    /// File name, null-padded (40 bytes).
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Flags byte containing name length and directory flag.
    /// </summary>
    public byte Flags { get; set; }

    /// <summary>
    /// Number of blocks containing valid data (24-bit little endian).
    /// </summary>
    public int ValidDataBlocks { get; set; }

    /// <summary>
    /// Number of blocks allocated for the file (24-bit little endian).
    /// May be larger than ValidDataBlocks for sparse files.
    /// </summary>
    public int AllocatedDataBlocks { get; set; }

    /// <summary>
    /// Starting block number of the file (24-bit little endian).
    /// </summary>
    public int StartingBlock { get; set; }

    /// <summary>
    /// Path indicator (-1 for root directory, otherwise index of parent directory).
    /// </summary>
    public short PathIndicator { get; set; }

    /// <summary>
    /// Size of the file in bytes.
    /// </summary>
    public int FileSize { get; set; }

    /// <summary>
    /// Update date (FAT format, big endian).
    /// </summary>
    public ushort UpdateDate { get; set; }

    /// <summary>
    /// Update time (FAT format, big endian).
    /// </summary>
    public ushort UpdateTime { get; set; }

    /// <summary>
    /// Access date (FAT format, big endian).
    /// </summary>
    public ushort AccessDate { get; set; }

    /// <summary>
    /// Access time (FAT format, big endian).
    /// </summary>
    public ushort AccessTime { get; set; }

    /// <summary>
    /// Gets whether this entry represents a directory.
    /// Bit 7 of the flags byte.
    /// </summary>
    public bool IsDirectory => (Flags & 0x80) == 0x80;

    /// <summary>
    /// Gets whether all blocks in the file are consecutive.
    /// Bit 6 of the flags byte.
    /// </summary>
    public bool HasConsecutiveBlocks => (Flags & 0x40) == 0x40;

    /// <summary>
    /// Gets or sets the length of the file name (first 6 bits of flags).
    /// </summary>
    public byte NameLength
    {
        get => (byte)(Flags & 0x3F);
        set => Flags = (byte)((Flags & 0xC0) | (value & 0x3F));
    }

    /// <summary>
    /// Gets the update timestamp as a DateTime.
    /// </summary>
    public DateTime UpdateDateTime => DecodeFatTimestamp(UpdateDate, UpdateTime);

    /// <summary>
    /// Gets the access timestamp as a DateTime.
    /// </summary>
    public DateTime AccessDateTime => DecodeFatTimestamp(AccessDate, AccessTime);

    /// <summary>
    /// Decodes a FAT timestamp into a DateTime.
    /// FAT date/time format stores dates and times in a compact 16-bit format.
    /// Date: bits 0-4=day, 5-8=month, 9-15=year (from 1980)
    /// Time: bits 0-4=seconds/2, 5-10=minutes, 11-15=hours
    /// </summary>
    /// <param name="date">The FAT date value.</param>
    /// <param name="time">The FAT time value.</param>
    /// <returns>The decoded DateTime.</returns>
    private static DateTime DecodeFatTimestamp(ushort date, ushort time)
    {
        if (date == 0 && time == 0)
        {
            return DateTime.MinValue;
        }

        int day = date & 0x1F;
        int month = (date >> 5) & 0x0F;
        int year = 1980 + ((date >> 9) & 0x7F);

        int seconds = (time & 0x1F) * 2;
        int minutes = (time >> 5) & 0x3F;
        int hours = (time >> 11) & 0x1F;

        // Validate date components
        if (month is < 1 or > 12)
        {
            month = 1;
        }
        if (day is < 1 or > 31)
        {
            day = 1;
        }
        if (hours > 23)
        {
            hours = 0;
        }
        if (minutes > 59)
        {
            minutes = 0;
        }
        if (seconds > 59)
        {
            seconds = 0;
        }

        try
        {
            return new DateTime(year, month, day, hours, minutes, seconds);
        }
        catch
        {
            return DateTime.MinValue;
        }
    }

    /// <summary>
    /// Parses an STFS file entry from raw bytes.
    /// </summary>
    /// <param name="data">The raw byte data containing the file entry.</param>
    /// <param name="offset">The offset in the data where the entry starts.</param>
    /// <returns>A populated StfsFileEntry instance, or an empty entry if data is insufficient or corrupted.</returns>
    public static StfsFileEntry FromBytes(byte[] data, int offset = 0)
    {
        // Check if data is enough to contain a file entry
        if (data.Length < offset + Size)
        {
            Logger.Warning<StfsFileEntry>($"Insufficient data for file entry at offset 0x{offset:X8} (data length: {data.Length}, required: {offset + Size})");
            return new StfsFileEntry();
        }

        Logger.Trace<StfsFileEntry>($"Parsing file entry at offset 0x{offset:X8}: {BitConverter.ToString(data.Skip(offset).Take(64).ToArray())}");

        // Check if this is an empty entry (end of file table)
        // Xenia checks if name[0] == 0 to detect end of file table
        if (data[offset] == 0)
        {
            Logger.Trace<StfsFileEntry>($"  Empty entry detected (end of file table)");
            return new StfsFileEntry();
        }

        // File name (40 bytes, ASCII, null-padded)
        // Note: While Xenia uses Windows-1252, ASCII works for most English filenames
        string fileName = Encoding.ASCII.GetString(data, offset, 40).TrimEnd('\0');

        StfsFileEntry entry = new StfsFileEntry
        {
            FileName = fileName,
            Flags = data[offset + 0x28],
            // Valid data blocks (24-bit little endian) - actual data size
            ValidDataBlocks = data[offset + 0x29] | (data[offset + 0x2A] << 8) | (data[offset + 0x2B] << 16),
            // Allocated data blocks (24-bit little endian) - may be larger for sparse files
            AllocatedDataBlocks = data[offset + 0x2C] | (data[offset + 0x2D] << 8) | (data[offset + 0x2E] << 16),
            // Starting block (24-bit little endian)
            StartingBlock = data[offset + 0x2F] | (data[offset + 0x30] << 8) | (data[offset + 0x31] << 16),
            // Path indicator (big endian)
            PathIndicator = BinaryPrimitives.ReadInt16BigEndian(data.AsSpan(offset + 0x32)),
            // File size (big endian)
            FileSize = BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(offset + 0x34)),
            // Timestamps (FAT format - separate date and time)
            UpdateDate = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(offset + 0x38)),
            UpdateTime = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(offset + 0x3A)),
            AccessDate = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(offset + 0x3C)),
            AccessTime = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(offset + 0x3E))
        };

        Logger.Trace<StfsFileEntry>($"  FileName: '{entry.FileName}' (NameLength: {entry.NameLength})");
        Logger.Trace<StfsFileEntry>($"  Flags: 0x{entry.Flags:X2} (IsDirectory: {entry.IsDirectory}, HasConsecutiveBlocks: {entry.HasConsecutiveBlocks})");
        Logger.Trace<StfsFileEntry>($"  ValidDataBlocks: {entry.ValidDataBlocks}, AllocatedDataBlocks: {entry.AllocatedDataBlocks}");
        Logger.Trace<StfsFileEntry>($"  StartingBlock: {entry.StartingBlock}");
        Logger.Trace<StfsFileEntry>($"  PathIndicator: {entry.PathIndicator}");
        Logger.Trace<StfsFileEntry>($"  FileSize: {entry.FileSize}");
        Logger.Trace<StfsFileEntry>($"  UpdateDateTime: {entry.UpdateDateTime}, AccessDateTime: {entry.AccessDateTime}");

        return entry;
    }

    /// <summary>
    /// Converts the file entry to a byte array.
    /// </summary>
    /// <returns>The file entry as a byte array.</returns>
    public byte[] ToBytes()
    {
        byte[] data = new byte[Size];

        // File name (40 bytes, ASCII, null-padded)
        byte[] nameBytes = Encoding.ASCII.GetBytes(FileName);
        int nameLen = Math.Min(nameBytes.Length, 40);
        Array.Copy(nameBytes, 0, data, 0, nameLen);

        // Flags
        data[0x28] = Flags;

        // Valid data blocks (24-bit little endian)
        data[0x29] = (byte)(ValidDataBlocks & 0xFF);
        data[0x2A] = (byte)((ValidDataBlocks >> 8) & 0xFF);
        data[0x2B] = (byte)((ValidDataBlocks >> 16) & 0xFF);

        // Allocated data blocks (24-bit little endian)
        data[0x2C] = (byte)(AllocatedDataBlocks & 0xFF);
        data[0x2D] = (byte)((AllocatedDataBlocks >> 8) & 0xFF);
        data[0x2E] = (byte)((AllocatedDataBlocks >> 16) & 0xFF);

        // Starting block (24-bit little endian)
        data[0x2F] = (byte)(StartingBlock & 0xFF);
        data[0x30] = (byte)((StartingBlock >> 8) & 0xFF);
        data[0x31] = (byte)((StartingBlock >> 16) & 0xFF);

        // Path indicator (big endian)
        BinaryPrimitives.WriteInt16BigEndian(data.AsSpan(0x32), PathIndicator);

        // File size (big endian)
        BinaryPrimitives.WriteInt32BigEndian(data.AsSpan(0x34), FileSize);

        // Timestamps (FAT format - separate date and time)
        BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(0x38), UpdateDate);
        BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(0x3A), UpdateTime);
        BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(0x3C), AccessDate);
        BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(0x3E), AccessTime);

        return data;
    }

    /// <summary>
    /// Returns a string representation of the file entry.
    /// </summary>
    /// <returns>A string describing the file entry.</returns>
    public override string ToString()
    {
        string type = IsDirectory ? "DIR" : "FILE";
        return $"{type}: {FileName} ({FileSize} bytes, Block {StartingBlock}, Valid: {ValidDataBlocks}, Allocated: {AllocatedDataBlocks})";
    }
}