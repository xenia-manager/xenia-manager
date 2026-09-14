using System.Reflection;
using XeniaManager.Files;
using XeniaManager.Logging;
using XeniaManager.Files.Models.Zar;

namespace XeniaManager.Tests.Files;

[TestFixture]
public class ZarFileTests
{
    private string _assetsFolder = string.Empty;
    private string _testZarPath = string.Empty;

    [SetUp]
    public void Setup()
    {
        string assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        _assetsFolder = Path.Combine(assemblyLocation, "Assets");
    }

    /// <summary>
    /// Tests that loading a non-existent file throws FileNotFoundException.
    /// </summary>
    [Test]
    public void Load_NonexistentFile_ThrowsFileNotFoundException()
    {
        string nonexistentPath = Path.Combine(_assetsFolder, "nonexistent.zar");
        Assert.Throws<FileNotFoundException>(() => ZarFile.Load(nonexistentPath));
    }

    /// <summary>
    /// Tests that loading a file with invalid data returns an invalid ZarFile with a validation error.
    /// </summary>
    [Test]
    public void Load_InvalidZarFile_ReturnsInvalidZarFile()
    {
        string tempPath = Path.Combine(Path.GetTempPath(), $"test_invalid_{Guid.NewGuid()}.zar");
        try
        {
            File.WriteAllBytes(tempPath, [0x00, 0x01, 0x02, 0x03, 0x04, 0x05]);

            using ZarFile zar = ZarFile.Load(tempPath);

            Assert.That(zar, Is.Not.Null);
            Assert.That(zar.IsValid, Is.False);
            Assert.That(zar.ValidationError, Is.Not.Null.And.Not.Empty);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    /// <summary>
    /// Tests that FromBytes returns an invalid ZarFile since the format requires stream-based loading.
    /// </summary>
    [Test]
    public void FromBytes_IsNotSupported_ReturnsInvalidZarFile()
    {
        byte[] dummyData = [0x00, 0x01, 0x02, 0x03];

        using ZarFile zar = ZarFile.FromBytes(dummyData);

        Assert.That(zar, Is.Not.Null);
        Assert.That(zar.IsValid, Is.False);
        Assert.That(zar.ValidationError, Does.Contain("FromBytes is not supported"));
    }

    /// <summary>
    /// Tests that IsZarArchive returns false for files smaller than the footer size.
    /// </summary>
    [Test]
    public void IsZarArchive_TooSmallFile_ReturnsFalse()
    {
        string tempPath = Path.Combine(Path.GetTempPath(), $"test_small_{Guid.NewGuid()}.zar");
        try
        {
            File.WriteAllBytes(tempPath, new byte[100]);
            Assert.That(ZarFile.IsZarArchive(tempPath), Is.False);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    /// <summary>
    /// Tests that IsZarArchive returns false for files with no valid ZAR footer.
    /// </summary>
    [Test]
    public void IsZarArchive_NoFooter_ReturnsFalse()
    {
        string tempPath = Path.Combine(Path.GetTempPath(), $"test_nofooter_{Guid.NewGuid()}.zar");
        try
        {
            byte[] data = new byte[200];
            File.WriteAllBytes(tempPath, data);
            Assert.That(ZarFile.IsZarArchive(tempPath), Is.False);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    /// <summary>
    /// Tests that loading a valid ZAR file parses successfully and populates basic properties.
    /// </summary>
    [Test]
    public void Load_ValidZarFile_ParsesSuccessfully()
    {
        Assume.That(File.Exists(_testZarPath), Is.True,
            $"Test ZAR file not found at {_testZarPath}. Update the path in Setup to run this test.");

        using ZarFile zar = ZarFile.Load(_testZarPath);

        Assert.That(zar, Is.Not.Null);
        Assert.That(zar.IsValid, Is.True, $"ZAR parsing failed: {zar.ValidationError}");

        Logger.Info<ZarFileTests>($"FilePath: {zar.FilePath}");
    }

    /// <summary>
    /// Tests that loading a valid ZAR file extracts and parses default.xex successfully.
    /// </summary>
    [Test]
    public void Load_ValidZarFile_ExtractsDefaultXex()
    {
        Assume.That(File.Exists(_testZarPath), Is.True,
            $"Test ZAR file not found at {_testZarPath}. Update the path in Setup to run this test.");

        using ZarFile zar = ZarFile.Load(_testZarPath);

        Assert.That(zar.IsValid, Is.True, $"ZAR parsing failed: {zar.ValidationError}");
        Assert.That(zar.XexFile, Is.Not.Null, "ZAR archive should contain default.xex");
        Assert.That(zar.XexFile.IsValid, Is.True, "default.xex should be valid");
        Assert.That(zar.XexFile.Execution.HasValue, Is.True);
        Assert.That(zar.XexFile.Execution.Value.TitleId, Is.GreaterThan(0));
        Assert.That(zar.XexFile.Execution.Value.MediaId, Is.GreaterThan(0));

        Logger.Info<ZarFileTests>($"TitleID: {zar.XexFile.TitleId}");
        Logger.Info<ZarFileTests>($"MediaID: {zar.XexFile.MediaId}");
    }

    /// <summary>
    /// Tests that Lookup returns a valid entry for an existing file in the archive.
    /// </summary>
    [Test]
    public void Lookup_ExistingFile_ReturnsEntry()
    {
        Assume.That(File.Exists(_testZarPath), Is.True);

        using ZarFile zar = ZarFile.Load(_testZarPath);
        Assume.That(zar.IsValid, Is.True);

        FileDirectoryEntry? entry = zar.Lookup("default.xex");

        Assert.That(entry, Is.Not.Null);
        Assert.That(entry.IsFile, Is.True);
        Assert.That(entry.GetFileSize(), Is.GreaterThan(0));
    }

    /// <summary>
    /// Tests that Lookup returns null for a non-existent file path.
    /// </summary>
    [Test]
    public void Lookup_NonexistentFile_ReturnsNull()
    {
        Assume.That(File.Exists(_testZarPath), Is.True);

        using ZarFile zar = ZarFile.Load(_testZarPath);
        Assume.That(zar.IsValid, Is.True);

        Assert.That(zar.Lookup("nonexistent.bin"), Is.Null);
    }

    /// <summary>
    /// Tests that ReadFile returns data for an existing file in the archive.
    /// </summary>
    [Test]
    public void ReadFile_ExistingFile_ReturnsData()
    {
        Assume.That(File.Exists(_testZarPath), Is.True);

        using ZarFile zar = ZarFile.Load(_testZarPath);
        Assume.That(zar.IsValid, Is.True);

        byte[]? data = zar.ReadFile("default.xex");

        Assert.That(data, Is.Not.Null);
        Assert.That(data.Length, Is.GreaterThan(0));
    }

    /// <summary>
    /// Tests that ReadFile returns null for a non-existent file path.
    /// </summary>
    [Test]
    public void ReadFile_NonexistentFile_ReturnsNull()
    {
        Assume.That(File.Exists(_testZarPath), Is.True);

        using ZarFile zar = ZarFile.Load(_testZarPath);
        Assume.That(zar.IsValid, Is.True);

        Assert.That(zar.ReadFile("nonexistent.bin"), Is.Null);
    }

    /// <summary>
    /// Tests that ListDirectory returns entries for the root directory of the archive.
    /// </summary>
    [Test]
    public void ListDirectory_Root_ReturnsEntries()
    {
        Assume.That(File.Exists(_testZarPath), Is.True);

        using ZarFile zar = ZarFile.Load(_testZarPath);
        Assume.That(zar.IsValid, Is.True);

        List<DirEntry>? entries = zar.ListDirectory("");

        Assert.That(entries, Is.Not.Null);
        Assert.That(entries.Count, Is.GreaterThan(0));
    }

    private static string WriteFooterTestFile(Action<byte[]> patchFooter)
    {
        string tempPath = Path.Combine(Path.GetTempPath(), $"test_footer_{Guid.NewGuid()}.zar");
        byte[] body = new byte[256];
        byte[] footer = new byte[ZarFooter.Size];
        patchFooter(footer);
        // totalSize, version, magic
        System.Buffers.Binary.BinaryPrimitives.WriteUInt64BigEndian(footer.AsSpan(128), (ulong)(body.Length + footer.Length));
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(footer.AsSpan(136), ZarFooter.ExpectedVersion);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(footer.AsSpan(140), ZarFooter.ExpectedMagic);
        File.WriteAllBytes(tempPath, [.. body, .. footer]);
        return tempPath;
    }

    /// <summary>
    /// Tests that a footer section outside the file bounds fails validation without throwing.
    /// </summary>
    [Test]
    public void Load_SectionOutsideFile_ReturnsInvalidZarFile()
    {
        string tempPath = WriteFooterTestFile(footer =>
        {
            // OffsetRecords points past the end of the file
            System.Buffers.Binary.BinaryPrimitives.WriteUInt64BigEndian(footer.AsSpan(16), 1000);
            System.Buffers.Binary.BinaryPrimitives.WriteUInt64BigEndian(footer.AsSpan(24), 40);
        });
        try
        {
            using ZarFile zar = ZarFile.Load(tempPath);

            Assert.That(zar.IsValid, Is.False);
            Assert.That(zar.ValidationError, Does.Contain("outside file bounds"));
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    /// <summary>
    /// Tests that an archive without offset records or file tree fails validation without throwing.
    /// </summary>
    [Test]
    public void Load_EmptySections_ReturnsInvalidZarFile()
    {
        string tempPath = WriteFooterTestFile(_ => { });
        try
        {
            using ZarFile zar = ZarFile.Load(tempPath);

            Assert.That(zar.IsValid, Is.False);
            Assert.That(zar.ValidationError, Does.Contain("no offset records or file tree"));
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    /// <summary>
    /// Writes a minimal ZAR archive with the given files stored raw (uncompressed 64 KiB blocks).
    /// Single offset record, so total payload must fit in 16 blocks (1 MiB).
    /// </summary>
    private static string WriteStoredZar((string Name, byte[] Content)[] files)
    {
        List<byte> blob = new List<byte>();
        List<(string Name, ulong Offset, ulong Size)> entries = new List<(string, ulong, ulong)>();
        foreach ((string name, byte[] content) in files)
        {
            entries.Add((name, (ulong)blob.Count, (ulong)content.Length));
            blob.AddRange(content);
        }

        int blockCount = (blob.Count + 65535) / 65536;
        Assert.That(blockCount, Is.GreaterThan(0).And.LessThanOrEqualTo(16));
        while (blob.Count < blockCount * 65536)
        {
            blob.Add(0);
        }

        byte[] records = new byte[CompressionOffsetRecord.Size];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt64BigEndian(records.AsSpan(0), 0);
        for (int i = 0; i < blockCount; i++)
        {
            System.Buffers.Binary.BinaryPrimitives.WriteUInt16BigEndian(records.AsSpan(8 + i * 2), 65535);
        }

        List<byte> names = new List<byte>();
        Dictionary<string, uint> nameOffsets = new Dictionary<string, uint>();
        foreach ((string name, _, _) in entries)
        {
            nameOffsets[name] = (uint)names.Count;
            byte[] nameBytes = System.Text.Encoding.UTF8.GetBytes(name);
            names.Add((byte)nameBytes.Length);
            names.AddRange(nameBytes);
        }

        byte[] tree = new byte[FileDirectoryEntry.Size * (1 + entries.Count)];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(tree.AsSpan(0), 0x7FFFFFFF);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(tree.AsSpan(4), 1);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(tree.AsSpan(8), (uint)entries.Count);
        for (int i = 0; i < entries.Count; i++)
        {
            int off = FileDirectoryEntry.Size * (i + 1);
            System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(tree.AsSpan(off), nameOffsets[entries[i].Name] | 0x80000000);
            System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(tree.AsSpan(off + 4), (uint)entries[i].Offset);
            System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(tree.AsSpan(off + 8), (uint)entries[i].Size);
        }

        byte[] blobBytes = blob.ToArray();
        byte[] nameBytes2 = names.ToArray();
        ulong dataSize = (ulong)blobBytes.Length;
        ulong recordsOffset = dataSize;
        ulong namesOffset = recordsOffset + (ulong)records.Length;
        ulong treeOffset = namesOffset + (ulong)nameBytes2.Length;

        byte[] footer = new byte[ZarFooter.Size];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt64BigEndian(footer.AsSpan(0), 0);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt64BigEndian(footer.AsSpan(8), dataSize);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt64BigEndian(footer.AsSpan(16), recordsOffset);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt64BigEndian(footer.AsSpan(24), (ulong)records.Length);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt64BigEndian(footer.AsSpan(32), namesOffset);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt64BigEndian(footer.AsSpan(40), (ulong)nameBytes2.Length);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt64BigEndian(footer.AsSpan(48), treeOffset);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt64BigEndian(footer.AsSpan(56), (ulong)tree.Length);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt64BigEndian(footer.AsSpan(128),
            dataSize + (ulong)records.Length + (ulong)nameBytes2.Length + (ulong)tree.Length + ZarFooter.Size);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(footer.AsSpan(136), ZarFooter.ExpectedVersion);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(footer.AsSpan(140), ZarFooter.ExpectedMagic);

        string tempPath = Path.Combine(Path.GetTempPath(), $"test_stored_{Guid.NewGuid()}.zar");
        using (FileStream fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write))
        {
            fs.Write(blobBytes, 0, blobBytes.Length);
            fs.Write(records, 0, records.Length);
            fs.Write(nameBytes2, 0, nameBytes2.Length);
            fs.Write(tree, 0, tree.Length);
            fs.Write(footer, 0, footer.Length);
        }

        return tempPath;
    }

    /// <summary>
    /// Tests that a file spanning multiple stored blocks round-trips through the reused zstd context.
    /// </summary>
    [Test]
    public void ReadFile_MultiBlockStored_RoundTrips()
    {
        byte[] content = new byte[100000];
        for (int i = 0; i < content.Length; i++)
        {
            content[i] = (byte)(i % 251);
        }

        string tempPath = WriteStoredZar([("hello.bin", content)]);
        try
        {
            using ZarFile zar = ZarFile.Load(tempPath);

            Assert.That(zar.IsValid, Is.True, $"ZAR parsing failed: {zar.ValidationError}");
            Assert.That(zar.ReadFile("hello.bin"), Is.EqualTo(content));

            FileDirectoryEntry entry = zar.Lookup("hello.bin")!;
            Assert.That(zar.ReadFile(entry, 65500, 100), Is.EqualTo(content[65500..65600]));
            Assert.That(zar.ReadFile(entry, (ulong)content.Length, 10), Is.Empty);
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    /// <summary>
    /// Tests that the alternative-XEX search reads candidates directly without a second tree lookup.
    /// </summary>
    [Test]
    public void TryGetSpaFile_AltXexSearch_InvalidXexReturnsFalse()
    {
        byte[] notXex = new byte[32];
        notXex[0] = 0x58;
        string tempPath = WriteStoredZar([("other.xex", notXex)]);
        try
        {
            using ZarFile zar = ZarFile.Load(tempPath);

            Assert.That(zar.IsValid, Is.True, $"ZAR parsing failed: {zar.ValidationError}");
            Assert.That(zar.XexFile, Is.Null);
            Assert.That(zar.TryGetSpaFile(out SpaFile? spa), Is.False);
            Assert.That(spa, Is.Null);
            Assert.That(zar.TryGetIcon(), Is.Null);
        }
        finally
        {
            File.Delete(tempPath);
        }
    }
}