using System.Buffers.Binary;
using System.Text;
using XeniaManager.Core.Logging;

namespace XeniaManager.Core.Models.Gpd;

/// <summary>
/// Represents an achievement entry in a GPD file.
/// Contains achievement information including unlocked status, gamerscore, and descriptions.
/// </summary>
public class AchievementEntry
{
    /// <summary>
    /// Size of the fixed structure (0x1C = 28 bytes).
    /// </summary>
    private const int STRUCT_SIZE = 0x1C;

    /// <summary>
    /// Achievement ID.
    /// Offset: 0x4, Length: 4 bytes
    /// </summary>
    public uint AchievementId { get; set; }

    /// <summary>
    /// Image ID associated with this achievement.
    /// Offset: 0x8, Length: 4 bytes
    /// </summary>
    public uint ImageId { get; set; }

    /// <summary>
    /// Gamerscore value for this achievement.
    /// Offset: 0xC, Length: 4 bytes (signed)
    /// </summary>
    public int Gamerscore { get; set; }

    /// <summary>
    /// Flags field containing achievement type and status information.
    /// Offset: 0x10, Length: 4 bytes
    /// </summary>
    public uint Flags { get; set; }

    /// <summary>
    /// Unlock time in the file time format (0 if not unlocked).
    /// Offset: 0x14, Length: 8 bytes
    /// </summary>
    public long UnlockTime { get; set; }

    /// <summary>
    /// Achievement name.
    /// Offset: 0x1C, Variable length, null-terminated Unicode string
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description shown when achievement is unlocked.
    /// Offset: After Name, Variable length, null-terminated Unicode string
    /// </summary>
    public string UnlockedDescription { get; set; } = string.Empty;

    /// <summary>
    /// Description shown when achievement is locked.
    /// Offset: After UnlockedDescription, Variable length, null-terminated Unicode string
    /// </summary>
    public string LockedDescription { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the achievement type.
    /// Uses bits 0-2 of the Flags field.
    /// </summary>
    public AchievementType AchievementType
    {
        get => (AchievementType)(Flags & 0x7);
        set
        {
            if ((byte)value > 7)
            {
                throw new ArgumentException("Invalid achievement type");
            }
            Flags = (Flags & 0xFFFFFFF8) | ((uint)value & 0x7);
        }
    }

    /// <summary>
    /// Gets or sets whether the achievement shows as unachieved (not secret).
    /// Uses bit 3 of the Flags field.
    /// </summary>
    public bool ShowUnachieved
    {
        get => (Flags & 0x8) == 0x8;
        set
        {
            if (value != ShowUnachieved)
            {
                Flags ^= 0x8;
            }
        }
    }

    /// <summary>
    /// Gets or sets whether the achievement was unlocked online.
    /// Uses bit 16 of the Flags field.
    /// </summary>
    public bool UnlockedOnline
    {
        get => (Flags & 0x10000) == 0x10000;
        set
        {
            if (value != UnlockedOnline)
            {
                Flags ^= 0x10000;
            }
        }
    }

    /// <summary>
    /// Gets or sets whether the achievement is earned/unlocked.
    /// Uses bit 17 of the Flags field.
    /// </summary>
    public bool IsEarned
    {
        get => (Flags & 0x20000) == 0x20000;
        set
        {
            if (value != IsEarned)
            {
                Flags ^= 0x20000;
            }
        }
    }

    /// <summary>
    /// Gets or sets whether the achievement has been edited.
    /// Uses bit 20 of the Flags field.
    /// </summary>
    public bool IsEdited
    {
        get => (Flags & 0x100000) == 0x100000;
        set
        {
            if (value != IsEdited)
            {
                Flags ^= 0x100000;
            }
        }
    }

    /// <summary>
    /// Gets a DateTime representation of the unlocking time.
    /// </summary>
    public DateTime? UnlockDateTime => UnlockTime != 0 ? DateTime.FromFileTime(UnlockTime) : null;

    /// <summary>
    /// Sets the unlocked time from a DateTime.
    /// </summary>
    public void SetUnlockTime(DateTime time)
    {
        UnlockTime = time.ToFileTime();
    }

    /// <summary>
    /// Unlocks this achievement.
    /// </summary>
    /// <param name="unlockTime">Optional unlock time (defaults now).</param>
    public void Unlock(DateTime? unlockTime = null)
    {
        Logger.Debug<AchievementEntry>($"Unlocking achievement '{Name}' (ID: 0x{AchievementId:X8})");
        IsEarned = true;
        UnlockTime = (unlockTime ?? DateTime.Now).ToFileTime();
        Logger.Info<AchievementEntry>($"Achievement '{Name}' unlocked at {UnlockDateTime}");
    }

    /// <summary>
    /// Locks this achievement (resets unlock status).
    /// </summary>
    public void Lock()
    {
        Logger.Debug<AchievementEntry>($"Locking achievement '{Name}' (ID: 0x{AchievementId:X8})");
        IsEarned = false;
        UnlockTime = 0;
        UnlockedOnline = false;
        Logger.Info<AchievementEntry>($"Achievement '{Name}' locked");
    }

    /// <summary>
    /// Gets whether this achievement entry is valid (successfully parsed).
    /// Invalid entries may be caused by corrupted data or non-standard formats (e.g., Blue Dragon).
    /// </summary>
    public bool IsValid { get; private set; } = true;

    /// <summary>
    /// Gets the error message if this entry is invalid.
    /// </summary>
    public string? ValidationError { get; private set; }

    /// <summary>
    /// Parses an achievement entry from raw bytes.
    /// If the data is invalid, returns an invalid entry instead of throwing.
    /// </summary>
    /// <param name="data">The raw byte data containing the achievement entry.</param>
    /// <param name="offset">The offset to start reading from.</param>
    /// <param name="length">The total length of the entry data.</param>
    /// <returns>The parsed AchievementEntry (can be invalid if data is corrupted).</returns>
    public static AchievementEntry FromBytes(byte[] data, int offset, uint length)
    {
        Logger.Trace<AchievementEntry>($"Parsing achievement entry from bytes at offset {offset}, length {length}");

        // Create a default entry that will be marked invalid if parsing fails
        AchievementEntry entry = new AchievementEntry();

        try
        {
            if (data.Length < offset + STRUCT_SIZE)
            {
                Logger.Error<AchievementEntry>($"Data too short for achievement entry (expected {STRUCT_SIZE}, got {data.Length - offset})");
                entry.IsValid = false;
                entry.ValidationError = $"Data too short for achievement entry (expected {STRUCT_SIZE}, got {data.Length - offset})";
                return entry;
            }

            uint structSize = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset));
            Logger.Trace<AchievementEntry>($"Achievement struct size: {structSize} (minimum: {STRUCT_SIZE})");

            if (structSize < STRUCT_SIZE)
            {
                Logger.Error<AchievementEntry>($"Invalid achievement struct size: {structSize} (minimum: {STRUCT_SIZE})");
                entry.IsValid = false;
                entry.ValidationError = $"Invalid achievement struct size: {structSize} (minimum: {STRUCT_SIZE})";
                return entry;
            }

            entry.AchievementId = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset + 0x4));
            entry.ImageId = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset + 0x8));
            entry.Gamerscore = BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(offset + 0xC));
            entry.Flags = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(offset + 0x10));
            entry.UnlockTime = BinaryPrimitives.ReadInt64BigEndian(data.AsSpan(offset + 0x14));

            // Parse strings (null-terminated Unicode)
            int stringOffset = offset + STRUCT_SIZE;
            entry.Name = ReadNullTerminatedUnicodeString(data, ref stringOffset);
            entry.UnlockedDescription = ReadNullTerminatedUnicodeString(data, ref stringOffset);
            entry.LockedDescription = ReadNullTerminatedUnicodeString(data, ref stringOffset);

            Logger.Debug<AchievementEntry>($"Parsed '{entry.Name}' (ID: 0x{entry.AchievementId:X8}, {entry.Gamerscore}G, Type: {entry.AchievementType})");

            if (entry.UnlockTime != 0)
            {
                Logger.Debug<AchievementEntry>($"Unlock time: {entry.UnlockDateTime?.ToString() ?? "Invalid"}");
            }

            entry.IsValid = true;
        }
        catch (Exception ex)
        {
            Logger.Error<AchievementEntry>($"Failed to parse achievement entry");
            Logger.LogExceptionDetails<AchievementEntry>(ex);
            entry.IsValid = false;
            entry.ValidationError = $"Failed to parse achievement entry: {ex.Message}";
        }

        return entry;
    }

    /// <summary>
    /// Converts the achievement entry to a byte array.
    /// </summary>
    /// <returns>The achievement entry as a byte array.</returns>
    public byte[] ToBytes()
    {
        // Calculate total size
        byte[] nameBytes = Encoding.BigEndianUnicode.GetBytes(Name + '\0');
        byte[] unlockedDescBytes = Encoding.BigEndianUnicode.GetBytes(UnlockedDescription + '\0');
        byte[] lockedDescBytes = Encoding.BigEndianUnicode.GetBytes(LockedDescription + '\0');

        int totalSize = STRUCT_SIZE + nameBytes.Length + unlockedDescBytes.Length + lockedDescBytes.Length;
        byte[] data = new byte[totalSize];

        // Write fixed structure
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(0x0), STRUCT_SIZE);
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(0x4), AchievementId);
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(0x8), ImageId);
        BinaryPrimitives.WriteInt32BigEndian(data.AsSpan(0xC), Gamerscore);
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(0x10), Flags);
        BinaryPrimitives.WriteInt64BigEndian(data.AsSpan(0x14), UnlockTime);

        // Write strings
        int offset = STRUCT_SIZE;
        nameBytes.CopyTo(data, offset);
        offset += nameBytes.Length;
        unlockedDescBytes.CopyTo(data, offset);
        offset += unlockedDescBytes.Length;
        lockedDescBytes.CopyTo(data, offset);

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
            // Check for null terminator (2 bytes)
            if (data[offset] == 0 && data[offset + 1] == 0)
            {
                string result = Encoding.BigEndianUnicode.GetString(data, start, offset - start);
                offset += 2; // Skip null terminator
                return result;
            }
            offset += 2;
        }
        return string.Empty;
    }

    /// <summary>
    /// Returns a string representation of the achievement.
    /// </summary>
    public override string ToString() => $"{Name} ({Gamerscore}G) - {(IsEarned ? "Unlocked" : "Locked")}";
}