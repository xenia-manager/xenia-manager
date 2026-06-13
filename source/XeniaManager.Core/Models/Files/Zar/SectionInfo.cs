using System.Buffers.Binary;

namespace XeniaManager.Core.Models.Files.Zar;

/// <summary>
/// Represents a section descriptor in the ZAR footer, specifying the offset and size of a data section within the archive.
/// </summary>
public struct SectionInfo
{
    /// <summary>
    /// Gets or sets the absolute offset of this section within the file.
    /// </summary>
    public ulong Offset { get; set; }

    /// <summary>
    /// Gets or sets the size of this section in bytes.
    /// </summary>
    public ulong Size { get; set; }

    /// <summary>
    /// Reads a SectionInfo from raw byte data at the specified offset using big-endian encoding.
    /// </summary>
    /// <param name="data">The raw byte array containing the section info.</param>
    /// <param name="offset">The offset within the data where the section info begins (2 × 8 bytes).</param>
    /// <returns>A populated SectionInfo structure.</returns>
    public static SectionInfo Read(byte[] data, int offset)
    {
        return new SectionInfo
        {
            Offset = BinaryPrimitives.ReadUInt64BigEndian(data.AsSpan(offset)),
            Size = BinaryPrimitives.ReadUInt64BigEndian(data.AsSpan(offset + 8))
        };
    }
}