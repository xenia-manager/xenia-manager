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

    private static void WriteEntry(byte[] package, int offset, string name, byte flags, int startBlock)
    {
        byte[] nameBytes = Encoding.ASCII.GetBytes(name);
        Array.Copy(nameBytes, 0, package, offset, nameBytes.Length);
        package[offset + 0x28] = (byte)(flags | nameBytes.Length);
        package[offset + 0x29] = 2; // valid blocks
        package[offset + 0x2C] = 2; // allocated blocks
        package[offset + 0x2F] = (byte)startBlock; // starting block (int24 LE)
        BinaryPrimitives.WriteInt16BigEndian(package.AsSpan(offset + 0x32), -1); // root
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
}