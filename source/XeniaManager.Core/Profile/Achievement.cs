using XeniaManager.Core.Utilities;
using XeniaManager.Core.VirtualFileSystem.XDBF;

namespace XeniaManager.Core.Profile;
[Flags]
public enum AchievementFlags : uint
{
    None = 0,
    ShowUnachieved = 0x8,
    EarnedOnline = 0x10000,
    Earned = 0x20000,
    Edited = 0x100000,
}
public enum AchievementType : byte
{
    Completion = 1,
    Leveling = 2,
    Unlock = 3,
    Event = 4,
    Tournament = 5,
    Checkpoint = 6,
    Other = 7,
}

public class Achievement
{
    public uint StructSize;
    public uint AchievementId;
    public uint ImageId;
    public int Gamerscore;
    public uint Flags;
    public long UnlockTime;
    public required string Name;
    public required string UnlockedDescription;
    public required string LockedDescription;
    public byte[]? ImageData { get; set; }

    // Properties for flag bits and type
    public AchievementType Type
    {
        get => (AchievementType)(Flags & 7);
        set
        {
            if ((byte)value > 7)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Invalid AchievementType");
            }
            Flags = (Flags & ~7u) | (uint)value;
        }
    }

    public bool ShowUnachieved
    {
        get => (Flags & 0x8) != 0;
        set => Flags = value ? (Flags | 0x8u) : (Flags & ~0x8u);
    }

    public bool EarnedOnline
    {
        get => (Flags & 0x10000) != 0;
        set => Flags = value ? (Flags | 0x10000u) : (Flags & ~0x10000u);
    }

    public bool Earned
    {
        get => (Flags & 0x20000) != 0;
        set => Flags = value ? (Flags | 0x20000u) : (Flags & ~0x20000u);
    }

    public bool Edited
    {
        get => (Flags & 0x100000) != 0;
        set => Flags = value ? (Flags | 0x100000u) : (Flags & ~0x100000u);
    }

    public bool IsUnlocked => Earned;

    /// <summary>
    /// Unlocks this achievement, setting flags and unlock time.
    /// </summary>
    public void Unlock(DateTime? unlockDate = null, bool earnedOnline = true, bool edited = true)
    {
        Earned = true;
        EarnedOnline = earnedOnline;
        Edited = edited;
        UnlockTime = (unlockDate ?? DateTime.UtcNow).ToFileTimeUtc();
    }

    public void Lock()
    {
        Earned = false;
        EarnedOnline = false;
        Edited = true;
        UnlockTime = 0;
    }

    /// <summary>
    /// Parse a GPD achievement entry from a byte array.
    /// </summary>
    public static Achievement Parse(byte[] data)
    {
        using MemoryStream ms = new MemoryStream(data);
        using BinaryReader br = new BinaryReader(ms);

        // All fields are Big Endian
        bool bigEndian = true;

        Achievement achievement = new Achievement
        {
            StructSize = EndianUtils.ReadUInt32(br, bigEndian),
            AchievementId = EndianUtils.ReadUInt32(br, bigEndian),
            ImageId = EndianUtils.ReadUInt32(br, bigEndian),
            Gamerscore = EndianUtils.ReadInt32(br, bigEndian),
            Flags = EndianUtils.ReadUInt32(br, bigEndian),
            UnlockTime = EndianUtils.ReadInt64(br, bigEndian),
            Name = EndianUtils.ReadUnicodeString(br, bigEndian),
            UnlockedDescription = EndianUtils.ReadUnicodeString(br, bigEndian),
            LockedDescription = EndianUtils.ReadUnicodeString(br, bigEndian)
        };
        return achievement;
    }

    /// <summary>
    /// Serialize this achievement back to a byte array (Big Endian).
    /// </summary>
    public byte[] ToBytes()
    {
        using MemoryStream ms = new MemoryStream();
        using BinaryWriter bw = new BinaryWriter(ms);

        // All fields are Big Endian
        bool bigEndian = true;

        EndianUtils.WriteUInt32(bw, StructSize, bigEndian);
        EndianUtils.WriteUInt32(bw, AchievementId, bigEndian);
        EndianUtils.WriteUInt32(bw, ImageId, bigEndian);
        EndianUtils.WriteInt32(bw, Gamerscore, bigEndian);
        EndianUtils.WriteUInt32(bw, Flags, bigEndian);
        EndianUtils.WriteInt64(bw, UnlockTime, bigEndian);
        EndianUtils.WriteUnicodeString(bw, Name, bigEndian);
        EndianUtils.WriteUnicodeString(bw, UnlockedDescription, bigEndian);
        EndianUtils.WriteUnicodeString(bw, LockedDescription, bigEndian);

        return ms.ToArray();
    }

    public static List<Achievement> ParseAchievements(XdbfFile xdbf)
    {
        List<Achievement> achievements = new List<Achievement>();
        Dictionary<uint, XdbfEntry> imageEntries = xdbf.Entries
        .Where(e => e.Namespace == 2)
        .ToDictionary(e => (uint)e.Id, e => e);
        foreach (XdbfEntry entry in xdbf.Entries)
        {
            if (entry.Namespace == 1) // Achievement
            {
                byte[] data = xdbf.GetEntryData(entry);
                Achievement achievement = Parse(data);
                // Try to get image data
                if (imageEntries.TryGetValue(achievement.ImageId, out XdbfEntry? imageEntry))
                {
                    achievement.ImageData = xdbf.GetEntryData(imageEntry);
                }
                else
                {
                    // Use backup image data
                }
                achievements.Add(achievement);
            }
        }
        return achievements;
    }
}