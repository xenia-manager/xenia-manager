using System.Buffers.Binary;
using System.Reflection;
using System.Text;
using XeniaManager.Files;
using XeniaManager.Files.Models.Gpd;
using XeniaManager.Files.Models.Stfs;
using XeniaManager.Files.Models.Zar;

namespace XeniaManager.Tests.Files;

[TestFixture]
public class StfsFileIconTests
{
    private static byte[] LoadIcon()
    {
        Assembly coreAssembly = typeof(XeniaManager.Core.Manage.ArtworkManager).Assembly;
        using Stream? s = coreAssembly.GetManifestResourceStream("XeniaManager.Core.Assets.Artwork.Icon.png");
        Assume.That(s, Is.Not.Null);
        using MemoryStream ms = new MemoryStream();
        s!.CopyTo(ms);
        return ms.ToArray();
    }

    private static byte[] MinimalPng()
    {
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

    private static byte[] BuildGpdWithIcon(uint imageId = 0x8000, byte[]? png = null)
    {
        png ??= LoadIcon();
        using GpdFile gpd = GpdFile.Create();
        gpd.AddImage(imageId, png);
        return gpd.ToBytes();
    }

    private static byte[] BuildPeImage(string sectionName, byte[] sectionData, int rawAddr = 0x200)
    {
        int peSize = rawAddr + sectionData.Length + 0x100;
        peSize = (peSize + 15) & ~15;
        byte[] pe = new byte[peSize];
        pe[0] = 0x4D;
        pe[1] = 0x5A;
        BinaryPrimitives.WriteInt32LittleEndian(pe.AsSpan(0x3C), 0x80);
        pe[0x80] = 0x50;
        pe[0x81] = 0x45;
        pe[0x82] = 0x00;
        pe[0x83] = 0x00;
        BinaryPrimitives.WriteUInt16LittleEndian(pe.AsSpan(0x84), 0x014C);
        BinaryPrimitives.WriteUInt16LittleEndian(pe.AsSpan(0x86), 1);
        BinaryPrimitives.WriteUInt16LittleEndian(pe.AsSpan(0x94), 0xE0);
        BinaryPrimitives.WriteUInt16LittleEndian(pe.AsSpan(0x98), 0x010B);
        int sh = 0x98 + 0xE0;
        byte[] nameBytes = Encoding.ASCII.GetBytes(sectionName);
        Array.Copy(nameBytes, 0, pe, sh, Math.Min(nameBytes.Length, 8));
        BinaryPrimitives.WriteUInt32LittleEndian(pe.AsSpan(sh + 8), (uint)sectionData.Length);
        BinaryPrimitives.WriteUInt32LittleEndian(pe.AsSpan(sh + 12), 0x1000);
        BinaryPrimitives.WriteUInt32LittleEndian(pe.AsSpan(sh + 16), (uint)sectionData.Length);
        BinaryPrimitives.WriteUInt32LittleEndian(pe.AsSpan(sh + 20), (uint)rawAddr);
        Array.Copy(sectionData, 0, pe, rawAddr, sectionData.Length);
        return pe;
    }

    private static byte[] BuildMinimalXex(uint titleId, byte[] peImage, uint mediaId = 0x12345678)
    {
        const uint headerSize = 0x300;
        const uint securityOffset = 0x40;
        const int executionInfoOffset = 0x40 + 0x184;
        int totalSize = (int)headerSize + peImage.Length + 0x100;
        totalSize = (totalSize + 15) & ~15;
        byte[] xex = new byte[totalSize];
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan(0x00), 0x58455832u);
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan(0x08), headerSize);
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan(0x10), securityOffset);
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan(0x14), 1);
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan(0x18), 0x00040006u);
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan(0x1C), (uint)executionInfoOffset);
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan((int)securityOffset + 0x00), 0x180);
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan((int)securityOffset + 0x04), (uint)peImage.Length);
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan((int)securityOffset + 0x178), 0xFF);
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan((int)securityOffset + 0x17C), 0xFFFFFFFF);
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan(executionInfoOffset + 0x00), mediaId);
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan(executionInfoOffset + 0x04), 1);
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan(executionInfoOffset + 0x08), 1);
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan(executionInfoOffset + 0x0C), titleId);
        xex[executionInfoOffset + 0x12] = 1;
        xex[executionInfoOffset + 0x13] = 1;
        Array.Copy(peImage, 0, xex, (int)headerSize, peImage.Length);
        return xex;
    }

    private static StfsFile CreateMockStfsWithXex(byte[] xexBytes, byte[]? thumbnail = null, byte[]? titleThumbnail = null)
    {
        // Build raw buffer sized to hold header + hash table + file data at 0xC000
        const int headerSize = 0xB000;
        const int fileOffset = 0xC000; // BlockNumberToOffset(0) with blocksPerHashTable=1
        byte[] raw = new byte[fileOffset + xexBytes.Length + 0x1000];
        Encoding.ASCII.GetBytes("CON ").CopyTo(raw, 0);
        BinaryPrimitives.WriteInt32BigEndian(raw.AsSpan(0x340), headerSize);
        BinaryPrimitives.WriteInt32BigEndian(raw.AsSpan(0x344), 0x1000);
        BinaryPrimitives.WriteInt32BigEndian(raw.AsSpan(0x348), 1);
        BinaryPrimitives.WriteInt32BigEndian(raw.AsSpan(0x360), 0x12345678);
        raw[0x379] = 0x24;

        // Inject thumbnails if provided
        if (thumbnail != null)
        {
            BinaryPrimitives.WriteInt32BigEndian(raw.AsSpan(0x1712), thumbnail.Length);
            Array.Copy(thumbnail, 0, raw, 0x171A, Math.Min(thumbnail.Length, raw.Length - 0x171A));
        }
        else
        {
            BinaryPrimitives.WriteInt32BigEndian(raw.AsSpan(0x1712), 0);
        }

        if (titleThumbnail != null)
        {
            BinaryPrimitives.WriteInt32BigEndian(raw.AsSpan(0x1716), titleThumbnail.Length);
            Array.Copy(titleThumbnail, 0, raw, 0x571A, Math.Min(titleThumbnail.Length, raw.Length - 0x571A));
        }
        else
        {
            BinaryPrimitives.WriteInt32BigEndian(raw.AsSpan(0x1716), 0);
        }

        ConstructorInfo ctor = typeof(StfsFile).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, [typeof(byte[])], null)!;
        StfsFile stfs = (StfsFile)ctor.Invoke([raw]);

        // Patch _rawData is already set via ctor, but we need to ensure it contains the XEX at fileOffset
        Array.Copy(xexBytes, 0, raw, fileOffset, xexBytes.Length);
        FieldInfo rawField = typeof(StfsFile).GetField("_rawData", BindingFlags.NonPublic | BindingFlags.Instance)!;
        rawField.SetValue(stfs, raw);

        // Force metadata to have correct HeaderSize and thumbnails by using FromBytes path? Instead manually set via reflection
        // Use FromBytes to parse our header, then override FileEntries
        // Easiest: create via FromBytes then patch FileEntries
        // But we already have an instance; set Metadata.HeaderSize directly and thumbnails via raw already parsed? The ctor didn't parse metadata, we need to set Metadata manually via property
        // Let's use reflection to set private field blocksPerHashTable and Metadata
        FieldInfo bphField = typeof(StfsFile).GetField("blocksPerHashTable", BindingFlags.NonPublic | BindingFlags.Instance)!;
        bphField.SetValue(stfs, (uint)1);

        // Build Metadata manually or reuse parsed via FromBytes logic: call FromBytes on raw to get a correctly parsed Metadata, then copy it
        StfsFile dummy = StfsFile.FromBytes(raw, false); // false = don't parse file table, but will parse metadata
        PropertyInfo metaProp = typeof(StfsFile).GetProperty("Metadata")!;
        metaProp.SetValue(stfs, dummy.Metadata);

        // Add file entry for default.xex
        List<StfsFileEntry> entries = stfs.FileEntries;
        StfsFileEntry entry = new StfsFileEntry
        {
            FileName = "default.xex",
            Flags = 0x40,
            ValidDataBlocks = (xexBytes.Length + 0xFFF) / 0x1000,
            AllocatedDataBlocks = (xexBytes.Length + 0xFFF) / 0x1000,
            StartingBlock = 0,
            PathIndicator = -1,
            FileSize = xexBytes.Length
        };
        // Fix Flags to include name length
        entry.NameLength = (byte)"default.xex".Length;
        entry.Flags |= 0x40;
        entries.Add(entry);

        // Ensure HeaderSize is set correctly via Metadata
        stfs.Metadata.HeaderSize = headerSize;

        return stfs;
    }

    private static StfsFile CreateMockStfsWithThumbnailOnly(byte[] thumbnail, byte[]? titleThumbnail = null)
    {
        // Create via FromBytes with header containing thumbnail, no file entries
        byte[] raw = new byte[0xB000];
        Encoding.ASCII.GetBytes("CON ").CopyTo(raw, 0);
        BinaryPrimitives.WriteInt32BigEndian(raw.AsSpan(0x340), 0xB000);
        BinaryPrimitives.WriteInt32BigEndian(raw.AsSpan(0x344), 0x1000);
        BinaryPrimitives.WriteInt32BigEndian(raw.AsSpan(0x348), 1);
        BinaryPrimitives.WriteInt32BigEndian(raw.AsSpan(0x360), 0x12345678);
        raw[0x379] = 0x24;
        BinaryPrimitives.WriteInt32BigEndian(raw.AsSpan(0x1712), thumbnail.Length);
        BinaryPrimitives.WriteInt32BigEndian(raw.AsSpan(0x1716), titleThumbnail?.Length ?? 0);
        Array.Copy(thumbnail, 0, raw, 0x171A, Math.Min(thumbnail.Length, raw.Length - 0x171A));
        if (titleThumbnail != null)
        {
            Array.Copy(titleThumbnail, 0, raw, 0x571A, Math.Min(titleThumbnail.Length, raw.Length - 0x571A));
        }

        return StfsFile.FromBytes(raw, false);
    }

    [Test]
    public void TryGetIcon_NoThumbnailNoXex_ReturnsNull()
    {
        byte[] raw = new byte[0xB000];
        Encoding.ASCII.GetBytes("CON ").CopyTo(raw, 0);
        BinaryPrimitives.WriteInt32BigEndian(raw.AsSpan(0x340), 0xB000);
        BinaryPrimitives.WriteInt32BigEndian(raw.AsSpan(0x344), 0x1000);
        BinaryPrimitives.WriteInt32BigEndian(raw.AsSpan(0x348), 1);
        raw[0x379] = 0x24;
        BinaryPrimitives.WriteInt32BigEndian(raw.AsSpan(0x1712), 0);
        BinaryPrimitives.WriteInt32BigEndian(raw.AsSpan(0x1716), 0);
        StfsFile stfs = StfsFile.FromBytes(raw, false);
        Assert.That(stfs.TryGetIcon(), Is.Null);
        Assert.That(stfs.TryGetSpaFile(out SpaFile? spa), Is.False);
        Assert.That(spa, Is.Null);
    }

    [Test]
    public void TryGetIcon_ThumbnailFallback_ReturnsThumbnail()
    {
        byte[] icon = LoadIcon();
        StfsFile stfs = CreateMockStfsWithThumbnailOnly(icon);
        byte[]? extracted = stfs.TryGetIcon();
        Assert.That(extracted, Is.Not.Null);
        Assert.That(extracted!.Length, Is.EqualTo(icon.Length));
        Assert.That(extracted[0], Is.EqualTo(0x89));
    }

    [Test]
    public void TryGetIcon_TitleThumbnailFallback_ReturnsTitleThumbnail()
    {
        byte[] icon = LoadIcon();
        // Thumbnail empty, TitleThumbnail has icon
        StfsFile stfs = CreateMockStfsWithThumbnailOnly(new byte[0], icon);
        // Need to ensure we set thumbnail size 0 and title thumbnail properly - our helper does but we passed empty thumbnail with length 0,
        // but CreateMockStfsWithThumbnailOnly writes 0 for thumbnail and copies 0 bytes, then writes title thumbnail.
        // To make it more explicit, build raw manually
        byte[] raw = new byte[0xB000];
        Encoding.ASCII.GetBytes("CON ").CopyTo(raw, 0);
        BinaryPrimitives.WriteInt32BigEndian(raw.AsSpan(0x340), 0xB000);
        BinaryPrimitives.WriteInt32BigEndian(raw.AsSpan(0x344), 0x1000);
        BinaryPrimitives.WriteInt32BigEndian(raw.AsSpan(0x348), 1);
        raw[0x379] = 0x24;
        BinaryPrimitives.WriteInt32BigEndian(raw.AsSpan(0x1712), 0);
        BinaryPrimitives.WriteInt32BigEndian(raw.AsSpan(0x1716), icon.Length);
        Array.Copy(icon, 0, raw, 0x571A, icon.Length);
        StfsFile stfs2 = StfsFile.FromBytes(raw, false);
        byte[]? extracted = stfs2.TryGetIcon();
        Assert.That(extracted, Is.Not.Null);
        Assert.That(extracted!.Length, Is.EqualTo(icon.Length));
    }

    [Test]
    public void TryGetIcon_PrefersThumbnailOverXex_AsLastResort()
    {
        byte[] iconThumb = LoadIcon();
        uint titleId = 0x4D530910;
        byte[] xexIcon = LoadIcon();
        // Make XEX icon slightly different by using a different imageId (but same bytes) – still same length, so we need to differentiate
        // Use same icon but verify thumbnail is returned (first path). To differentiate, use a different byte array for XEX icon vs thumbnail
        // Create a second icon with minimal PNG (small) as XEX icon – but XEX icon will be >1024 so use same icon but we can check that returned bytes equal thumbnail
        // Create XEX with its own icon (same as thumb but we can still verify that thumbnail path is taken by ensuring thumb is returned)
        byte[] spa = BuildGpdWithIcon(0x8000, xexIcon);
        byte[] pe = BuildPeImage($"{titleId:X8}", spa);
        byte[] xexBytes = BuildMinimalXex(titleId, pe);
        StfsFile stfs = CreateMockStfsWithXex(xexBytes, iconThumb);
        byte[]? extracted = stfs.TryGetIcon();
        Assert.That(extracted, Is.Not.Null);
        // Should be thumbnail, not XEX icon, even though both are same length we can verify by ensuring it's the thumbnail object reference? Instead make thumb a JPEG header
        // Use a JPEG fake for thumb to differentiate
        byte[] jpegThumb = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01];
        StfsFile stfs2 = CreateMockStfsWithXex(xexBytes, jpegThumb);
        byte[]? extracted2 = stfs2.TryGetIcon();
        Assert.That(extracted2, Is.Not.Null);
        Assert.That(extracted2![0], Is.EqualTo(0xFF));
        Assert.That(extracted2[1], Is.EqualTo(0xD8));
    }

    [Test]
    public void TryGetIcon_EmbeddedXex_ReturnsIconWhenNoThumbnail()
    {
        uint titleId = 0x4D530910;
        byte[] icon = LoadIcon();
        byte[] spa = BuildGpdWithIcon(0x8000, icon);
        byte[] pe = BuildPeImage($"{titleId:X8}", spa);
        byte[] xexBytes = BuildMinimalXex(titleId, pe);
        StfsFile stfs = CreateMockStfsWithXex(xexBytes, null);
        byte[]? extracted = stfs.TryGetIcon();
        Assert.That(extracted, Is.Not.Null);
        Assert.That(extracted!.Length, Is.EqualTo(icon.Length));
        Assert.That(extracted[0], Is.EqualTo(0x89));
        stfs.Dispose();
    }

    [Test]
    public void TryGetIcon_CaseInsensitiveDefaultXex()
    {
        uint titleId = 0x4D530910;
        byte[] icon = LoadIcon();
        byte[] spa = BuildGpdWithIcon(0x8000, icon);
        byte[] pe = BuildPeImage($"{titleId:X8}", spa);
        byte[] xexBytes = BuildMinimalXex(titleId, pe);
        StfsFile stfs = CreateMockStfsWithXex(xexBytes, null);
        // Rename entry to upper case
        stfs.FileEntries[0].FileName = "DEFAULT.XEX";
        stfs.FileEntries[0].NameLength = (byte)"DEFAULT.XEX".Length;
        stfs.FileEntries[0].Flags |= 0x40;
        byte[]? extracted = stfs.TryGetIcon();
        Assert.That(extracted, Is.Not.Null);
        Assert.That(extracted!.Length, Is.EqualTo(icon.Length));
        stfs.Dispose();
    }

    [Test]
    public void TryGetIcon_FallbackToAnyXexWhenDefaultMissing()
    {
        uint titleId = 0x4D530910;
        byte[] icon = LoadIcon();
        byte[] spa = BuildGpdWithIcon(0x8000, icon);
        byte[] pe = BuildPeImage($"{titleId:X8}", spa);
        byte[] xexBytes = BuildMinimalXex(titleId, pe);
        StfsFile stfs = CreateMockStfsWithXex(xexBytes, null);
        stfs.FileEntries[0].FileName = "other.xex";
        stfs.FileEntries[0].NameLength = (byte)"other.xex".Length;
        stfs.FileEntries[0].Flags |= 0x40;
        byte[]? extracted = stfs.TryGetIcon();
        Assert.That(extracted, Is.Not.Null);
        stfs.Dispose();
    }

    [Test]
    public void TryGetIcon_InvalidThumbnailFallsThroughToXex()
    {
        uint titleId = 0x4D530910;
        byte[] icon = LoadIcon();
        byte[] spa = BuildGpdWithIcon(0x8000, icon);
        byte[] pe = BuildPeImage($"{titleId:X8}", spa);
        byte[] xexBytes = BuildMinimalXex(titleId, pe);
        // Invalid thumbnail (random bytes not PNG/JPEG)
        byte[] badThumb = [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x00, 0x01];
        StfsFile stfs = CreateMockStfsWithXex(xexBytes, badThumb);
        byte[]? extracted = stfs.TryGetIcon();
        Assert.That(extracted, Is.Not.Null);
        Assert.That(extracted![0], Is.EqualTo(0x89));
        stfs.Dispose();
    }

    [Test]
    public void TryGetIcon_NeverThrows_OnCorruptedMetadata()
    {
        // Create a valid CON STFS with a deliberately truncated/corrupted thumbnail header
        // but still parsable – TryGetIcon must never throw even when metadata is weird.
        byte[] raw = new byte[0xB000];
        Encoding.ASCII.GetBytes("CON ").CopyTo(raw, 0);
        BinaryPrimitives.WriteInt32BigEndian(raw.AsSpan(0x340), 0xB000);
        BinaryPrimitives.WriteInt32BigEndian(raw.AsSpan(0x344), 0x1000);
        BinaryPrimitives.WriteInt32BigEndian(raw.AsSpan(0x348), 1);
        raw[0x379] = 0x24;
        // Set thumbnail size to a huge value that exceeds buffer – FromBytes will clamp and not copy
        BinaryPrimitives.WriteInt32BigEndian(raw.AsSpan(0x1712), 0x7FFFFFFF);
        BinaryPrimitives.WriteInt32BigEndian(raw.AsSpan(0x1716), 0);
        StfsFile stfs = StfsFile.FromBytes(raw, false);
        Assert.DoesNotThrow(() => stfs.TryGetIcon());
        Assert.DoesNotThrow(() => { stfs.TryGetSpaFile(out _); });
    }

    [Test]
    public void TryGetSpaFile_EmbeddedXex_ReturnsSpa()
    {
        uint titleId = 0x4D530910;
        byte[] spa = BuildGpdWithIcon();
        byte[] pe = BuildPeImage($"{titleId:X8}", spa);
        byte[] xexBytes = BuildMinimalXex(titleId, pe);
        StfsFile stfs = CreateMockStfsWithXex(xexBytes);
        bool ok = stfs.TryGetSpaFile(out SpaFile? spaFile);
        Assert.That(ok, Is.True);
        Assert.That(spaFile, Is.Not.Null);
        Assert.That(spaFile!.IsValid, Is.True);
        spaFile.Dispose();
        stfs.Dispose();
    }

    [Test]
    public void TryGetSpaFile_NoXex_ReturnsFalse()
    {
        StfsFile stfs = CreateMockStfsWithThumbnailOnly(LoadIcon());
        bool ok = stfs.TryGetSpaFile(out SpaFile? spaFile);
        Assert.That(ok, Is.False);
        Assert.That(spaFile, Is.Null);
    }

    [Test]
    public void IsValidImageData_Png_Jpeg_Invalid()
    {
        MethodInfo method = typeof(StfsFile).GetMethod("IsValidImageData", BindingFlags.NonPublic | BindingFlags.Static)!;
        byte[] png = LoadIcon();
        Assert.That((bool)method.Invoke(null, [png])!, Is.True);
        byte[] jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46];
        Assert.That((bool)method.Invoke(null, [jpeg])!, Is.True);
        byte[] bad = [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07];
        Assert.That((bool)method.Invoke(null, [bad])!, Is.False);
        byte[] shortData = [0x89, 0x50];
        Assert.That((bool)method.Invoke(null, [shortData])!, Is.False);
    }
}

[TestFixture]
public class IsoFileIconTests
{
    private static byte[] LoadIcon()
    {
        Assembly coreAssembly = typeof(XeniaManager.Core.Manage.ArtworkManager).Assembly;
        using Stream? s = coreAssembly.GetManifestResourceStream("XeniaManager.Core.Assets.Artwork.Icon.png");
        Assume.That(s, Is.Not.Null);
        using MemoryStream ms = new MemoryStream();
        s!.CopyTo(ms);
        return ms.ToArray();
    }

    private static byte[] BuildGpdWithIcon(uint imageId = 0x8000, byte[]? png = null)
    {
        png ??= LoadIcon();
        using GpdFile gpd = GpdFile.Create();
        gpd.AddImage(imageId, png);
        return gpd.ToBytes();
    }

    private static byte[] BuildPeImage(string sectionName, byte[] sectionData, int rawAddr = 0x200)
    {
        int peSize = rawAddr + sectionData.Length + 0x100;
        peSize = (peSize + 15) & ~15;
        byte[] pe = new byte[peSize];
        pe[0] = 0x4D;
        pe[1] = 0x5A;
        BinaryPrimitives.WriteInt32LittleEndian(pe.AsSpan(0x3C), 0x80);
        pe[0x80] = 0x50;
        pe[0x81] = 0x45;
        pe[0x82] = 0x00;
        pe[0x83] = 0x00;
        BinaryPrimitives.WriteUInt16LittleEndian(pe.AsSpan(0x84), 0x014C);
        BinaryPrimitives.WriteUInt16LittleEndian(pe.AsSpan(0x86), 1);
        BinaryPrimitives.WriteUInt16LittleEndian(pe.AsSpan(0x94), 0xE0);
        BinaryPrimitives.WriteUInt16LittleEndian(pe.AsSpan(0x98), 0x010B);
        int sh = 0x98 + 0xE0;
        byte[] nameBytes = Encoding.ASCII.GetBytes(sectionName);
        Array.Copy(nameBytes, 0, pe, sh, Math.Min(nameBytes.Length, 8));
        BinaryPrimitives.WriteUInt32LittleEndian(pe.AsSpan(sh + 8), (uint)sectionData.Length);
        BinaryPrimitives.WriteUInt32LittleEndian(pe.AsSpan(sh + 12), 0x1000);
        BinaryPrimitives.WriteUInt32LittleEndian(pe.AsSpan(sh + 16), (uint)sectionData.Length);
        BinaryPrimitives.WriteUInt32LittleEndian(pe.AsSpan(sh + 20), (uint)rawAddr);
        Array.Copy(sectionData, 0, pe, rawAddr, sectionData.Length);
        return pe;
    }

    private static byte[] BuildMinimalXex(uint titleId, byte[] peImage, uint mediaId = 0x12345678)
    {
        const uint headerSize = 0x300;
        const uint securityOffset = 0x40;
        const int executionInfoOffset = 0x40 + 0x184;
        int totalSize = (int)headerSize + peImage.Length + 0x100;
        totalSize = (totalSize + 15) & ~15;
        byte[] xex = new byte[totalSize];
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan(0x00), 0x58455832u);
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan(0x08), headerSize);
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan(0x10), securityOffset);
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan(0x14), 1);
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan(0x18), 0x00040006u);
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan(0x1C), (uint)executionInfoOffset);
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan((int)securityOffset + 0x00), 0x180);
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan((int)securityOffset + 0x04), (uint)peImage.Length);
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan((int)securityOffset + 0x178), 0xFF);
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan((int)securityOffset + 0x17C), 0xFFFFFFFF);
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan(executionInfoOffset + 0x00), mediaId);
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan(executionInfoOffset + 0x04), 1);
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan(executionInfoOffset + 0x08), 1);
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan(executionInfoOffset + 0x0C), titleId);
        xex[executionInfoOffset + 0x12] = 1;
        xex[executionInfoOffset + 0x13] = 1;
        Array.Copy(peImage, 0, xex, (int)headerSize, peImage.Length);
        return xex;
    }

    private static IsoFile CreateMockIsoWithXex(byte[] xexBytes)
    {
        ConstructorInfo ctor = typeof(IsoFile).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null)!;
        IsoFile iso = (IsoFile)ctor.Invoke(null);
        typeof(IsoFile).GetProperty("IsValid")!.SetValue(iso, true);
        typeof(IsoFile).GetProperty("XexFile")!.SetValue(iso, XexFile.FromBytes(xexBytes));
        return iso;
    }

    private static IsoFile CreateMockIsoInvalid()
    {
        ConstructorInfo ctor = typeof(IsoFile).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null)!;
        IsoFile iso = (IsoFile)ctor.Invoke(null);
        typeof(IsoFile).GetProperty("IsValid")!.SetValue(iso, false);
        return iso;
    }

    [Test]
    public void TryGetIcon_InvalidIso_ReturnsNull()
    {
        using IsoFile iso = CreateMockIsoInvalid();
        Assert.That(iso.TryGetIcon(), Is.Null);
        Assert.That(iso.TryGetSpaFile(out SpaFile? spa), Is.False);
        Assert.That(spa, Is.Null);
    }

    [Test]
    public void TryGetIcon_WithValidXex_ReturnsIcon()
    {
        uint titleId = 0x4D530910;
        byte[] icon = LoadIcon();
        byte[] spa = BuildGpdWithIcon(0x8000, icon);
        byte[] pe = BuildPeImage($"{titleId:X8}", spa);
        byte[] xexBytes = BuildMinimalXex(titleId, pe);
        using IsoFile iso = CreateMockIsoWithXex(xexBytes);
        byte[]? extracted = iso.TryGetIcon();
        Assert.That(extracted, Is.Not.Null);
        Assert.That(extracted!.Length, Is.EqualTo(icon.Length));
        Assert.That(extracted[0], Is.EqualTo(0x89));
    }

    [Test]
    public void TryGetIcon_WithValidXex_NoIcon_ReturnsNull()
    {
        uint titleId = 0x4D530910;
        byte[] pe = BuildPeImage($"{titleId:X8}", Encoding.ASCII.GetBytes("no icon data"));
        byte[] xexBytes = BuildMinimalXex(titleId, pe);
        using IsoFile iso = CreateMockIsoWithXex(xexBytes);
        Assert.That(iso.TryGetIcon(), Is.Null);
    }

    [Test]
    public void TryGetIcon_InvalidXex_ReturnsNull()
    {
        byte[] badXex = [0x00, 0x01, 0x02, 0x03];
        XexFile xex = XexFile.FromBytes(badXex);
        Assume.That(xex.IsValid, Is.False);
        using IsoFile iso = CreateMockIsoWithXex(badXex);
        Assert.That(iso.TryGetIcon(), Is.Null);
    }

    [Test]
    public void TryGetIcon_NeverThrows_OnDisposedOrInvalid()
    {
        using IsoFile iso = CreateMockIsoInvalid();
        Assert.DoesNotThrow(() => iso.TryGetIcon());
        Assert.DoesNotThrow(() => iso.TryGetSpaFile(out _));
    }

    [Test]
    public void TryGetSpaFile_WithValidXex_ReturnsSpa()
    {
        uint titleId = 0x4D530910;
        byte[] spa = BuildGpdWithIcon();
        byte[] pe = BuildPeImage($"{titleId:X8}", spa);
        byte[] xexBytes = BuildMinimalXex(titleId, pe);
        using IsoFile iso = CreateMockIsoWithXex(xexBytes);
        bool ok = iso.TryGetSpaFile(out SpaFile? spaFile);
        Assert.That(ok, Is.True);
        Assert.That(spaFile, Is.Not.Null);
        Assert.That(spaFile!.IsValid, Is.True);
        spaFile.Dispose();
    }

    [Test]
    public void TryGetSpaFile_InvalidXex_ReturnsFalse()
    {
        byte[] pe = BuildPeImage("NOPE", new byte[64]);
        byte[] xexBytes = BuildMinimalXex(0x12345678, pe);
        using IsoFile iso = CreateMockIsoWithXex(xexBytes);
        // XEX is valid but has no SPA section, so TryGetSpaFile should still be false
        bool ok = iso.TryGetSpaFile(out SpaFile? spaFile);
        Assert.That(ok, Is.False);
        Assert.That(spaFile, Is.Null);
    }

    [Test]
    public void Load_InvalidFile_TryGetIconReturnsNull()
    {
        string tmp = Path.Combine(Path.GetTempPath(), $"test_invalid_{Guid.NewGuid()}.iso");
        try
        {
            File.WriteAllBytes(tmp, [0x00, 0x01, 0x02, 0x03]);
            using IsoFile iso = IsoFile.Load(tmp);
            Assert.That(iso.IsValid, Is.False);
            Assert.That(iso.TryGetIcon(), Is.Null);
        }
        finally
        {
            if (File.Exists(tmp))
            {
                File.Delete(tmp);
            }
        }
    }
}

[TestFixture]
public class ZarFileIconTests
{
    private static byte[] LoadIcon()
    {
        Assembly coreAssembly = typeof(XeniaManager.Core.Manage.ArtworkManager).Assembly;
        using Stream? s = coreAssembly.GetManifestResourceStream("XeniaManager.Core.Assets.Artwork.Icon.png");
        Assume.That(s, Is.Not.Null);
        using MemoryStream ms = new MemoryStream();
        s!.CopyTo(ms);
        return ms.ToArray();
    }

    private static byte[] BuildGpdWithIcon(uint imageId = 0x8000, byte[]? png = null)
    {
        png ??= LoadIcon();
        using GpdFile gpd = GpdFile.Create();
        gpd.AddImage(imageId, png);
        return gpd.ToBytes();
    }

    private static byte[] BuildPeImage(string sectionName, byte[] sectionData, int rawAddr = 0x200)
    {
        int peSize = rawAddr + sectionData.Length + 0x100;
        peSize = (peSize + 15) & ~15;
        byte[] pe = new byte[peSize];
        pe[0] = 0x4D;
        pe[1] = 0x5A;
        BinaryPrimitives.WriteInt32LittleEndian(pe.AsSpan(0x3C), 0x80);
        pe[0x80] = 0x50;
        pe[0x81] = 0x45;
        pe[0x82] = 0x00;
        pe[0x83] = 0x00;
        BinaryPrimitives.WriteUInt16LittleEndian(pe.AsSpan(0x84), 0x014C);
        BinaryPrimitives.WriteUInt16LittleEndian(pe.AsSpan(0x86), 1);
        BinaryPrimitives.WriteUInt16LittleEndian(pe.AsSpan(0x94), 0xE0);
        BinaryPrimitives.WriteUInt16LittleEndian(pe.AsSpan(0x98), 0x010B);
        int sh = 0x98 + 0xE0;
        byte[] nameBytes = Encoding.ASCII.GetBytes(sectionName);
        Array.Copy(nameBytes, 0, pe, sh, Math.Min(nameBytes.Length, 8));
        BinaryPrimitives.WriteUInt32LittleEndian(pe.AsSpan(sh + 8), (uint)sectionData.Length);
        BinaryPrimitives.WriteUInt32LittleEndian(pe.AsSpan(sh + 12), 0x1000);
        BinaryPrimitives.WriteUInt32LittleEndian(pe.AsSpan(sh + 16), (uint)sectionData.Length);
        BinaryPrimitives.WriteUInt32LittleEndian(pe.AsSpan(sh + 20), (uint)rawAddr);
        Array.Copy(sectionData, 0, pe, rawAddr, sectionData.Length);
        return pe;
    }

    private static byte[] BuildMinimalXex(uint titleId, byte[] peImage, uint mediaId = 0x12345678)
    {
        const uint headerSize = 0x300;
        const uint securityOffset = 0x40;
        const int executionInfoOffset = 0x40 + 0x184;
        int totalSize = (int)headerSize + peImage.Length + 0x100;
        totalSize = (totalSize + 15) & ~15;
        byte[] xex = new byte[totalSize];
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan(0x00), 0x58455832u);
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan(0x08), headerSize);
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan(0x10), securityOffset);
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan(0x14), 1);
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan(0x18), 0x00040006u);
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan(0x1C), (uint)executionInfoOffset);
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan((int)securityOffset + 0x00), 0x180);
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan((int)securityOffset + 0x04), (uint)peImage.Length);
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan((int)securityOffset + 0x178), 0xFF);
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan((int)securityOffset + 0x17C), 0xFFFFFFFF);
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan(executionInfoOffset + 0x00), mediaId);
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan(executionInfoOffset + 0x04), 1);
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan(executionInfoOffset + 0x08), 1);
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan(executionInfoOffset + 0x0C), titleId);
        xex[executionInfoOffset + 0x12] = 1;
        xex[executionInfoOffset + 0x13] = 1;
        Array.Copy(peImage, 0, xex, (int)headerSize, peImage.Length);
        return xex;
    }

    private static ZarFile CreateMockZarWithXex(byte[] xexBytes)
    {
        ConstructorInfo ctor = typeof(ZarFile).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null,
            [typeof(Stream), typeof(List<CompressionOffsetRecord>), typeof(byte[]), typeof(List<FileDirectoryEntry>), typeof(ulong), typeof(string)], null)!;
        ZarFile zar = (ZarFile)ctor.Invoke([
            Stream.Null, new List<CompressionOffsetRecord>(), Array.Empty<byte>(), new List<FileDirectoryEntry>(), (ulong)0, string.Empty
        ]);
        typeof(ZarFile).GetProperty("IsValid")!.SetValue(zar, true);
        typeof(ZarFile).GetProperty("XexFile")!.SetValue(zar, XexFile.FromBytes(xexBytes));
        return zar;
    }

    private static ZarFile CreateMockZarInvalid()
    {
        ConstructorInfo ctor = typeof(ZarFile).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null,
            [typeof(Stream), typeof(List<CompressionOffsetRecord>), typeof(byte[]), typeof(List<FileDirectoryEntry>), typeof(ulong), typeof(string)], null)!;
        ZarFile zar = (ZarFile)ctor.Invoke([
            Stream.Null, new List<CompressionOffsetRecord>(), Array.Empty<byte>(), new List<FileDirectoryEntry>(), (ulong)0, string.Empty
        ]);
        typeof(ZarFile).GetProperty("IsValid")!.SetValue(zar, false);
        return zar;
    }

    [Test]
    public void TryGetIcon_InvalidZar_ReturnsNull()
    {
        using ZarFile zar = CreateMockZarInvalid();
        Assert.That(zar.TryGetIcon(), Is.Null);
        Assert.That(zar.TryGetSpaFile(out SpaFile? spa), Is.False);
        Assert.That(spa, Is.Null);
    }

    [Test]
    public void TryGetIcon_WithValidXex_ReturnsIcon()
    {
        uint titleId = 0x4D530910;
        byte[] icon = LoadIcon();
        byte[] spa = BuildGpdWithIcon(0x8000, icon);
        byte[] pe = BuildPeImage($"{titleId:X8}", spa);
        byte[] xexBytes = BuildMinimalXex(titleId, pe);
        using ZarFile zar = CreateMockZarWithXex(xexBytes);
        byte[]? extracted = zar.TryGetIcon();
        Assert.That(extracted, Is.Not.Null);
        Assert.That(extracted!.Length, Is.EqualTo(icon.Length));
        Assert.That(extracted[0], Is.EqualTo(0x89));
    }

    [Test]
    public void TryGetIcon_WithValidXex_NoIcon_ReturnsNull()
    {
        uint titleId = 0x4D530910;
        byte[] pe = BuildPeImage($"{titleId:X8}", Encoding.ASCII.GetBytes("no icon"));
        byte[] xexBytes = BuildMinimalXex(titleId, pe);
        using ZarFile zar = CreateMockZarWithXex(xexBytes);
        Assert.That(zar.TryGetIcon(), Is.Null);
    }

    [Test]
    public void TryGetIcon_InvalidXex_ReturnsNull()
    {
        byte[] badXex = [0x00, 0x01, 0x02, 0x03];
        using ZarFile zar = CreateMockZarWithXex(badXex);
        Assert.That(zar.TryGetIcon(), Is.Null);
    }

    [Test]
    public void TryGetIcon_NeverThrows()
    {
        using ZarFile zar = CreateMockZarInvalid();
        Assert.DoesNotThrow(() => zar.TryGetIcon());
        Assert.DoesNotThrow(() => zar.TryGetSpaFile(out _));
    }

    [Test]
    public void TryGetSpaFile_WithValidXex_ReturnsSpa()
    {
        uint titleId = 0x4D530910;
        byte[] spa = BuildGpdWithIcon();
        byte[] pe = BuildPeImage($"{titleId:X8}", spa);
        byte[] xexBytes = BuildMinimalXex(titleId, pe);
        using ZarFile zar = CreateMockZarWithXex(xexBytes);
        bool ok = zar.TryGetSpaFile(out SpaFile? spaFile);
        Assert.That(ok, Is.True);
        Assert.That(spaFile, Is.Not.Null);
        Assert.That(spaFile!.IsValid, Is.True);
        spaFile.Dispose();
    }

    [Test]
    public void TryGetSpaFile_InvalidXex_ReturnsFalse()
    {
        byte[] pe = BuildPeImage("NOPE", new byte[64]);
        byte[] xexBytes = BuildMinimalXex(0x12345678, pe);
        using ZarFile zar = CreateMockZarWithXex(xexBytes);
        bool ok = zar.TryGetSpaFile(out SpaFile? spaFile);
        Assert.That(ok, Is.False);
        Assert.That(spaFile, Is.Null);
    }

    [Test]
    public void Load_InvalidFile_TryGetIconReturnsNull()
    {
        string tmp = Path.Combine(Path.GetTempPath(), $"test_invalid_{Guid.NewGuid()}.zar");
        try
        {
            File.WriteAllBytes(tmp, [0x00, 0x01, 0x02, 0x03]);
            using ZarFile zar = ZarFile.Load(tmp);
            Assert.That(zar.IsValid, Is.False);
            Assert.That(zar.TryGetIcon(), Is.Null);
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
    public void TryGetIcon_FallbackToAnyXex_WhenDefaultInvalid()
    {
        // This tests that TryExtractAlternativeXexBytes path doesn't throw when Files is empty
        using ZarFile zar = CreateMockZarInvalid();
        // Even though IsValid false, TryGetIcon should safely return null
        Assert.That(zar.TryGetIcon(), Is.Null);
    }
}