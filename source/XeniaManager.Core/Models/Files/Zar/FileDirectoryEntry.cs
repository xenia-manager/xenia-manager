using System.Buffers.Binary;

namespace XeniaManager.Core.Models.Files.Zar;

/// <summary>
/// Represents a single entry in the ZAR file tree directory structure.
/// Entries are stored in breadth-first order (BFS) and use the same 16-byte layout for both files and directories.
/// <para>
/// Structure (16 bytes):
/// - NameOffsetAndTypeFlag (uint): bit 31 = type (0 = directory, 1 = file), bits 0-30 = offset into name table
/// - FileOffsetLow / NodeStartIndex (uint): file data offset (low 32 bits) or first child index for directories
/// - FileSizeLow / Count (uint): file size (low 32 bits) or number of children for directories
/// - FileOffsetAndSizeHigh (uint): upper bits (bits 0-15 = file offset high, bits 16-31 = file size high)
/// </para>
/// </summary>
public class FileDirectoryEntry
{
    /// <summary>
    /// Size of a single entry in bytes.
    /// </summary>
    public const int Size = 16;

    /// <summary>
    /// Gets or sets the combined name offset and type flag.
    /// Bit 31: type flag (0 = directory, 1 = file).
    /// Bits 0-30: offset into the name table.
    /// </summary>
    public uint NameOffsetAndTypeFlag { get; set; }

    /// <summary>
    /// Gets or sets the low 32 bits of the file data offset (for files) or the first child index (for directories).
    /// </summary>
    public uint FileOffsetLow { get; set; }

    /// <summary>
    /// Gets or sets the low 32 bits of the file size (for files) or the number of children (for directories).
    /// </summary>
    public uint FileSizeLow { get; set; }

    /// <summary>
    /// Gets or sets the upper bits for both file offset and size.
    /// Bits 0-15: high 16 bits of the file data offset.
    /// Bits 16-31: high 16 bits of the file size.
    /// </summary>
    public uint FileOffsetAndSizeHigh { get; set; }

    /// <summary>
    /// Gets whether this entry represents a file (true) or a directory (false).
    /// </summary>
    public bool IsFile => (NameOffsetAndTypeFlag & 0x80000000) != 0;

    /// <summary>
    /// Gets the offset into the name table for this entry's name.
    /// </summary>
    public uint NameOffset => NameOffsetAndTypeFlag & 0x7FFFFFFF;

    /// <summary>
    /// Gets the index of the first child entry in the file tree (directory entries only).
    /// </summary>
    internal uint NodeStartIndex => FileOffsetLow;

    /// <summary>
    /// Gets the number of child entries (directory entries only).
    /// </summary>
    internal uint Count => FileSizeLow;

    /// <summary>
    /// Computes the 48-bit file data offset by combining the low and high portions.
    /// </summary>
    /// <returns>The absolute uncompressed byte offset of this file's data.</returns>
    public ulong GetFileOffset() => (ulong)FileOffsetLow | ((ulong)(FileOffsetAndSizeHigh & 0xFFFF) << 32);

    /// <summary>
    /// Computes the 48-bit file size by combining the low and high portions.
    /// </summary>
    /// <returns>The uncompressed size of this file in bytes.</returns>
    public ulong GetFileSize() => (ulong)FileSizeLow | ((ulong)(FileOffsetAndSizeHigh & 0xFFFF0000) << 16);

    /// <summary>
    /// Reads a FileDirectoryEntry from raw byte data at the specified offset using big-endian encoding.
    /// </summary>
    /// <param name="data">The raw byte array containing the entry.</param>
    /// <param name="offset">The offset within the data where the entry begins (16 bytes).</param>
    /// <returns>A populated FileDirectoryEntry instance.</returns>
    public static FileDirectoryEntry Read(byte[] data, int offset)
    {
        return new FileDirectoryEntry
        {
            NameOffsetAndTypeFlag = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset)),
            FileOffsetLow = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset + 4)),
            FileSizeLow = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset + 8)),
            FileOffsetAndSizeHigh = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset + 12))
        };
    }
}