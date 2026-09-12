using System.Text;
using XeniaManager.Files;
using XeniaManager.Files.Models.Iso;

namespace XeniaManager.Tests.Files;

/// <summary>
/// Tests for the IsoFile GDFX browsing API (Lookup/ListDirectory/ReadFile/ExtractAll)
/// against a minimal synthetic XDKI image. No real disc required.
/// </summary>
[TestFixture]
public class IsoFileBrowsingTests
{
    private const uint SectorSize = 0x800;
    private const uint HeaderSector = 0x20;
    private const uint RootSector = 40;
    private const uint HelloSector = 41;
    private const uint SubDirSector = 42;
    private const uint InnerSector = 43;

    private string _isoPath = string.Empty;

    [SetUp]
    public void Setup()
    {
        _isoPath = Path.Combine(Path.GetTempPath(), $"iso_browse_{Guid.NewGuid():N}.iso");
        File.WriteAllBytes(_isoPath, BuildTestImage());
    }

    [TearDown]
    public void Teardown()
    {
        if (File.Exists(_isoPath))
        {
            File.Delete(_isoPath);
        }
    }

    private static void WriteDirent(Stream stream, ushort left, ushort right, uint sector, uint size, byte attr, string name)
    {
        byte[] nameBytes = Encoding.ASCII.GetBytes(name);
        BinaryWriter writer = new BinaryWriter(stream, Encoding.ASCII, true);
        writer.Write(left);
        writer.Write(right);
        writer.Write(sector);
        writer.Write(size);
        writer.Write(attr);
        writer.Write((byte)nameBytes.Length);
        writer.Write(nameBytes);
    }

    private static void WriteHeader(byte[] image, uint rootSector, uint rootSize)
    {
        using MemoryStream header = new MemoryStream(image, (int)(HeaderSector * SectorSize), (int)SectorSize, true);
        {
            BinaryWriter writer = new BinaryWriter(header, Encoding.ASCII, true);
            writer.Write(Encoding.ASCII.GetBytes("MICROSOFT*XBOX*MEDIA"));
            writer.Write(rootSector);
            writer.Write(rootSize);
            writer.Write(0L);
        }
    }

    private static byte[] BuildTestImage()
    {
        // Tree:
        //   hello.txt (11 bytes @ HelloSector)
        //   sub/ (dir @ SubDirSector)
        //     sub/inner.bin (4 bytes @ InnerSector)
        using MemoryStream subDir = new MemoryStream();
        WriteDirent(subDir, 0, 0, InnerSector, 4, 0x00, "inner.bin");
        byte[] subDirBytes = subDir.ToArray();

        using MemoryStream rootDir = new MemoryStream();
        // entry0 occupies 14 + 9 = 23 bytes, padded to 24, so entry1 starts at ordinal 6.
        WriteDirent(rootDir, 0, 6, HelloSector, 11, 0x00, "hello.txt");
        // Pad to a 4-byte boundary so the next ordinal is exact.
        while (rootDir.Length % 4 != 0)
        {
            rootDir.WriteByte(0);
        }

        WriteDirent(rootDir, 0, 0, SubDirSector, (uint)subDirBytes.Length, 0x10, "sub");
        byte[] rootDirBytes = rootDir.ToArray();

        byte[] image = new byte[48 * SectorSize];
        WriteHeader(image, RootSector, (uint)rootDirBytes.Length);
        Array.Copy(rootDirBytes, 0, image, RootSector * SectorSize, rootDirBytes.Length);
        Array.Copy(Encoding.ASCII.GetBytes("hello world"), 0, image, HelloSector * SectorSize, 11);
        Array.Copy(subDirBytes, 0, image, SubDirSector * SectorSize, subDirBytes.Length);
        image[InnerSector * SectorSize] = 0x01;
        image[InnerSector * SectorSize + 1] = 0x02;
        image[InnerSector * SectorSize + 2] = 0x03;
        image[InnerSector * SectorSize + 3] = 0x04;
        return image;
    }

    [Test]
    public void Files_ListsAllFilesWithPaths()
    {
        using IsoFile iso = IsoFile.Load(_isoPath);

        Assert.That(iso.XgdInformation, Is.Not.Null);
        List<GdfxEntry> files = iso.Files.ToList();
        Assert.That(files.Count, Is.EqualTo(2));
        Assert.That(files.Select(f => f.FullPath), Is.EquivalentTo(new[]
        {
            "hello.txt", "sub/inner.bin"
        }));
        Assert.That(files.All(f => f.IsFile), Is.True);
        Assert.That(files.Single(f => f.FullPath == "hello.txt").Size, Is.EqualTo(11UL));
        Assert.That(files.Single(f => f.FullPath == "sub/inner.bin").Size, Is.EqualTo(4UL));
    }

    [Test]
    public void Entries_IncludesDirectoriesWithZeroSize()
    {
        using IsoFile iso = IsoFile.Load(_isoPath);

        List<GdfxEntry> entries = iso.Entries.ToList();
        Assert.That(entries.Count, Is.EqualTo(3));
        GdfxEntry sub = entries.Single(e => e.FullPath == "sub");
        Assert.That(sub.IsFile, Is.False);
        Assert.That(sub.Size, Is.EqualTo(0UL));
    }

    [Test]
    public void Lookup_ResolvesPathsCaseInsensitively()
    {
        using IsoFile iso = IsoFile.Load(_isoPath);

        Assert.That(iso.Lookup("sub\\inner.bin")?.Size, Is.EqualTo(4UL));
        Assert.That(iso.Lookup("SUB/INNER.BIN")?.Size, Is.EqualTo(4UL));
        Assert.That(iso.Lookup("sub")?.IsFile, Is.False);
        Assert.That(iso.Lookup("missing.bin"), Is.Null);
        Assert.That(iso.Lookup("hello.txt/nope"), Is.Null);
    }

    [Test]
    public void ListDirectory_ReturnsImmediateChildren()
    {
        using IsoFile iso = IsoFile.Load(_isoPath);

        List<GdfxEntry>? root = iso.ListDirectory(string.Empty);
        Assert.That(root, Is.Not.Null);
        Assert.That(root!.Select(e => e.Name), Is.EquivalentTo(new[]
        {
            "hello.txt", "sub"
        }));

        List<GdfxEntry>? sub = iso.ListDirectory("sub");
        Assert.That(sub, Is.Not.Null);
        Assert.That(sub!.Select(e => e.Name), Is.EquivalentTo(new[]
        {
            "inner.bin"
        }));

        Assert.That(iso.ListDirectory("hello.txt"), Is.Null);
        Assert.That(iso.ListDirectory("missing"), Is.Null);
    }

    [Test]
    public void ReadFile_ReturnsContentsAndSupportsRanges()
    {
        using IsoFile iso = IsoFile.Load(_isoPath);

        Assert.That(Encoding.ASCII.GetString(iso.ReadFile("hello.txt")!), Is.EqualTo("hello world"));
        Assert.That(iso.ReadFile("sub/inner.bin"), Is.EqualTo(new byte[]
        {
            0x01, 0x02, 0x03, 0x04
        }));
        Assert.That(iso.ReadFile("missing.bin"), Is.Null);
        Assert.That(iso.ReadFile("sub"), Is.Null);

        GdfxEntry hello = iso.Lookup("hello.txt")!;
        Assert.That(Encoding.ASCII.GetString(iso.ReadFile(hello, 6, 5)), Is.EqualTo("world"));
        Assert.That(iso.ReadFile(hello, (ulong)hello.Size, 10), Is.Empty);
        Assert.That(iso.ReadFile(hello, 0, 1000), Has.Length.EqualTo(11));
    }

    [Test]
    public void ExtractAll_DotDotEntry_DoesNotWriteOutsideOutputDirectory()
    {
        // Root holds a traversal name plus a benign file; only the benign one may land on disk.
        using MemoryStream rootDir = new MemoryStream();
        // entry0 is 14 + 16 = 30 bytes, padded to 32, so entry1 starts at ordinal 8.
        WriteDirent(rootDir, 0, 8, HelloSector, 4, 0x00, "..\\iso_pwned.txt");
        while (rootDir.Length % 4 != 0)
        {
            rootDir.WriteByte(0);
        }

        WriteDirent(rootDir, 0, 0, InnerSector, 4, 0x00, "ok.txt");
        byte[] rootDirBytes = rootDir.ToArray();

        byte[] image = new byte[48 * SectorSize];
        WriteHeader(image, RootSector, (uint)rootDirBytes.Length);
        Array.Copy(rootDirBytes, 0, image, RootSector * SectorSize, rootDirBytes.Length);
        image[HelloSector * SectorSize] = 0xAA;
        image[InnerSector * SectorSize] = 0xBB;

        string isoPath = Path.Combine(Path.GetTempPath(), $"iso_traversal_{Guid.NewGuid():N}.iso");
        string outputDir = Path.Combine(Path.GetTempPath(), $"iso_traversal_out_{Guid.NewGuid():N}");
        string escapedPath = Path.Combine(Directory.GetParent(outputDir)!.FullName, "iso_pwned.txt");
        try
        {
            File.WriteAllBytes(isoPath, image);
            using IsoFile iso = IsoFile.Load(isoPath);
            iso.ExtractAll(outputDir);

            Assert.That(File.Exists(escapedPath), Is.False, "Traversal entry escaped the output directory");
            Assert.That(File.ReadAllBytes(Path.Combine(outputDir, "ok.txt")), Is.EqualTo(new byte[]
            {
                0xBB, 0x00, 0x00, 0x00
            }));
        }
        finally
        {
            if (File.Exists(isoPath))
            {
                File.Delete(isoPath);
            }

            if (Directory.Exists(outputDir))
            {
                Directory.Delete(outputDir, true);
            }

            if (File.Exists(escapedPath))
            {
                File.Delete(escapedPath);
            }
        }
    }

    [Test]
    public void ExtractAll_PreservesStructure()
    {
        string outputDir = Path.Combine(Path.GetTempPath(), $"iso_extract_{Guid.NewGuid():N}");
        try
        {
            using IsoFile iso = IsoFile.Load(_isoPath);
            iso.ExtractAll(outputDir);

            Assert.That(File.ReadAllText(Path.Combine(outputDir, "hello.txt")), Is.EqualTo("hello world"));
            Assert.That(File.ReadAllBytes(Path.Combine(outputDir, "sub", "inner.bin")),
                Is.EqualTo(new byte[]
                {
                    0x01, 0x02, 0x03, 0x04
                }));
        }
        finally
        {
            if (Directory.Exists(outputDir))
            {
                Directory.Delete(outputDir, true);
            }
        }
    }
}