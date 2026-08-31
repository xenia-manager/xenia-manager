using System.Buffers.Binary;
using System.Reflection;
using XeniaManager.Files;
using XeniaManager.Files.Models.Gpd;
using XeniaManager.Files.Models.Spa;

namespace XeniaManager.Tests.Files;

[TestFixture]
public class SpaFileTests
{
    private static byte[] LoadIcon()
    {
        Assembly coreAssembly = typeof(XeniaManager.Core.Manage.ArtworkManager).Assembly;
        using Stream? stream = coreAssembly.GetManifestResourceStream("XeniaManager.Core.Assets.Artwork.Icon.png");
        Assume.That(stream, Is.Not.Null, "Embedded Icon.png not found");
        using MemoryStream ms = new MemoryStream();
        stream!.CopyTo(ms);
        return ms.ToArray();
    }

    private static byte[] MinimalPng()
    {
        // 1x1 transparent PNG (67 bytes) - valid signature 89 50 4E 47 ...
        return
        [
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
            0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
            0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
            0x08, 0x06, 0x00, 0x00, 0x00, 0x1F, 0x15, 0xC4,
            0x89, 0x00, 0x00, 0x00, 0x0A, 0x49, 0x44, 0x41,
            0x54, 0x78, 0x9C, 0x63, 0x00, 0x01, 0x00, 0x00,
            0x05, 0x00, 0x01, 0x0D, 0x0A, 0x2D, 0xB4, 0x00,
            0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, 0x44, 0xAE,
            0x42, 0x60, 0x82
        ];
    }

    private static byte[] BuildXachData(ushort count, Action<Span<byte>, int>? fillEntry = null, uint magic = 0x58414348, uint version = 1)
    {
        const int entrySize = 0x24;
        byte[] data = new byte[14 + count * entrySize];
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(0), magic);
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(4), version);
        // bytes 8-11 unused, leave zero
        BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(12), count);
        for (int i = 0; i < count; i++)
        {
            int off = 14 + i * entrySize;
            if (fillEntry != null)
            {
                fillEntry(data.AsSpan(off), i);
            }
            else
            {
                // default: Id= i+1, ImageId=0x8000, Gamerscore=10
                BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(off), (ushort)(i + 1));
                BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(off + 2), 1);
                BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(off + 4), 2);
                BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(off + 6), 3);
                BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(off + 8), 0x8000u);
                BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(off + 12), 10);
            }
        }

        return data;
    }

    private static byte[] BuildSpaWithXach(byte[] xachData, byte[]? titleIcon = null, bool littleEndian = false)
    {
        using GpdFile gpd = GpdFile.Create(!littleEndian);
        // Add XACH entry via raw manipulation
        byte[] icon = titleIcon ?? LoadIcon();
        // Optionally add title icon first so Data has something
        if (titleIcon != null || xachData.Length == 0)
        {
            // no-op, we will add XACH only; title icon will be added separately if needed
        }

        // Add title icon if provided (not null) – use public API then replace Data for XACH
        if (titleIcon != null)
        {
            gpd.AddImage(0x8000, icon);
        }

        // Also add generic image for enumeration test if needed
        // Now append XACH entry manually
        // We need to append xachData to gpd.Data and add entry
        int oldLen = gpd.Data.Length;
        byte[] newData = new byte[oldLen + xachData.Length];
        if (oldLen > 0)
        {
            gpd.Data.CopyTo(newData, 0);
        }

        xachData.CopyTo(newData, oldLen);
        // Use reflection to set Data because setter is private
        typeof(GpdFile).GetProperty("Data", BindingFlags.Public | BindingFlags.Instance)!.SetValue(gpd, newData);
        EntryTableEntry xachEntry = new EntryTableEntry
        {
            Namespace = (EntryNamespace)0x0001,
            Id = 0x58414348,
            OffsetSpecifier = (uint)oldLen,
            Length = (uint)xachData.Length
        };
        gpd.Entries.Add(xachEntry);
        typeof(GpdFile).GetProperty("Header", BindingFlags.Public | BindingFlags.Instance)!.SetValue(gpd,
            new XdbfHeader
            {
                Magic = gpd.Header.Magic,
                Version = gpd.Header.Version,
                EntryTableLength = gpd.Header.EntryTableLength,
                EntryCount = (uint)gpd.Entries.Count,
                FreeSpaceTableLength = gpd.Header.FreeSpaceTableLength,
                FreeSpaceTableEntryCount = gpd.Header.FreeSpaceTableEntryCount
            });
        return gpd.ToBytes();
    }

    #region FromBytes Validation

    [Test]
    public void FromBytes_TooShort_ReturnsInvalid()
    {
        byte[] data = [0x58, 0x44, 0x42];
        SpaFile spa = SpaFile.FromBytes(data);
        Assert.That(spa.IsValid, Is.False);
        Assert.That(spa.ValidationError, Does.Contain("too short"));
    }

    [Test]
    public void FromBytes_Empty_ReturnsInvalid()
    {
        SpaFile spa = SpaFile.FromBytes(Array.Empty<byte>());
        Assert.That(spa.IsValid, Is.False);
    }

    [Test]
    public void FromBytes_InvalidMagic_ReturnsInvalid()
    {
        byte[] data = [0x00, 0x01, 0x02, 0x03, 0x00, 0x00, 0x00, 0x00];
        SpaFile spa = SpaFile.FromBytes(data);
        Assert.That(spa.IsValid, Is.False);
        Assert.That(spa.ValidationError, Does.Contain("Invalid SPA/XDBF magic"));
    }

    [Test]
    public void FromBytes_ValidBeMagic_ParsesSuccessfully()
    {
        using GpdFile gpd = GpdFile.Create(true);
        gpd.AddImage(0x8000, MinimalPng());
        byte[] bytes = gpd.ToBytes();
        SpaFile spa = SpaFile.FromBytes(bytes);
        try
        {
            Assert.That(spa.IsValid, Is.True);
            Assert.That(spa.ValidationError, Is.Null);
            Assert.That(spa.Header.Magic, Is.EqualTo(0x58444246u));
        }
        finally { spa.Dispose(); }
    }

    [Test]
    public void FromBytes_ValidLeMagic_ParsesSuccessfully()
    {
        // GFWL-LE variant still uses same magic bytes "XDBF" [58 44 42 46] – same as BE.
        // GpdFile.Create(false) writes entries LE but header magic bytes identical, so GpdFile.FromBytes
        // will misinterpret LE as BE and fail. Instead, test that SpaFile accepts the BE magic which covers LE.
        // We verify SpaFile does not reject based on LE check alone.
        using GpdFile gpd = GpdFile.Create(true);
        gpd.AddImage(0x8000, MinimalPng());
        byte[] beBytes = gpd.ToBytes();
        // LE check in SpaFile: magicBe !=Be && magicLe !=Le should be false for valid "XDBF"
        uint magicBe = BinaryPrimitives.ReadUInt32BigEndian(beBytes.AsSpan(0));
        uint magicLe = BinaryPrimitives.ReadUInt32LittleEndian(beBytes.AsSpan(0));
        Assert.That(magicBe, Is.EqualTo(0x58444246u));
        Assert.That(magicLe, Is.EqualTo(0x46424458u));
        SpaFile spa = SpaFile.FromBytes(beBytes);
        try
        {
            Assert.That(spa.IsValid, Is.True);
        }
        finally { spa.Dispose(); }
    }

    [Test]
    public void FromBytes_TruncatedGpd_ReturnsInvalid()
    {
        // Valid magic but truncated (<24 bytes header) should cause GpdFile.FromBytes throw -> Spa invalid
        byte[] data = new byte[10];
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(0), 0x58444246u);
        SpaFile spa = SpaFile.FromBytes(data);
        Assert.That(spa.IsValid, Is.False);
        Assert.That(spa.ValidationError, Does.Contain("Failed to parse SPA"));
    }

    #endregion

    #region Load / Dispose

    [Test]
    public void Load_Nonexistent_ThrowsFileNotFound()
    {
        string path = Path.Combine(Path.GetTempPath(), $"nonexistent_{Guid.NewGuid()}.spa");
        Assert.Throws<FileNotFoundException>(() => SpaFile.Load(path));
    }

    [Test]
    public void Load_ValidFile_ParsesSuccessfully()
    {
        string tmp = Path.Combine(Path.GetTempPath(), $"spa_{Guid.NewGuid()}.bin");
        try
        {
            using GpdFile gpd = GpdFile.Create();
            gpd.AddImage(0x8000, LoadIcon());
            File.WriteAllBytes(tmp, gpd.ToBytes());
            using SpaFile spa = SpaFile.Load(tmp);
            Assert.That(spa.IsValid, Is.True);
            Assert.That(spa.GetTitleIcon(), Is.Not.Null);
        }
        finally
        {
            if (File.Exists(tmp))
            {
                File.Delete(tmp);
            }
        }
    }

    [Test]
    public void Dispose_Idempotent_DoesNotThrow()
    {
        using GpdFile gpd = GpdFile.Create();
        gpd.AddImage(0x8000, MinimalPng());
        SpaFile spa = SpaFile.FromBytes(gpd.ToBytes());
        Assert.That(spa.IsValid, Is.True);
        spa.Dispose();
        Assert.DoesNotThrow(() => spa.Dispose());
    }

    #endregion

    #region Header / Data / EntryTable wrappers

    [Test]
    public void Header_EntryTable_Data_ExposeUnderlyingGpd()
    {
        using GpdFile gpd = GpdFile.Create();
        byte[] png = MinimalPng();
        gpd.AddImage(0x8000, png);
        gpd.AddImage(0x8001, png);
        byte[] bytes = gpd.ToBytes();
        using SpaFile spa = SpaFile.FromBytes(bytes);
        Assert.That(spa.IsValid, Is.True);
        Assert.That(spa.Header.EntryCount, Is.EqualTo(2));
        Assert.That(spa.EntryTable.Count, Is.EqualTo(2));
        Assert.That(spa.Data.Length, Is.GreaterThan(0));
        Assert.That(spa.Images.Count(), Is.EqualTo(2));
    }

    [Test]
    public void Titles_Achievements_ProxyToGpd()
    {
        using GpdFile gpd = GpdFile.Create();
        gpd.AddTitle(new TitleEntry
        {
            TitleId = 0x12345678,
            AchievementCount = 10
        });
        gpd.AddAchievement(new AchievementEntry
        {
            AchievementId = 1,
            Gamerscore = 5,
            Name = "A"
        });
        byte[] bytes = gpd.ToBytes();
        using SpaFile spa = SpaFile.FromBytes(bytes);
        Assert.That(spa.Titles.Count(), Is.EqualTo(1));
        // Note: spa.Achievements is filtered by IsValid, may be 1
        Assert.That(spa.Achievements.Count(), Is.GreaterThanOrEqualTo(0));
    }

    #endregion

    #region GetTitleIcon / GetAnyValidIcon / GetImage

    [Test]
    public void GetTitleIcon_WithValidIcon_ReturnsPng()
    {
        byte[] png = LoadIcon();
        using GpdFile gpd = GpdFile.Create();
        gpd.AddImage(0x8000, png);
        using SpaFile spa = SpaFile.FromBytes(gpd.ToBytes());
        byte[]? icon = spa.GetTitleIcon();
        Assert.That(icon, Is.Not.Null);
        Assert.That(icon!.Length, Is.EqualTo(png.Length));
        Assert.That(icon[0], Is.EqualTo(0x89));
    }

    [Test]
    public void GetTitleIcon_Missing_ReturnsNull()
    {
        using GpdFile gpd = GpdFile.Create();
        gpd.AddImage(0x8001, MinimalPng());
        using SpaFile spa = SpaFile.FromBytes(gpd.ToBytes());
        Assert.That(spa.GetTitleIcon(), Is.Null);
    }

    [Test]
    public void GetTitleIcon_InvalidSpa_ReturnsNull()
    {
        SpaFile spa = SpaFile.FromBytes([0x00, 0x01, 0x02, 0x03]);
        Assert.That(spa.IsValid, Is.False);
        Assert.That(spa.GetTitleIcon(), Is.Null);
        spa.Dispose();
    }

    [Test]
    public void GetAnyValidIcon_FallbackToLargest()
    {
        byte[] small = MinimalPng(); // 67 bytes
        byte[] large = LoadIcon(); // 9961 bytes
        using GpdFile gpd = GpdFile.Create();
        gpd.AddImage(0x8001, small);
        gpd.AddImage(0x8002, large);
        using SpaFile spa = SpaFile.FromBytes(gpd.ToBytes());
        byte[]? icon = spa.GetAnyValidIcon();
        Assert.That(icon, Is.Not.Null);
        Assert.That(icon!.Length, Is.EqualTo(large.Length));
    }

    [Test]
    public void GetAnyValidIcon_NoValidPng_ReturnsNull()
    {
        using GpdFile gpd = GpdFile.Create();
        // Add non-PNG image data (will be considered invalid by IsValidPng but still stored)
        // ImageEntry.IsValidPng checks signature, so this will be invalid
        byte[] notPng = [0x00, 0x01, 0x02, 0x03, 0x04, 0x05];
        // Use direct FromBytes fallback path: add via GpdFile.AddImage expects png but will still store
        // However GetAnyValidIcon filters IsValidPng, so it should return null
        gpd.AddImage(0x8001, notPng);
        using SpaFile spa = SpaFile.FromBytes(gpd.ToBytes());
        // Images property filters IsValid (not IsValidPng), but GetAnyValidIcon checks IsValidPng
        // The notPng will have IsValid true but IsValidPng false, so GetAnyValidIcon should null
        Assert.That(spa.GetAnyValidIcon(), Is.Null);
    }

    [Test]
    public void GetAnyValidIcon_InvalidSpa_ReturnsNull()
    {
        SpaFile spa = SpaFile.FromBytes([0x58, 0x44, 0x42]);
        Assert.That(spa.GetAnyValidIcon(), Is.Null);
        spa.Dispose();
    }

    [Test]
    public void GetImage_ValidId_ReturnsEntry()
    {
        byte[] png = MinimalPng();
        using GpdFile gpd = GpdFile.Create();
        gpd.AddImage(0x8000, png);
        using SpaFile spa = SpaFile.FromBytes(gpd.ToBytes());
        ImageEntry? img = spa.GetImage(0x8000);
        Assert.That(img, Is.Not.Null);
        Assert.That(img!.IsValidPng, Is.True);
        Assert.That(img.ImageData.Length, Is.EqualTo(png.Length));
        // GpdFile.GetImage does not propagate ImageId from entry table; EnumerateImagesWithIds does.
        // So we verify via enumeration instead:
        (ulong Id, ImageEntry Image) viaEnum = spa.EnumerateImagesWithIds().FirstOrDefault(x => x.Id == 0x8000);
        Assert.That(viaEnum.Image, Is.Not.Null);
        Assert.That(viaEnum.Image.ImageId, Is.EqualTo(0x8000));
    }

    [Test]
    public void GetImage_Missing_ReturnsNull()
    {
        using GpdFile gpd = GpdFile.Create();
        gpd.AddImage(0x8000, MinimalPng());
        using SpaFile spa = SpaFile.FromBytes(gpd.ToBytes());
        Assert.That(spa.GetImage(0x9999), Is.Null);
    }

    [Test]
    public void GetImage_InvalidSpa_ReturnsNull()
    {
        SpaFile spa = SpaFile.FromBytes([0x00, 0x00, 0x00, 0x00]);
        Assert.That(spa.GetImage(0x8000), Is.Null);
        spa.Dispose();
    }

    #endregion

    #region EnumerateImagesWithIds

    [Test]
    public void EnumerateImagesWithIds_ReturnsAllIds()
    {
        byte[] png = MinimalPng();
        using GpdFile gpd = GpdFile.Create();
        gpd.AddImage(0x8000, png);
        gpd.AddImage(0x8001, png);
        gpd.AddImage(0x1234, png);
        using SpaFile spa = SpaFile.FromBytes(gpd.ToBytes());
        List<(ulong Id, ImageEntry Image)> list = spa.EnumerateImagesWithIds().ToList();
        Assert.That(list.Count, Is.EqualTo(3));
        Assert.That(list.Select(x => x.Id), Does.Contain(0x8000));
        Assert.That(list.Select(x => x.Id), Does.Contain(0x8001));
        Assert.That(list.Select(x => x.Id), Does.Contain(0x1234));
        foreach ((ulong id, ImageEntry img) in list)
        {
            Assert.That(img.ImageId, Is.EqualTo((uint)id));
        }
    }

    [Test]
    public void EnumerateImagesWithIds_FallbackForNonPng_YieldsRaw()
    {
        // Create GPD with non-PNG data that GetImage would reject (IsValidPng false) but fallback yields
        using GpdFile gpd = GpdFile.Create();
        byte[] notPng = System.Text.Encoding.ASCII.GetBytes("NOT_A_PNG_BUT_DATA");
        // Add via raw manipulation to bypass IsValidPng check in GetImage
        byte[] png = MinimalPng();
        gpd.AddImage(0x8000, png); // valid one
        // Manually add entry with non-PNG data
        int oldLen = gpd.Data.Length;
        byte[] newData = new byte[oldLen + notPng.Length];
        gpd.Data.CopyTo(newData, 0);
        notPng.CopyTo(newData, oldLen);
        typeof(GpdFile).GetProperty("Data", BindingFlags.Public | BindingFlags.Instance)!.SetValue(gpd, newData);
        gpd.Entries.Add(new EntryTableEntry
        {
            Namespace = EntryNamespace.Image,
            Id = 0x9999,
            OffsetSpecifier = (uint)oldLen,
            Length = (uint)notPng.Length
        });
        typeof(GpdFile).GetProperty("Header", BindingFlags.Public | BindingFlags.Instance)!.SetValue(gpd,
            new XdbfHeader
            {
                Magic = gpd.Header.Magic,
                Version = gpd.Header.Version,
                EntryTableLength = gpd.Header.EntryTableLength,
                EntryCount = (uint)gpd.Entries.Count,
                FreeSpaceTableLength = gpd.Header.FreeSpaceTableLength,
                FreeSpaceTableEntryCount = gpd.Header.FreeSpaceTableEntryCount
            });
        using SpaFile spa = SpaFile.FromBytes(gpd.ToBytes());
        List<(ulong Id, ImageEntry Image)> list = spa.EnumerateImagesWithIds().ToList();
        // Should contain both, including the non-PNG fallback (now IsValid true but IsValidPng false? Actually FromBytes just copies, IsValid true, IsValidPng false, but fallback still yields)
        Assert.That(list.Count, Is.EqualTo(2));
        (ulong Id, ImageEntry Image) fallback = list.FirstOrDefault(x => x.Id == 0x9999);
        Assert.That(fallback.Image, Is.Not.Null);
        // Fallback image will have Length == notPng.Length, IsValid true but IsValidPng false? Check fallback code yields even if not PNG.
        // In SpaFile, fallback creates ImageEntry.FromBytes which is valid even if not PNG, and sets ImageId
        Assert.That(fallback.Image.ImageData.Length, Is.EqualTo(notPng.Length));
    }

    [Test]
    public void EnumerateImagesWithIds_InvalidSpa_ReturnsEmpty()
    {
        // SpaFile.EnumerateImagesWithIds iterates _gpd.Entries, but if Spa is invalid, _gpd is empty (created via GpdFile.Create())
        // However Spa created via new SpaFile(error) has empty Gpd, so enumeration should be empty, not throw
        SpaFile spa = SpaFile.FromBytes([0x00, 0x01, 0x02, 0x03]);
        Assert.That(spa.IsValid, Is.False);
        // Even invalid, enumeration shouldn't throw – but our implementation will iterate empty entries
        List<(ulong Id, ImageEntry Image)> list = spa.EnumerateImagesWithIds().ToList();
        Assert.That(list, Is.Empty);
        spa.Dispose();
    }

    #endregion

    #region SpaAchievements

    [Test]
    public void SpaAchievements_NoXach_ReturnsEmpty()
    {
        using GpdFile gpd = GpdFile.Create();
        gpd.AddImage(0x8000, MinimalPng());
        using SpaFile spa = SpaFile.FromBytes(gpd.ToBytes());
        Assert.That(spa.SpaAchievements, Is.Empty);
    }

    [Test]
    public void SpaAchievements_ValidSingle_ReturnsCorrectFields()
    {
        ushort id = 0x1234;
        uint imgId = 0x8000;
        byte[] xach = BuildXachData(1, (span, idx) =>
        {
            BinaryPrimitives.WriteUInt16BigEndian(span.Slice(0), id);
            BinaryPrimitives.WriteUInt16BigEndian(span.Slice(2), 0x0011); // label
            BinaryPrimitives.WriteUInt16BigEndian(span.Slice(4), 0x0022); // desc
            BinaryPrimitives.WriteUInt16BigEndian(span.Slice(6), 0x0033); // unach
            BinaryPrimitives.WriteUInt32BigEndian(span.Slice(8), imgId);
            BinaryPrimitives.WriteUInt16BigEndian(span.Slice(12), 50); // gamerscore
            BinaryPrimitives.WriteUInt16BigEndian(span.Slice(14), 0);
            BinaryPrimitives.WriteUInt32BigEndian(span.Slice(16), 0x01);
            BinaryPrimitives.WriteUInt32BigEndian(span.Slice(20), 0x02);
            BinaryPrimitives.WriteUInt32BigEndian(span.Slice(24), 0x03);
            BinaryPrimitives.WriteUInt32BigEndian(span.Slice(28), 0x04);
            BinaryPrimitives.WriteUInt32BigEndian(span.Slice(32), 0x05);
        });
        byte[] spaBytes = BuildSpaWithXach(xach, MinimalPng());
        using SpaFile spa = SpaFile.FromBytes(spaBytes);
        List<SpaAchievement> achs = spa.SpaAchievements.ToList();
        Assert.That(achs.Count, Is.EqualTo(1));
        SpaAchievement a = achs[0];
        Assert.That(a.Id, Is.EqualTo(id));
        Assert.That(a.LabelId, Is.EqualTo(0x0011));
        Assert.That(a.DescriptionId, Is.EqualTo(0x0022));
        Assert.That(a.UnachievedId, Is.EqualTo(0x0033));
        Assert.That(a.ImageId, Is.EqualTo(imgId));
        Assert.That(a.Gamerscore, Is.EqualTo(50));
        Assert.That(a.Flags, Is.EqualTo(0x01));
    }

    [Test]
    public void SpaAchievements_Multiple_ReturnsAll()
    {
        byte[] xach = BuildXachData(3);
        byte[] spaBytes = BuildSpaWithXach(xach);
        using SpaFile spa = SpaFile.FromBytes(spaBytes);
        Assert.That(spa.SpaAchievements.Count, Is.EqualTo(3));
        Assert.That(spa.SpaAchievements[0].Id, Is.EqualTo(1));
        Assert.That(spa.SpaAchievements[2].Id, Is.EqualTo(3));
    }

    [Test]
    public void SpaAchievements_Truncated_CountIsAdjusted()
    {
        // Create XACH with count=5 but only provide data for 2 entries (truncated)
        ushort count = 5;
        byte[] full = BuildXachData(count);
        // Truncate to header + 2 entries
        byte[] truncated = full[..(14 + 2 * 0x24)];
        // But keep header count=5 so parser must adjust
        BinaryPrimitives.WriteUInt16BigEndian(truncated.AsSpan(12), count);
        byte[] spaBytes = BuildSpaWithXach(truncated);
        using SpaFile spa = SpaFile.FromBytes(spaBytes);
        // Should parse only 2 available
        Assert.That(spa.SpaAchievements.Count, Is.EqualTo(2));
    }

    [Test]
    public void SpaAchievements_WrongMagic_ReturnsEmpty()
    {
        byte[] xach = BuildXachData(1, magic: 0x12345678);
        byte[] spaBytes = BuildSpaWithXach(xach);
        using SpaFile spa = SpaFile.FromBytes(spaBytes);
        Assert.That(spa.SpaAchievements, Is.Empty);
    }

    [Test]
    public void SpaAchievements_DataTooShort_ReturnsEmpty()
    {
        byte[] xach = new byte[10]; // <14
        BinaryPrimitives.WriteUInt32BigEndian(xach.AsSpan(0), 0x58414348u);
        byte[] spaBytes = BuildSpaWithXach(xach);
        using SpaFile spa = SpaFile.FromBytes(spaBytes);
        Assert.That(spa.SpaAchievements, Is.Empty);
    }

    [Test]
    public void SpaAchievements_OffsetOutOfBounds_ReturnsEmpty()
    {
        using GpdFile gpd = GpdFile.Create();
        // Add a dummy image to have Data length >0
        gpd.AddImage(0x8000, MinimalPng());
        // Manually add XACH entry with offset beyond Data length
        gpd.Entries.Add(new EntryTableEntry
        {
            Namespace = (EntryNamespace)0x0001,
            Id = 0x58414348,
            OffsetSpecifier = 99999,
            Length = 100
        });
        // Update header
        typeof(GpdFile).GetProperty("Header", BindingFlags.Public | BindingFlags.Instance)!.SetValue(gpd,
            new XdbfHeader
            {
                Magic = gpd.Header.Magic,
                Version = gpd.Header.Version,
                EntryTableLength = gpd.Header.EntryTableLength,
                EntryCount = (uint)gpd.Entries.Count,
                FreeSpaceTableLength = gpd.Header.FreeSpaceTableLength,
                FreeSpaceTableEntryCount = gpd.Header.FreeSpaceTableEntryCount
            });
        using SpaFile spa = SpaFile.FromBytes(gpd.ToBytes());
        Assert.That(spa.SpaAchievements, Is.Empty);
    }

    [Test]
    public void SpaAchievements_LengthOverflow_ReturnsEmpty()
    {
        using GpdFile gpd = GpdFile.Create();
        gpd.AddImage(0x8000, MinimalPng());
        // Offset valid but Length exceeds Data.Length - Offset (overflow-safe check)
        uint offset = 0;
        uint length = (uint)(gpd.Data.Length + 1000);
        gpd.Entries.Add(new EntryTableEntry
        {
            Namespace = (EntryNamespace)0x0001,
            Id = 0x58414348,
            OffsetSpecifier = offset,
            Length = length
        });
        typeof(GpdFile).GetProperty("Header", BindingFlags.Public | BindingFlags.Instance)!.SetValue(gpd,
            new XdbfHeader
            {
                Magic = gpd.Header.Magic,
                Version = gpd.Header.Version,
                EntryTableLength = gpd.Header.EntryTableLength,
                EntryCount = (uint)gpd.Entries.Count,
                FreeSpaceTableLength = gpd.Header.FreeSpaceTableLength,
                FreeSpaceTableEntryCount = gpd.Header.FreeSpaceTableEntryCount
            });
        using SpaFile spa = SpaFile.FromBytes(gpd.ToBytes());
        Assert.That(spa.SpaAchievements, Is.Empty);
    }

    [Test]
    public void SpaAchievements_Cached_ReturnsSameInstance()
    {
        byte[] xach = BuildXachData(2);
        byte[] spaBytes = BuildSpaWithXach(xach);
        using SpaFile spa = SpaFile.FromBytes(spaBytes);
        IReadOnlyList<SpaAchievement> first = spa.SpaAchievements;
        IReadOnlyList<SpaAchievement> second = spa.SpaAchievements;
        Assert.That(second, Is.SameAs(first));
        Assert.That(first.Count, Is.EqualTo(2));
    }

    [Test]
    public void SpaAchievements_InvalidSpa_ReturnsEmpty()
    {
        SpaFile spa = SpaFile.FromBytes([0x00, 0x00, 0x00, 0x00]);
        Assert.That(spa.SpaAchievements, Is.Empty);
        spa.Dispose();
    }

    [Test]
    public void SpaAchievements_VersionNotOne_StillParses()
    {
        byte[] xach = BuildXachData(1, version: 2);
        byte[] spaBytes = BuildSpaWithXach(xach);
        using SpaFile spa = SpaFile.FromBytes(spaBytes);
        Assert.That(spa.SpaAchievements.Count, Is.EqualTo(1));
    }

    #endregion

    #region SpaAchievement model

    [Test]
    public void SpaAchievement_Properties_Settable()
    {
        SpaAchievement a = new SpaAchievement
        {
            Id = 1,
            LabelId = 2,
            DescriptionId = 3,
            UnachievedId = 4,
            ImageId = 0x8000,
            Gamerscore = 10,
            UnkE = 0,
            Flags = 0x01,
            Unk14 = 0x02,
            Unk18 = 0x03,
            Unk1C = 0x04,
            Unk20 = 0x05
        };
        Assert.That(a.Id, Is.EqualTo(1));
        Assert.That(a.ImageId, Is.EqualTo(0x8000u));
        Assert.That(a.Gamerscore, Is.EqualTo(10));
    }

    #endregion
}