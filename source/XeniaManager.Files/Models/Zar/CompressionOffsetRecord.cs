using System.Buffers.Binary;

namespace XeniaManager.Core.Models.Files.Zar;

/// <summary>
/// Maps block indices to their positions within the compressed data section.
/// Each record covers 16 consecutive 64 KiB uncompressed blocks.
/// <para>
/// Structure (40 bytes each):
/// - BaseOffset (ulong): absolute offset within the compressed data section
/// - Sizes[16] (ushort × 16): compressed sizes for 16 consecutive blocks, stored as (compressedSize - 1)
/// </para>
/// </summary>
public class CompressionOffsetRecord
{
    /// <summary>
    /// Size of a single record in bytes.
    /// </summary>
    public const int Size = 40;

    /// <summary>
    /// Gets or sets the base offset within the compressed data section for the first of 16 blocks.
    /// </summary>
    public ulong BaseOffset { get; set; }

    /// <summary>
    /// Gets or sets the compressed sizes for 16 consecutive blocks.
    /// Each value stores (compressedSize - 1). A value of 65535 indicates an uncompressed block (stored raw).
    /// </summary>
    public ushort[] Sizes { get; set; } = new ushort[16];

    /// <summary>
    /// Reads a CompressionOffsetRecord from raw byte data at the specified offset using big-endian encoding.
    /// </summary>
    /// <param name="data">The raw byte array containing the record.</param>
    /// <param name="offset">The offset within the data where the record begins (40 bytes).</param>
    /// <returns>A populated CompressionOffsetRecord instance.</returns>
    public static CompressionOffsetRecord Read(byte[] data, int offset)
    {
        CompressionOffsetRecord record = new CompressionOffsetRecord
        {
            BaseOffset = BinaryPrimitives.ReadUInt64BigEndian(data.AsSpan(offset))
        };
        for (int i = 0; i < 16; i++)
        {
            record.Sizes[i] = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(offset + 8 + i * 2));
        }
        return record;
    }
}