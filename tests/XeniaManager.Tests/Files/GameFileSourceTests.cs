using System.Buffers.Binary;
using System.Text;
using XeniaManager.Files.Browsing;

namespace XeniaManager.Tests.Files;

/// <summary>
/// Tests for <see cref="GameFileSourceFactory"/> routing and the <see cref="IGameFileSource"/>
/// adapters against minimal synthetic containers. No real game files required.
/// </summary>
[TestFixture]
public class GameFileSourceTests
{
    private const uint SectorSize = 0x800;

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

    private static byte[] BuildIsoImage()
    {
        // Single file at root: hello.txt (11 bytes @ sector 41), XDKI header @ sector 0x20.
        using MemoryStream rootDir = new MemoryStream();
        WriteDirent(rootDir, 0, 0, 41, 11, 0x00, "hello.txt");
        byte[] rootDirBytes = rootDir.ToArray();

        byte[] image = new byte[44 * SectorSize];
        using (MemoryStream header = new MemoryStream(image, (int)(0x20 * SectorSize), (int)SectorSize, true))
        {
            BinaryWriter writer = new BinaryWriter(header, Encoding.ASCII, true);
            writer.Write(Encoding.ASCII.GetBytes("MICROSOFT*XBOX*MEDIA"));
            writer.Write(40u);
            writer.Write((uint)rootDirBytes.Length);
            writer.Write(0L);
        }

        Array.Copy(rootDirBytes, 0, image, 40 * SectorSize, rootDirBytes.Length);
        Array.Copy(Encoding.ASCII.GetBytes("hello world"), 0, image, 41 * SectorSize, 11);
        return image;
    }

    private static void WriteStfsEntry(byte[] data, int offset, string name, byte flags, int startBlock, int fileSize, short parent)
    {
        Encoding.ASCII.GetBytes(name, data.AsSpan(offset, 40));
        data[offset + 0x28] = flags;
        data[offset + 0x29] = 0x01; // valid blocks LE24 = 1
        data[offset + 0x2C] = 0x01; // allocated blocks LE24 = 1
        data[offset + 0x2F] = (byte)(startBlock & 0xFF);
        data[offset + 0x30] = (byte)((startBlock >> 8) & 0xFF);
        data[offset + 0x31] = (byte)((startBlock >> 16) & 0xFF);
        BinaryPrimitives.WriteInt16BigEndian(data.AsSpan(offset + 0x32), parent);
        BinaryPrimitives.WriteInt32BigEndian(data.AsSpan(offset + 0x34), fileSize);
    }

    private static byte[] BuildStfsPackage()
    {
        // Single consecutive file: hello.txt (11 bytes, block 2 @ 0x4000).
        byte[] data = new byte[0xB100];
        Encoding.ASCII.GetBytes("LIVE", data.AsSpan(0, 4));
        BinaryPrimitives.WriteInt32BigEndian(data.AsSpan(0x340), 0x1000); // header size
        data[0x37B] = 0x01; // volume flags: read-only
        BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(0x37C), 1); // file table block count
        data[0x37E] = 0x01; // file table block LE24 = 1
        WriteStfsEntry(data, 0x3000, "hello.txt", 0x49, 2, 11, -1);
        Encoding.ASCII.GetBytes("hello world", data.AsSpan(0x4000, 11));
        return data;
    }

    private static byte[] BuildSvodProbe(bool isSvod)
    {
        // Header-only package: SVOD iff DescriptorType == 1 (no GDFX, thumbnail-only).
        byte[] data = new byte[0xA000];
        Encoding.ASCII.GetBytes("LIVE", data.AsSpan(0, 4));
        BinaryPrimitives.WriteInt32BigEndian(data.AsSpan(0x3A9), isSvod ? 1 : 0);
        return data;
    }

    private static string WriteTempFile(byte[] data, string extension)
    {
        string path = Path.Combine(Path.GetTempPath(), $"gfs_{Guid.NewGuid():N}{extension}");
        File.WriteAllBytes(path, data);
        return path;
    }

    [Test]
    public void TryOpen_IsoImage_ReturnsIsoSource()
    {
        string path = WriteTempFile(BuildIsoImage(), ".iso");
        try
        {
            using IGameFileSource? source = GameFileSourceFactory.TryOpen(path);

            Assert.That(source, Is.InstanceOf<IsoGameFileSource>());
            Assert.That(source!.Files.Select(f => f.FullPath), Is.EquivalentTo(new[]
            {
                "hello.txt"
            }));
            Assert.That(source.ListDirectory(string.Empty)!.Select(e => e.Name), Is.EquivalentTo(new[]
            {
                "hello.txt"
            }));
            Assert.That(Encoding.ASCII.GetString(source.ReadFile("hello.txt")!), Is.EqualTo("hello world"));
            Assert.That(source.FindDefaultXexPath(), Is.Null);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void TryOpen_StfsPackage_ReturnsStfsSource()
    {
        string path = WriteTempFile(BuildStfsPackage(), ".stfs");
        try
        {
            using IGameFileSource? source = GameFileSourceFactory.TryOpen(path);

            Assert.That(source, Is.InstanceOf<StfsGameFileSource>());
            Assert.That(source!.Files.Select(f => f.FullPath), Is.EquivalentTo(new[]
            {
                "hello.txt"
            }));
            Assert.That(source.ListDirectory(string.Empty)!.Select(e => e.Name), Is.EquivalentTo(new[]
            {
                "hello.txt"
            }));
            Assert.That(Encoding.ASCII.GetString(source.ReadFile("hello.txt")!), Is.EqualTo("hello world"));
            Assert.That(source.ReadFile("missing.bin"), Is.Null);
            Assert.That(source.ListDirectory("missing"), Is.Null);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void TryOpen_SvodHeader_ReturnsSvodSourceWithEmptyTree()
    {
        string path = WriteTempFile(BuildSvodProbe(true), ".bin");
        try
        {
            using IGameFileSource? source = GameFileSourceFactory.TryOpen(path);

            Assert.That(source, Is.InstanceOf<SvodGameFileSource>());
            Assert.That(source!.Files, Is.Empty);
            Assert.That(source.ListDirectory(string.Empty), Is.Empty);
            Assert.That(source.ReadFile("hello.txt"), Is.Null);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void TryOpen_StfsHeader_ReturnsStfsSource()
    {
        string path = WriteTempFile(BuildSvodProbe(false), ".bin");
        try
        {
            using IGameFileSource? source = GameFileSourceFactory.TryOpen(path);

            Assert.That(source, Is.InstanceOf<StfsGameFileSource>());
            Assert.That(source!.Files, Is.Empty);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void TryOpen_ZarFooter_ReturnsZarSourceWithEmptyTree()
    {
        byte[] data = new byte[150];
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(146), 0x169F52D6); // ZAR footer magic
        string path = WriteTempFile(data, ".zar");
        try
        {
            using IGameFileSource? source = GameFileSourceFactory.TryOpen(path);

            Assert.That(source, Is.InstanceOf<ZarGameFileSource>());
            Assert.That(source!.Files, Is.Empty);
            Assert.That(source.ListDirectory(string.Empty), Is.Empty);
            Assert.That(source.ListDirectory("missing"), Is.Null);
            Assert.That(source.ReadFile("missing"), Is.Null);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void TryOpen_XexFile_ReturnsLooseSourceOverContainingDirectory()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"gfs_xex_{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            File.WriteAllBytes(Path.Combine(directory, "default.xex"), Encoding.ASCII.GetBytes("XEX2...."));

            using IGameFileSource? source = GameFileSourceFactory.TryOpen(Path.Combine(directory, "default.xex"));

            Assert.That(source, Is.InstanceOf<LooseGameFileSource>());
            Assert.That(source!.FindDefaultXexPath(), Is.EqualTo("default.xex"));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void TryOpen_UnsupportedOrMissing_ReturnsNull()
    {
        string path = WriteTempFile([0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07], ".bin");
        try
        {
            Assert.That(GameFileSourceFactory.TryOpen(path), Is.Null);
            Assert.That(GameFileSourceFactory.TryOpen(Path.Combine(Path.GetTempPath(), $"gfs_missing_{Guid.NewGuid():N}.iso")), Is.Null);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void LooseSource_BrowsesDirectoryTree()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"gfs_loose_{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(directory, "sub"));
        try
        {
            File.WriteAllText(Path.Combine(directory, "root.txt"), "root");
            File.WriteAllBytes(Path.Combine(directory, "sub", "inner.bin"), [0x01, 0x02]);

            using IGameFileSource? source = GameFileSourceFactory.TryOpen(directory);

            Assert.That(source, Is.InstanceOf<LooseGameFileSource>());
            Assert.That(source!.ListDirectory(string.Empty)!.Select(e => e.Name), Is.EquivalentTo(new[]
            {
                "root.txt", "sub"
            }));
            Assert.That(source.ListDirectory("sub")!.Select(e => e.Name), Is.EquivalentTo(new[]
            {
                "inner.bin"
            }));
            Assert.That(Encoding.ASCII.GetString(source.ReadFile("root.txt")!), Is.EqualTo("root"));
            Assert.That(source.ReadFile("sub"), Is.Null);
            Assert.That(source.ReadFile("missing"), Is.Null);
            Assert.That(source.ListDirectory("root.txt"), Is.Null);
            Assert.That(source.ListDirectory("..\\.."), Is.Null);
            Assert.That(source.FindDefaultXexPath(), Is.Null);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void ReadFileRange_StfsSource_MatchesSlicesAndContract()
    {
        string path = WriteTempFile(BuildStfsPackage(), ".stfs");
        try
        {
            using IGameFileSource? source = GameFileSourceFactory.TryOpen(path);

            Assert.That(source, Is.InstanceOf<StfsGameFileSource>());
            byte[] full = source!.ReadFile("hello.txt")!;
            Assert.That(Encoding.ASCII.GetString(full), Is.EqualTo("hello world"));
            Assert.That(Encoding.ASCII.GetString(source.ReadFileRange("hello.txt", 0, 5)!), Is.EqualTo("hello"));
            Assert.That(Encoding.ASCII.GetString(source.ReadFileRange("hello.txt", 6, 100)!), Is.EqualTo("world"));
            Assert.That(source.ReadFileRange("hello.txt", 11, 10), Is.Empty);
            Assert.That(source.ReadFileRange("hello.txt", 100, 10), Is.Empty);
            Assert.That(source.ReadFileRange("missing.bin", 0, 4), Is.Null);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void ReadFileRange_LooseSource_MatchesSlicesAndContract()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"gfs_range_{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            File.WriteAllText(Path.Combine(directory, "root.txt"), "hello world");

            using LooseGameFileSource source = new LooseGameFileSource(directory);

            Assert.That(Encoding.ASCII.GetString(source.ReadFileRange("root.txt", 0, 5)!), Is.EqualTo("hello"));
            Assert.That(Encoding.ASCII.GetString(source.ReadFileRange("root.txt", 6, 100)!), Is.EqualTo("world"));
            Assert.That(source.ReadFileRange("root.txt", 11, 10), Is.Empty);
            Assert.That(source.ReadFileRange("missing", 0, 4), Is.Null);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void ReadFileRange_IsoSource_MatchesSlices()
    {
        string path = WriteTempFile(BuildIsoImage(), ".iso");
        try
        {
            using IGameFileSource? source = GameFileSourceFactory.TryOpen(path);

            Assert.That(source, Is.InstanceOf<IsoGameFileSource>());
            Assert.That(Encoding.ASCII.GetString(source!.ReadFileRange("hello.txt", 0, 5)!), Is.EqualTo("hello"));
            Assert.That(source.ReadFileRange("missing", 0, 4), Is.Null);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void FindDefaultXexPath_PrefersDefaultXexOverOtherXex()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"gfs_xexfind_{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(directory, "sub"));
        try
        {
            File.WriteAllText(Path.Combine(directory, "other.xex"), "x");
            File.WriteAllText(Path.Combine(directory, "sub", "default.xex"), "x");

            using LooseGameFileSource source = new LooseGameFileSource(directory);

            Assert.That(source.FindDefaultXexPath(), Is.EqualTo("sub/default.xex"));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }
}