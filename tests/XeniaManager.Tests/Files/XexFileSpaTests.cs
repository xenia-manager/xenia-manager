using System.Buffers.Binary;
using System.Reflection;
using System.Text;
using XeniaManager.Files;
using XeniaManager.Files.Models.Gpd;

namespace XeniaManager.Tests.Files;

[TestFixture]
public class XexFileSpaTests
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
        // Ensure sectionName is 8 chars max, padded
        int peSize = rawAddr + sectionData.Length + 0x100;
        peSize = (peSize + 15) & ~15; // align 16
        byte[] pe = new byte[peSize];
        pe[0] = 0x4D;
        pe[1] = 0x5A;
        BinaryPrimitives.WriteInt32LittleEndian(pe.AsSpan(0x3C), 0x80);
        // NT header at 0x80
        pe[0x80] = 0x50;
        pe[0x81] = 0x45;
        pe[0x82] = 0x00;
        pe[0x83] = 0x00;
        // File header at 0x84
        BinaryPrimitives.WriteUInt16LittleEndian(pe.AsSpan(0x84), 0x014C); // machine
        BinaryPrimitives.WriteUInt16LittleEndian(pe.AsSpan(0x86), 1); // sections
        BinaryPrimitives.WriteUInt32LittleEndian(pe.AsSpan(0x88), 0); // timestamp
        BinaryPrimitives.WriteUInt32LittleEndian(pe.AsSpan(0x8C), 0);
        BinaryPrimitives.WriteUInt32LittleEndian(pe.AsSpan(0x90), 0);
        BinaryPrimitives.WriteUInt16LittleEndian(pe.AsSpan(0x94), 0xE0); // sizeOfOptionalHeader
        BinaryPrimitives.WriteUInt16LittleEndian(pe.AsSpan(0x96), 0x0002);
        // Optional header at 0x98, size 0xE0
        BinaryPrimitives.WriteUInt16LittleEndian(pe.AsSpan(0x98), 0x010B); // magic PE32
        // Section header at 0x98+0xE0 =0x178
        int sh = 0x98 + 0xE0;
        byte[] nameBytes = Encoding.ASCII.GetBytes(sectionName);
        Array.Copy(nameBytes, 0, pe, sh, Math.Min(nameBytes.Length, 8));
        BinaryPrimitives.WriteUInt32LittleEndian(pe.AsSpan(sh + 8), (uint)sectionData.Length); // VirtualSize
        BinaryPrimitives.WriteUInt32LittleEndian(pe.AsSpan(sh + 12), 0x1000); // VirtualAddr
        BinaryPrimitives.WriteUInt32LittleEndian(pe.AsSpan(sh + 16), (uint)sectionData.Length); // RawSize
        BinaryPrimitives.WriteUInt32LittleEndian(pe.AsSpan(sh + 20), (uint)rawAddr); // PointerToRawData
        // Copy section data
        Array.Copy(sectionData, 0, pe, rawAddr, sectionData.Length);
        return pe;
    }

    private static byte[] BuildMinimalXex(uint titleId, byte[] peImage, uint mediaId = 0x12345678)
    {
        const uint headerSize = 0x300;
        const uint securityOffset = 0x40;
        const int executionInfoOffset = 0x40 + 0x184; // after security
        int totalSize = (int)headerSize + peImage.Length + 0x100;
        totalSize = (totalSize + 15) & ~15;
        byte[] xex = new byte[totalSize];
        // Header 24 bytes BE
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan(0x00), 0x58455832u); // XEX2
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan(0x04), 0); // moduleFlags
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan(0x08), headerSize);
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan(0x0C), 0);
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan(0x10), securityOffset);
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan(0x14), 1); // headerCount =1
        // Directory at 0x18: key 0x00040006, value executionInfoOffset
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan(0x18), 0x00040006u);
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan(0x1C), (uint)executionInfoOffset);
        // Security info at securityOffset
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan((int)securityOffset + 0x00), 0x180); // Size
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan((int)securityOffset + 0x04), (uint)peImage.Length); // ImageSize
        // ImageKey at +0x150 (16 bytes zeros already)
        // Ensure zeros for signature etc – already zero
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan((int)securityOffset + 0x178), 0xFF); // GameRegion
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan((int)securityOffset + 0x17C), 0xFFFFFFFF); // AllowedMedia
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan((int)securityOffset + 0x180), 0); // PageDescCount
        // Execution info at executionInfoOffset
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan(executionInfoOffset + 0x00), mediaId);
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan(executionInfoOffset + 0x04), 1); // version
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan(executionInfoOffset + 0x08), 1); // baseVersion
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan(executionInfoOffset + 0x0C), titleId);
        xex[executionInfoOffset + 0x10] = 0; // platform
        xex[executionInfoOffset + 0x11] = 0;
        xex[executionInfoOffset + 0x12] = 1; // discNum
        xex[executionInfoOffset + 0x13] = 1;
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan(executionInfoOffset + 0x14), 0); // saveGameId
        // Ensure execution info within file before headerSize
        // PeImage at headerSize
        Array.Copy(peImage, 0, xex, (int)headerSize, peImage.Length);
        return xex;
    }

    private static byte[] BuildMinimalXexWithFileFormat(uint titleId, byte[] peImage, int compressionType, byte[]? blockTable = null, int windowBits = 17)
    {
        // Similar to BuildMinimalXex but adds fileFormat info entry 0x000003FF
        const uint headerSize = 0x400;
        const uint securityOffset = 0x40;
        const int executionInfoOffset = 0x40 + 0x184 + 0x20; // shift
        const uint fileFormatOffset = 0x200; // somewhere between security and headerSize, but must be > directory
        // We'll need headerCount=2, directory 16 bytes
        int totalSize = (int)headerSize + Math.Max(peImage.Length, 0x1000) + 0x100;
        totalSize = (totalSize + 15) & ~15;
        byte[] xex = new byte[totalSize];
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan(0x00), 0x58455832u);
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan(0x04), 0);
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan(0x08), headerSize);
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan(0x0C), 0);
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan(0x10), securityOffset);
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan(0x14), 2); // headerCount 2
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan(0x18), 0x00040006u);
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan(0x1C), (uint)executionInfoOffset);
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan(0x20), 0x000003FFu);
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan(0x24), fileFormatOffset);
        // Security
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan((int)securityOffset + 0x00), 0x180);
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan((int)securityOffset + 0x04), (uint)peImage.Length);
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan((int)securityOffset + 0x178), 0xFF);
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan((int)securityOffset + 0x17C), 0xFFFFFFFF);
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan((int)securityOffset + 0x180), 0);

        // Execution
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan(executionInfoOffset + 0x00), 0x12345678u);
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan(executionInfoOffset + 0x04), 1);
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan(executionInfoOffset + 0x08), 1);
        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan(executionInfoOffset + 0x0C), titleId);
        xex[executionInfoOffset + 0x12] = 1;
        xex[executionInfoOffset + 0x13] = 1;

        // File format info at fileFormatOffset
        // Structure: infoSize (4) , encryptionType (2), compressionType (2), then data
        // For Basic: data = block table (dataSize, zeroSize per 8)
        // For Normal: windowSize (4), firstBlockSize (4), hash (20)
        byte[]? ffData = null;
        int infoSize = 8;
        if (compressionType == 1 && blockTable != null)
        {
            infoSize = 8 + blockTable.Length;
            ffData = blockTable;
        }
        else if (compressionType == 2)
        {
            infoSize = 8 + 4 + 4 + 20; // 36
        }

        BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan((int)fileFormatOffset + 0x00), (uint)infoSize);
        BinaryPrimitives.WriteUInt16BigEndian(xex.AsSpan((int)fileFormatOffset + 0x04), 0); // encryptionType 0 (none)
        BinaryPrimitives.WriteUInt16BigEndian(xex.AsSpan((int)fileFormatOffset + 0x06), (ushort)compressionType);
        if (compressionType == 1 && ffData != null)
        {
            Array.Copy(ffData, 0, xex, (int)fileFormatOffset + 8, ffData.Length);
        }
        else if (compressionType == 2)
        {
            uint windowSize = (uint)(1 << windowBits);
            BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan((int)fileFormatOffset + 0x08), windowSize);
            BinaryPrimitives.WriteUInt32BigEndian(xex.AsSpan((int)fileFormatOffset + 0x0C), 0); // firstBlockSize 0 = already deblocked
            // hash zeros
        }

        Array.Copy(peImage, 0, xex, (int)headerSize, peImage.Length);
        return xex;
    }

    #region FromBytes / RawData

    [Test]
    public void FromBytes_PreservesRawData()
    {
        uint titleId = 0x4D530910u;
        byte[] spaBytes = BuildGpdWithIcon();
        byte[] pe = BuildPeImage($"{titleId:X8}", spaBytes);
        byte[] xexBytes = BuildMinimalXex(titleId, pe);
        XexFile xex = XexFile.FromBytes(xexBytes);
        Assert.That(xex.IsValid, Is.True);
        Assert.That(xex.RawData.Count, Is.EqualTo(xexBytes.Length));
        Assert.That(xex.RawData[0], Is.EqualTo(xexBytes[0]));
    }

    [Test]
    public void FromBytes_InvalidSecurityOffset_ReturnsInvalid()
    {
        byte[] data = new byte[0x100];
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(0x00), 0x58455832u);
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(0x08), 0x200);
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(0x10), 0x5000); // out of bounds
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(0x14), 0);
        XexFile xex = XexFile.FromBytes(data);
        Assert.That(xex.IsValid, Is.False);
        Assert.That(xex.ValidationError, Does.Contain("Invalid security info offset").Or.Contain("Failed to parse"));
    }

    [Test]
    public void TitleId_MediaId_FormattedCorrectly()
    {
        uint titleId = 0x45410914u;
        uint mediaId = 0xAABBCCDDu;
        byte[] spaBytes = BuildGpdWithIcon();
        byte[] pe = BuildPeImage($"{titleId:X8}", spaBytes);
        byte[] xexBytes = BuildMinimalXex(titleId, pe, mediaId);
        XexFile xex = XexFile.FromBytes(xexBytes);
        Assert.That(xex.IsValid, Is.True);
        Assert.That(xex.TitleId, Is.EqualTo($"{titleId:X8}"));
        Assert.That(xex.MediaId, Is.EqualTo($"{mediaId:X8}"));
        Assert.That(xex.Execution!.Value.TitleId, Is.EqualTo(titleId));
    }

    #endregion

    #region TryGetSpaFile

    [Test]
    public void TryGetSpaFile_InvalidXex_ReturnsFalse()
    {
        XexFile xex = XexFile.FromBytes([0x00, 0x01, 0x02]);
        bool ok = xex.TryGetSpaFile(out SpaFile? spa);
        Assert.That(ok, Is.False);
        Assert.That(spa, Is.Null);
    }

    [Test]
    public void TryGetSpaFile_TooShortRawData_ReturnsFalse()
    {
        // Valid header but RawData <0x18 after FromBytes? FromBytes stores rawData as provided even if invalid
        // But TryGetSpaFile checks IsValid and length <0x18
        byte[] data = new byte[20];
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(0), 0x58455832u);
        XexFile xex = XexFile.FromBytes(data);
        // Ensure IsValid false
        Assert.That(xex.IsValid, Is.False);
        bool ok = xex.TryGetSpaFile(out SpaFile? spa);
        Assert.That(ok, Is.False);
    }

    [Test]
    public void TryGetSpaFile_NoPeSectionAndNoXdbf_ReturnsFalse()
    {
        uint titleId = 0x4D530910u;
        // Build PE with section name NOT matching titleId and no XDBF inside
        byte[] pe = BuildPeImage("WRONGSEC", Encoding.ASCII.GetBytes("no data"));
        byte[] xexBytes = BuildMinimalXex(titleId, pe);
        XexFile xex = XexFile.FromBytes(xexBytes);
        Assert.That(xex.IsValid, Is.True);
        bool ok = xex.TryGetSpaFile(out SpaFile? spa);
        Assert.That(ok, Is.False);
        Assert.That(spa, Is.Null);
    }

    [Test]
    public void TryGetSpaFile_SectionFound_ReturnsValidSpa()
    {
        uint titleId = 0x4D530910u;
        byte[] spaBytes = BuildGpdWithIcon(0x8000, LoadIcon());
        byte[] pe = BuildPeImage($"{titleId:X8}", spaBytes);
        byte[] xexBytes = BuildMinimalXex(titleId, pe);
        XexFile xex = XexFile.FromBytes(xexBytes);
        Assert.That(xex.IsValid, Is.True);
        bool ok = xex.TryGetSpaFile(out SpaFile? spa);
        Assert.That(ok, Is.True);
        Assert.That(spa, Is.Not.Null);
        Assert.That(spa!.IsValid, Is.True);
        Assert.That(spa.GetTitleIcon(), Is.Not.Null);
        spa.Dispose();
    }

    [Test]
    public void TryGetSpaFile_SectionFound_CaseInsensitive()
    {
        uint titleId = 0xFFAA0011u;
        byte[] spaBytes = BuildGpdWithIcon();
        // section name lower case
        byte[] pe = BuildPeImage($"{titleId:x8}", spaBytes);
        byte[] xexBytes = BuildMinimalXex(titleId, pe);
        XexFile xex = XexFile.FromBytes(xexBytes);
        bool ok = xex.TryGetSpaFile(out SpaFile? spa);
        Assert.That(ok, Is.True);
        spa!.Dispose();
    }

    [Test]
    public void TryGetSpaFile_FallbackScanForXdbf_FindsEmbeddedXdbf()
    {
        uint titleId = 0x12345678u;
        byte[] spaBytes = BuildGpdWithIcon();
        // PE has wrong section but contains XDBF magic somewhere
        byte[] pe = BuildPeImage("WRONG!!", Encoding.ASCII.GetBytes("padding"));
        // Embed spaBytes at offset 0x300 (after PE headers but within file)
        // Ensure pe large enough
        byte[] peLarge = new byte[pe.Length + spaBytes.Length + 0x100];
        Array.Copy(pe, peLarge, pe.Length);
        Array.Copy(spaBytes, 0, peLarge, pe.Length, spaBytes.Length);
        byte[] xexBytes = BuildMinimalXex(titleId, peLarge);
        XexFile xex = XexFile.FromBytes(xexBytes);
        bool ok = xex.TryGetSpaFile(out SpaFile? spa);
        Assert.That(ok, Is.True);
        Assert.That(spa!.IsValid, Is.True);
        spa.Dispose();
    }

    [Test]
    public void TryGetSpaFile_InvalidSpaBytes_ReturnsFalse()
    {
        uint titleId = 0x4D530910u;
        // SPA bytes that are not valid XDBF (bad magic)
        byte[] badSpa = Encoding.ASCII.GetBytes("NOTXDBF");
        byte[] pe = BuildPeImage($"{titleId:X8}", badSpa);
        byte[] xexBytes = BuildMinimalXex(titleId, pe);
        XexFile xex = XexFile.FromBytes(xexBytes);
        bool ok = xex.TryGetSpaFile(out SpaFile? spa);
        Assert.That(ok, Is.False);
        Assert.That(spa, Is.Null);
    }

    #endregion

    #region TryGetIcon

    [Test]
    public void TryGetIcon_InvalidXex_ReturnsNull()
    {
        XexFile xex = XexFile.FromBytes([0x00, 0x01, 0x02, 0x03]);
        Assert.That(xex.TryGetIcon(), Is.Null);
    }

    [Test]
    public void TryGetIcon_WithTitleIcon_ReturnsPng()
    {
        uint titleId = 0x4D530910u;
        byte[] icon = LoadIcon();
        byte[] spaBytes = BuildGpdWithIcon(0x8000, icon);
        byte[] pe = BuildPeImage($"{titleId:X8}", spaBytes);
        byte[] xexBytes = BuildMinimalXex(titleId, pe);
        XexFile xex = XexFile.FromBytes(xexBytes);
        byte[]? extracted = xex.TryGetIcon();
        Assert.That(extracted, Is.Not.Null);
        Assert.That(extracted!.Length, Is.EqualTo(icon.Length));
        Assert.That(extracted[0], Is.EqualTo(0x89)); // PNG signature
    }

    [Test]
    public void TryGetIcon_FallbackToGetAnyValidIcon()
    {
        uint titleId = 0x4D530910u;
        byte[] icon = LoadIcon();
        // SPA without 0x8000 but with 0x8001 large PNG
        using GpdFile gpd = GpdFile.Create();
        gpd.AddImage(0x8001, icon);
        byte[] spaBytes = gpd.ToBytes();
        byte[] pe = BuildPeImage($"{titleId:X8}", spaBytes);
        byte[] xexBytes = BuildMinimalXex(titleId, pe);
        XexFile xex = XexFile.FromBytes(xexBytes);
        byte[]? extracted = xex.TryGetIcon();
        Assert.That(extracted, Is.Not.Null);
        Assert.That(extracted!.Length, Is.EqualTo(icon.Length));
    }

    [Test]
    public void TryGetIcon_FallbackScanForPngWhenSpaInvalid()
    {
        uint titleId = 0x4D530910u;
        byte[] icon = LoadIcon(); // 9961 >1024 so ScanForPng will find it
        // Create section that is NOT valid XDBF but contains PNG bytes directly
        // So TryGetSpaBytes will find section, but SpaFile.FromBytes will be invalid (since not XDBF), then TryGetIcon falls back to ScanForPng
        // We need section data that contains PNG but invalid XDBF header
        byte[] notXdbfButPng = new byte[icon.Length + 16];
        Array.Copy(icon, 0, notXdbfButPng, 8, icon.Length);
        // Ensure not starting with XDBF magic, so SpaFile invalid
        byte[] pe = BuildPeImage($"{titleId:X8}", notXdbfButPng);
        byte[] xexBytes = BuildMinimalXex(titleId, pe);
        XexFile xex = XexFile.FromBytes(xexBytes);
        byte[]? extracted = xex.TryGetIcon();
        // Should fallback to ScanForPng and return icon (since SPA invalid, ScanForPng scans spaBytes which is notXdbfButPng)
        // The png inside notXdbfButPng starts at offset 8
        Assert.That(extracted, Is.Not.Null);
        Assert.That(extracted!.Length, Is.EqualTo(icon.Length));
    }

    [Test]
    public void TryGetIcon_NoSpaSection_ReturnsNull()
    {
        uint titleId = 0x12345678u;
        byte[] pe = BuildPeImage("NOPE", new byte[64]);
        byte[] xexBytes = BuildMinimalXex(titleId, pe);
        XexFile xex = XexFile.FromBytes(xexBytes);
        Assert.That(xex.TryGetIcon(), Is.Null);
    }

    #endregion

    #region Private helpers via reflection (coverage of XexFile internals)

    private static byte[]? InvokeTryFindPeSection(byte[] peImage, string name)
    {
        MethodInfo? m = typeof(XexFile).GetMethod("TryFindPeSection", BindingFlags.Static | BindingFlags.NonPublic);
        return (byte[]?)m!.Invoke(null, new object[]
        {
            peImage, name
        });
    }

    private static byte[]? InvokeScanForXdbf(byte[] data)
    {
        MethodInfo? m = typeof(XexFile).GetMethod("ScanForXdbf", BindingFlags.Static | BindingFlags.NonPublic);
        return (byte[]?)m!.Invoke(null, new object[]
        {
            data
        });
    }

    private static byte[]? InvokeScanForPng(byte[] data)
    {
        MethodInfo? m = typeof(XexFile).GetMethod("ScanForPng", BindingFlags.Static | BindingFlags.NonPublic);
        return (byte[]?)m!.Invoke(null, new object[]
        {
            data
        });
    }

    private static int InvokeFindPngEnd(byte[] data, int start)
    {
        MethodInfo? m = typeof(XexFile).GetMethod("FindPngEnd", BindingFlags.Static | BindingFlags.NonPublic);
        return (int)m!.Invoke(null, new object[]
        {
            data, start
        })!;
    }

    private static byte[] InvokeDeblockXexLzx(byte[] data, int firstBlockSize)
    {
        MethodInfo? m = typeof(XexFile).GetMethod("DeblockXexLzx", BindingFlags.Static | BindingFlags.NonPublic);
        return (byte[])m!.Invoke(null, new object[]
        {
            data, firstBlockSize
        })!;
    }

    private static byte[]? InvokeTryDecompress(byte[] data, int compressionType, byte[]? basicBlocks, int windowBits, byte[] xexData, uint secOff)
    {
        MethodInfo? m = typeof(XexFile).GetMethod("TryDecompress", BindingFlags.Static | BindingFlags.NonPublic);
        return (byte[]?)m!.Invoke(null, new object?[]
        {
            data, compressionType, basicBlocks, windowBits, xexData, secOff, 0, null
        })!;
    }

    [Test]
    public void TryFindPeSection_Valid_ReturnsSectionData()
    {
        byte[] secData = Encoding.ASCII.GetBytes("hello spa");
        byte[] pe = BuildPeImage("4D530910", secData);
        byte[]? found = InvokeTryFindPeSection(pe, "4D530910");
        Assert.That(found, Is.Not.Null);
        Assert.That(found, Is.EqualTo(secData));
    }

    [Test]
    public void TryFindPeSection_InvalidMz_ReturnsNull()
    {
        byte[] bad = new byte[100];
        Assert.That(InvokeTryFindPeSection(bad, "TEST"), Is.Null);
    }

    [Test]
    public void TryFindPeSection_BadELfanew_ReturnsNull()
    {
        byte[] pe = BuildPeImage("TEST", new byte[10]);
        // corrupt e_lfanew
        BinaryPrimitives.WriteInt32LittleEndian(pe.AsSpan(0x3C), 0x5000);
        Assert.That(InvokeTryFindPeSection(pe, "TEST"), Is.Null);
    }

    [Test]
    public void TryFindPeSection_WrongSignature_ReturnsNull()
    {
        byte[] pe = BuildPeImage("TEST", new byte[10]);
        pe[0x80] = 0x00; // corrupt PE\0\0
        Assert.That(InvokeTryFindPeSection(pe, "TEST"), Is.Null);
    }

    [Test]
    public void TryFindPeSection_MissingSection_ReturnsNull()
    {
        byte[] pe = BuildPeImage("AAAA", new byte[10]);
        Assert.That(InvokeTryFindPeSection(pe, "BBBB"), Is.Null);
    }

    [Test]
    public void TryFindPeSection_VirtualAddressFallback()
    {
        // Build PE where PointerToRawData is out of bounds but VirtualAddress is valid
        byte[] secData = Encoding.ASCII.GetBytes("data");
        byte[] pe = BuildPeImage("TEST", secData, 0x1000); // rawAddr beyond pe length, but VirtualAddr 0x1000? Wait rawAddr beyond length will trigger fallback
        // Our BuildPeImage sets VirtualAddress 0x1000 and rawAddr 0x200; to test fallback we need rawAddr invalid but virtualAddress valid
        // Manually patch: set rawAddr to 0x5000 (out of bounds), virtualAddr to 0x200 (where data actually is)
        int sh = 0x98 + 0xE0;
        BinaryPrimitives.WriteUInt32LittleEndian(pe.AsSpan(sh + 12), 0x200); // VirtualAddress = 0x200 (where data is)
        BinaryPrimitives.WriteUInt32LittleEndian(pe.AsSpan(sh + 20), 0x5000); // PointerToRawData out of bounds
        // Data at 0x200 is still secData? Our data is at 0x1000? Wait we built with rawAddr 0x1000, so data at 0x1000. Now virtual 0x200 is empty.
        // Let's rebuild correctly: create pe with rawAddr 0x200 where data lives, then patch rawAddr to invalid and virtual to 0x200
        byte[] pe2 = BuildPeImage("TEST2", secData, 0x200);
        // now patch: rawAddr out of bounds, virtualAddress =0x200 (same as data location)
        int sh2 = 0x98 + 0xE0;
        BinaryPrimitives.WriteUInt32LittleEndian(pe2.AsSpan(sh2 + 12), 0x200);
        BinaryPrimitives.WriteUInt32LittleEndian(pe2.AsSpan(sh2 + 20), 0x5000);
        byte[]? found = InvokeTryFindPeSection(pe2, "TEST2");
        Assert.That(found, Is.Not.Null);
        Assert.That(found, Is.EqualTo(secData));
    }

    [Test]
    public void ScanForXdbf_FindsEmbeddedGpd()
    {
        byte[] spaBytes = BuildGpdWithIcon();
        byte[] pe = new byte[spaBytes.Length + 100];
        Array.Copy(spaBytes, 0, pe, 50, spaBytes.Length);
        byte[]? found = InvokeScanForXdbf(pe);
        Assert.That(found, Is.Not.Null);
        // Should be candidate from offset 50 to end
        Assert.That(found!.Length, Is.EqualTo(pe.Length - 50));
        // Verify it starts with XDBF magic
        Assert.That(found[0], Is.EqualTo(0x58));
        Assert.That(found[1], Is.EqualTo(0x44));
    }

    [Test]
    public void ScanForXdbf_NoMatch_ReturnsNull()
    {
        byte[] pe = new byte[100];
        Assert.That(InvokeScanForXdbf(pe), Is.Null);
    }

    [Test]
    public void ScanForPng_FindsLargePng()
    {
        byte[] icon = LoadIcon(); // >1024
        byte[] data = new byte[icon.Length + 20];
        Array.Copy(icon, 0, data, 10, icon.Length);
        byte[]? found = InvokeScanForPng(data);
        Assert.That(found, Is.Not.Null);
        Assert.That(found!.Length, Is.EqualTo(icon.Length));
    }

    [Test]
    public void ScanForPng_SmallPng_Ignored()
    {
        byte[] small = MinimalPng(); // 67 <1024
        byte[] data = new byte[small.Length + 20];
        Array.Copy(small, 0, data, 5, small.Length);
        Assert.That(InvokeScanForPng(data), Is.Null);
    }

    [Test]
    public void FindPngEnd_Valid_ReturnsEndOffset()
    {
        byte[] icon = LoadIcon();
        byte[] data = new byte[icon.Length + 10];
        Array.Copy(icon, 0, data, 5, icon.Length);
        int end = InvokeFindPngEnd(data, 5);
        Assert.That(end, Is.EqualTo(5 + icon.Length));
    }

    [Test]
    public void FindPngEnd_InvalidSignature_ReturnsMinusOne()
    {
        byte[] data = new byte[100];
        data[10] = 0x00;
        Assert.That(InvokeFindPngEnd(data, 10), Is.EqualTo(-1));
        Assert.That(InvokeFindPngEnd(data, 95), Is.EqualTo(-1)); // not enough for 8
    }

    [Test]
    public void FindPngEnd_Truncated_ReturnsMinusOne()
    {
        byte[] icon = LoadIcon();
        // Truncate after first chunk header
        byte[] truncated = icon[..20];
        int end = InvokeFindPngEnd(truncated, 0);
        Assert.That(end, Is.EqualTo(-1));
    }

    [Test]
    public void DeblockXexLzx_NoBlocking_ReturnsSame()
    {
        byte[] data = Encoding.ASCII.GetBytes("no block");
        byte[] outData = InvokeDeblockXexLzx(data, 0);
        Assert.That(outData, Is.SameAs(data));
        byte[] out2 = InvokeDeblockXexLzx(data, 9999); // > length
        Assert.That(out2, Is.SameAs(data));
    }

    [Test]
    public void DeblockXexLzx_SingleBlock_DeblocksCorrectly()
    {
        // Build a de-blocked stream: firstBlockSize = block length (24+2+chunk+2)
        // Block: [4 bytes nextSize BE 0][20 bytes hash zeros][2 bytes chunkLen BE][chunk bytes][2 bytes 0 terminator]
        byte[] chunk = Encoding.ASCII.GetBytes("HELLO_LZX");
        int chunkLen = chunk.Length;
        int blockSize = 24 + 2 + chunkLen + 2;
        byte[] data = new byte[blockSize];
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(0), 0); // nextSize 0 (last block)
        // 20 bytes hash already zero
        BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(24), (ushort)chunkLen);
        Array.Copy(chunk, 0, data, 26, chunkLen);
        BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(26 + chunkLen), 0); // terminator
        byte[] result = InvokeDeblockXexLzx(data, blockSize);
        Assert.That(result, Is.EqualTo(chunk));
    }

    [Test]
    public void DeblockXexLzx_TwoBlocks_Concatenates()
    {
        byte[] c1 = Encoding.ASCII.GetBytes("AAA");
        byte[] c2 = Encoding.ASCII.GetBytes("BBB");
        int b1Size = 24 + 2 + c1.Length + 2;
        int b2Size = 24 + 2 + c2.Length + 2;
        byte[] data = new byte[b1Size + b2Size];
        // block1 header: nextSize = b2Size
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(0), (uint)b2Size);
        BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(24), (ushort)c1.Length);
        Array.Copy(c1, 0, data, 26, c1.Length);
        BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(26 + c1.Length), 0);
        // block2 at offset b1Size
        int off2 = b1Size;
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(off2), 0);
        BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(off2 + 24), (ushort)c2.Length);
        Array.Copy(c2, 0, data, off2 + 26, c2.Length);
        BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(off2 + 26 + c2.Length), 0);
        byte[] result = InvokeDeblockXexLzx(data, b1Size);
        byte[] expected = Encoding.ASCII.GetBytes("AAABBB");
        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void TryDecompress_Basic_ZeroFill()
    {
        // Build Basic blocks: one block dataSize=3, zeroSize=2 => output 5 bytes: "ABC" + 2 zeros
        byte[] data = Encoding.ASCII.GetBytes("ABC");
        byte[] blockTable = new byte[8];
        BinaryPrimitives.WriteUInt32BigEndian(blockTable.AsSpan(0), 3);
        BinaryPrimitives.WriteUInt32BigEndian(blockTable.AsSpan(4), 2);
        byte[] xexData = new byte[0x100];
        // ImageSize at securityOffset+4 must be set, but TryDecompress reads xexData at security offset
        uint secOff = 0x40;
        BinaryPrimitives.WriteUInt32BigEndian(xexData.AsSpan((int)secOff + 4), 5);
        byte[]? result = InvokeTryDecompress(data, 1, blockTable, 17, xexData, secOff);
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Length, Is.EqualTo(5));
        Assert.That(result[0], Is.EqualTo((byte)'A'));
        Assert.That(result[3], Is.EqualTo(0));
        Assert.That(result[4], Is.EqualTo(0));
    }

    [Test]
    public void TryDecompress_NoCompression_ReturnsSame()
    {
        byte[] data = Encoding.ASCII.GetBytes("RAWPE");
        byte[]? result = InvokeTryDecompress(data, 0, null, 17, new byte[0x100], 0x40);
        Assert.That(result, Is.SameAs(data));
    }

    [Test]
    public void TryDecompress_Basic_NoBlocks_ReturnsSame()
    {
        byte[] data = Encoding.ASCII.GetBytes("DATA");
        byte[]? result = InvokeTryDecompress(data, 1, null, 17, new byte[0x100], 0x40);
        Assert.That(result, Is.SameAs(data));
        byte[]? result2 = InvokeTryDecompress(data, 1, Array.Empty<byte>(), 17, new byte[0x100], 0x40);
        Assert.That(result2, Is.SameAs(data));
    }

    #endregion
}