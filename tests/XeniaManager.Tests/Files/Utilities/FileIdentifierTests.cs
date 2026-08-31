using XeniaManager.Files;
using XeniaManager.Files.Models;
using XeniaManager.Files.Utilities;

namespace XeniaManager.Tests.Files.Utilities;

public class FileIdentifierTests
{
    private string _tempDir = null!;

    [SetUp]
    public void Setup()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"FileIdentifierTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    [TearDown]
    public void TearDown()
    {
        try
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, true);
            }
        }
        catch { }
    }

    private string CreateTempFile(string extension, byte[] header)
    {
        string path = Path.Combine(_tempDir, $"{Guid.NewGuid():N}{extension}");
        File.WriteAllBytes(path, header);
        return path;
    }

    private string CreateTempFileWithHeader(string extension, uint magic)
    {
        byte[] header = new byte[4];
        header[0] = (byte)((magic >> 24) & 0xFF);
        header[1] = (byte)((magic >> 16) & 0xFF);
        header[2] = (byte)((magic >> 8) & 0xFF);
        header[3] = (byte)(magic & 0xFF);
        return CreateTempFile(extension, header);
    }

    [Test]
    public void IdentifyFileType_NonexistentFile_ThrowsFileNotFoundException()
    {
        string path = Path.Combine(_tempDir, "nonexistent.bin");
        Assert.Throws<FileNotFoundException>(() => FileIdentifier.IdentifyFileType(path));
    }

    [Test]
    public void IdentifyFileType_IsoExtension_ReturnsIso()
    {
        string path = CreateTempFile(".iso", [0x00, 0x01, 0x02, 0x03]);
        FileSignature sig = FileIdentifier.IdentifyFileType(path);
        Assert.That(sig, Is.EqualTo(FileSignature.ISO));
    }

    [Test]
    public void IdentifyFileType_XisoExtension_ReturnsIso()
    {
        string path = CreateTempFile(".xiso", [0x00, 0x01, 0x02, 0x03]);
        Assert.That(FileIdentifier.IdentifyFileType(path), Is.EqualTo(FileSignature.ISO));
    }

    [Test]
    public void IdentifyFileType_XexExtension_ReturnsXex2()
    {
        string path = CreateTempFile(".xex", [0x00, 0x01, 0x02, 0x03]);
        Assert.That(FileIdentifier.IdentifyFileType(path), Is.EqualTo(FileSignature.XEX2));
    }

    [Test]
    public void IdentifyFileType_ZarExtension_ReturnsZar()
    {
        string path = CreateTempFile(".zar", [0x00, 0x01, 0x02, 0x03]);
        Assert.That(FileIdentifier.IdentifyFileType(path), Is.EqualTo(FileSignature.ZAR));
    }

    [Test]
    public void IdentifyFileType_IsoExtensionCaseInsensitive_ReturnsIso()
    {
        string path = CreateTempFile(".ISO", [0x00]);
        Assert.That(FileIdentifier.IdentifyFileType(path), Is.EqualTo(FileSignature.ISO));
    }

    [Test]
    public void IdentifyFileType_Xex1Magic_ReturnsXex1()
    {
        string path = CreateTempFileWithHeader(".bin", 0x58455831);
        Assert.That(FileIdentifier.IdentifyFileType(path), Is.EqualTo(FileSignature.XEX1));
    }

    [Test]
    public void IdentifyFileType_Xex2Magic_ReturnsXex2()
    {
        string path = CreateTempFileWithHeader(".bin", 0x58455832);
        Assert.That(FileIdentifier.IdentifyFileType(path), Is.EqualTo(FileSignature.XEX2));
    }

    [Test]
    public void IdentifyFileType_ConMagic_ReturnsCon()
    {
        string path = CreateTempFileWithHeader(".bin", 0x434F4E20);
        Assert.That(FileIdentifier.IdentifyFileType(path), Is.EqualTo(FileSignature.CON));
    }

    [Test]
    public void IdentifyFileType_LiveMagic_ReturnsLive()
    {
        string path = CreateTempFileWithHeader(".bin", 0x4C495645);
        Assert.That(FileIdentifier.IdentifyFileType(path), Is.EqualTo(FileSignature.LIVE));
    }

    [Test]
    public void IdentifyFileType_PirsMagic_ReturnsPirs()
    {
        string path = CreateTempFileWithHeader(".bin", 0x50495253);
        Assert.That(FileIdentifier.IdentifyFileType(path), Is.EqualTo(FileSignature.PIRS));
    }

    [Test]
    public void IdentifyFileType_UnknownHeader_ReturnsUnknown()
    {
        string path = CreateTempFile(".bin", [0x00, 0x00, 0x00, 0x00]);
        FileSignature sig = FileIdentifier.IdentifyFileType(path);
        // Unknown header with invalid ISO -> Unknown
        Assert.That(sig, Is.EqualTo(FileSignature.Unknown));
    }

    [Test]
    public void IdentifyFileType_EmptyFile_ReturnsUnknown()
    {
        string path = CreateTempFile(".bin", []);
        FileSignature sig = FileIdentifier.IdentifyFileType(path);
        Assert.That(sig, Is.EqualTo(FileSignature.Unknown));
    }

    [Test]
    public void IdentifyFileType_ShortFile_LessThan4Bytes_ReturnsUnknownOrDoesNotThrow()
    {
        string path = CreateTempFile(".bin", [0x58, 0x45]);
        Assert.DoesNotThrow(() => FileIdentifier.IdentifyFileType(path));
    }
}