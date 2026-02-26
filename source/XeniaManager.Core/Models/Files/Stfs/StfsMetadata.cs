using System.Buffers.Binary;
using XeniaManager.Core.Logging;

namespace XeniaManager.Core.Models.Files.Stfs;

/// <summary>
/// Represents the metadata header of an STFS package.
/// This contains all the descriptive information about the package content.
/// </summary>
public class StfsMetadata
{
    /// <summary>
    /// Licensing data (license entries).
    /// </summary>
    public List<LicenseEntry> LicenseEntries { get; set; } = new List<LicenseEntry>();

    /// <summary>
    /// Header SHA1 hash.
    /// </summary>
    public byte[] HeaderSha1 { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Header size.
    /// </summary>
    public int HeaderSize { get; set; }

    /// <summary>
    /// Content type.
    /// </summary>
    public ContentType ContentType { get; set; }

    /// <summary>
    /// Metadata version (1 or 2).
    /// </summary>
    public int MetadataVersion { get; set; }

    /// <summary>
    /// Content size.
    /// </summary>
    public long ContentSize { get; set; }

    /// <summary>
    /// Media ID.
    /// </summary>
    public int MediaId { get; set; }

    /// <summary>
    /// Media ID converted as Hex string
    /// Returns "00000000" if Media ID is not set.
    /// </summary>
    public string MediaIdHex => MediaId != 0 ? MediaId.ToString("X8") : "00000000";

    /// <summary>
    /// Version (for system/title updates).
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Base version (for system/title updates).
    /// </summary>
    public int BaseVersion { get; set; }

    /// <summary>
    /// Title ID.
    /// </summary>
    public int TitleId { get; set; }

    /// <summary>
    /// Title ID converted as Hex string
    /// Returns "00000000" if Title ID is not set.
    /// </summary>
    public string TitleIdHex => TitleId != 0 ? TitleId.ToString("X8") : "00000000";

    /// <summary>
    /// Platform (Xbox 360 = 2, PC = 4).
    /// </summary>
    public byte Platform { get; set; }

    /// <summary>
    /// Executable type.
    /// </summary>
    public byte ExecutableType { get; set; }

    /// <summary>
    /// Disc number.
    /// </summary>
    public byte DiscNumber { get; set; }

    /// <summary>
    /// Disc in the set.
    /// </summary>
    public byte DiscInSet { get; set; }

    /// <summary>
    /// Save game ID.
    /// </summary>
    public int SaveGameId { get; set; }

    /// <summary>
    /// Console ID (5 bytes).
    /// </summary>
    public byte[] ConsoleId { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Profile ID (8 bytes).
    /// </summary>
    public byte[] ProfileId { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Volume descriptor.
    /// </summary>
    public StfsVolumeDescriptor VolumeDescriptor { get; set; }

    /// <summary>
    /// Data file count.
    /// </summary>
    public int DataFileCount { get; set; }

    /// <summary>
    /// Data file combined size.
    /// </summary>
    public long DataFileCombinedSize { get; set; }

    /// <summary>
    /// Descriptor type (STFS = 0, SVOD = 1).
    /// </summary>
    public int DescriptorType { get; set; }

    /// <summary>
    /// Device ID (20 bytes).
    /// </summary>
    public byte[] DeviceId { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Display name (multiple locales).
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Display description (multiple locales).
    /// </summary>
    public string DisplayDescription { get; set; } = string.Empty;

    /// <summary>
    /// Publisher name.
    /// </summary>
    public string PublisherName { get; set; } = string.Empty;

    /// <summary>
    /// Title name.
    /// </summary>
    public string TitleName { get; set; } = string.Empty;

    /// <summary>
    /// Transfer flags.
    /// </summary>
    public byte TransferFlags { get; set; }

    /// <summary>
    /// Thumbnail image size.
    /// </summary>
    public int ThumbnailImageSize { get; set; }

    /// <summary>
    /// Title thumbnail image size.
    /// </summary>
    public int TitleThumbnailImageSize { get; set; }

    /// <summary>
    /// Thumbnail image data.
    /// </summary>
    public byte[] ThumbnailImage { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Title thumbnail image data.
    /// </summary>
    public byte[] TitleThumbnailImage { get; set; } = Array.Empty<byte>();

    // Version 2 specific fields
    /// <summary>
    /// Series ID (Version 2 only).
    /// </summary>
    public byte[] SeriesId { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Season ID (Version 2 only).
    /// </summary>
    public byte[] SeasonId { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Season number (Version 2 only).
    /// </summary>
    public short SeasonNumber { get; set; }

    /// <summary>
    /// Episode number (Version 2 only).
    /// </summary>
    public short EpisodeNumber { get; set; }

    /// <summary>
    /// Additional display names (Version 2 only).
    /// </summary>
    public string AdditionalDisplayNames { get; set; } = string.Empty;

    /// <summary>
    /// Additional display descriptions (Version 2 only).
    /// </summary>
    public string AdditionalDisplayDescriptions { get; set; } = string.Empty;

    /// <summary>
    /// Parses STFS metadata from raw bytes.
    /// </summary>
    /// <param name="data">The raw byte data containing the metadata.</param>
    /// <param name="signatureType">The signature type to determine the header layout.</param>
    /// <returns>A populated StfsMetadata instance.</returns>
    public static StfsMetadata FromBytes(byte[] data, SignatureType signatureType)
    {
        StfsMetadata metadata = new StfsMetadata();

        Logger.Trace<StfsMetadata>($"Parsing metadata from bytes (SignatureType: {signatureType})");
        Logger.Trace<StfsMetadata>($"License data block (0x22C-0x32B): {BitConverter.ToString(data.Skip(0x22C).Take(64).ToArray())}");

        // Parse license entries (0x100 bytes = 0x10 entries of 0xC bytes each)
        for (int i = 0; i < 0x10; i++)
        {
            int entryOffset = 0x22C + (i * LicenseEntry.Size);
            Logger.Trace<StfsMetadata>($"License entry {i} at 0x{entryOffset:X4}: {BitConverter.ToString(data.Skip(entryOffset).Take(16).ToArray())}");

            LicenseEntry entry = LicenseEntry.FromBytes(data, entryOffset);
            // Only add non-empty entries
            if (entry.LicenseId != 0 || entry.LicenseBits != 0 || entry.LicenseFlags != 0)
            {
                metadata.LicenseEntries.Add(entry);
                Logger.Trace<StfsMetadata>($"  Added: LicenseId=0x{entry.LicenseId:X16}, Bits=0x{entry.LicenseBits:X8}, Flags=0x{entry.LicenseFlags:X8}");
            }
        }

        // Header SHA1 (0x14 bytes) - at offset 0x032C
        Logger.Trace<StfsMetadata>($"Header SHA1 block (0x32C-0x33F): {BitConverter.ToString(data.Skip(0x32C).Take(20).ToArray())}");
        metadata.HeaderSha1 = new byte[0x14];
        Array.Copy(data, 0x032C, metadata.HeaderSha1, 0, 0x14);
        Logger.Trace<StfsMetadata>($"Header SHA1: {BitConverter.ToString(metadata.HeaderSha1)}");

        // Header Size - at offset 0x0340
        metadata.HeaderSize = BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(0x0340));
        Logger.Trace<StfsMetadata>($"HeaderSize: {metadata.HeaderSize} (0x{metadata.HeaderSize:X8})");

        // Content Type - at offset 0x0344
        metadata.ContentType = (ContentType)BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(0x0344));
        Logger.Trace<StfsMetadata>($"ContentType: {metadata.ContentType} (0x{(uint)metadata.ContentType:X8})");

        // Metadata Version - at offset 0x0348
        metadata.MetadataVersion = BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(0x0348));
        Logger.Trace<StfsMetadata>($"MetadataVersion: {metadata.MetadataVersion}");

        // Content Size - at offset 0x034C
        metadata.ContentSize = BinaryPrimitives.ReadInt64BigEndian(data.AsSpan(0x034C));
        Logger.Trace<StfsMetadata>($"ContentSize: {metadata.ContentSize} (0x{metadata.ContentSize:X16})");

        // Media ID - at offset 0x0354
        metadata.MediaId = BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(0x0354));
        Logger.Trace<StfsMetadata>($"MediaId: 0x{metadata.MediaId:X8}");

        // Version - at offset 0x0358
        metadata.Version = BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(0x0358));
        Logger.Trace<StfsMetadata>($"Version: {metadata.Version}");

        // Base Version - at offset 0x035C
        metadata.BaseVersion = BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(0x035C));
        Logger.Trace<StfsMetadata>($"BaseVersion: {metadata.BaseVersion}");

        // Title ID - at offset 0x0360
        metadata.TitleId = BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(0x0360));
        Logger.Trace<StfsMetadata>($"TitleId: 0x{metadata.TitleId:X8}");

        // Platform - at offset 0x0364
        metadata.Platform = data[0x0364];
        Logger.Trace<StfsMetadata>($"Platform: {metadata.Platform}");

        // Executable Type - at offset 0x0365
        metadata.ExecutableType = data[0x0365];
        Logger.Trace<StfsMetadata>($"ExecutableType: {metadata.ExecutableType}");

        // Disc Number - at offset 0x0366
        metadata.DiscNumber = data[0x0366];
        Logger.Trace<StfsMetadata>($"DiscNumber: {metadata.DiscNumber}");

        // Disc In Set - at offset 0x0367
        metadata.DiscInSet = data[0x0367];
        Logger.Trace<StfsMetadata>($"DiscInSet: {metadata.DiscInSet}");

        // Save Game ID - at offset 0x0368
        metadata.SaveGameId = BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(0x0368));
        Logger.Trace<StfsMetadata>($"SaveGameId: 0x{metadata.SaveGameId:X8}");

        // Console ID (5 bytes) - at offset 0x036C
        metadata.ConsoleId = new byte[5];
        Array.Copy(data, 0x036C, metadata.ConsoleId, 0, 5);
        Logger.Trace<StfsMetadata>($"ConsoleId: {BitConverter.ToString(metadata.ConsoleId)}");

        // Profile ID (8 bytes) - at offset 0x0371
        metadata.ProfileId = new byte[8];
        Array.Copy(data, 0x0371, metadata.ProfileId, 0, 8);
        Logger.Trace<StfsMetadata>($"ProfileId: {BitConverter.ToString(metadata.ProfileId)}");

        // Volume Descriptor (0x24 bytes) - at offset 0x0379
        Logger.Trace<StfsMetadata>($"Volume Descriptor block (0x0379): {BitConverter.ToString(data.Skip(0x0379).Take(36).ToArray())}");
        metadata.VolumeDescriptor = StfsVolumeDescriptor.FromBytes(data, 0x0379);
        Logger.Trace<StfsMetadata>($"VolumeDescriptor: Size={metadata.VolumeDescriptor.VolumeDescriptorSize}, BlockCount={metadata.VolumeDescriptor.FileTableBlockCount}, BlockNumber={metadata.VolumeDescriptor.FileTableBlockNumber}");

        // Data File Count - at offset 0x039D
        metadata.DataFileCount = BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(0x039D));
        Logger.Trace<StfsMetadata>($"DataFileCount: {metadata.DataFileCount}");

        // Data File Combined Size - at offset 0x03A1
        metadata.DataFileCombinedSize = BinaryPrimitives.ReadInt64BigEndian(data.AsSpan(0x03A1));
        Logger.Trace<StfsMetadata>($"DataFileCombinedSize: {metadata.DataFileCombinedSize}");

        // Descriptor Type - at offset 0x03A9
        metadata.DescriptorType = BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(0x03A9));
        Logger.Trace<StfsMetadata>($"DescriptorType: {metadata.DescriptorType}");

        // Reserved (4 bytes) - at offset 0x03AD
        // Padding (0x4C bytes) - at offset 0x03B1 (or 0x03AD+4)

        // Device ID (0x14 bytes) - at offset 0x03FD
        metadata.DeviceId = new byte[0x14];
        Array.Copy(data, 0x03FD, metadata.DeviceId, 0, 0x14);
        Logger.Trace<StfsMetadata>($"DeviceId: {BitConverter.ToString(metadata.DeviceId)}");

        // Display Name (0x900 bytes, UTF-16 BE) - at offset 0x0411
        // Note: 0x900 bytes total, but each locale is 0x80 bytes (multiple locales possible)
        metadata.DisplayName = ReadUtf16BeString(data, 0x0411, 0x900);
        Logger.Trace<StfsMetadata>($"DisplayName: '{metadata.DisplayName}' (raw bytes at 0x0411: {BitConverter.ToString(data.Skip(0x0411).Take(32).ToArray())})");

        // Display Description (0x900 bytes, UTF-16 BE) - at offset 0x0D11
        metadata.DisplayDescription = ReadUtf16BeString(data, 0x0D11, 0x900);
        Logger.Trace<StfsMetadata>($"DisplayDescription: '{metadata.DisplayDescription}' (raw bytes at 0x0D11: {BitConverter.ToString(data.Skip(0x0D11).Take(32).ToArray())})");

        // Publisher Name (0x80 bytes, UTF-16 BE) - at offset 0x1611
        metadata.PublisherName = ReadUtf16BeString(data, 0x1611, 0x80);
        Logger.Trace<StfsMetadata>($"PublisherName: '{metadata.PublisherName}' (raw bytes at 0x1611: {BitConverter.ToString(data.Skip(0x1611).Take(32).ToArray())})");

        // Title Name (0x80 bytes, UTF-16 BE) - at offset 0x1691
        metadata.TitleName = ReadUtf16BeString(data, 0x1691, 0x80);
        Logger.Trace<StfsMetadata>($"TitleName: '{metadata.TitleName}' (raw bytes at 0x1691: {BitConverter.ToString(data.Skip(0x1691).Take(32).ToArray())})");

        // Transfer Flags - at offset 0x1711
        metadata.TransferFlags = data[0x1711];
        Logger.Trace<StfsMetadata>($"TransferFlags: 0x{metadata.TransferFlags:X2}");

        // Thumbnail Image Size - at offset 0x1712
        metadata.ThumbnailImageSize = BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(0x1712));
        Logger.Trace<StfsMetadata>($"ThumbnailImageSize: {metadata.ThumbnailImageSize}");

        // Title Thumbnail Image Size - at offset 0x1716
        metadata.TitleThumbnailImageSize = BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(0x1716));
        Logger.Trace<StfsMetadata>($"TitleThumbnailImageSize: {metadata.TitleThumbnailImageSize}");

        // Thumbnail Image - at offset 0x171A
        // Size depends on the metadata version
        int thumbnailSize = metadata.MetadataVersion == 2 ? 0x3D00 : 0x4000;
        if (metadata.ThumbnailImageSize > 0 && metadata.ThumbnailImageSize <= thumbnailSize)
        {
            metadata.ThumbnailImage = new byte[metadata.ThumbnailImageSize];
            Array.Copy(data, 0x171A, metadata.ThumbnailImage, 0, metadata.ThumbnailImageSize);
            Logger.Trace<StfsMetadata>($"ThumbnailImage: {metadata.ThumbnailImageSize} bytes, first 16: {BitConverter.ToString(metadata.ThumbnailImage.Take(16).ToArray())}");
        }

        // Version 2 specific fields
        if (metadata.MetadataVersion == 2)
        {
            Logger.Trace<StfsMetadata>($"Parsing Version 2 metadata fields");

            // Series ID (0x10 bytes) - at offset 0x03B1
            metadata.SeriesId = new byte[0x10];
            Array.Copy(data, 0x03B1, metadata.SeriesId, 0, 0x10);
            Logger.Trace<StfsMetadata>($"SeriesId: {BitConverter.ToString(metadata.SeriesId)}");

            // Season ID (0x10 bytes) - at offset 0x03C1
            metadata.SeasonId = new byte[0x10];
            Array.Copy(data, 0x03C1, metadata.SeasonId, 0, 0x10);
            Logger.Trace<StfsMetadata>($"SeasonId: {BitConverter.ToString(metadata.SeasonId)}");

            // Season Number - at offset 0x03D1
            metadata.SeasonNumber = BinaryPrimitives.ReadInt16BigEndian(data.AsSpan(0x03D1));
            Logger.Trace<StfsMetadata>($"SeasonNumber: {metadata.SeasonNumber}");

            // Episode Number - at offset 0x03D3
            metadata.EpisodeNumber = BinaryPrimitives.ReadInt16BigEndian(data.AsSpan(0x03D3));
            Logger.Trace<StfsMetadata>($"EpisodeNumber: {metadata.EpisodeNumber}");

            // Padding (0x28 bytes) - at offset 0x03D5
            // No need to read, just for documentation

            // Additional Display Names (Version 2) - at offset 0x541A, 0x300 bytes, UTF-16 BE
            metadata.AdditionalDisplayNames = ReadUtf16BeString(data, 0x541A, 0x300);
            Logger.Trace<StfsMetadata>($"AdditionalDisplayNames: '{metadata.AdditionalDisplayNames}'");

            // Title Thumbnail Image (Version 2) - at offset 0x571A, 0x3D00 bytes
            if (metadata.TitleThumbnailImageSize > 0 && metadata.TitleThumbnailImageSize <= 0x3D00)
            {
                metadata.TitleThumbnailImage = new byte[metadata.TitleThumbnailImageSize];
                Array.Copy(data, 0x571A, metadata.TitleThumbnailImage, 0, metadata.TitleThumbnailImageSize);
                Logger.Trace<StfsMetadata>($"TitleThumbnailImage (V2): {metadata.TitleThumbnailImageSize} bytes, first 16: {BitConverter.ToString(metadata.TitleThumbnailImage.Take(16).ToArray())}");
            }

            // Additional Display Descriptions (Version 2) - at offset 0x941A, 0x300 bytes, UTF-16 BE
            metadata.AdditionalDisplayDescriptions = ReadUtf16BeString(data, 0x941A, 0x300);
            Logger.Trace<StfsMetadata>($"AdditionalDisplayDescriptions: '{metadata.AdditionalDisplayDescriptions}'");
        }
        else
        {
            // Version 1: Title Thumbnail Image at 0x571A, 0x4000 bytes
            if (metadata.TitleThumbnailImageSize > 0 && metadata.TitleThumbnailImageSize <= 0x4000)
            {
                metadata.TitleThumbnailImage = new byte[metadata.TitleThumbnailImageSize];
                Array.Copy(data, 0x571A, metadata.TitleThumbnailImage, 0, metadata.TitleThumbnailImageSize);
                Logger.Trace<StfsMetadata>($"TitleThumbnailImage (V1): {metadata.TitleThumbnailImageSize} bytes, first 16: {BitConverter.ToString(metadata.TitleThumbnailImage.Take(16).ToArray())}");
            }
        }

        Logger.Trace<StfsMetadata>($"Metadata parsing completed successfully");
        return metadata;
    }

    /// <summary>
    /// Reads a UTF-16 BE string from a fixed-length field, trimming null terminators.
    /// Note: Despite the specification saying UTF-8, the actual encoding is UTF-16 BE.
    /// </summary>
    /// <param name="data">The source byte array.</param>
    /// <param name="offset">The offset to start reading from.</param>
    /// <param name="length">The maximum length of the field in bytes.</param>
    /// <returns>The decoded string.</returns>
    private static string ReadUtf16BeString(byte[] data, int offset, int length)
    {
        // Find the null terminator (two consecutive zero bytes for UTF-16)
        int endOffset = offset;
        while (endOffset < offset + length - 1)
        {
            if (data[endOffset] == 0 && data[endOffset + 1] == 0)
            {
                break;
            }
            endOffset += 2;
        }

        if (endOffset == offset)
        {
            return string.Empty;
        }

        // Read as UTF-16 BE
        int charCount = (endOffset - offset) / 2;
        char[] chars = new char[charCount];
        for (int i = 0; i < charCount; i++)
        {
            chars[i] = (char)((data[offset + i * 2] << 8) | data[offset + i * 2 + 1]);
        }

        return new string(chars);
    }

    /// <summary>
    /// Returns a string representation of the metadata.
    /// </summary>
    /// <returns>A string describing the metadata.</returns>
    public override string ToString()
    {
        return $"STFS Metadata: {DisplayName} ({ContentType}, Version {MetadataVersion})";
    }
}