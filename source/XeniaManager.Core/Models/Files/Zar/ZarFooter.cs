using System.Buffers.Binary;

namespace XeniaManager.Core.Models.Files.Zar;

/// <summary>
/// Represents the 144-byte footer structure found at the end of a ZAR archive.
/// The footer contains section offsets and sizes for all data regions in the archive,
/// along with an integrity hash, version, and magic number for validation.
/// <para>
/// Layout (144 bytes):
/// - 6 × SectionInfo (offset + size, 16 bytes each) = 96 bytes
/// - SHA-256 integrity hash = 32 bytes
/// - Total file size (ulong) = 8 bytes
/// - Version (uint) = 4 bytes
/// - Magic (uint) = 4 bytes
/// </para>
/// </summary>
public class ZarFooter
{
    /// <summary>
    /// Total size of the footer structure in bytes.
    /// </summary>
    public const int Size = 144;

    /// <summary>
    /// Expected magic number for ZAR archives (<c>0x169f52d6</c>).
    /// </summary>
    public const uint ExpectedMagic = 0x169f52d6;

    /// <summary>
    /// Expected version number for ZAR archives (<c>0x61bf3a01</c>).
    /// </summary>
    public const uint ExpectedVersion = 0x61bf3a01;

    /// <summary>
    /// Gets or sets the compressed data section descriptor.
    /// </summary>
    public SectionInfo CompressedData { get; set; }

    /// <summary>
    /// Gets or sets the compression offset records section descriptor.
    /// </summary>
    public SectionInfo OffsetRecords { get; set; }

    /// <summary>
    /// Gets or sets the name table section descriptor.
    /// </summary>
    public SectionInfo Names { get; set; }

    /// <summary>
    /// Gets or sets the file tree (directory entries) section descriptor.
    /// </summary>
    public SectionInfo FileTree { get; set; }

    /// <summary>
    /// Gets or sets the meta directory section descriptor (reserved, usually zero).
    /// </summary>
    public SectionInfo MetaDirectory { get; set; }

    /// <summary>
    /// Gets or sets the metadata section descriptor (reserved, usually zero).
    /// </summary>
    public SectionInfo MetaData { get; set; }

    /// <summary>
    /// Gets or sets the SHA-256 integrity hash of the archive (32 bytes).
    /// Computed over the entire file with the hash field zeroed.
    /// </summary>
    public byte[] IntegrityHash { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Gets or sets the total file size. Should match the actual file size.
    /// </summary>
    public ulong TotalSize { get; set; }

    /// <summary>
    /// Gets or sets the archive version. Must equal <see cref="ExpectedVersion"/>.
    /// </summary>
    public uint Version { get; set; }

    /// <summary>
    /// Gets or sets the magic number. Must equal <see cref="ExpectedMagic"/>.
    /// </summary>
    public uint Magic { get; set; }

    /// <summary>
    /// Reads a ZarFooter from raw byte data at the specified offset.
    /// </summary>
    /// <param name="data">The raw byte array containing the footer (must be at least 144 bytes from offset).</param>
    /// <param name="offset">The offset within the data where the footer begins.</param>
    /// <returns>A populated ZarFooter instance.</returns>
    public static ZarFooter Read(byte[] data, int offset)
    {
        return new ZarFooter
        {
            CompressedData = SectionInfo.Read(data, offset),
            OffsetRecords = SectionInfo.Read(data, offset + 16),
            Names = SectionInfo.Read(data, offset + 32),
            FileTree = SectionInfo.Read(data, offset + 48),
            MetaDirectory = SectionInfo.Read(data, offset + 64),
            MetaData = SectionInfo.Read(data, offset + 80),
            IntegrityHash = data.Skip(offset + 96).Take(32).ToArray(),
            TotalSize = BinaryPrimitives.ReadUInt64BigEndian(data.AsSpan(offset + 128)),
            Version = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset + 136)),
            Magic = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset + 140))
        };
    }
}