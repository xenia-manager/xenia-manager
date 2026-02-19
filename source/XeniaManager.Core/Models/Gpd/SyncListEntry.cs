using System.Buffers.Binary;
using XeniaManager.Core.Logging;

namespace XeniaManager.Core.Models.Gpd;

/// <summary>
/// Represents a sync item in a Sync List entry.
/// Sync Lists track which entries need to be synchronized.
/// </summary>
public struct SyncItem
{
    /// <summary>
    /// Entry ID being synchronized.
    /// Offset: 0x0, Length: 8 bytes
    /// </summary>
    public ulong EntryId;

    /// <summary>
    /// Sync ID for the entry.
    /// Offset: 0x8, Length: 8 bytes
    /// </summary>
    public ulong SyncId;

    /// <summary>
    /// Parses a sync item from raw bytes.
    /// </summary>
    /// <param name="data">The raw byte data containing the sync item.</param>
    /// <param name="offset">The offset to start reading from.</param>
    /// <param name="isBigEndian">Whether the data is in big-endian format.</param>
    /// <returns>The parsed SyncItem.</returns>
    public static SyncItem FromBytes(byte[] data, int offset, bool isBigEndian = true)
    {
        Logger.Trace<SyncItem>($"Parsing sync item from bytes at offset {offset}");

        if (data.Length < offset + 16)
        {
            Logger.Error<SyncItem>($"Data too short for sync item (expected 16, got {data.Length - offset})");
            throw new ArgumentException("Data too short for sync item");
        }

        SyncItem item = new SyncItem
        {
            EntryId = isBigEndian
                ? BinaryPrimitives.ReadUInt64BigEndian(data.AsSpan(offset))
                : BinaryPrimitives.ReadUInt64LittleEndian(data.AsSpan(offset)),
            SyncId = isBigEndian
                ? BinaryPrimitives.ReadUInt64BigEndian(data.AsSpan(offset + 8))
                : BinaryPrimitives.ReadUInt64LittleEndian(data.AsSpan(offset + 8))
        };

        Logger.Trace<SyncItem>($"Sync item parsed - Entry ID: 0x{item.EntryId:X16}, " +
                               $"Sync ID: 0x{item.SyncId:X16}");
        return item;
    }

    /// <summary>
    /// Converts the sync item to a byte array.
    /// </summary>
    /// <param name="isBigEndian">Whether to write in big-endian format.</param>
    /// <returns>The sync item as a byte array.</returns>
    public byte[] ToBytes(bool isBigEndian = true)
    {
        Logger.Trace<SyncItem>($"Converting sync item to bytes (Entry ID: 0x{EntryId:X16})");

        byte[] data = new byte[16];

        if (isBigEndian)
        {
            BinaryPrimitives.WriteUInt64BigEndian(data.AsSpan(0x0), EntryId);
            BinaryPrimitives.WriteUInt64BigEndian(data.AsSpan(0x8), SyncId);
        }
        else
        {
            BinaryPrimitives.WriteUInt64LittleEndian(data.AsSpan(0x0), EntryId);
            BinaryPrimitives.WriteUInt64LittleEndian(data.AsSpan(0x8), SyncId);
        }

        Logger.Trace<SyncItem>($"Sync item converted to bytes (16 bytes)");
        return data;
    }

    /// <summary>
    /// Returns a string representation of the sync item.
    /// </summary>
    public override string ToString() => $"Entry: 0x{EntryId:X16}, Sync: 0x{SyncId:X16}";
}

/// <summary>
/// Represents a Sync List entry in a GPD file.
/// Sync Lists contain items that need to be synchronized.
/// </summary>
public class SyncListEntry
{
    /// <summary>
    /// Gets whether this sync list entry is valid (successfully parsed).
    /// </summary>
    public bool IsValid { get; private set; } = true;

    /// <summary>
    /// Gets the error message if this entry is invalid.
    /// </summary>
    public string? ValidationError { get; private set; }

    /// <summary>
    /// List of sync items.
    /// </summary>
    public List<SyncItem> Items { get; set; } = new List<SyncItem>();

    /// <summary>
    /// Gets the total number of sync items (excluding the header entry).
    /// </summary>
    public int TotalSyncItems => Math.Max(0, Items.Count - 1);

    /// <summary>
    /// Parses a sync list entry from raw bytes.
    /// If the data is invalid, returns an invalid entry instead of throwing.
    /// </summary>
    /// <param name="data">The raw byte data containing the sync list entry.</param>
    /// <param name="offset">The offset to start reading from.</param>
    /// <param name="length">The total length of the entry data.</param>
    /// <param name="isBigEndian">Whether the data is in big-endian format.</param>
    /// <returns>The parsed SyncListEntry (can be invalid if data is corrupted).</returns>
    public static SyncListEntry FromBytes(byte[] data, int offset, uint length, bool isBigEndian = true)
    {
        Logger.Trace<SyncListEntry>($"Parsing sync list entry from bytes at offset {offset}, length {length}");

        SyncListEntry entry = new SyncListEntry();

        try
        {
            // Each sync item is 16 bytes
            int itemCount = (int)length / 16;
            Logger.Debug<SyncListEntry>($"Sync list contains {itemCount} items");

            for (int i = 0; i < itemCount; i++)
            {
                entry.Items.Add(SyncItem.FromBytes(data, offset + (i * 16), isBigEndian));
            }

            entry.IsValid = true;
            Logger.Debug<SyncListEntry>($"Successfully parsed sync list with {entry.TotalSyncItems} sync items");
        }
        catch (Exception ex)
        {
            Logger.Error<SyncListEntry>($"Failed to parse sync list entry: {ex.Message}");
            entry.IsValid = false;
            entry.ValidationError = $"Failed to parse sync list entry: {ex.Message}";
        }

        return entry;
    }

    /// <summary>
    /// Converts the sync list entry to a byte array.
    /// </summary>
    /// <param name="isBigEndian">Whether to write in big-endian format.</param>
    /// <returns>The sync list entry as a byte array.</returns>
    public byte[] ToBytes(bool isBigEndian = true)
    {
        Logger.Trace<SyncListEntry>($"Converting sync list entry to bytes ({Items.Count} items)");

        byte[] data = new byte[Items.Count * 16];

        for (int i = 0; i < Items.Count; i++)
        {
            byte[] itemBytes = Items[i].ToBytes(isBigEndian);
            itemBytes.CopyTo(data, i * 16);
        }

        Logger.Debug<SyncListEntry>($"Sync list converted to bytes ({data.Length} bytes)");
        return data;
    }

    /// <summary>
    /// Adds a sync item to the list.
    /// </summary>
    /// <param name="entryId">The entry ID.</param>
    /// <param name="syncId">The sync ID.</param>
    public void AddItem(ulong entryId, ulong syncId)
    {
        Logger.Debug<SyncListEntry>($"Adding sync item - Entry ID: 0x{entryId:X16}, Sync ID: 0x{syncId:X16}");
        Items.Add(new SyncItem { EntryId = entryId, SyncId = syncId });
        Logger.Info<SyncListEntry>($"Sync item added (total items: {TotalSyncItems})");
    }

    /// <summary>
    /// Removes a sync item by entry ID.
    /// </summary>
    /// <param name="entryId">The entry ID to remove.</param>
    /// <returns>True if the item was found and removed.</returns>
    public bool RemoveItem(ulong entryId)
    {
        Logger.Debug<SyncListEntry>($"Removing sync item with Entry ID: 0x{entryId:X16}");
        bool removed = Items.RemoveAll(item => item.EntryId == entryId) > 0;

        if (removed)
        {
            Logger.Info<SyncListEntry>($"Sync item removed (total items: {TotalSyncItems})");
        }
        else
        {
            Logger.Warning<SyncListEntry>($"Sync item with Entry ID 0x{entryId:X16} not found");
        }

        return removed;
    }

    /// <summary>
    /// Returns a string representation of the sync list entry.
    /// </summary>
    public override string ToString() => $"Sync List ({TotalSyncItems} items)";
}