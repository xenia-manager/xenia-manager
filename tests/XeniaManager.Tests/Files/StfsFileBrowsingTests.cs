using System.Buffers.Binary;
using System.Text;
using XeniaManager.Files;
using XeniaManager.Files.Models.Stfs;

namespace XeniaManager.Tests.Files;

/// <summary>
/// Tests for the StfsFile browsing API (Lookup/ListDirectory/ReadFile)
/// against a minimal synthetic package. No real STFS file required.
/// </summary>
[TestFixture]
public class StfsFileBrowsingTests
{
    private static byte[] BuildTestPackage()
    {
        // Tree:
        //   hello.txt (11 bytes, consecutive block 2 @ 0x4000)
        //   sub/ (dir)
        //     sub/inner.bin (4 bytes, consecutive block 3 @ 0x5000)
        //   orphan.txt (invalid parent index -> surfaces at root, block 4 @ 0x6000)
        byte[] data = new byte[0xB100];
        Encoding.ASCII.GetBytes("LIVE", data.AsSpan(0, 4));
        BinaryPrimitives.WriteInt32BigEndian(data.AsSpan(0x340), 0x1000); // header size
        data[0x37B] = 0x01; // volume flags: read-only
        BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(0x37C), 1); // file table block count
        data[0x37E] = 0x01; // file table block LE24 = 1
        data[0x37F] = 0x00;
        data[0x380] = 0x00;

        WriteEntry(data, 0x3000, "hello.txt", 0x49, 1, 1, 2, 11, -1);
        WriteEntry(data, 0x3040, "sub", 0x83, 0, 0, 0, 0, -1);
        WriteEntry(data, 0x3080, "inner.bin", 0x49, 1, 1, 3, 4, 1);
        WriteEntry(data, 0x30C0, "orphan.txt", 0x4A, 1, 1, 4, 6, 99);

        Encoding.ASCII.GetBytes("hello world", data.AsSpan(0x4000, 11));
        data[0x5000] = 0x01;
        data[0x5000 + 1] = 0x02;
        data[0x5000 + 2] = 0x03;
        data[0x5000 + 3] = 0x04;
        Encoding.ASCII.GetBytes("orphan", data.AsSpan(0x6000, 6));
        return data;
    }

    private static void WriteEntry(byte[] data, int offset, string name, byte flags, int validBlocks, int allocatedBlocks,
        int startBlock, int fileSize, short parent)
    {
        Encoding.ASCII.GetBytes(name, data.AsSpan(offset, 40));
        data[offset + 0x28] = flags;
        data[offset + 0x29] = (byte)(validBlocks & 0xFF);
        data[offset + 0x2A] = (byte)((validBlocks >> 8) & 0xFF);
        data[offset + 0x2B] = (byte)((validBlocks >> 16) & 0xFF);
        data[offset + 0x2C] = (byte)(allocatedBlocks & 0xFF);
        data[offset + 0x2D] = (byte)((allocatedBlocks >> 8) & 0xFF);
        data[offset + 0x2E] = (byte)((allocatedBlocks >> 16) & 0xFF);
        data[offset + 0x2F] = (byte)(startBlock & 0xFF);
        data[offset + 0x30] = (byte)((startBlock >> 8) & 0xFF);
        data[offset + 0x31] = (byte)((startBlock >> 16) & 0xFF);
        BinaryPrimitives.WriteInt16BigEndian(data.AsSpan(offset + 0x32), parent);
        BinaryPrimitives.WriteInt32BigEndian(data.AsSpan(offset + 0x34), fileSize);
    }

    [Test]
    public void ListDirectory_Root_ReturnsImmediateChildren()
    {
        using StfsFile stfs = StfsFile.FromBytes(BuildTestPackage());

        List<StfsFileEntry>? root = stfs.ListDirectory(string.Empty);
        Assert.That(root, Is.Not.Null);
        Assert.That(root!.Select(e => e.FileName), Is.EquivalentTo(new[]
        {
            "hello.txt", "sub", "orphan.txt"
        }));
    }

    [Test]
    public void ListDirectory_Sub_ReturnsInnerFile()
    {
        using StfsFile stfs = StfsFile.FromBytes(BuildTestPackage());

        List<StfsFileEntry>? sub = stfs.ListDirectory("sub");
        Assert.That(sub, Is.Not.Null);
        Assert.That(sub!.Select(e => e.FileName), Is.EquivalentTo(new[]
        {
            "inner.bin"
        }));
    }

    [Test]
    public void ListDirectory_FileOrMissing_ReturnsNull()
    {
        using StfsFile stfs = StfsFile.FromBytes(BuildTestPackage());

        Assert.That(stfs.ListDirectory("hello.txt"), Is.Null);
        Assert.That(stfs.ListDirectory("missing"), Is.Null);
        Assert.That(stfs.ListDirectory("hello.txt/nope"), Is.Null);
    }

    [Test]
    public void Lookup_ResolvesPathsCaseInsensitively()
    {
        using StfsFile stfs = StfsFile.FromBytes(BuildTestPackage());

        Assert.That(stfs.Lookup("sub\\inner.bin")?.FileSize, Is.EqualTo(4));
        Assert.That(stfs.Lookup("SUB/INNER.BIN")?.FileSize, Is.EqualTo(4));
        Assert.That(stfs.Lookup("sub")?.IsDirectory, Is.True);
        Assert.That(stfs.Lookup("missing.bin"), Is.Null);
        Assert.That(stfs.Lookup("hello.txt/nope"), Is.Null);
        Assert.That(stfs.Lookup(string.Empty), Is.Null);
    }

    [Test]
    public void ReadFile_ReturnsContentsAndSupportsRanges()
    {
        using StfsFile stfs = StfsFile.FromBytes(BuildTestPackage());

        Assert.That(Encoding.ASCII.GetString(stfs.ReadFile("hello.txt")!), Is.EqualTo("hello world"));
        Assert.That(stfs.ReadFile("sub/inner.bin"), Is.EqualTo(new byte[]
        {
            0x01, 0x02, 0x03, 0x04
        }));
        Assert.That(stfs.ReadFile("missing.bin"), Is.Null);
        Assert.That(stfs.ReadFile("sub"), Is.Null);

        StfsFileEntry hello = stfs.Lookup("hello.txt")!;
        Assert.That(Encoding.ASCII.GetString(stfs.ReadFile(hello, 6, 5)), Is.EqualTo("world"));
        Assert.That(stfs.ReadFile(hello, (ulong)hello.FileSize, 10), Is.Empty);
        Assert.That(stfs.ReadFile(hello, 0, 1000), Has.Length.EqualTo(11));
    }
}