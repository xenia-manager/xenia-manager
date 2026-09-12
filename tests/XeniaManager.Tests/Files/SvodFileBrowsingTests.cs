using System.Buffers.Binary;
using System.Text;
using XeniaManager.Files;
using XeniaManager.Files.Models.Iso;

namespace XeniaManager.Tests.Files;

/// <summary>
/// Tests for the SvodFile GDFX browsing API (Files/Entries/Lookup/ListDirectory/ReadFile)
/// against a minimal synthetic single-file GOD image. No real SVOD required.
/// </summary>
[TestFixture]
public class SvodFileBrowsingTests
{
    private const uint SectorSize = 0x800;
    private const uint RootSector = 40;
    private const uint HelloSector = 41;
    private const uint SubDirSector = 42;
    private const uint InnerSector = 43;
    private const int GdfxHeaderOffset = 0xD000; // SingleFile layout magic offset

    private string _svodPath = string.Empty;

    [SetUp]
    public void Setup()
    {
        _svodPath = Path.Combine(Path.GetTempPath(), $"svod_browse_{Guid.NewGuid():N}.bin");
        File.WriteAllBytes(_svodPath, BuildTestPackage(true));
    }

    [TearDown]
    public void Teardown()
    {
        if (File.Exists(_svodPath))
        {
            File.Delete(_svodPath);
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

    private static byte[] BuildTestPackage(bool withGdfx)
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

        byte[] image = new byte[0x16000];
        Encoding.ASCII.GetBytes("LIVE", image.AsSpan(0, 4));
        BinaryPrimitives.WriteInt32BigEndian(image.AsSpan(0x340), 0x1000); // header size
        BinaryPrimitives.WriteInt32BigEndian(image.AsSpan(0x3A9), 1); // DescriptorType = SVOD
        // SVOD volume descriptor @0x379: not enhanced, direct-read threshold 0x2000 sectors.
        image[0x379] = 0x24;
        image[0x379 + 0x18] = 0x00;
        image[0x379 + 0x19] = 0x00;
        image[0x379 + 0x1A] = 0x10;
        image[0x379 + 0x1B] = 0x00;
        image[0x379 + 0x1C] = 0x00;
        image[0x379 + 0x1D] = 0x10;
        image[0x379 + 0x1E] = 0x00;

        if (withGdfx)
        {
            Encoding.ASCII.GetBytes("MICROSOFT*XBOX*MEDIA", image.AsSpan(GdfxHeaderOffset, 20));
            BinaryPrimitives.WriteUInt32LittleEndian(image.AsSpan(GdfxHeaderOffset + 20), RootSector);
            BinaryPrimitives.WriteUInt32LittleEndian(image.AsSpan(GdfxHeaderOffset + 24), (uint)rootDirBytes.Length);
            Array.Copy(rootDirBytes, 0, image, RootSector * SectorSize, rootDirBytes.Length);
            Array.Copy(Encoding.ASCII.GetBytes("hello world"), 0, image, HelloSector * SectorSize, 11);
            Array.Copy(subDirBytes, 0, image, SubDirSector * SectorSize, subDirBytes.Length);
            image[InnerSector * SectorSize] = 0x01;
            image[InnerSector * SectorSize + 1] = 0x02;
            image[InnerSector * SectorSize + 2] = 0x03;
            image[InnerSector * SectorSize + 3] = 0x04;
        }

        return image;
    }

    [Test]
    public void Load_SyntheticPackage_IsValidWithoutXex()
    {
        using SvodFile svod = SvodFile.Load(_svodPath);

        Assert.That(svod.IsValid, Is.True);
        // No default.xex in the synthetic image: XEX-dependent details stay empty, browsing still works.
        Assert.That(svod.XexFile, Is.Null);
        Assert.That(svod.TryGetNxeBackground(), Is.Null);
    }

    [Test]
    public void Files_ListsAllFilesWithPaths()
    {
        using SvodFile svod = SvodFile.Load(_svodPath);

        List<GdfxEntry> files = svod.Files.Where(f => f.IsFile).ToList();
        Assert.That(files.Count, Is.EqualTo(2));
        Assert.That(files.Select(f => f.FullPath), Is.EquivalentTo(new[]
        {
            "hello.txt", "sub/inner.bin"
        }));
        Assert.That(files.Single(f => f.FullPath == "hello.txt").Size, Is.EqualTo(11UL));
        Assert.That(files.Single(f => f.FullPath == "sub/inner.bin").Size, Is.EqualTo(4UL));
    }

    [Test]
    public void Entries_IncludesDirectoriesWithZeroSize()
    {
        using SvodFile svod = SvodFile.Load(_svodPath);

        List<GdfxEntry> entries = svod.Entries.ToList();
        Assert.That(entries.Count, Is.EqualTo(3));
        GdfxEntry sub = entries.Single(e => e.FullPath == "sub");
        Assert.That(sub.IsFile, Is.False);
        Assert.That(sub.Size, Is.EqualTo(0UL));
    }

    [Test]
    public void Lookup_ResolvesPathsCaseInsensitively()
    {
        using SvodFile svod = SvodFile.Load(_svodPath);

        Assert.That(svod.Lookup("sub\\inner.bin")?.Size, Is.EqualTo(4UL));
        Assert.That(svod.Lookup("SUB/INNER.BIN")?.Size, Is.EqualTo(4UL));
        Assert.That(svod.Lookup("sub")?.IsFile, Is.False);
        Assert.That(svod.Lookup("missing.bin"), Is.Null);
        Assert.That(svod.Lookup("hello.txt/nope"), Is.Null);
    }

    [Test]
    public void ListDirectory_ReturnsImmediateChildren()
    {
        using SvodFile svod = SvodFile.Load(_svodPath);

        List<GdfxEntry>? root = svod.ListDirectory(string.Empty);
        Assert.That(root, Is.Not.Null);
        Assert.That(root!.Select(e => e.Name), Is.EquivalentTo(new[]
        {
            "hello.txt", "sub"
        }));

        List<GdfxEntry>? sub = svod.ListDirectory("sub");
        Assert.That(sub, Is.Not.Null);
        Assert.That(sub!.Select(e => e.Name), Is.EquivalentTo(new[]
        {
            "inner.bin"
        }));

        Assert.That(svod.ListDirectory("hello.txt"), Is.Null);
        Assert.That(svod.ListDirectory("missing"), Is.Null);
    }

    [Test]
    public void ReadFile_ReturnsContentsAndSupportsRanges()
    {
        using SvodFile svod = SvodFile.Load(_svodPath);

        Assert.That(Encoding.ASCII.GetString(svod.ReadFile("hello.txt")!), Is.EqualTo("hello world"));
        Assert.That(svod.ReadFile("sub/inner.bin"), Is.EqualTo(new byte[]
        {
            0x01, 0x02, 0x03, 0x04
        }));
        Assert.That(svod.ReadFile("missing.bin"), Is.Null);
        Assert.That(svod.ReadFile("sub"), Is.Null);

        GdfxEntry hello = svod.Lookup("hello.txt")!;
        Assert.That(Encoding.ASCII.GetString(svod.ReadFile(hello, 6, 5)), Is.EqualTo("world"));
        Assert.That(svod.ReadFile(hello, hello.Size, 10), Is.Empty);
        Assert.That(svod.ReadFile(hello, 0, 1000), Has.Length.EqualTo(11));
    }

    [Test]
    public void BrowseWithoutGdfx_ReturnsEmpty()
    {
        string path = Path.Combine(Path.GetTempPath(), $"svod_nogdfx_{Guid.NewGuid():N}.bin");
        try
        {
            File.WriteAllBytes(path, BuildTestPackage(false));
            using SvodFile svod = SvodFile.Load(path);

            // Thumbnail-only package stays valid but has no browsable tree.
            Assert.That(svod.IsValid, Is.True);
            Assert.That(svod.Files, Is.Empty);
            Assert.That(svod.Entries, Is.Empty);
            Assert.That(svod.Lookup("hello.txt"), Is.Null);
            Assert.That(svod.ListDirectory(string.Empty), Is.Empty);
            Assert.That(svod.ReadFile("hello.txt"), Is.Null);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}