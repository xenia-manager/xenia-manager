using System.Buffers.Binary;
using XeniaManager.Logging;

namespace XeniaManager.Files.Models.Stfs;

/// <summary>
/// Represents the SVOD (Storage Volume Descriptor) structure for disc-based STFS packages (GOD / Installed Game).
/// See Documentation 01-STFS-XContent.md §1.3 and 05-disc-image-formats.md §5.2.
/// </summary>
public struct SvodVolumeDescriptor
{
    /// <summary>
    /// Size of the volume descriptor (should be 0x24).
    /// </summary>
    public byte VolumeDescriptorSize;

    /// <summary>
    /// Block cache element count.
    /// </summary>
    public byte BlockCacheElementCount;

    /// <summary>
    /// Worker thread processor.
    /// </summary>
    public byte WorkerThreadProcessor;

    /// <summary>
    /// Worker thread priority.
    /// </summary>
    public byte WorkerThreadPriority;

    /// <summary>
    /// Root hash (20 bytes SHA1).
    /// </summary>
    public byte[] RootHash;

    /// <summary>
    /// Device features (SVODFeatures flags, 0x40 = EnhancedGDFLayout).
    /// </summary>
    public byte Flags;

    /// <summary>
    /// Data block count (24-bit big endian at 0x19).
    /// </summary>
    public int DataBlockCount;

    /// <summary>
    /// Data block offset (24-bit big endian at 0x1C).
    /// </summary>
    public int DataBlockOffset;

    /// <summary>
    /// Gets whether EnhancedGDFLayout is set (0x40).
    /// </summary>
    public bool IsEnhancedGdfLayout
    {
        get
        {
            return (Flags & 0x40) != 0;
        }
    }

    /// <summary>
    /// Size of the volume descriptor structure.
    /// </summary>
    public const int Size = 0x24;

    /// <summary>
    /// Parses an SVOD Volume Descriptor from raw bytes.
    /// </summary>
    /// <param name="data">The raw byte data containing the descriptor.</param>
    /// <param name="offset">The offset in the data where the descriptor starts (usually 0x379).</param>
    /// <returns>A populated SvodVolumeDescriptor.</returns>
    public static SvodVolumeDescriptor FromBytes(byte[] data, int offset = 0)
    {
        Logger.Trace<SvodVolumeDescriptor>(
            $"Parsing SVOD Volume Descriptor at offset 0x{offset:X4}: {BitConverter.ToString(data.Skip(offset).Take(36).ToArray())}");

        SvodVolumeDescriptor descriptor = new SvodVolumeDescriptor
        {
            VolumeDescriptorSize = data[offset + 0x00],
            BlockCacheElementCount = data[offset + 0x01],
            WorkerThreadProcessor = data[offset + 0x02],
            WorkerThreadPriority = data[offset + 0x03],
            RootHash = new byte[0x14],
            Flags = data[offset + 0x18],
            DataBlockCount = (data[offset + 0x19] << 16) | (data[offset + 0x1A] << 8) | data[offset + 0x1B],
            DataBlockOffset = (data[offset + 0x1C] << 16) | (data[offset + 0x1D] << 8) | data[offset + 0x1E]
        };

        Array.Copy(data, offset + 0x04, descriptor.RootHash, 0, 0x14);

        Logger.Trace<SvodVolumeDescriptor>($"  VolumeDescriptorSize: {descriptor.VolumeDescriptorSize}");
        Logger.Trace<SvodVolumeDescriptor>($"  Flags: 0x{descriptor.Flags:X2} (EnhancedGDFLayout: {descriptor.IsEnhancedGdfLayout})");
        Logger.Trace<SvodVolumeDescriptor>($"  DataBlockCount: {descriptor.DataBlockCount}");
        Logger.Trace<SvodVolumeDescriptor>($"  DataBlockOffset: {descriptor.DataBlockOffset}");

        return descriptor;
    }

    /// <summary>
    /// Converts the volume descriptor to a byte array.
    /// </summary>
    /// <returns>The volume descriptor as a byte array.</returns>
    public byte[] ToBytes()
    {
        byte[] data = new byte[Size];
        data[0x00] = VolumeDescriptorSize;
        data[0x01] = BlockCacheElementCount;
        data[0x02] = WorkerThreadProcessor;
        data[0x03] = WorkerThreadPriority;
        RootHash.CopyTo(data, 0x04);
        data[0x18] = Flags;
        data[0x19] = (byte)((DataBlockCount >> 16) & 0xFF);
        data[0x1A] = (byte)((DataBlockCount >> 8) & 0xFF);
        data[0x1B] = (byte)(DataBlockCount & 0xFF);
        data[0x1C] = (byte)((DataBlockOffset >> 16) & 0xFF);
        data[0x1D] = (byte)((DataBlockOffset >> 8) & 0xFF);
        data[0x1E] = (byte)(DataBlockOffset & 0xFF);
        // 0x1F-0x23 padding zeros
        return data;
    }
}