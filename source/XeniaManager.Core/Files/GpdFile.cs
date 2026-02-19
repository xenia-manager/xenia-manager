using XeniaManager.Core.Logging;
using XeniaManager.Core.Models.Gpd;

namespace XeniaManager.Core.Files;

/// <summary>
/// Handles loading, saving, and manipulation of XDBF/GPD (Xbox Database File / Game Profile Data) files.
/// Xbox 360 uses these files to store profile information, achievements, settings, and images.
/// The format is based on the XDBF structure with entries in different namespaces.
/// </summary>
public class GpdFile : IDisposable
{
    private bool _disposed;
    private readonly bool _isBigEndian;

    /// <summary>
    /// The XDBF file header.
    /// </summary>
    public XdbfHeader Header { get; private set; }

    /// <summary>
    /// List of all entry table entries.
    /// </summary>
    public List<EntryTableEntry> Entries { get; private set; } = new List<EntryTableEntry>();

    /// <summary>
    /// List of free space entries.
    /// </summary>
    public List<FreeSpaceEntry> FreeSpaceEntries { get; private set; } = new List<FreeSpaceEntry>();

    /// <summary>
    /// Raw data section of the file.
    /// </summary>
    public byte[] Data { get; private set; } = Array.Empty<byte>();

    /// <summary>
    /// Gets the base offset where entry data begins (after header and tables).
    /// Formula: ((EntryTableLength * 18) + (FreeSpaceTableLength * 8)) + 24
    /// </summary>
    public uint DataOffset => ((Header.EntryTableLength * 18) + (Header.FreeSpaceTableLength * 8)) + 24;

    /// <summary>
    /// Gets whether the file is in big-endian (Xbox) format.
    /// </summary>
    public bool IsBigEndian => _isBigEndian;

    /// <summary>
    /// Gets whether the file is in little-endian (GFWL) format.
    /// </summary>
    public bool IsLittleEndian => !_isBigEndian;

    /// <summary>
    /// Gets all valid achievement entries (skips invalid/corrupted entries).
    /// Invalid entries are kept in the file but excluded from this collection.
    /// </summary>
    public IEnumerable<AchievementEntry> Achievements => GetEntriesByNamespace<AchievementEntry>(EntryNamespace.Achievement).Where(a => a.IsValid);

    /// <summary>
    /// Gets all achievement entries, including invalid/corrupted ones.
    /// </summary>
    public IEnumerable<AchievementEntry> AllAchievements => GetEntriesByNamespace<AchievementEntry>(EntryNamespace.Achievement);

    /// <summary>
    /// Gets all valid image entries (skips invalid/corrupted entries).
    /// </summary>
    public IEnumerable<ImageEntry> Images => GetEntriesByNamespace<ImageEntry>(EntryNamespace.Image).Where(i => i.IsValid);

    /// <summary>
    /// Gets all image entries, including invalid/corrupted ones.
    /// </summary>
    public IEnumerable<ImageEntry> AllImages => GetEntriesByNamespace<ImageEntry>(EntryNamespace.Image);

    /// <summary>
    /// Gets all valid setting entries (skips invalid/corrupted entries).
    /// </summary>
    public IEnumerable<SettingEntry> Settings => GetEntriesByNamespace<SettingEntry>(EntryNamespace.Setting).Where(s => s.IsValid);

    /// <summary>
    /// Gets all setting entries, including invalid/corrupted ones.
    /// </summary>
    public IEnumerable<SettingEntry> AllSettings => GetEntriesByNamespace<SettingEntry>(EntryNamespace.Setting);

    /// <summary>
    /// Gets all valid title entries (skips invalid/corrupted entries).
    /// </summary>
    public IEnumerable<TitleEntry> Titles => GetEntriesByNamespace<TitleEntry>(EntryNamespace.Title).Where(t => t.IsValid);

    /// <summary>
    /// Gets all title entries, including invalid/corrupted ones.
    /// </summary>
    public IEnumerable<TitleEntry> AllTitles => GetEntriesByNamespace<TitleEntry>(EntryNamespace.Title);

    /// <summary>
    /// Gets all valid string entries (skips invalid/corrupted entries).
    /// </summary>
    public IEnumerable<StringEntry> Strings => GetEntriesByNamespace<StringEntry>(EntryNamespace.String).Where(s => s.IsValid);

    /// <summary>
    /// Gets all string entries, including invalid/corrupted ones.
    /// </summary>
    public IEnumerable<StringEntry> AllStrings => GetEntriesByNamespace<StringEntry>(EntryNamespace.String);

    /// <summary>
    /// Gets the sync list entry if it exists.
    /// Returns null if not found or if the entry is invalid.
    /// </summary>
    public SyncListEntry? SyncList
    {
        get
        {
            SyncListEntry? syncList = _syncList;
            return syncList?.IsValid == true ? syncList : null;
        }
        private set => _syncList = value;
    }

    private SyncListEntry? _syncList;

    /// <summary>
    /// Gets the sync data entry if it exists.
    /// Returns null if not found or if the entry is invalid.
    /// </summary>
    public SyncDataEntry? SyncData
    {
        get
        {
            SyncDataEntry? syncData = _syncData;
            return syncData?.IsValid == true ? syncData : null;
        }
        private set => _syncData = value;
    }

    private SyncDataEntry? _syncData;

    /// <summary>
    /// Private constructor to enforce factory methods.
    /// </summary>
    private GpdFile(bool isBigEndian)
    {
        _isBigEndian = isBigEndian;
        Header = new XdbfHeader
        {
            Magic = isBigEndian ? 0x58444246u : 0x46424458u,
            Version = 0x10000,
            EntryTableLength = 512,
            EntryCount = 0,
            FreeSpaceTableLength = 512,
            FreeSpaceTableEntryCount = 0
        };
    }

    /// <summary>
    /// Loads a GPD/XDBF file from the specified path.
    /// Automatically detects endianness based on the magic number.
    /// </summary>
    /// <param name="filePath">The path to the GPD file to load.</param>
    /// <returns>A new GpdFile instance.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
    /// <exception cref="ArgumentException">Thrown when the file has an invalid magic number.</exception>
    public static GpdFile Load(string filePath)
    {
        Logger.Debug<GpdFile>($"Loading GPD file from {filePath}");

        if (!File.Exists(filePath))
        {
            Logger.Error<GpdFile>($"GPD file does not exist: {filePath}");
            throw new FileNotFoundException($"GPD file does not exist at {filePath}", filePath);
        }

        byte[] fileData = File.ReadAllBytes(filePath);
        Logger.Info<GpdFile>($"Loaded GPD file: {filePath} ({fileData.Length} bytes)");
        return FromBytes(fileData);
    }

    /// <summary>
    /// Creates a new empty GPD file.
    /// </summary>
    /// <param name="isBigEndian">Whether to use the big-endian format (default: true for Xbox).</param>
    /// <returns>A new GpdFile instance.</returns>
    public static GpdFile Create(bool isBigEndian = true)
    {
        Logger.Info<GpdFile>($"Creating new GPD file (BigEndian: {isBigEndian})");
        return new GpdFile(isBigEndian);
    }

    /// <summary>
    /// Parses a GPD file from raw bytes.
    /// </summary>
    /// <param name="data">The raw byte data of the GPD file.</param>
    /// <returns>A new GpdFile instance.</returns>
    /// <exception cref="ArgumentException">Thrown when the magic number is invalid.</exception>
    public static GpdFile FromBytes(byte[] data)
    {
        Logger.Trace<GpdFile>($"Parsing GPD from bytes ({data.Length} bytes)");

        // Parse header
        XdbfHeader header = XdbfHeader.FromBytes(data);
        Logger.Debug<GpdFile>($"XDBF Magic: 0x{header.Magic:X8}, Entries: {header.EntryCount}/{header.EntryTableLength}, Free Space: {header.FreeSpaceTableEntryCount}/{header.FreeSpaceTableLength}");

        GpdFile gpd = new GpdFile(header.IsBigEndian);
        gpd.Header = header;

        // Parse entry table
        int entryTableOffset = 24;
        for (int i = 0; i < header.EntryCount; i++)
        {
            EntryTableEntry entry = EntryTableEntry.FromBytes(data, entryTableOffset + (i * 18), header.IsBigEndian);
            gpd.Entries.Add(entry);
            Logger.Trace<GpdFile>($"Entry {i}: Namespace={entry.Namespace}, ID=0x{entry.Id:X16}, Offset=0x{entry.OffsetSpecifier:X8}, Length={entry.Length}");
        }

        // Parse free space table
        int freeSpaceTableOffset = entryTableOffset + (int)(header.EntryTableLength * 18);
        for (int i = 0; i < header.FreeSpaceTableEntryCount; i++)
        {
            FreeSpaceEntry entry = FreeSpaceEntry.FromBytes(data, freeSpaceTableOffset + (i * 8), header.IsBigEndian);
            gpd.FreeSpaceEntries.Add(entry);
        }

        // Parse data section
        uint dataOffset = gpd.DataOffset;
        if (dataOffset < data.Length)
        {
            gpd.Data = new byte[data.Length - dataOffset];
            Array.Copy(data, (int)dataOffset, gpd.Data, 0, gpd.Data.Length);
        }

        // Parse special entries (Sync List, Sync Data)
        gpd.ParseSpecialEntries();

        Logger.Info<GpdFile>($"Successfully parsed GPD file with {gpd.Entries.Count} entries");
        return gpd;
    }

    /// <summary>
    /// Saves the GPD file to the specified path.
    /// </summary>
    /// <param name="filePath">The path to save the GPD file to.</param>
    public void Save(string filePath)
    {
        Logger.Debug<GpdFile>($"Saving GPD file to {filePath}");

        try
        {
            byte[] fileData = ToBytes();

            // Ensure directory exists
            string? directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Logger.Info<GpdFile>($"Creating directory: {directory}");
                Directory.CreateDirectory(directory);
            }

            File.WriteAllBytes(filePath, fileData);
            Logger.Info<GpdFile>($"Successfully saved GPD file to {filePath} ({fileData.Length} bytes)");
        }
        catch (Exception ex)
        {
            Logger.Error<GpdFile>($"Failed to save GPD file to {filePath}: {ex.Message}");
            Logger.LogExceptionDetails<GpdFile>(ex);
            throw;
        }
    }

    /// <summary>
    /// Converts the GPD file to a byte array.
    /// </summary>
    /// <returns>The complete GPD file as a byte array.</returns>
    public byte[] ToBytes()
    {
        Logger.Trace<GpdFile>("Converting GPD to bytes");

        // Update header counts (create a copy since Header is a struct)
        XdbfHeader header = Header;
        header.EntryCount = (uint)Entries.Count;
        header.FreeSpaceTableEntryCount = (uint)FreeSpaceEntries.Count;
        Header = header;

        // Calculate sizes
        int entryTableSize = (int)(Header.EntryTableLength * 18);
        int freeSpaceTableSize = (int)(Header.FreeSpaceTableLength * 8);
        int dataOffset = 24 + entryTableSize + freeSpaceTableSize;

        // Create a file buffer
        byte[] fileData = new byte[dataOffset + Data.Length];

        // Write header
        byte[] headerBytes = Header.ToBytes();
        headerBytes.CopyTo(fileData, 0);

        // Write entry table
        int offset = 24;
        for (int i = 0; i < Header.EntryTableLength; i++)
        {
            byte[] entryBytes = i < Entries.Count
                ? Entries[i].ToBytes(_isBigEndian)
                : new byte[18]; // Zero-fill unused entries
            entryBytes.CopyTo(fileData, offset + (i * 18));
        }

        // Write a free space table
        offset = 24 + entryTableSize;
        for (int i = 0; i < Header.FreeSpaceTableLength; i++)
        {
            byte[] entryBytes = i < FreeSpaceEntries.Count
                ? FreeSpaceEntries[i].ToBytes(_isBigEndian)
                : new byte[8]; // Zero-fill unused entries
            entryBytes.CopyTo(fileData, offset + (i * 8));
        }

        // Write data section
        Data.CopyTo(fileData, dataOffset);

        Logger.Debug<GpdFile>($"Generated file data: {fileData.Length} bytes");
        return fileData;
    }

    /// <summary>
    /// Gets an achievement by its ID.
    /// Returns null if the achievement is not found or is invalid/corrupted.
    /// </summary>
    /// <param name="achievementId">The achievement ID to find.</param>
    /// <returns>The AchievementEntry if found and valid, null otherwise.</returns>
    public AchievementEntry? GetAchievement(uint achievementId)
    {
        EntryTableEntry entry = Entries.FirstOrDefault(e =>
            e.Namespace == EntryNamespace.Achievement &&
            (e.Id == achievementId || e.Id == 0x8000));

        // Check if the entry is default (not found)
        if (entry.Namespace == default)
        {
            return null;
        }

        AchievementEntry? achievement = ParseAchievementEntry(entry);

        // Return null for invalid entries
        return achievement?.IsValid == true ? achievement : null;
    }

    /// <summary>
    /// Unlocks an achievement by its ID.
    /// Invalid/corrupted entries are skipped and cannot be modified.
    /// </summary>
    /// <param name="achievementId">The achievement ID to unlock.</param>
    /// <param name="unlockTime">Optional unlock time (defaults now).</param>
    /// <returns>True if the achievement was found and unlocked, false otherwise.</returns>
    public bool UnlockAchievement(uint achievementId, DateTime? unlockTime = null)
    {
        Logger.Info<GpdFile>($"Unlocking achievement 0x{achievementId:X8}");

        EntryTableEntry entry = Entries.FirstOrDefault(e =>
            e.Namespace == EntryNamespace.Achievement &&
            e.Id == achievementId);

        // Check if the entry is default (not found)
        if (entry.Namespace == default)
        {
            Logger.Warning<GpdFile>($"Achievement 0x{achievementId:X8} not found");
            return false;
        }

        AchievementEntry? achievement = ParseAchievementEntry(entry);
        if (achievement == null)
        {
            Logger.Warning<GpdFile>($"Failed to parse achievement 0x{achievementId:X8}");
            return false;
        }

        if (!achievement.IsValid)
        {
            Logger.Warning<GpdFile>($"Achievement 0x{achievementId:X8} is invalid/corrupted and cannot be unlocked. Reason: {achievement.ValidationError}");
            return false;
        }

        achievement.Unlock(unlockTime);
        UpdateAchievementEntry(entry, achievement);

        Logger.Info<GpdFile>($"Successfully unlocked achievement: {achievement.Name}");
        return true;
    }

    /// <summary>
    /// Locks an achievement by its ID.
    /// Invalid/corrupted entries are skipped and cannot be modified.
    /// </summary>
    /// <param name="achievementId">The achievement ID to lock.</param>
    /// <returns>True if the achievement was found and locked, false otherwise.</returns>
    public bool LockAchievement(uint achievementId)
    {
        Logger.Info<GpdFile>($"Locking achievement 0x{achievementId:X8}");

        EntryTableEntry entry = Entries.FirstOrDefault(e =>
            e.Namespace == EntryNamespace.Achievement &&
            e.Id == achievementId);

        // Check if the entry is default (not found)
        if (entry.Namespace == default)
        {
            Logger.Warning<GpdFile>($"Achievement 0x{achievementId:X8} not found");
            return false;
        }

        AchievementEntry? achievement = ParseAchievementEntry(entry);
        if (achievement == null)
        {
            Logger.Warning<GpdFile>($"Failed to parse achievement 0x{achievementId:X8}");
            return false;
        }

        if (!achievement.IsValid)
        {
            Logger.Warning<GpdFile>($"Achievement 0x{achievementId:X8} is invalid/corrupted and cannot be locked. Reason: {achievement.ValidationError}");
            return false;
        }

        achievement.Lock();
        UpdateAchievementEntry(entry, achievement);

        Logger.Info<GpdFile>($"Successfully locked achievement: {achievement.Name}");
        return true;
    }

    /// <summary>
    /// Adds a new achievement entry to the GPD file.
    /// </summary>
    /// <param name="achievement">The achievement to add.</param>
    /// <returns>The added AchievementEntry.</returns>
    public AchievementEntry AddAchievement(AchievementEntry achievement)
    {
        Logger.Info<GpdFile>($"Adding new achievement: {achievement.Name} (ID: 0x{achievement.AchievementId:X8})");

        // Create entry table entry
        EntryTableEntry entry = new EntryTableEntry
        {
            Namespace = EntryNamespace.Achievement,
            Id = achievement.AchievementId,
            OffsetSpecifier = (uint)Data.Length,
            Length = (uint)achievement.ToBytes().Length
        };

        // Add to the data section
        byte[] achievementData = achievement.ToBytes();
        byte[] newData = new byte[Data.Length + achievementData.Length];
        Data.CopyTo(newData, 0);
        achievementData.CopyTo(newData, Data.Length);
        Data = newData;

        // Add to entries
        Entries.Add(entry);

        // Update header counts (create a copy since Header is a struct)
        XdbfHeader header = Header;
        header.EntryCount = (uint)Entries.Count;
        Header = header;

        Logger.Info<GpdFile>($"Successfully added achievement: {achievement.Name}");
        return achievement;
    }

    /// <summary>
    /// Adds a new image entry to the GPD file.
    /// </summary>
    /// <param name="imageId">The image ID.</param>
    /// <param name="pngData">The PNG image data.</param>
    /// <returns>The added ImageEntry.</returns>
    public ImageEntry AddImage(uint imageId, byte[] pngData)
    {
        Logger.Info<GpdFile>($"Adding new image (ID: 0x{imageId:X8}, {pngData.Length} bytes)");

        ImageEntry image = ImageEntry.FromPngData(pngData);
        image.ImageId = imageId;

        // Create entry table entry
        EntryTableEntry entry = new EntryTableEntry
        {
            Namespace = EntryNamespace.Image,
            Id = imageId,
            OffsetSpecifier = (uint)Data.Length,
            Length = (uint)pngData.Length
        };

        // Add to the data section
        byte[] newData = new byte[Data.Length + pngData.Length];
        Data.CopyTo(newData, 0);
        pngData.CopyTo(newData, Data.Length);
        Data = newData;

        // Add to entries
        Entries.Add(entry);

        // Update header counts (create a copy since Header is a struct)
        XdbfHeader header = Header;
        header.EntryCount = (uint)Entries.Count;
        Header = header;

        Logger.Info<GpdFile>($"Successfully added image (ID: 0x{imageId:X8})");
        return image;
    }

    /// <summary>
    /// Adds a new title entry to the GPD file.
    /// </summary>
    /// <param name="title">The title to add.</param>
    /// <returns>The added TitleEntry.</returns>
    public TitleEntry AddTitle(TitleEntry title)
    {
        Logger.Info<GpdFile>($"Adding new title (ID: 0x{title.TitleId:X8})");

        // Create entry table entry
        EntryTableEntry entry = new EntryTableEntry
        {
            Namespace = EntryNamespace.Title,
            Id = title.TitleId,
            OffsetSpecifier = (uint)Data.Length,
            Length = (uint)title.ToBytes().Length
        };

        // Add to the data section
        byte[] titleData = title.ToBytes();
        byte[] newData = new byte[Data.Length + titleData.Length];
        Data.CopyTo(newData, 0);
        titleData.CopyTo(newData, Data.Length);
        Data = newData;

        // Add to entries
        Entries.Add(entry);

        // Update header counts (create a copy since Header is a struct)
        XdbfHeader header = Header;
        header.EntryCount = (uint)Entries.Count;
        Header = header;

        Logger.Info<GpdFile>($"Successfully added title (ID: 0x{title.TitleId:X8})");
        return title;
    }

    /// <summary>
    /// Adds a new setting entry to the GPD file.
    /// </summary>
    /// <param name="setting">The setting to add.</param>
    /// <returns>The added SettingEntry.</returns>
    public SettingEntry AddSetting(SettingEntry setting)
    {
        Logger.Info<GpdFile>($"Adding new setting (ID: 0x{setting.SettingId:X8})");

        // Create entry table entry
        EntryTableEntry entry = new EntryTableEntry
        {
            Namespace = EntryNamespace.Setting,
            Id = setting.SettingId,
            OffsetSpecifier = (uint)Data.Length,
            Length = (uint)setting.ToBytes().Length
        };

        // Add to the data section
        byte[] settingData = setting.ToBytes();
        byte[] newData = new byte[Data.Length + settingData.Length];
        Data.CopyTo(newData, 0);
        settingData.CopyTo(newData, Data.Length);
        Data = newData;

        // Add to entries
        Entries.Add(entry);

        // Update header counts (create a copy since Header is a struct)
        XdbfHeader header = Header;
        header.EntryCount = (uint)Entries.Count;
        Header = header;

        Logger.Info<GpdFile>($"Successfully added setting (ID: 0x{setting.SettingId:X8})");
        return setting;
    }

    /// <summary>
    /// Removes an achievement by its ID.
    /// </summary>
    /// <param name="achievementId">The achievement ID to remove.</param>
    /// <returns>True if the achievement was found and removed, false otherwise.</returns>
    public bool RemoveAchievement(uint achievementId)
    {
        Logger.Info<GpdFile>($"Removing achievement 0x{achievementId:X8}");

        EntryTableEntry entry = Entries.FirstOrDefault(e =>
            e.Namespace == EntryNamespace.Achievement &&
            e.Id == achievementId);

        // Check if the entry is default (not found)
        if (entry.Namespace == default)
        {
            Logger.Warning<GpdFile>($"Achievement 0x{achievementId:X8} not found");
            return false;
        }

        Entries.Remove(entry);

        // Update header counts (create a copy since Header is a struct)
        XdbfHeader header = Header;
        header.EntryCount = (uint)Entries.Count;
        Header = header;

        // Note: This doesn't reclaim the data space - would need compaction for that
        Logger.Info<GpdFile>($"Successfully removed achievement");
        return true;
    }

    /// <summary>
    /// Gets all achievements for a specific title.
    /// </summary>
    /// <param name="titleId">The title ID to filter by.</param>
    /// <returns>A list of achievements for the specified title.</returns>
    public List<AchievementEntry> GetAchievementsForTitle(uint titleId)
    {
        return Achievements.Where(a => a.AchievementId >> 16 == titleId).ToList();
    }

    /// <summary>
    /// Gets the total gamerscore from unlocked achievements.
    /// </summary>
    /// <returns>The total unlocked gamerscore.</returns>
    public int GetTotalGamerscore()
    {
        return Achievements.Where(a => a.IsEarned).Sum(a => a.Gamerscore);
    }

    /// <summary>
    /// Gets the count of unlocked achievements.
    /// </summary>
    /// <returns>The number of unlocked achievements.</returns>
    public int GetUnlockedAchievementCount()
    {
        return Achievements.Count(a => a.IsEarned);
    }

    /// <summary>
    /// Gets the total possible gamerscore.
    /// </summary>
    /// <returns>The total possible gamerscore from valid achievements only.</returns>
    public int GetTotalPossibleGamerscore()
    {
        return Achievements.Sum(a => a.Gamerscore);
    }

    /// <summary>
    /// Gets the total possible achievement count (valid achievements only).
    /// </summary>
    /// <returns>The total number of valid achievements.</returns>
    public int GetTotalAchievementCount()
    {
        return Achievements.Count();
    }

    /// <summary>
    /// Gets all invalid/corrupted achievement entries.
    /// These entries are kept in the file but cannot be used.
    /// </summary>
    /// <returns>A list of invalid achievement entries with error information.</returns>
    public List<AchievementEntry> GetInvalidAchievements()
    {
        return AllAchievements.Where(a => !a.IsValid).ToList();
    }

    /// <summary>
    /// Gets all invalid/corrupted image entries.
    /// </summary>
    /// <returns>A list of invalid image entries with error information.</returns>
    public List<ImageEntry> GetInvalidImages()
    {
        return AllImages.Where(i => !i.IsValid).ToList();
    }

    /// <summary>
    /// Gets all invalid/corrupted setting entries.
    /// </summary>
    /// <returns>A list of invalid setting entries with error information.</returns>
    public List<SettingEntry> GetInvalidSettings()
    {
        return AllSettings.Where(s => !s.IsValid).ToList();
    }

    /// <summary>
    /// Gets all invalid/corrupted title entries.
    /// </summary>
    /// <returns>A list of invalid title entries with error information.</returns>
    public List<TitleEntry> GetInvalidTitles()
    {
        return AllTitles.Where(t => !t.IsValid).ToList();
    }

    /// <summary>
    /// Gets all invalid/corrupted string entries.
    /// </summary>
    /// <returns>A list of invalid string entries with error information.</returns>
    public List<StringEntry> GetInvalidStrings()
    {
        return AllStrings.Where(s => !s.IsValid).ToList();
    }

    /// <summary>
    /// Gets all invalid entries in the GPD file.
    /// </summary>
    /// <returns>A dictionary mapping entry types to their invalid entries.</returns>
    public Dictionary<string, List<object>> GetAllInvalidEntries()
    {
        return new Dictionary<string, List<object>>
        {
            ["Achievements"] = GetInvalidAchievements().Cast<object>().ToList(),
            ["Images"] = GetInvalidImages().Cast<object>().ToList(),
            ["Settings"] = GetInvalidSettings().Cast<object>().ToList(),
            ["Titles"] = GetInvalidTitles().Cast<object>().ToList(),
            ["Strings"] = GetInvalidStrings().Cast<object>().ToList()
        };
    }

    /// <summary>
    /// Parses special entries (Sync List, Sync Data).
    /// Invalid entries are stored but can be checked via IsValid property.
    /// </summary>
    private void ParseSpecialEntries()
    {
        // Find Sync List
        EntryTableEntry syncListEntry = Entries.FirstOrDefault(e => e.IsSyncList);
        if (syncListEntry.Namespace != default)
        {
            byte[] data = GetDataForEntry(syncListEntry);
            _syncList = SyncListEntry.FromBytes(data, 0, syncListEntry.Length, _isBigEndian);

            if (_syncList.IsValid)
            {
                Logger.Debug<GpdFile>($"Found Sync List with {_syncList.TotalSyncItems} items");
            }
            else
            {
                Logger.Warning<GpdFile>($"Found invalid Sync List: {_syncList.ValidationError}");
            }
        }

        // Find Sync Data
        EntryTableEntry syncDataEntry = Entries.FirstOrDefault(e => e.IsSyncData);
        if (syncDataEntry.Namespace != default)
        {
            byte[] data = GetDataForEntry(syncDataEntry);
            _syncData = SyncDataEntry.FromBytes(data, 0, _isBigEndian);

            if (_syncData.IsValid)
            {
                Logger.Debug<GpdFile>($"Found Sync Data: {_syncData}");
            }
            else
            {
                Logger.Warning<GpdFile>($"Found invalid Sync Data: {_syncData.ValidationError}");
            }
        }
    }

    /// <summary>
    /// Gets entries by namespace with type conversion.
    /// </summary>
    private IEnumerable<T> GetEntriesByNamespace<T>(EntryNamespace ns) where T : class
    {
        foreach (EntryTableEntry entry in Entries.Where(e => e.Namespace == ns))
        {
            T? result = ParseEntry<T>(entry);
            if (result != null)
            {
                yield return result;
            }
        }
    }

    /// <summary>
    /// Parses an entry based on its type.
    /// </summary>
    private T? ParseEntry<T>(EntryTableEntry entry) where T : class
    {
        byte[] data = GetDataForEntry(entry);

        return entry.Namespace switch
        {
            EntryNamespace.Achievement => AchievementEntry.FromBytes(data, 0, entry.Length) as T,
            EntryNamespace.Image => ImageEntry.FromBytes(data, 0, entry.Length) as T,
            EntryNamespace.Setting => SettingEntry.FromBytes(data, 0, entry.Length) as T,
            EntryNamespace.Title => TitleEntry.FromBytes(data, 0, entry.Length) as T,
            EntryNamespace.String => StringEntry.FromBytes(data, 0, entry.Length) as T,
            _ => null
        };
    }

    /// <summary>
    /// Parses an achievement entry from an entry table entry.
    /// </summary>
    private AchievementEntry? ParseAchievementEntry(EntryTableEntry entry)
    {
        byte[] data = GetDataForEntry(entry);
        return AchievementEntry.FromBytes(data, 0, entry.Length);
    }

    /// <summary>
    /// Updates an achievement entry in the data section.
    /// </summary>
    private void UpdateAchievementEntry(EntryTableEntry entry, AchievementEntry achievement)
    {
        byte[] newAchievementData = achievement.ToBytes();

        // Calculate data offset for this entry
        int dataOffset = (int)DataOffset + (int)entry.OffsetSpecifier;

        // If new data is the same size, just overwrite
        if (newAchievementData.Length == entry.Length)
        {
            for (int i = 0; i < newAchievementData.Length; i++)
            {
                Data[(int)entry.OffsetSpecifier + i] = newAchievementData[i];
            }
        }
        else
        {
            // TODO: Need to resize, remove old and add new
            // Currently we just append and mark old as free space

            // Update the entry to point to a new location
            entry.OffsetSpecifier = (uint)Data.Length;
            entry.Length = (uint)newAchievementData.Length;

            // Append new data
            byte[] newData = new byte[Data.Length + newAchievementData.Length];
            Data.CopyTo(newData, 0);
            newAchievementData.CopyTo(newData, Data.Length);
            Data = newData;

            // Add old space to the free space table
            FreeSpaceEntries.Add(new FreeSpaceEntry
            {
                OffsetSpecifier = (uint)(dataOffset - DataOffset),
                Length = entry.Length
            });
        }
    }

    /// <summary>
    /// Gets the raw data for an entry.
    /// </summary>
    private byte[] GetDataForEntry(EntryTableEntry entry)
    {
        if (entry.OffsetSpecifier >= Data.Length)
        {
            return Array.Empty<byte>();
        }

        uint length = Math.Min(entry.Length, (uint)(Data.Length - entry.OffsetSpecifier));
        byte[] result = new byte[length];
        Array.Copy(Data, (int)entry.OffsetSpecifier, result, 0, (int)length);
        return result;
    }

    /// <summary>
    /// Disposes of resources.
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            Data = Array.Empty<byte>();
            Entries.Clear();
            FreeSpaceEntries.Clear();
        }
    }
}