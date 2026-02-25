using System.Buffers.Binary;
using System.Text;
using XeniaManager.Core.Logging;

namespace XeniaManager.Core.Models.Files.Gpd;

/// <summary>
/// Represents a title entry in a GPD file.
/// Title entries contain game-specific information and are only located in the FFFE07D1 GPD.
/// </summary>
public class TitleEntry
{
    /// <summary>
    /// Gets whether this title entry is valid (successfully parsed).
    /// </summary>
    public bool IsValid { get; private set; } = true;

    /// <summary>
    /// Gets the error message if this entry is invalid.
    /// </summary>
    public string? ValidationError { get; private set; }

    /// <summary>
    /// Title ID.
    /// Offset: 0x0, Length: 4 bytes
    /// </summary>
    public uint TitleId { get; set; }

    /// <summary>
    /// Total achievement count for this title.
    /// Offset: 0x4, Length: 4 bytes (signed)
    /// </summary>
    public int AchievementCount { get; set; }

    /// <summary>
    /// Number of achievements unlocked.
    /// Offset: 0x8, Length: 4 bytes (signed)
    /// </summary>
    public int AchievementUnlockedCount { get; set; }

    /// <summary>
    /// Total gamerscore for this title.
    /// Offset: 0xC, Length: 4 bytes (signed)
    /// </summary>
    public int GamerscoreTotal { get; set; }

    /// <summary>
    /// Gamerscore unlocked.
    /// Offset: 0x10, Length: 4 bytes (signed)
    /// </summary>
    public int GamerscoreUnlocked { get; set; }

    /// <summary>
    /// Unknown field.
    /// Offset: 0x14, Length: 1 byte
    /// </summary>
    public byte Unknown { get; set; }

    /// <summary>
    /// Achievement unlocked online count.
    /// Offset: 0x15, Length: 1 byte
    /// </summary>
    public byte AchievementUnlockedOnlineCount { get; set; }

    /// <summary>
    /// Avatar assets earned.
    /// Offset: 0x16, Length: 1 byte
    /// </summary>
    public byte AvatarAssetsEarned { get; set; }

    /// <summary>
    /// Avatar assets max.
    /// Offset: 0x17, Length: 1 byte
    /// </summary>
    public byte AvatarAssetsMax { get; set; }

    /// <summary>
    /// Male avatar assets earned.
    /// Offset: 0x18, Length: 1 byte
    /// </summary>
    public byte MaleAvatarAssetsEarned { get; set; }

    /// <summary>
    /// Male avatar assets max.
    /// Offset: 0x19, Length: 1 byte
    /// </summary>
    public byte MaleAvatarAssetsMax { get; set; }

    /// <summary>
    /// Female avatar assets earned.
    /// Offset: 0x1A, Length: 1 byte
    /// </summary>
    public byte FemaleAvatarAssetsEarned { get; set; }

    /// <summary>
    /// Female avatar assets max.
    /// Offset: 0x1B, Length: 1 byte
    /// </summary>
    public byte FemaleAvatarAssetsMax { get; set; }

    /// <summary>
    /// Flags field.
    /// Offset: 0x1C, Length: 4 bytes
    /// </summary>
    public uint Flags { get; set; }

    /// <summary>
    /// Last played time in file time format.
    /// Offset: 0x20, Length: 8 bytes
    /// </summary>
    public long LastPlayedTime { get; set; }

    /// <summary>
    /// Title name.
    /// Offset: 0x28, Variable length, null-terminated Unicode string
    /// </summary>
    public string TitleName { get; set; } = string.Empty;

    /// <summary>
    /// Gets whether achievements need to be synced (unlocked offline).
    /// Uses bit 0 (0x1) of the Flags field.
    /// </summary>
    public bool NeedsSync => (Flags & 0x1) == 0x1;

    /// <summary>
    /// Gets whether the achievement image needs to be downloaded.
    /// Uses bit 1 (0x2) of the Flags field.
    /// </summary>
    public bool ImageNeedsDownload => (Flags & 0x2) == 0x2;

    /// <summary>
    /// Gets whether an avatar award needs to be downloaded.
    /// Uses bit 4 (0x10) of the Flags field.
    /// </summary>
    public bool AvatarAwardNeedsDownload => (Flags & 0x10) == 0x10;

    /// <summary>
    /// Gets a DateTime representation of the last played time.
    /// </summary>
    public DateTime? LastPlayedDateTime => LastPlayedTime != 0 ? DateTime.FromFileTime(LastPlayedTime) : null;

    /// <summary>
    /// Sets the last played time from a DateTime.
    /// </summary>
    public void SetLastPlayedTime(DateTime time)
    {
        LastPlayedTime = time.ToFileTime();
    }

    /// <summary>
    /// Parses a title entry from raw bytes.
    /// If the data is invalid, returns an invalid entry instead of throwing.
    /// </summary>
    /// <param name="data">The raw byte data containing the title entry.</param>
    /// <param name="offset">The offset to start reading from.</param>
    /// <param name="length">The total length of the entry data.</param>
    /// <returns>The parsed TitleEntry (can be invalid if data is corrupted).</returns>
    public static TitleEntry FromBytes(byte[] data, int offset, uint length)
    {
        Logger.Trace<TitleEntry>($"Parsing title entry from bytes at offset {offset}, length {length}");

        TitleEntry entry = new TitleEntry();

        try
        {
            if (data.Length < offset + 0x28)
            {
                Logger.Error<TitleEntry>($"Data too short for title entry (expected 40, got {data.Length - offset})");
                entry.IsValid = false;
                entry.ValidationError = $"Data too short for title entry (expected 40, got {data.Length - offset})";
                return entry;
            }

            entry.TitleId = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset));
            entry.AchievementCount = BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(offset + 0x4));
            entry.AchievementUnlockedCount = BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(offset + 0x8));
            entry.GamerscoreTotal = BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(offset + 0xC));
            entry.GamerscoreUnlocked = BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(offset + 0x10));
            entry.Unknown = data[offset + 0x14];
            entry.AchievementUnlockedOnlineCount = data[offset + 0x15];
            entry.AvatarAssetsEarned = data[offset + 0x16];
            entry.AvatarAssetsMax = data[offset + 0x17];
            entry.MaleAvatarAssetsEarned = data[offset + 0x18];
            entry.MaleAvatarAssetsMax = data[offset + 0x19];
            entry.FemaleAvatarAssetsEarned = data[offset + 0x1A];
            entry.FemaleAvatarAssetsMax = data[offset + 0x1B];
            entry.Flags = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset + 0x1C));
            entry.LastPlayedTime = BinaryPrimitives.ReadInt64BigEndian(data.AsSpan(offset + 0x20));

            // Parse title name (null-terminated Unicode)
            int stringOffset = offset + 0x28;
            entry.TitleName = ReadNullTerminatedUnicodeString(data, ref stringOffset);

            Logger.Debug<TitleEntry>($"Parsed '{entry.TitleName}' (0x{entry.TitleId:X8}) - {entry.AchievementUnlockedCount}/{entry.AchievementCount} achievements, {entry.GamerscoreUnlocked}/{entry.GamerscoreTotal}G");
            entry.IsValid = true;
        }
        catch (Exception ex)
        {
            Logger.Error<TitleEntry>($"Failed to parse title entry: {ex.Message}");
            entry.IsValid = false;
            entry.ValidationError = $"Failed to parse title entry: {ex.Message}";
        }

        return entry;
    }

    /// <summary>
    /// Converts the title entry to a byte array.
    /// </summary>
    /// <returns>The title entry as a byte array.</returns>
    public byte[] ToBytes()
    {
        byte[] nameBytes = Encoding.BigEndianUnicode.GetBytes(TitleName + '\0');
        int totalSize = 0x28 + nameBytes.Length;
        byte[] data = new byte[totalSize];

        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(0x0), TitleId);
        BinaryPrimitives.WriteInt32BigEndian(data.AsSpan(0x4), AchievementCount);
        BinaryPrimitives.WriteInt32BigEndian(data.AsSpan(0x8), AchievementUnlockedCount);
        BinaryPrimitives.WriteInt32BigEndian(data.AsSpan(0xC), GamerscoreTotal);
        BinaryPrimitives.WriteInt32BigEndian(data.AsSpan(0x10), GamerscoreUnlocked);
        data[0x14] = Unknown;
        data[0x15] = AchievementUnlockedOnlineCount;
        data[0x16] = AvatarAssetsEarned;
        data[0x17] = AvatarAssetsMax;
        data[0x18] = MaleAvatarAssetsEarned;
        data[0x19] = MaleAvatarAssetsMax;
        data[0x1A] = FemaleAvatarAssetsEarned;
        data[0x1B] = FemaleAvatarAssetsMax;
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(0x1C), Flags);
        BinaryPrimitives.WriteInt64BigEndian(data.AsSpan(0x20), LastPlayedTime);

        nameBytes.CopyTo(data, 0x28);

        return data;
    }

    /// <summary>
    /// Reads a null-terminated Unicode string from data.
    /// </summary>
    private static string ReadNullTerminatedUnicodeString(byte[] data, ref int offset)
    {
        int start = offset;
        while (offset < data.Length - 1)
        {
            if (data[offset] == 0 && data[offset + 1] == 0)
            {
                string result = Encoding.BigEndianUnicode.GetString(data, start, offset - start);
                offset += 2;
                return result;
            }
            offset += 2;
        }
        return string.Empty;
    }

    /// <summary>
    /// Returns a string representation of the title entry.
    /// </summary>
    public override string ToString() => $"{TitleName} (0x{TitleId:X8}) - {AchievementUnlockedCount}/{AchievementCount} achievements, {GamerscoreUnlocked}/{GamerscoreTotal}G";
}