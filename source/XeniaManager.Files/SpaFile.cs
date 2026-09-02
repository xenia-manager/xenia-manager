using System.Buffers.Binary;
using XeniaManager.Files.Models.Gpd;
using XeniaManager.Files.Models.Spa;
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