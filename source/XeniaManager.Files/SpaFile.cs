using System.Buffers.Binary;
using System.Text;
using XeniaManager.Files.Models.Gpd;
using XeniaManager.Files.Models.Spa;
using XeniaManager.Files.Models.XConfig;
using XeniaManager.Logging;

namespace XeniaManager.Files;

/// <summary>
/// Handles loading and parsing of SPA (System Partition Application) files, the XDBF containers
/// extracted from XEX PE sections. Contains the dashboard title icon (image <c>0x8000</c>) and
/// other title metadata. Wraps <see cref="GpdFile"/> which implements the XDBF format.
/// </summary>
/// <remarks>
/// File structure:
/// <list type="bullet">
/// <item>XDBF header (magic "XDBF" <c>0x58444246</c> BE / <c>0x46424458</c> LE, 24 bytes) + entry table (18 bytes per entry) + free-space table + data section.</item>
/// <item>Entry namespaces: Image, Achievement, Title, Setting, String, Sync. SPA typically holds 1 title icon + 50+ images.</item>
/// <item>Title icon is XDBF image <c>0x8000</c> (PNG, usually 64x64). Retrieved via <see cref="GetTitleIcon"/>.</item>
/// </list>
/// This class reuses <see cref="GpdFile"/> for all parsing and only adds SPA-specific helpers and validation.
/// </remarks>
public sealed class SpaFile : IDisposable
{
    /// <summary>
    /// XDBF magic "XDBF" big-endian (<c>0x58444246</c>). Used to validate SPA files.
    /// </summary>
    private const uint XdbfMagicBe = 0x58444246;

    /// <summary>
    /// XDBF magic "XDBF" little-endian (<c>0x46424458</c>). Accepted for GFWL-LE variants.
    /// </summary>
    private const uint XdbfMagicLe = 0x46424458;

    /// <summary>
    /// Tracks whether <see cref="Dispose()"/> has been called.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Underlying XDBF file that holds all entries and data.
    /// </summary>
    private readonly GpdFile _gpd;

    /// <summary>
    /// Gets the underlying XDBF header (magic, version, entry counts, free-space counts).
    /// </summary>
    public XdbfHeader Header
    {
        get
        {
            return _gpd.Header;
        }
    }

    /// <summary>
    /// Gets all valid image entries (PNG, validated via <see cref="ImageEntry.IsValidPng"/>) from the SPA.
    /// </summary>
    public IEnumerable<ImageEntry> Images
    {
        get
        {
            return _gpd.Images;
        }
    }

    /// <summary>
    /// Gets all valid achievement entries from the SPA. For title SPA, achievements are title-defined
    /// "XACH" entries and may not parse as user GPD achievements; expect 0 for most title SPAs.
    /// </summary>
    public IEnumerable<AchievementEntry> Achievements
    {
        get
        {
            return _gpd.Achievements;
        }
    }

    /// <summary>
    /// Gets all valid title entries from the SPA.
    /// </summary>
    public IEnumerable<TitleEntry> Titles
    {
        get
        {
            return _gpd.Titles;
        }
    }

    /// <summary>
    /// Gets the raw entry table (all namespaces, including Image, Achievement, Title, etc.).
    /// Useful for diagnostics and for enumerating image IDs that are lost on <see cref="Images"/>.
    /// </summary>
    public IReadOnlyList<EntryTableEntry> EntryTable
    {
        get
        {
            return _gpd.Entries.AsReadOnly();
        }
    }

    /// <summary>
    /// Gets the raw XDBF data section bytes (after header + tables). For diagnostics only.
    /// </summary>
    public byte[] Data
    {
        get
        {
            return _gpd.Data;
        }
    }

    /// <summary>
    /// XDBF section for SPA metadata (contains XACH, XPRP, XTHD, etc.).
    /// </summary>
    private const ushort SpaSectionMetadata = 0x0001;

    /// <summary>
    /// XDBF section for images (contains title icon 0x8000 and other PNGs).
    /// </summary>
    private const ushort SpaSectionImage = 0x0002;

    /// <summary>
    /// XDBF section for string tables (contains per-language title names).
    /// </summary>
    private const ushort SpaSectionStringTable = 0x0003;

    /// <summary>
    /// XDBF entry ID for the context table ("XCTX" = 0x58435854 BE).
    /// </summary>
    private const ulong XctxId = 0x58435854;

    /// <summary>
    /// XCTX section magic "XCTX" (0x58435854 BE) at start of the context table data.
    /// </summary>
    private const uint XctxMagic = 0x58435854;

    /// <summary>
    /// XDBF entry ID for the property table ("XPRP" = 0x58505250 BE).
    /// </summary>
    private const ulong XprpId = 0x58505250;

    /// <summary>
    /// XPRP section magic "XPRP" (0x58505250 BE) at start of the property table data.
    /// </summary>
    private const uint XprpMagic = 0x58505250;

    /// <summary>
    /// XDBF entry ID for the title header ("XTHD" = 0x58544844 BE).
    /// </summary>
    private const ulong XthdId = 0x58544844;

    /// <summary>
    /// XTHD section magic "XTHD" (0x58544844 BE) at start of the title header data.
    /// </summary>
    private const uint XthdMagic = 0x58544844;

    /// <summary>
    /// XDBF entry ID for the title defaults ("XSTC" = 0x58535443 BE).
    /// </summary>
    private const ulong XstcId = 0x58535443;

    /// <summary>
    /// XSTC section magic "XSTC" (0x58535443 BE) at start of the defaults data.
    /// </summary>
    private const uint XstcMagic = 0x58535443;

    /// <summary>
    /// XSTR section magic "XSTR" (0x58535452 BE) at start of a language string table.
    /// </summary>
    private const uint XstrMagic = 0x58535452;

    /// <summary>
    /// String ID of the title name inside a language string table (same value as the title icon ID).
    /// </summary>
    private const ushort TitleNameStringId = 0x8000;

    /// <summary>
    /// Title flag forcing profile inclusion.
    /// </summary>
    private const uint TitleFlagAlwaysIncludeInProfile = 1;

    /// <summary>
    /// Title flag forcing profile exclusion.
    /// </summary>
    private const uint TitleFlagNeverIncludeInProfile = 2;

    /// <summary>
    /// XDBF entry ID for the achievement table ("XACH" = 0x58414348 BE).
    /// </summary>
    private const ulong XachId = 0x58414348;

    /// <summary>
    /// XACH section magic "XACH" (0x58414348 BE) at start of the achievement table data.
    /// </summary>
    private const uint XachMagic = 0x58414348;

    /// <summary>
    /// Cached SPA achievements parsed from the XACH section. Null until first access.
    /// </summary>
    private List<SpaAchievement>? _spaAchievements;

    /// <summary>
    /// Gets the SPA achievements parsed from the XACH section (section 0x0001, id "XACH").
    /// </summary>
    /// <remarks>
    /// Title SPA stores all achievements inside a single XACH entry's data (header + array),
    /// not as individual XDBF entries. This property parses that table and returns 0..N entries.
    /// </remarks>
    public IReadOnlyList<SpaAchievement> SpaAchievements
    {
        get
        {
            if (_spaAchievements != null)
            {
                return _spaAchievements;
            }

            _spaAchievements = ParseSpaAchievements();
            return _spaAchievements;
        }
    }

    /// <summary>
    /// Parses the XACH section's achievement table from the XDBF data section.
    /// </summary>
    /// <returns>List of achievements (0..N); empty if SPA invalid or XACH missing/truncated.</returns>
    /// <remarks>
    /// Finds the XDBF entry with section <see cref="SpaSectionMetadata"/> and id <see cref="XachId"/>,
    /// validates the <c>XACH</c> header (magic, version, count at offset 12), then reads <c>count</c>
    /// entries of <c>0x24</c> bytes each. Logs warnings for out-of-bounds or truncated data.
    /// </remarks>
    private List<SpaAchievement> ParseSpaAchievements()
    {
        List<SpaAchievement> result = new List<SpaAchievement>();
        if (!IsValid)
        {
            return result;
        }

        EntryTableEntry xachEntry = _gpd.Entries.FirstOrDefault(e => (ushort)e.Namespace == SpaSectionMetadata && e.Id == XachId);
        if (xachEntry.Namespace == default)
        {
            Logger.Trace<SpaFile>("XACH section not found in SPA");
            return result;
        }

        // Overflow-safe bounds check: offset+length must not exceed data length and must not wrap uint.
        // Cast Data.Length to uint (safe, max 2 GiB < 4 GiB) and check offset first to avoid underflow on subtraction.
        if (xachEntry.OffsetSpecifier >= (uint)_gpd.Data.Length || xachEntry.Length > (uint)_gpd.Data.Length - xachEntry.OffsetSpecifier)
        {
            Logger.Warning<SpaFile>($"XACH entry data out of bounds (off={xachEntry.OffsetSpecifier} len={xachEntry.Length} dataLen={_gpd.Data.Length})");
            return result;
        }

        byte[] data = _gpd.Data[(int)xachEntry.OffsetSpecifier..(int)(xachEntry.OffsetSpecifier + xachEntry.Length)];
        if (data.Length < 14)
        {
            Logger.Warning<SpaFile>($"XACH data too short ({data.Length}) for header");
            return result;
        }

        uint magic = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(0));
        uint version = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(4));
        ushort count = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(12));
        if (magic != XachMagic)
        {
            Logger.Warning<SpaFile>($"XACH magic mismatch: 0x{magic:X8} (expected 0x{XachMagic:X8})");
            return result;
        }

        if (version != 1)
        {
            Logger.Trace<SpaFile>($"XACH version {version} (expected 1)");
        }

        const int entrySize = 0x24;
        int expected = 14 + count * entrySize;
        if (data.Length < expected)
        {
            Logger.Warning<SpaFile>($"XACH data truncated: need {expected}, have {data.Length} (count={count})");
            count = (ushort)((data.Length - 14) / entrySize);
        }

        for (int i = 0; i < count; i++)
        {
            int off = 14 + i * entrySize;
            SpaAchievement a = new SpaAchievement
            {
                Id = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(off)),
                LabelId = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(off + 2)),
                DescriptionId = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(off + 4)),
                UnachievedId = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(off + 6)),
                ImageId = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(off + 8)),
                Gamerscore = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(off + 12)),
                UnkE = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(off + 14)),
                Flags = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(off + 16)),
                Unk14 = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(off + 20)),
                Unk18 = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(off + 24)),
                Unk1C = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(off + 28)),
                Unk20 = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(off + 32))
            };
            result.Add(a);
        }

        Logger.Debug<SpaFile>($"Parsed {result.Count} SPA achievements from XACH (count={count})");
        return result;
    }

    /// <summary>
    /// Cached title header parsed from the XTHD section. Null until first access.
    /// </summary>
    private TitleHeaderData? _titleHeader;

    /// <summary>
    /// Whether the title header was already looked up (present or not).
    /// </summary>
    private bool _titleHeaderParsed;

    /// <summary>
    /// Cached default language parsed from the XSTC section. Null until first access.
    /// </summary>
    private XLanguage? _defaultLanguage;

    /// <summary>
    /// Cached language string tables (language ID → string ID → text). Null until first access.
    /// </summary>
    private Dictionary<ushort, Dictionary<ushort, string>>? _languageStrings;

    /// <summary>
    /// Gets the title header parsed from the XTHD section (title ID, type, version, flags).
    /// Null when the SPA is invalid or has no XTHD section.
    /// </summary>
    public TitleHeaderData? TitleHeader
    {
        get
        {
            if (!_titleHeaderParsed)
            {
                _titleHeader = ParseTitleHeader();
                _titleHeaderParsed = true;
            }

            return _titleHeader;
        }
    }

    /// <summary>
    /// Gets the title ID from the XTHD section, or 0 when absent.
    /// </summary>
    public uint TitleId
    {
        get
        {
            return TitleHeader?.TitleId ?? 0;
        }
    }

    /// <summary>
    /// Gets the title type from the XTHD section, or <see cref="TitleType.Unknown"/> when absent.
    /// </summary>
    public TitleType TitleType
    {
        get
        {
            return TitleHeader?.TitleType ?? TitleType.Unknown;
        }
    }

    /// <summary>
    /// Gets whether the title is a system application.
    /// </summary>
    public bool IsSystemApp
    {
        get
        {
            return TitleType == TitleType.System;
        }
    }

    /// <summary>
    /// Gets whether the title is a demo.
    /// </summary>
    public bool IsDemo
    {
        get
        {
            return TitleType == TitleType.Demo;
        }
    }

    /// <summary>
    /// Gets whether the title should be included in the profile.
    /// Forced by flags when set, otherwise demos are excluded.
    /// </summary>
    public bool IncludeInProfile
    {
        get
        {
            uint flags = TitleHeader?.Flags ?? 0;
            if ((flags & TitleFlagAlwaysIncludeInProfile) != 0)
            {
                return true;
            }

            if ((flags & TitleFlagNeverIncludeInProfile) != 0)
            {
                return false;
            }

            return !IsDemo;
        }
    }

    /// <summary>
    /// Gets the game's default language from the XSTC section, or English when absent.
    /// </summary>
    public XLanguage DefaultLanguage
    {
        get
        {
            _defaultLanguage ??= ParseDefaultLanguage();
            return _defaultLanguage.Value;
        }
    }

    /// <summary>
    /// Gets the game's title in its default language.
    /// </summary>
    public string TitleName() => TitleName(DefaultLanguage);

    /// <summary>
    /// Gets the game's title in the requested language, falling back to the default
    /// language, then English, then empty string.
    /// </summary>
    /// <param name="language">The requested language.</param>
    public string TitleName(XLanguage language) => GetString((ushort)language, TitleNameStringId);

    /// <summary>
    /// Gets a string table entry for a language, falling back to the default
    /// language, then English, then empty string.
    /// </summary>
    /// <param name="languageId">The requested language ID.</param>
    /// <param name="stringId">The string ID within the language table.</param>
    public string GetString(ushort languageId, ushort stringId)
    {
        _languageStrings ??= ParseLanguageStrings();
        if (_languageStrings.TryGetValue(languageId, out Dictionary<ushort, string>? strings) &&
            strings.TryGetValue(stringId, out string? value))
        {
            return value;
        }

        ushort fallback = (ushort)DefaultLanguage;
        if (fallback != languageId &&
            _languageStrings.TryGetValue(fallback, out strings) &&
            strings.TryGetValue(stringId, out value))
        {
            return value;
        }

        if (languageId != (ushort)XLanguage.English && fallback != (ushort)XLanguage.English &&
            _languageStrings.TryGetValue((ushort)XLanguage.English, out strings) &&
            strings.TryGetValue(stringId, out value))
        {
            return value;
        }

        return string.Empty;
    }

    /// <summary>
    /// Parses the XTHD section's title header from the XDBF data section.
    /// </summary>
    /// <returns>The title header, or null when missing, truncated, or invalid.</returns>
    /// <remarks>
    /// Finds the XDBF entry with section <see cref="SpaSectionMetadata"/> and id <see cref="XthdId"/>,
    /// validates the 12-byte section header (magic, version), then reads the 32-byte title data.
    /// </remarks>
    private TitleHeaderData? ParseTitleHeader()
    {
        if (!IsValid)
        {
            return null;
        }

        EntryTableEntry xthdEntry = _gpd.Entries.FirstOrDefault(e => (ushort)e.Namespace == SpaSectionMetadata && e.Id == XthdId);
        if (xthdEntry.Namespace == default)
        {
            Logger.Trace<SpaFile>("XTHD section not found in SPA");
            return null;
        }

        if (xthdEntry.OffsetSpecifier >= (uint)_gpd.Data.Length || xthdEntry.Length > (uint)_gpd.Data.Length - xthdEntry.OffsetSpecifier)
        {
            Logger.Warning<SpaFile>($"XTHD entry data out of bounds (off={xthdEntry.OffsetSpecifier} len={xthdEntry.Length} dataLen={_gpd.Data.Length})");
            return null;
        }

        byte[] data = _gpd.Data[(int)xthdEntry.OffsetSpecifier..(int)(xthdEntry.OffsetSpecifier + xthdEntry.Length)];
        if (data.Length < 12 + 32)
        {
            Logger.Warning<SpaFile>($"XTHD data too short ({data.Length}) for title header");
            return null;
        }

        uint magic = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(0));
        uint version = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(4));
        if (magic != XthdMagic)
        {
            Logger.Warning<SpaFile>($"XTHD magic mismatch: 0x{magic:X8} (expected 0x{XthdMagic:X8})");
            return null;
        }

        if (version != 1)
        {
            Logger.Trace<SpaFile>($"XTHD version {version} (expected 1)");
        }

        TitleHeaderData header = new TitleHeaderData
        {
            TitleId = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(12)),
            TitleType = (TitleType)BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(16)),
            Major = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(20)),
            Minor = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(22)),
            Build = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(24)),
            Revision = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(26)),
            Flags = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(28))
        };
        Logger.Debug<SpaFile>($"Parsed title header: ID 0x{header.TitleId:X8}, type {header.TitleType}");
        return header;
    }

    /// <summary>
    /// Parses the default language from the XSTC section.
    /// </summary>
    /// <returns>The default language, or English when missing, truncated, or invalid.</returns>
    private XLanguage ParseDefaultLanguage()
    {
        if (!IsValid)
        {
            return XLanguage.English;
        }

        EntryTableEntry xstcEntry = _gpd.Entries.FirstOrDefault(e => (ushort)e.Namespace == SpaSectionMetadata && e.Id == XstcId);
        if (xstcEntry.Namespace == default)
        {
            return XLanguage.English;
        }

        if (xstcEntry.OffsetSpecifier >= (uint)_gpd.Data.Length || xstcEntry.Length > (uint)_gpd.Data.Length - xstcEntry.OffsetSpecifier)
        {
            Logger.Warning<SpaFile>("XSTC entry data out of bounds");
            return XLanguage.English;
        }

        byte[] data = _gpd.Data[(int)xstcEntry.OffsetSpecifier..(int)(xstcEntry.OffsetSpecifier + xstcEntry.Length)];
        if (data.Length < 16)
        {
            Logger.Warning<SpaFile>($"XSTC data too short ({data.Length})");
            return XLanguage.English;
        }

        if (BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(0)) != XstcMagic)
        {
            Logger.Warning<SpaFile>("XSTC magic mismatch");
            return XLanguage.English;
        }

        return (XLanguage)BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(12));
    }

    /// <summary>
    /// Parses all language string tables (section <see cref="SpaSectionStringTable"/>).
    /// </summary>
    /// <returns>Map of language ID to string ID to text; empty when none parse.</returns>
    /// <remarks>
    /// Each table starts with a 14-byte header (magic, version, size, count) followed by
    /// <c>count</c> entries of id (2 bytes), length (2 bytes), and UTF-8 text.
    /// </remarks>
    private Dictionary<ushort, Dictionary<ushort, string>> ParseLanguageStrings()
    {
        Dictionary<ushort, Dictionary<ushort, string>> tables = new Dictionary<ushort, Dictionary<ushort, string>>();
        if (!IsValid)
        {
            return tables;
        }

        foreach (EntryTableEntry entry in _gpd.Entries.Where(e => (ushort)e.Namespace == SpaSectionStringTable))
        {
            if (entry.OffsetSpecifier >= (uint)_gpd.Data.Length || entry.Length > (uint)_gpd.Data.Length - entry.OffsetSpecifier)
            {
                Logger.Warning<SpaFile>($"XSTR entry 0x{entry.Id:X} data out of bounds, skipping");
                continue;
            }

            byte[] data = _gpd.Data[(int)entry.OffsetSpecifier..(int)(entry.OffsetSpecifier + entry.Length)];
            if (data.Length < 14)
            {
                Logger.Warning<SpaFile>($"XSTR entry 0x{entry.Id:X} too short ({data.Length}) for header, skipping");
                continue;
            }

            if (BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(0)) != XstrMagic)
            {
                Logger.Warning<SpaFile>($"XSTR entry 0x{entry.Id:X} magic mismatch, skipping");
                continue;
            }

            ushort count = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(12));
            Dictionary<ushort, string> strings = new Dictionary<ushort, string>();
            int pos = 14;
            for (int i = 0; i < count; i++)
            {
                if (pos + 4 > data.Length)
                {
                    Logger.Warning<SpaFile>($"XSTR entry 0x{entry.Id:X} truncated at string {i}, stopping");
                    break;
                }

                ushort id = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(pos));
                ushort length = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(pos + 2));
                pos += 4;
                if (pos + length > data.Length)
                {
                    Logger.Warning<SpaFile>($"XSTR entry 0x{entry.Id:X} string {id} overruns table, stopping");
                    break;
                }

                strings[id] = Encoding.UTF8.GetString(data, pos, length);
                pos += length;
            }

            tables[(ushort)entry.Id] = strings;
        }

        return tables;
    }

    /// <summary>
    /// Cached contexts parsed from the XCTX section. Null until first access.
    /// </summary>
    private List<SpaContext>? _contexts;

    /// <summary>
    /// Cached properties parsed from the XPRP section. Null until first access.
    /// </summary>
    private List<SpaProperty>? _properties;

    /// <summary>
    /// Gets the contexts parsed from the XCTX section (section 0x0001, id "XCTX").
    /// </summary>
    public IReadOnlyList<SpaContext> Contexts
    {
        get
        {
            _contexts ??= ParseContexts();
            return _contexts;
        }
    }

    /// <summary>
    /// Gets the properties parsed from the XPRP section (section 0x0001, id "XPRP").
    /// </summary>
    public IReadOnlyList<SpaProperty> Properties
    {
        get
        {
            _properties ??= ParseProperties();
            return _properties;
        }
    }

    /// <summary>
    /// Gets a context by its ID, or null when absent.
    /// </summary>
    /// <param name="id">The context ID.</param>
    public SpaContext? GetContext(uint id) => Contexts.FirstOrDefault(c => c.Id == id);

    /// <summary>
    /// Gets a property by its ID, or null when absent.
    /// </summary>
    /// <param name="id">The property ID.</param>
    public SpaProperty? GetProperty(uint id) => Properties.FirstOrDefault(p => p.Id == id);

    /// <summary>
    /// Parses the XCTX section's context table from the XDBF data section.
    /// </summary>
    /// <returns>List of contexts (0..N); empty when missing, truncated, or invalid.</returns>
    /// <remarks>
    /// Finds the XDBF entry with section <see cref="SpaSectionMetadata"/> and id <see cref="XctxId"/>,
    /// validates the 12-byte section header (magic, version), reads the 4-byte count, then
    /// <c>count</c> entries of 16 bytes each.
    /// </remarks>
    private List<SpaContext> ParseContexts()
    {
        List<SpaContext> result = [];
        if (!IsValid)
        {
            return result;
        }

        EntryTableEntry entry = _gpd.Entries.FirstOrDefault(e => (ushort)e.Namespace == SpaSectionMetadata && e.Id == XctxId);
        if (entry.Namespace == default)
        {
            return result;
        }

        if (entry.OffsetSpecifier >= (uint)_gpd.Data.Length || entry.Length > (uint)_gpd.Data.Length - entry.OffsetSpecifier)
        {
            Logger.Warning<SpaFile>("XCTX entry data out of bounds");
            return result;
        }

        byte[] data = _gpd.Data[(int)entry.OffsetSpecifier..(int)(entry.OffsetSpecifier + entry.Length)];
        if (data.Length < 16)
        {
            Logger.Warning<SpaFile>($"XCTX data too short ({data.Length}) for header and count");
            return result;
        }

        if (BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(0)) != XctxMagic)
        {
            Logger.Warning<SpaFile>("XCTX magic mismatch");
            return result;
        }

        uint count = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(12));
        int pos = 16;
        for (uint i = 0; i < count; i++)
        {
            if (pos + 16 > data.Length)
            {
                Logger.Warning<SpaFile>($"XCTX data truncated at context {i}, stopping");
                break;
            }

            result.Add(new SpaContext
            {
                Id = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(pos)),
                Unk1 = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(pos + 4)),
                StringId = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(pos + 6)),
                MaxValue = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(pos + 8)),
                DefaultValue = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(pos + 12))
            });
            pos += 16;
        }

        return result;
    }

    /// <summary>
    /// Parses the XPRP section's property table from the XDBF data section.
    /// </summary>
    /// <returns>List of properties (0..N); empty when missing, truncated, or invalid.</returns>
    /// <remarks>
    /// Finds the XDBF entry with section <see cref="SpaSectionMetadata"/> and id <see cref="XprpId"/>,
    /// validates the 12-byte section header (magic, version), reads the 2-byte count, then
    /// <c>count</c> entries of 8 bytes each.
    /// </remarks>
    private List<SpaProperty> ParseProperties()
    {
        List<SpaProperty> result = [];
        if (!IsValid)
        {
            return result;
        }

        EntryTableEntry entry = _gpd.Entries.FirstOrDefault(e => (ushort)e.Namespace == SpaSectionMetadata && e.Id == XprpId);
        if (entry.Namespace == default)
        {
            return result;
        }

        if (entry.OffsetSpecifier >= (uint)_gpd.Data.Length || entry.Length > (uint)_gpd.Data.Length - entry.OffsetSpecifier)
        {
            Logger.Warning<SpaFile>("XPRP entry data out of bounds");
            return result;
        }

        byte[] data = _gpd.Data[(int)entry.OffsetSpecifier..(int)(entry.OffsetSpecifier + entry.Length)];
        if (data.Length < 14)
        {
            Logger.Warning<SpaFile>($"XPRP data too short ({data.Length}) for header and count");
            return result;
        }

        if (BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(0)) != XprpMagic)
        {
            Logger.Warning<SpaFile>("XPRP magic mismatch");
            return result;
        }

        ushort count = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(12));
        int pos = 14;
        for (int i = 0; i < count; i++)
        {
            if (pos + 8 > data.Length)
            {
                Logger.Warning<SpaFile>($"XPRP data truncated at property {i}, stopping");
                break;
            }

            result.Add(new SpaProperty
            {
                Id = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(pos)),
                StringId = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(pos + 4)),
                DataSize = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(pos + 6))
            });
            pos += 8;
        }

        return result;
    }

    /// <summary>
    /// Enumerates all image entries together with their XDBF entry IDs (e.g., <c>0x8000</c> for title icon).
    /// </summary>
    /// <returns>Sequence of <c>(Id, Image)</c> where <c>Image.ImageId</c> is populated from the entry table.</returns>
    /// <remarks>
    /// <see cref="GpdFile.GetImage"/> validates PNG; this method falls back to raw <see cref="ImageEntry.FromBytes"/>
    /// when that validation rejects but data exists (e.g., non-PNG or truncated). The returned <see cref="ImageEntry.ImageId"/>
    /// is set from the entry table for convenience.
    /// </remarks>
    public IEnumerable<(ulong Id, ImageEntry Image)> EnumerateImagesWithIds()
    {
        foreach (EntryTableEntry e in _gpd.Entries.Where(entry => entry.Namespace == EntryNamespace.Image))
        {
            uint id32 = (uint)e.Id;
            ImageEntry? img = _gpd.GetImage(id32);
            // Fall back to raw parse if GetImage rejected (e.g., not a valid PNG) but data exists.
            if (img == null)
            {
                // Overflow-safe check: ensure offset and length are within data bounds without wrapping.
                bool hasData = e.OffsetSpecifier < (uint)_gpd.Data.Length && e.Length <= (uint)_gpd.Data.Length - e.OffsetSpecifier;
                byte[] data = hasData
                    ? _gpd.Data[(int)e.OffsetSpecifier..(int)(e.OffsetSpecifier + e.Length)]
                    : Array.Empty<byte>();
                if (data.Length > 0)
                {
                    img = ImageEntry.FromBytes(data, 0, (uint)data.Length);
                    img.ImageId = id32;
                    yield return (e.Id, img);
                }

                continue;
            }

            img.ImageId = id32;
            yield return (e.Id, img);
        }
    }

    /// <summary>
    /// Gets whether the SPA was successfully parsed (magic valid and XDBF structure intact).
    /// </summary>
    public bool IsValid { get; private set; }

    /// <summary>
    /// Gets the validation error for invalid files; null when <see cref="IsValid"/> is true.
    /// </summary>
    public string? ValidationError { get; private set; }

    /// <summary>
    /// Creates a valid SPA wrapper around an already-parsed <see cref="GpdFile"/>.
    /// </summary>
    /// <param name="gpd">Parsed XDBF file.</param>
    private SpaFile(GpdFile gpd)
    {
        _gpd = gpd;
        IsValid = true;
    }

    /// <summary>
    /// Creates an invalid SPA wrapper with an error message.
    /// </summary>
    /// <param name="error">Validation error to expose via <see cref="ValidationError"/>.</param>
    private SpaFile(string error)
    {
        _gpd = GpdFile.Create();
        IsValid = false;
        ValidationError = error;
    }

    /// <summary>
    /// Loads the SPA file from disk and parses it via <see cref="FromBytes"/>.
    /// </summary>
    /// <param name="filePath">Absolute or relative path to a raw XDBF <c>.spa</c>/<c>.gpd</c> file.</param>
    /// <returns>A new <see cref="SpaFile"/> (check <see cref="IsValid"/> before use).</returns>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
    public static SpaFile Load(string filePath)
    {
        Logger.Debug<SpaFile>($"Loading SPA file from {filePath}");

        if (!File.Exists(filePath))
        {
            Logger.Error<SpaFile>($"SPA file does not exist: {filePath}");
            throw new FileNotFoundException($"SPA file does not exist at {filePath}", filePath);
        }

        byte[] data = File.ReadAllBytes(filePath);
        Logger.Info<SpaFile>($"Loaded SPA file: {filePath} ({data.Length} bytes)");
        return FromBytes(data);
    }

    /// <summary>
    /// Parses the SPA file from raw XDBF bytes.
    /// </summary>
    /// <param name="data">Complete file bytes (any size ≥ 4).</param>
    /// <returns>
    /// A new <see cref="SpaFile"/>; <see cref="IsValid"/> false and <see cref="ValidationError"/> set
    /// on failure (too short, bad magic, or XDBF parse exception). Never throws.
    /// </returns>
    /// <remarks>
    /// Validates magic "XDBF" BE/LE (first 4 bytes), then delegates to <see cref="GpdFile.FromBytes"/>.
    /// </remarks>
    public static SpaFile FromBytes(byte[] data)
    {
        Logger.Trace<SpaFile>($"Parsing SPA from bytes ({data.Length} bytes)");

        if (data.Length < 4)
        {
            string err = "Data too short for SPA/XDBF";
            Logger.Error<SpaFile>(err);
            return new SpaFile(err);
        }

        uint magicBe = BinaryPrimitives.ReadUInt32BigEndian(data.AsSpan(0));
        uint magicLe = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(0));
        if (magicBe != XdbfMagicBe && magicLe != XdbfMagicLe)
        {
            string err = $"Invalid SPA/XDBF magic: 0x{magicBe:X8} (expected 0x{XdbfMagicBe:X8})";
            Logger.Error<SpaFile>(err);
            return new SpaFile(err);
        }

        try
        {
            GpdFile gpd = GpdFile.FromBytes(data);
            Logger.Info<SpaFile>($"Successfully parsed SPA with {gpd.Entries.Count} entries");
            return new SpaFile(gpd);
        }
        catch (Exception ex)
        {
            string err = $"Failed to parse SPA: {ex.Message}";
            Logger.Error<SpaFile>(err);
            Logger.LogExceptionDetails<SpaFile>(ex);
            return new SpaFile(err);
        }
    }

    /// <summary>
    /// Gets the dashboard title icon (XDBF image <c>0x8000</c>) as PNG bytes.
    /// </summary>
    /// <returns>PNG bytes if found and <see cref="ImageEntry.IsValidPng"/> true, null otherwise.</returns>
    /// <remarks>
    /// The title icon is the 64x64 PNG shown on the Xbox 360 dashboard. Extracted via <see cref="GpdFile.GetImage"/>(0x8000).
    /// </remarks>
    public byte[]? GetTitleIcon()
    {
        if (!IsValid)
        {
            return null;
        }

        ImageEntry? titleIcon = _gpd.GetImage(0x8000);
        if (titleIcon is { IsValidPng: true, ImageData.Length: > 0 })
        {
            Logger.Debug<SpaFile>($"Found title_icon 0x8000 ({titleIcon.ImageData.Length} bytes)");
            return titleIcon.ImageData;
        }

        Logger.Trace<SpaFile>("title_icon 0x8000 not found or invalid in SPA");
        return null;
    }

    /// <summary>
    /// Gets a single image entry by XDBF ID (e.g., <c>0x8000</c> for title icon).
    /// </summary>
    /// <param name="imageId">Image ID as stored in the entry table.</param>
    /// <returns>The <see cref="ImageEntry"/> if found and valid PNG, null otherwise.</returns>
    public ImageEntry? GetImage(uint imageId) => IsValid ? _gpd.GetImage(imageId) : null;

    /// <summary>
    /// Finds the largest valid PNG among all images, used as fallback when <c>0x8000</c> is missing.
    /// </summary>
    /// <returns>Largest PNG bytes or null if no valid PNG exists or SPA is invalid.</returns>
    public byte[]? GetAnyValidIcon()
    {
        if (!IsValid)
        {
            return null;
        }

        ImageEntry? best = null;
        foreach (ImageEntry img in _gpd.Images)
        {
            if (!img.IsValidPng || img.ImageData.Length == 0)
            {
                continue;
            }

            if (best == null || img.ImageData.Length > best.ImageData.Length)
            {
                best = img;
            }
        }

        if (best != null)
        {
            Logger.Debug<SpaFile>($"Fallback icon found ID 0x{best.ImageId:X} ({best.ImageData.Length} bytes)");
            return best.ImageData;
        }

        return null;
    }

    /// <summary>
    /// Releases the underlying <see cref="GpdFile"/> resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _gpd.Dispose();
        _disposed = true;
    }
}