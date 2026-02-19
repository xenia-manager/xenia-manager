using System.Buffers.Binary;
using XeniaManager.Core.Logging;

namespace XeniaManager.Core.Models.Gpd;

/// <summary>
/// Represents a Sync Data entry in a GPD file.
/// Sync Data contains synchronization state information.
/// </summary>
public class SyncDataEntry
{
    /// <summary>
    /// Gets whether this sync data entry is valid (successfully parsed).
    /// </summary>
    public bool IsValid { get; private set; } = true;

    /// <summary>
    /// Gets the error message if this entry is invalid.
    /// </summary>
    public string? ValidationError { get; private set; }

    /// <summary>
    /// Next sync ID to be assigned.
    /// Offset: 0x0, Length: 8 bytes
    /// </summary>
    public ulong NextSyncId { get; set; }

    /// <summary>
    /// Last synced ID.
    /// Offset: 0x8, Length: 8 bytes
    /// </summary>
    public ulong LastSyncedId { get; set; }

    /// <summary>
    /// Last synced time in file time format.
    /// Offset: 0x10, Length: 8 bytes
    /// </summary>
    public long LastSyncedTime { get; set; }

    /// <summary>
    /// Gets a DateTime representation of the last synced time.
    /// </summary>
    public DateTime? LastSyncedDateTime
    {
        get
        {
            if (LastSyncedTime == 0)
            {
                return null;
            }

            try
            {
                return DateTime.FromFileTime(LastSyncedTime);
            }
            catch (ArgumentOutOfRangeException)
            {
                // Invalid file time (e.g., from corrupted data)
                return null;
            }
        }
    }

    /// <summary>
    /// Sets the last synced time from a DateTime.
    /// </summary>
    public void SetLastSyncedTime(DateTime time)
    {
        LastSyncedTime = time.ToFileTime();
    }

    /// <summary>
    /// Parses a sync data entry from raw bytes.
    /// If the data is invalid, returns an invalid entry instead of throwing.
    /// </summary>
    /// <param name="data">The raw byte data containing the sync data entry.</param>
    /// <param name="offset">The offset to start reading from.</param>
    /// <param name="isBigEndian">Whether the data is in big-endian format.</param>
    /// <returns>The parsed SyncDataEntry (can be invalid if data is corrupted).</returns>
    public static SyncDataEntry FromBytes(byte[] data, int offset, bool isBigEndian = true)
    {
        Logger.Trace<SyncDataEntry>($"Parsing sync data entry from bytes at offset {offset}");

        SyncDataEntry entry = new SyncDataEntry();

        try
        {
            if (data.Length < offset + 0x18)
            {
                Logger.Error<SyncDataEntry>($"Data too short for sync data entry (expected 24, got {data.Length - offset})");
                entry.IsValid = false;
                entry.ValidationError = $"Data too short for sync data entry (expected 24, got {data.Length - offset})";
                return entry;
            }

            entry.NextSyncId = isBigEndian
                ? BinaryPrimitives.ReadUInt64BigEndian(data.AsSpan(offset))
                : BinaryPrimitives.ReadUInt64LittleEndian(data.AsSpan(offset));
            entry.LastSyncedId = isBigEndian
                ? BinaryPrimitives.ReadUInt64BigEndian(data.AsSpan(offset + 0x8))
                : BinaryPrimitives.ReadUInt64LittleEndian(data.AsSpan(offset + 0x8));
            entry.LastSyncedTime = isBigEndian
                ? BinaryPrimitives.ReadInt64BigEndian(data.AsSpan(offset + 0x10))
                : BinaryPrimitives.ReadInt64LittleEndian(data.AsSpan(offset + 0x10));
            entry.IsValid = true;

            Logger.Debug<SyncDataEntry>($"Sync Data parsed - Next Sync ID: 0x{entry.NextSyncId:X16}, " +
                                        $"Last Sync ID: 0x{entry.LastSyncedId:X16}, Time: {entry.LastSyncedDateTime?.ToString() ?? "Never"}");
        }
        catch (Exception ex)
        {
            Logger.Error<SyncDataEntry>($"Failed to parse sync data entry: {ex.Message}");
            entry.IsValid = false;
            entry.ValidationError = $"Failed to parse sync data entry: {ex.Message}";
        }

        return entry;
    }

    /// <summary>
    /// Converts the sync data entry to a byte array.
    /// </summary>
    /// <param name="isBigEndian">Whether to write in big-endian format.</param>
    /// <returns>The sync data entry as a byte array.</returns>
    public byte[] ToBytes(bool isBigEndian = true)
    {
        Logger.Trace<SyncDataEntry>($"Converting sync data entry to bytes (NextSyncId: 0x{NextSyncId:X16})");

        byte[] data = new byte[0x18];

        if (isBigEndian)
        {
            BinaryPrimitives.WriteUInt64BigEndian(data.AsSpan(0x0), NextSyncId);
            BinaryPrimitives.WriteUInt64BigEndian(data.AsSpan(0x8), LastSyncedId);
            BinaryPrimitives.WriteInt64BigEndian(data.AsSpan(0x10), LastSyncedTime);
        }
        else
        {
            BinaryPrimitives.WriteUInt64LittleEndian(data.AsSpan(0x0), NextSyncId);
            BinaryPrimitives.WriteUInt64LittleEndian(data.AsSpan(0x8), LastSyncedId);
            BinaryPrimitives.WriteInt64LittleEndian(data.AsSpan(0x10), LastSyncedTime);
        }

        Logger.Debug<SyncDataEntry>($"Sync data converted to bytes (24 bytes)");
        return data;
    }

    /// <summary>
    /// Gets the next available sync ID and increments the counter.
    /// </summary>
    /// <returns>The next sync ID.</returns>
    public ulong GetNextSyncId()
    {
        Logger.Debug<SyncDataEntry>($"Getting next sync ID: 0x{NextSyncId:X16}");
        return NextSyncId++;
    }

    /// <summary>
    /// Updates the last synced ID and time.
    /// </summary>
    /// <param name="syncId">The sync ID that was just synced.</param>
    /// <param name="syncTime">Optional sync time (defaults to now).</param>
    public void UpdateLastSynced(ulong syncId, DateTime? syncTime = null)
    {
        Logger.Debug<SyncDataEntry>($"Updating last synced ID to 0x{syncId:X16}");
        LastSyncedId = syncId;
        LastSyncedTime = (syncTime ?? DateTime.Now).ToFileTime();
        Logger.Info<SyncDataEntry>($"Sync state updated - Last Sync ID: 0x{LastSyncedId:X16}, Time: {LastSyncedDateTime}");
    }

    /// <summary>
    /// Returns a string representation of the sync data entry.
    /// </summary>
    public override string ToString() => $"Next: 0x{NextSyncId:X16}, Last: 0x{LastSyncedId:X16} @ {LastSyncedDateTime?.ToString() ?? "Never"}";
}