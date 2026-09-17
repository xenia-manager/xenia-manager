using System.Buffers.Binary;
using System.Text;
using XeniaManager.Files;
using XeniaManager.Files.Models.Stfs;

namespace XeniaManager.Tests.Files;

/// <summary>
/// Tests for StfsFile ranged reads against synthetic multi-block packages.
/// Ranged reads must match slices of the full extraction for consecutive
/// and hash-chained files. No real STFS file required.
/// </summary>
[TestFixture]
public class StfsFileRangedReadTests
{
    private const int BlockSize = 0x1000;
    private const int FileSize = 5000; // spans blocks 1-2 (consecutive) and 3-4 (chained)

    private static byte[] Pattern(int seed)
    {
        byte[] data = new byte[FileSize];
        for (int i = 0; i < data.Length; i++)
        {
            data[i] = (byte)((i * 7 + seed) % 251);
        }

        return data;
    }

    private static void WriteEntry(byte[] package, int offset, string name, byte flags, int startBlock, short pathIndicator = -1)
    {
        byte[] nameBytes = Encoding.ASCII.GetBytes(name);
        Array.Copy(nameBytes, 0, package, offset, nameBytes.Length);
        package[offset + 0x28] = (byte)(flags | nameBytes.Length);
        package[offset + 0x29] = 2; // valid blocks
        package[offset + 0x2C] = 2; // allocated blocks
        package[offset + 0x2F] = (byte)startBlock; // starting block (int24 LE)
        BinaryPrimitives.WriteInt16BigEndian(package.AsSpan(offset + 0x32), pathIndicator);
        BinaryPrimitives.WriteInt32BigEndian(package.AsSpan(offset + 0x34), FileSize);
    }

    private static void WriteHashLink(byte[] package, int block, uint nextBlock)
    {
        // Level-0 hash table lives in block 0 (@0x1000 read-only); record = block % 170.
        int infoOffset = 0x1000 + block * 0x18 + 0x14;
        BinaryPrimitives.WriteUInt32BigEndian(package.AsSpan(infoOffset), 0x80000000 | (nextBlock & 0xFFFFFF));
    }

    private static byte[] BuildTestPackage()
    {
        byte[] package = new byte[0xA000];
        Encoding.ASCII.GetBytes("CON ").CopyTo(package, 0x000);
        BinaryPrimitives.WriteInt32BigEndian(package.AsSpan(0x340), 0x1000); // HeaderSize
        BinaryPrimitives.WriteUInt32BigEndian(package.AsSpan(0x344), 0x00000002); // ContentType
        BinaryPrimitives.WriteInt32BigEndian(package.AsSpan(0x348), 1); // MetadataVersion
        BinaryPrimitives.WriteInt32BigEndian(package.AsSpan(0x360), 0x4D5309C9); // TitleID
        Encoding.BigEndianUnicode.GetBytes("Test Content").CopyTo(package, 0x411);
        package[0x379] = 0x24; // descriptor size
        package[0x37B] = 0x01; // read-only: 1 block per hash table
        BinaryPrimitives.WriteInt16LittleEndian(package.AsSpan(0x37C), 1); // file table block count
        // file table block number = 0 (@0x2000)

        WriteEntry(package, 0x2000, "big.bin", 0x40, 1);
        WriteEntry(package, 0x2040, "chain.bin", 0x00, 3);
        WriteEntry(package, 0x2080, "sub", 0x80, 0);

        byte[] big = Pattern(0);
        Array.Copy(big, 0, package, 0x3000, BlockSize);
        Array.Copy(big, BlockSize, package, 0x4000, FileSize - BlockSize);

        byte[] chained = Pattern(1);
        Array.Copy(chained, 0, package, 0x5000, BlockSize);
        Array.Copy(chained, BlockSize, package, 0x6000, FileSize - BlockSize);
        WriteHashLink(package, 3, 4);
        WriteHashLink(package, 4, 0xFFFFFF);

        return package;
    }

    private static void AssertMatchesSlices(StfsFile stfs, string fileName)
    {
        StfsFileEntry entry = stfs.Lookup(fileName)!;
        byte[] full = stfs.ExtractFile(entry);
        Assert.That(full, Has.Length.EqualTo(FileSize));

        (ulong Offset, ulong Length)[] vectors =
        [
            (0, FileSize),
            (0, 1),
            (100, 200),
            (BlockSize - 1, 2),
            (BlockSize, (ulong)(FileSize - BlockSize)),
            (FileSize - 1, 1),
            (FileSize - 1, 100),
            (0, 0),
            (100, 0)
        ];
        foreach ((ulong offset, ulong length) in vectors)
        {
            byte[] expected = full.Skip((int)offset).Take((int)Math.Min(length, (ulong)full.Length - offset)).ToArray();
            Assert.That(stfs.ReadFile(entry, offset, length), Is.EqualTo(expected),
                $"Mismatch at offset {offset} length {length}");
        }

        Assert.That(stfs.ReadFile(entry, FileSize, 10), Is.Empty);
        Assert.That(stfs.ReadFile(entry, FileSize + 100, 10), Is.Empty);
    }

    [Test]
    public void ReadFile_Consecutive_MatchesFullExtractSlices()
    {
        using StfsFile stfs = StfsFile.FromBytes(BuildTestPackage());
        AssertMatchesSlices(stfs, "big.bin");
    }

    [Test]
    public void ReadFile_Chained_MatchesFullExtractSlices()
    {
        using StfsFile stfs = StfsFile.FromBytes(BuildTestPackage());
        AssertMatchesSlices(stfs, "chain.bin");
    }

    [Test]
    public void ReadFile_Directory_ReturnsEmpty()
    {
        using StfsFile stfs = StfsFile.FromBytes(BuildTestPackage());
        StfsFileEntry dir = stfs.Lookup("sub")!;
        Assert.That(stfs.ReadFile(dir, 0, 10), Is.Empty);
    }

    [Test]
    public void Lookup_IndexedResult_MatchesBrowsing()
    {
        using StfsFile stfs = StfsFile.FromBytes(BuildTestPackage());
        Assert.That(stfs.Lookup("BIG.BIN")?.FileSize, Is.EqualTo(FileSize));
        Assert.That(stfs.Lookup("missing.bin"), Is.Null);
        Assert.That(stfs.ListDirectory(string.Empty)!.Select(e => e.FileName),
            Is.EquivalentTo(new[]
            {
                "big.bin", "chain.bin", "sub"
            }));
    }

    [Test]
    public void ListDirectory_Nested_ResolvesThroughParentMap()
    {
        byte[] package = BuildTestPackage();
        // "sub" is file-table index 2; nest a file under it plus a file with a dangling parent.
        WriteEntry(package, 0x20C0, "nested.bin", 0x40, 1, 2);
        WriteEntry(package, 0x2100, "orphan.bin", 0x40, 1, 99);
        using StfsFile stfs = StfsFile.FromBytes(package);

        Assert.That(stfs.ListDirectory("sub")!.Select(e => e.FileName), Is.EqualTo(new[]
        {
            "nested.bin"
        }));
        Assert.That(stfs.ListDirectory(string.Empty)!.Select(e => e.FileName),
            Is.EquivalentTo(new[]
            {
                "big.bin", "chain.bin", "sub", "orphan.bin"
            }));
        Assert.That(stfs.Lookup("sub/nested.bin")?.FileSize, Is.EqualTo(FileSize));
        Assert.That(stfs.ListDirectory("big.bin"), Is.Null);
        Assert.That(stfs.ListDirectory("missing"), Is.Null);
    }

    [Test]
    public void Load_StreamBacked_MatchesMemoryExtractAndRanges()
    {
        string path = Path.Combine(Path.GetTempPath(), $"stfs_stream_{Guid.NewGuid():N}.bin");
        File.WriteAllBytes(path, BuildTestPackage());
        try
        {
            using StfsFile memory = StfsFile.FromBytes(BuildTestPackage());
            using StfsFile streamed = StfsFile.Load(path);
            foreach (string name in new[]
                     {
                         "big.bin", "chain.bin"
                     })
            {
                StfsFileEntry memoryEntry = memory.Lookup(name)!;
                StfsFileEntry streamEntry = streamed.Lookup(name)!;
                Assert.That(streamEntry.FileSize, Is.EqualTo(memoryEntry.FileSize));
                Assert.That(streamed.ExtractFile(streamEntry), Is.EqualTo(memory.ExtractFile(memoryEntry)));
                Assert.That(streamed.ReadFile(streamEntry, 100, 200), Is.EqualTo(memory.ReadFile(memoryEntry, 100, 200)));
                Assert.That(streamed.ReadFile(name)!, Is.EqualTo(memory.ReadFile(name)!));
            }

            Assert.That(streamed.ReadFile("missing.bin"), Is.Null);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public void Load_Dispose_ReleasesFileHandle()
    {
        string path = Path.Combine(Path.GetTempPath(), $"stfs_stream_{Guid.NewGuid():N}.bin");
        File.WriteAllBytes(path, BuildTestPackage());
        StfsFile streamed = StfsFile.Load(path);
        Assert.That(streamed.Lookup("big.bin"), Is.Not.Null);
        streamed.Dispose();
        File.Delete(path);
        Assert.That(File.Exists(path), Is.False);
    }

    [Test]
    public void BlockNumberToOffset_LargeBlock_Returns64BitOffset()
    {
        using StfsFile stfs = StfsFile.FromBytes(BuildTestPackage());
        Assert.That(stfs.BlockNumberToOffset(0), Is.EqualTo(0x2000L));

        // Block 600000 with HeaderSize 0x1000 and 1 block per hash table:
        // 600000 + 3530 + 21 + 1 = 603552 -> 0x1000 + 603552 * 0x1000 (overflows int).
        long offset = stfs.BlockNumberToOffset(600000);
        Assert.That(offset, Is.EqualTo(0x1000L + 603552L * 0x1000L));
        Assert.That(offset, Is.GreaterThan(int.MaxValue));
    }

    [Test]
    public void ExtractFile_ChainedEarlyEnd_ThrowsIOException()
    {
        byte[] package = BuildTestPackage();
        WriteHashLink(package, 3, 0xFFFFFF); // chain.bin needs blocks 3->4; end after first block
        using StfsFile stfs = StfsFile.FromBytes(package);
        StfsFileEntry entry = stfs.Lookup("chain.bin")!;
        Assert.Throws<IOException>(() => stfs.ExtractFile(entry));
        Assert.Throws<IOException>(() => stfs.ReadFile(entry, 0, FileSize));
    }

    [Test]
    public void ExtractFile_PastEndOfPackage_ThrowsIOException()
    {
        using StfsFile stfs = StfsFile.FromBytes(BuildTestPackage());
        StfsFileEntry entry = stfs.Lookup("big.bin")!;
        entry.FileSize = 0x10000; // claim blocks past the end of the package
        Assert.Throws<IOException>(() => stfs.ExtractFile(entry));
        Assert.Throws<IOException>(() => stfs.ReadFile(entry, 0, 0x10000));
    }

    [Test]
    public void ExtractFileToStream_MatchesExtractFile()
    {
        using StfsFile stfs = StfsFile.FromBytes(BuildTestPackage());
        foreach (string name in new[]
                 {
                     "big.bin", "chain.bin"
                 })
        {
            StfsFileEntry entry = stfs.Lookup(name)!;
            byte[] expected = stfs.ExtractFile(entry);
            using MemoryStream ms = new MemoryStream();
            stfs.ExtractFileToStream(entry, ms);
            Assert.That(ms.ToArray(), Is.EqualTo(expected));
        }
    }
}