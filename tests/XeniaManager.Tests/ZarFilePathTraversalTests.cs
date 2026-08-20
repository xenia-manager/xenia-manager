using System.Buffers.Binary;
using System.Text;
using XeniaManager.Core.Files;
using XeniaManager.Core.Models.Files.Zar;

namespace XeniaManager.Tests;

[TestFixture]
public class ZarFilePathTraversalTests
{
    private string _testOutputDirectory = string.Empty;
    private string _testZarPath = string.Empty;
    private readonly List<string> _escapedPaths = [];

    [SetUp]
    public void Setup()
    {
        _testOutputDirectory = Path.Combine(Path.GetTempPath(), $"XeniaManagerZarTest_{Guid.NewGuid():N}");
        _testZarPath = Path.Combine(Path.GetTempPath(), $"XeniaManagerZarTest_{Guid.NewGuid():N}.zar");
        _escapedPaths.Clear();
    }

    [TearDown]
    public void Teardown()
    {
        if (Directory.Exists(_testOutputDirectory))
        {
            Directory.Delete(_testOutputDirectory, true);
        }

        if (File.Exists(_testZarPath))
        {
            File.Delete(_testZarPath);
        }

        foreach (string escapedPath in _escapedPaths)
        {
            if (File.Exists(escapedPath))
            {
                File.Delete(escapedPath);
            }
        }
    }

    [Test]
    public void ExtractAll_EntryWithDotDotName_DoesNotWriteOutsideOutputDirectory()
    {
        // Arrange
        const string fileName = @"..\..\..\PWNED.txt";
        byte[] payload = Encoding.ASCII.GetBytes("@echo off\r\necho PWNED\r\n");
        File.WriteAllBytes(_testZarPath, BuildZarArchive(fileName, payload));

        using ZarFile zar = ZarFile.Load(_testZarPath);
        Assert.That(zar.IsValid, Is.True, $"ZAR parsing failed: {zar.ValidationError}");

        string escapedPath = Path.GetFullPath(Path.Combine(_testOutputDirectory, fileName));
        _escapedPaths.Add(escapedPath);

        // Act & Assert - the traversal entry must be rejected and never written
        Assert.Throws<IOException>(() => zar.ExtractAll(_testOutputDirectory));
        Assert.That(File.Exists(escapedPath), Is.False);
    }

    [Test]
    public void ExtractAll_BenignEntry_ExtractsInsideOutputDirectory()
    {
        // Arrange
        const string fileName = "test.bin";
        byte[] payload = Encoding.ASCII.GetBytes("benign payload data");
        File.WriteAllBytes(_testZarPath, BuildZarArchive(fileName, payload));

        using ZarFile zar = ZarFile.Load(_testZarPath);
        Assert.That(zar.IsValid, Is.True, $"ZAR parsing failed: {zar.ValidationError}");

        string outputPath = Path.Combine(_testOutputDirectory, fileName);

        // Act
        zar.ExtractAll(_testOutputDirectory);

        // Assert - the benign file is extracted inside the output directory with exact content
        Assert.That(File.Exists(outputPath), Is.True);
        Assert.That(File.ReadAllBytes(outputPath), Is.EqualTo(payload));
    }

    /// <summary>
    /// Builds a minimal valid ZAR archive with a single file at the root,
    /// stored as one uncompressed 64 KiB block.
    /// Layout: data block | offset records | name table | file tree | footer.
    /// </summary>
    private static byte[] BuildZarArchive(string fileName, byte[] payload)
    {
        const int blockSize = 65536;

        // Data section: one stored-uncompressed 64 KiB block
        byte[] dataBlock = new byte[blockSize];
        Array.Copy(payload, 0, dataBlock, 0, payload.Length);

        // Compression offset records section: one record covering block 0
        byte[] offsetRecords = new byte[CompressionOffsetRecord.Size];
        BinaryPrimitives.WriteUInt64BigEndian(offsetRecords.AsSpan(0), 0); // BaseOffset = 0
        BinaryPrimitives.WriteUInt16BigEndian(offsetRecords.AsSpan(8), 65535); // 65536 bytes stored raw

        // Name table: length-prefixed UTF-8 name
        byte[] nameBytes = Encoding.UTF8.GetBytes(fileName);
        byte[] nameTable = new byte[1 + nameBytes.Length];
        nameTable[0] = (byte)nameBytes.Length;
        Array.Copy(nameBytes, 0, nameTable, 1, nameBytes.Length);

        // File tree (BFS): node 0 = root directory, node 1 = the file
        byte[] fileTree = new byte[FileDirectoryEntry.Size * 2];
        BinaryPrimitives.WriteUInt32BigEndian(fileTree.AsSpan(0), 0x7FFFFFFF); // Root: sentinel name, directory
        BinaryPrimitives.WriteUInt32BigEndian(fileTree.AsSpan(4), 1); // Root: first child index
        BinaryPrimitives.WriteUInt32BigEndian(fileTree.AsSpan(8), 1); // Root: child count
        BinaryPrimitives.WriteUInt32BigEndian(fileTree.AsSpan(16), 0x80000000); // File: name offset 0, type flag
        BinaryPrimitives.WriteUInt32BigEndian(fileTree.AsSpan(20), 0); // File: data offset low
        BinaryPrimitives.WriteUInt32BigEndian(fileTree.AsSpan(24), (uint)payload.Length); // File: size low
        BinaryPrimitives.WriteUInt32BigEndian(fileTree.AsSpan(28), 0); // File: offset/size high

        // Assemble sections
        int recordsOffset = dataBlock.Length;
        int namesOffset = recordsOffset + offsetRecords.Length;
        int treeOffset = namesOffset + nameTable.Length;
        int footerOffset = treeOffset + fileTree.Length;

        byte[] zar = new byte[footerOffset + ZarFooter.Size];
        Array.Copy(dataBlock, 0, zar, 0, dataBlock.Length);
        Array.Copy(offsetRecords, 0, zar, recordsOffset, offsetRecords.Length);
        Array.Copy(nameTable, 0, zar, namesOffset, nameTable.Length);
        Array.Copy(fileTree, 0, zar, treeOffset, fileTree.Length);

        // Footer
        WriteSectionInfo(zar, footerOffset + 0, 0, (ulong)dataBlock.Length);
        WriteSectionInfo(zar, footerOffset + 16, (ulong)recordsOffset, (ulong)offsetRecords.Length);
        WriteSectionInfo(zar, footerOffset + 32, (ulong)namesOffset, (ulong)nameTable.Length);
        WriteSectionInfo(zar, footerOffset + 48, (ulong)treeOffset, (ulong)fileTree.Length);
        WriteSectionInfo(zar, footerOffset + 64, 0, 0); // Meta directory (unused)
        WriteSectionInfo(zar, footerOffset + 80, 0, 0); // Metadata (unused)
        BinaryPrimitives.WriteUInt64BigEndian(zar.AsSpan(footerOffset + 128), (ulong)zar.Length); // Total size
        BinaryPrimitives.WriteUInt32BigEndian(zar.AsSpan(footerOffset + 136), ZarFooter.ExpectedVersion);
        BinaryPrimitives.WriteUInt32BigEndian(zar.AsSpan(footerOffset + 140), ZarFooter.ExpectedMagic);

        return zar;
    }

    private static void WriteSectionInfo(byte[] data, int offset, ulong sectionOffset, ulong size)
    {
        BinaryPrimitives.WriteUInt64BigEndian(data.AsSpan(offset), sectionOffset);
        BinaryPrimitives.WriteUInt64BigEndian(data.AsSpan(offset + 8), size);
    }
}