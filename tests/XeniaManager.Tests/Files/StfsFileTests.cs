using System.Buffers.Binary;
using System.Reflection;
using System.Text;
using XeniaManager.Files;
using XeniaManager.Files.Models.Stfs;

namespace XeniaManager.Tests.Files;

[TestFixture]
public class StfsFileTests
{
    private string _testStfsFilePath = string.Empty;
    private string _testOutputDirectory = string.Empty;

    [SetUp]
    public void Setup()
    {
        // Assembly Location
        string assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

        _testStfsFilePath = @"TU12345_12345"; // Needs to use real file, otherwise it's skipped
        _testOutputDirectory = @"output"; // Needs to be modified
    }

    [Test]
    public void Load_ValidStfsFile_ReturnsStfsFile()
    {
        // Skip if the test file doesn't exist
        if (!File.Exists(_testStfsFilePath))
        {
            Assert.Ignore("STFS test file not found");
            return;
        }

        // Act
        StfsFile stfs = StfsFile.Load(_testStfsFilePath);
        stfs.ExtractToXeniaStructure(_testOutputDirectory);

        // Assert
        Assert.That(stfs, Is.Not.Null);
    }

    private static byte[] BuildMinimalPackage(int fileTableBlockCount = 1)
    {
        // Minimal synthetic STFS: read-only (1 hash block per level), header size 0x1000,
        // file table at block 1 (offset 0x3000), hash tables at offset 0x1000.
        byte[] data = new byte[0xB100];
        Encoding.ASCII.GetBytes("LIVE", data.AsSpan(0, 4));
        BinaryPrimitives.WriteInt32BigEndian(data.AsSpan(0x340), 0x1000); // header size
        data[0x37B] = 0x01; // volume flags: read-only
        BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(0x37C), (ushort)fileTableBlockCount);
        data[0x37E] = 0x01; // file table block LE24 = 1
        data[0x37F] = 0x00;
        data[0x380] = 0x00;
        return data;
    }

    private static void WriteEntry(byte[] data, int offset, string name, byte flags, int validBlocks, int allocatedBlocks,
        int startBlock, int fileSize)
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
        BinaryPrimitives.WriteInt16BigEndian(data.AsSpan(offset + 0x32), -1); // root parent
        BinaryPrimitives.WriteInt32BigEndian(data.AsSpan(offset + 0x34), fileSize);
    }

    private static void WriteHashInfo(byte[] data, int blockNumber, uint nextBlock)
    {
        // Hash tables start at 0x1000; entry = record * 0x18, info at +0x14 (big endian).
        int infoOffset = 0x1000 + blockNumber % 170 * 0x18 + 0x14;
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(infoOffset), 0x80000000u | (nextBlock & 0xFFFFFFu));
    }

    [Test]
    public void FromBytes_TruncatedPackage_ThrowsArgumentException()
    {
        byte[] data = new byte[0x100];
        Encoding.ASCII.GetBytes("LIVE", data.AsSpan(0, 4));

        Assert.Throws<ArgumentException>(() => StfsFile.FromBytes(data));
    }

    [Test]
    public void FromBytes_ContentId_ReadsHeaderHashField()
    {
        byte[] data = BuildMinimalPackage();
        byte[] marker = [0x42, 0x87, 0xCC, 0x74, 0xC5, 0x91, 0xDB, 0x37, 0xB5, 0x77, 0xDA, 0xEA, 0x43, 0xBB, 0x6E, 0x51, 0x69, 0x2D, 0x7A, 0x8E];
        Array.Copy(marker, 0, data, 0x32C, marker.Length);

        StfsFile stfs = StfsFile.FromBytes(data, false);

        Assert.That(stfs.ContentId, Is.EqualTo(marker));
    }

    [Test]
    public void FromBytes_Version2MediaFields_ReadAtMediaDataOffsets()
    {
        byte[] data = BuildMinimalPackage();
        BinaryPrimitives.WriteInt32BigEndian(data.AsSpan(0x348), 2); // metadata version
        byte[] series = Encoding.ASCII.GetBytes("SERIES1234567890");
        byte[] season = Encoding.ASCII.GetBytes("SEASON1234567890");
        Array.Copy(series, 0, data, 0x3D9, series.Length);
        Array.Copy(season, 0, data, 0x3E9, season.Length);
        BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(0x3F9), 7);
        BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(0x3FB), 3);

        StfsFile stfs = StfsFile.FromBytes(data, false);

        Assert.That(stfs.Metadata.SeriesId, Is.EqualTo(series));
        Assert.That(stfs.Metadata.SeasonId, Is.EqualTo(season));
        Assert.That(stfs.Metadata.SeasonNumber, Is.EqualTo(7));
        Assert.That(stfs.Metadata.EpisodeNumber, Is.EqualTo(3));
    }

    [Test]
    public void ExtractFile_NonConsecutiveBlocks_FollowsBigEndianHashChain()
    {
        byte[] data = BuildMinimalPackage();
        WriteEntry(data, 0x3000, "test.bin", 0x08, 2, 2, 5, 5000);
        WriteHashInfo(data, 5, 6);
        WriteHashInfo(data, 6, 0xFFFFFF);
        Array.Fill(data, (byte)0xAA, 0x7000, 0x1000); // block 5
        Array.Fill(data, (byte)0xBB, 0x8000, 0x1000); // block 6

        StfsFile stfs = StfsFile.FromBytes(data);

        byte[]? extracted = stfs.ExtractFile("test.bin");
        Assert.That(extracted, Is.Not.Null);
        Assert.That(extracted!.Length, Is.EqualTo(5000));
        Assert.That(extracted.Take(0x1000).All(b => b == 0xAA), Is.True);
        Assert.That(extracted.Skip(0x1000).All(b => b == 0xBB), Is.True);
    }

    [Test]
    public void ParseFileTable_MultiBlockTable_FollowsHashChain()
    {
        byte[] data = BuildMinimalPackage(2);
        WriteEntry(data, 0x3000, "first.bin", 0x49, 1, 1, 5, 100);
        Array.Fill(data, (byte)0xAA, 0x7000, 100); // block 5
        WriteHashInfo(data, 1, 7); // table block 1 -> table block 7
        WriteHashInfo(data, 7, 0xFFFFFF);
        WriteEntry(data, 0x9000, "second.bin", 0x4A, 1, 1, 9, 100);
        Array.Fill(data, (byte)0xCC, 0xB000, 100); // block 9

        StfsFile stfs = StfsFile.FromBytes(data);

        Assert.That(stfs.FileEntries.Count, Is.EqualTo(2));
        byte[]? second = stfs.ExtractFile("second.bin");
        Assert.That(second, Is.Not.Null);
        Assert.That(second!.All(b => b == 0xCC), Is.True);
    }
}