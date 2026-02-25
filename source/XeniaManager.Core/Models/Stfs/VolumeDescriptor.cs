using System.Buffers.Binary;
using XeniaManager.Core.Logging;

namespace XeniaManager.Core.Models.Stfs;

/// <summary>
/// Represents the STFS Volume Descriptor structure.
/// There are two types: STFS and SVOD. This class represents the STFS variant.
/// </summary>
public struct StfsVolumeDescriptor
{
    /// <summary>
    /// Size of the volume descriptor (should be 0x24).
    /// </summary>
    public byte VolumeDescriptorSize;

    /// <summary>
    /// Reserved byte (version).
    /// </summary>
    public byte Version;

    /// <summary>
    /// Flags byte containing STFS format flags.
    /// Bit 0: read_only_format - if set, uses a single hash table block (CON packages)
    /// Bit 1: root_active_index - if set, uses a secondary hash table block for the highest level
    /// Bit 2: directory_overallocated
    /// Bit 3: directory_index_bounds_valid
    /// </summary>
    public byte Flags;

    /// <summary>
    /// Gets whether this is a read-only format (single hash table block per level).
    /// </summary>
    public bool IsReadOnlyFormat => (Flags & 0x01) != 0;

    /// <summary>
    /// Gets the number of hash table blocks per level (1 for read-only, 2 for writable).
    /// </summary>
    public int BlocksPerHashTable => IsReadOnlyFormat ? 1 : 2;

    /// <summary>
    /// Number of blocks in the file table.
    /// </summary>
    public short FileTableBlockCount;

    /// <summary>
    /// Starting block number of the file table (24-bit integer).
    /// </summary>
    public int FileTableBlockNumber;

    /// <summary>
    /// SHA1 hash of the top hash table.
    /// </summary>
    public byte[] TopHashTableHash;

    /// <summary>
    /// Total number of allocated blocks.
    /// </summary>
    public int TotalAllocatedBlockCount;

    /// <summary>
    /// Total number of unallocated blocks.
    /// </summary>
    public int TotalUnallocatedBlockCount;

    /// <summary>
    /// Size of the volume descriptor structure.
    /// </summary>
    public const int Size = 0x24;

    /// <summary>
    /// Parses an STFS Volume Descriptor from raw bytes.
    /// Note: File Table Block Count is little-endian, File Table Block Number is little-endian (like file entry block fields).
    /// </summary>
    /// <param name="data">The raw byte data containing the volume descriptor.</param>
    /// <param name="offset">The offset in the data where the descriptor starts.</param>
    /// <returns>A populated StfsVolumeDescriptor structure.</returns>
    public static StfsVolumeDescriptor FromBytes(byte[] data, int offset = 0)
    {
        Logger.Trace<StfsVolumeDescriptor>($"Parsing Volume Descriptor at offset 0x{offset:X4}: {BitConverter.ToString(data.Skip(offset).Take(36).ToArray())}");

        StfsVolumeDescriptor descriptor = new StfsVolumeDescriptor
        {
            VolumeDescriptorSize = data[offset + 0x00],
            Version = data[offset + 0x01],
            Flags = data[offset + 0x02],
            // File Table Block Count is little-endian (2 bytes)
            FileTableBlockCount = BinaryPrimitives.ReadInt16LittleEndian(data.AsSpan(offset + 0x03)),
            // File Table Block Number is little-endian (3 bytes, int24), same as file entry block fields
            FileTableBlockNumber = data[offset + 0x05] | (data[offset + 0x06] << 8) | (data[offset + 0x07] << 16),
            TopHashTableHash = new byte[0x14],
            TotalAllocatedBlockCount = BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(offset + 0x1C)),
            TotalUnallocatedBlockCount = BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(offset + 0x20))
        };

        Array.Copy(data, offset + 0x08, descriptor.TopHashTableHash, 0, 0x14);

        Logger.Trace<StfsVolumeDescriptor>($"  VolumeDescriptorSize: {descriptor.VolumeDescriptorSize}");
        Logger.Trace<StfsVolumeDescriptor>($"  Version: {descriptor.Version}");
        Logger.Trace<StfsVolumeDescriptor>($"  Flags: 0x{descriptor.Flags:X2} (IsReadOnlyFormat: {descriptor.IsReadOnlyFormat}, BlocksPerHashTable: {descriptor.BlocksPerHashTable})");
        Logger.Trace<StfsVolumeDescriptor>($"  FileTableBlockCount: {descriptor.FileTableBlockCount}");
        Logger.Trace<StfsVolumeDescriptor>($"  FileTableBlockNumber: {descriptor.FileTableBlockNumber}");
        Logger.Trace<StfsVolumeDescriptor>($"  TopHashTableHash: {BitConverter.ToString(descriptor.TopHashTableHash)}");
        Logger.Trace<StfsVolumeDescriptor>($"  TotalAllocatedBlockCount: {descriptor.TotalAllocatedBlockCount}");
        Logger.Trace<StfsVolumeDescriptor>($"  TotalUnallocatedBlockCount: {descriptor.TotalUnallocatedBlockCount}");

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
        data[0x01] = Version;
        data[0x02] = Flags;
        // File Table Block Count is little-endian (2 bytes)
        BinaryPrimitives.WriteInt16LittleEndian(data.AsSpan(0x03), FileTableBlockCount);
        // File Table Block Number is little-endian (3 bytes, int24)
        data[0x05] = (byte)(FileTableBlockNumber & 0xFF);
        data[0x06] = (byte)((FileTableBlockNumber >> 8) & 0xFF);
        data[0x07] = (byte)((FileTableBlockNumber >> 16) & 0xFF);
        Array.Copy(TopHashTableHash, 0, data, 0x08, 0x14);
        BinaryPrimitives.WriteInt32BigEndian(data.AsSpan(0x1C), TotalAllocatedBlockCount);
        BinaryPrimitives.WriteInt32BigEndian(data.AsSpan(0x20), TotalUnallocatedBlockCount);
        return data;
    }
}