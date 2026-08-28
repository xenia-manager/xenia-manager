using System.Buffers.Binary;
using System.Text;
using XeniaManager.Files;

namespace XeniaManager.Tests;

[TestFixture]
public class StfsFilePathTraversalTests
{
    private string _testOutputDirectory = string.Empty;
    private readonly List<string> _escapedPaths = [];

    [SetUp]
    public void Setup()
    {
        _testOutputDirectory = Path.Combine(Path.GetTempPath(), $"XeniaManagerStfsTest_{Guid.NewGuid():N}");
        _escapedPaths.Clear();
    }

    [TearDown]
    public void Teardown()
    {
        if (Directory.Exists(_testOutputDirectory))
        {
            Directory.Delete(_testOutputDirectory, true);
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
    public void ExtractToXeniaStructure_EntryWithDotDotName_DoesNotWriteOutsideOutputDirectory()
    {
        // Arrange
        const string fileName = @"..\..\..\..\..\PWNED.txt";
        byte[] payload = Encoding.ASCII.GetBytes("@echo off\r\necho PWNED\r\n");
        byte[] package = BuildPackage(fileName, payload);

        using StfsFile stfs = StfsFile.FromBytes(package);
        Assert.That(stfs.FileEntries, Has.Count.EqualTo(1));
        Assert.That(stfs.FileEntries[0].FileName, Is.EqualTo(fileName));

        string packageFolderPath = Path.Combine(_testOutputDirectory, "4D5309C9", "00000002", "unknown");
        string escapedPath = Path.GetFullPath(Path.Combine(packageFolderPath, fileName));
        _escapedPaths.Add(escapedPath);
        stfs.ExtractToXeniaStructure(_testOutputDirectory);

        // Assert - the traversal file must not be written outside the package folder
        Assert.That(File.Exists(escapedPath), Is.False);
    }

    [Test]
    public void ExtractToXeniaStructure_AbsolutePathName_DoesNotWriteOutsideOutputDirectory()
    {
        // Arrange - a rooted/drive-qualified name makes Path.Combine discard the package folder.
        // C:\Users\Public is user-writable so a regression would land the file there.
        string escapedPath = @"C:\Users\Public\XM_Stfs_Escaped.txt";
        if (File.Exists(escapedPath))
        {
            File.Delete(escapedPath);
        }
        _escapedPaths.Add(escapedPath);
        byte[] payload = Encoding.ASCII.GetBytes("@echo off\r\necho PWNED\r\n");
        byte[] package = BuildPackage(escapedPath, payload);

        using StfsFile stfs = StfsFile.FromBytes(package);

        // Act
        stfs.ExtractToXeniaStructure(_testOutputDirectory);

        // Assert - no file may land at the attacker-chosen absolute path
        Assert.That(File.Exists(escapedPath), Is.False);
    }

    [Test]
    public void ExtractToXeniaStructure_DirectoryChainWithRootedParent_DoesNotWriteOutsideOutputDirectory()
    {
        // Arrange - the advisory's directory-chain shape:
        // dir 0 is rooted (drive-qualified), children are relative so the chain survives
        string chainBase = @"C:\Users\Public\XM_Esc";
        string escapedFile = Path.Combine(chainBase, @"Microsoft\Windows\Start Menu\Programs\Startup\PWNED.cmd");
        if (Directory.Exists(chainBase))
        {
            Directory.Delete(chainBase, true);
        }
        _escapedPaths.Add(escapedFile);
        byte[] payload = Encoding.ASCII.GetBytes("@echo off\r\necho PWNED\r\n");
        byte[] package = BuildChainPackage(chainBase, payload);

        using StfsFile stfs = StfsFile.FromBytes(package);
        Assert.That(stfs.FileEntries, Has.Count.EqualTo(3));

        // Act
        stfs.ExtractToXeniaStructure(_testOutputDirectory);

        // Assert - the payload must not land at the attacker-chosen Startup path
        Assert.That(File.Exists(escapedFile), Is.False);
    }

    [Test]
    public void ExtractToXeniaStructure_BenignEntry_ExtractsInsidePackageFolder()
    {
        // Arrange
        const string fileName = "test.bin";
        byte[] payload = Encoding.ASCII.GetBytes("benign payload data");
        byte[] package = BuildPackage(fileName, payload);

        using StfsFile stfs = StfsFile.FromBytes(package);
        Assert.That(stfs.FileEntries, Has.Count.EqualTo(1));

        string packageFolderPath = Path.Combine(_testOutputDirectory, "4D5309C9", "00000002", "unknown");
        string outputPath = Path.Combine(packageFolderPath, fileName);

        // Act
        stfs.ExtractToXeniaStructure(_testOutputDirectory);

        // Assert - the benign file is extracted inside the package folder with exact content
        Assert.That(File.Exists(outputPath), Is.True);
        Assert.That(File.ReadAllBytes(outputPath), Is.EqualTo(payload));
    }

    /// <summary>
    /// Builds a minimal valid STFS package (CON, 16 KiB) with a single file entry.
    /// Layout: header with HeaderSize 0x1000, read-only volume descriptor (file table
    /// block 0 at 0x2000), payload block 1 at 0x3000.
    /// </summary>
    private static byte[] BuildPackage(string fileName, byte[] payload)
    {
        byte[] package = new byte[0x4000];

        // Magic
        Encoding.ASCII.GetBytes("CON ").CopyTo(package, 0x000);

        // Metadata header
        BinaryPrimitives.WriteInt32BigEndian(package.AsSpan(0x340), 0x1000); // HeaderSize
        BinaryPrimitives.WriteUInt32BigEndian(package.AsSpan(0x344), 0x00000002); // ContentType: MarketplaceContent
        BinaryPrimitives.WriteInt32BigEndian(package.AsSpan(0x348), 1); // MetadataVersion
        BinaryPrimitives.WriteInt32BigEndian(package.AsSpan(0x360), 0x4D5309C9); // TitleID

        // Display name (UTF-16 BE) at 0x411
        Encoding.BigEndianUnicode.GetBytes("Test Content").CopyTo(package, 0x411);

        // Volume descriptor at 0x379 (STFS variant)
        package[0x379] = 0x24; // Descriptor size
        package[0x37B] = 0x01; // Flags: read-only format (1 block per hash table)
        BinaryPrimitives.WriteInt16LittleEndian(package.AsSpan(0x37C), 1); // File table block count
        package[0x37E] = 0; // File table block number (int24 LE) = 0
        package[0x37F] = 0;
        package[0x380] = 0;

        // File entry at 0x2000 (file table block 0)
        byte[] nameBytes = Encoding.ASCII.GetBytes(fileName);
        Array.Copy(nameBytes, 0, package, 0x2000, nameBytes.Length);
        package[0x2028] = (byte)(0x40 | nameBytes.Length); // Flags: consecutive blocks + name length
        package[0x202F] = 1; // Starting block (int24 LE) = 1
        BinaryPrimitives.WriteInt16BigEndian(package.AsSpan(0x2032), -1); // PathIndicator: root
        BinaryPrimitives.WriteInt32BigEndian(package.AsSpan(0x2034), payload.Length); // File size

        // Payload at 0x3000 (block 1)
        Array.Copy(payload, 0, package, 0x3000, payload.Length);

        return package;
    }

    /// <summary>
    /// Builds a minimal STFS package with a directory chain whose first entry is a
    /// rooted (drive-qualified) directory name, matching the advisory's PoC shape.
    /// Entries: dir(chainBase\AppData\Roaming) -&gt; dir(Microsoft\Windows\Start Menu\Programs)
    /// -&gt; file(Startup\PWNED.cmd).
    /// </summary>
    private static byte[] BuildChainPackage(string chainBase, byte[] payload)
    {
        byte[] package = new byte[0x4000];

        // Magic
        Encoding.ASCII.GetBytes("CON ").CopyTo(package, 0x000);

        // Metadata header
        BinaryPrimitives.WriteInt32BigEndian(package.AsSpan(0x340), 0x1000); // HeaderSize
        BinaryPrimitives.WriteUInt32BigEndian(package.AsSpan(0x344), 0x00000002); // ContentType: MarketplaceContent
        BinaryPrimitives.WriteInt32BigEndian(package.AsSpan(0x348), 1); // MetadataVersion
        BinaryPrimitives.WriteInt32BigEndian(package.AsSpan(0x360), 0x4D5309C9); // TitleID

        // Display name (UTF-16 BE) at 0x411
        Encoding.BigEndianUnicode.GetBytes("Test Content").CopyTo(package, 0x411);

        // Volume descriptor at 0x379 (STFS variant)
        package[0x379] = 0x24; // Descriptor size
        package[0x37B] = 0x01; // Flags: read-only format (1 block per hash table)
        BinaryPrimitives.WriteInt16LittleEndian(package.AsSpan(0x37C), 1); // File table block count
        package[0x37E] = 0; // File table block number (int24 LE) = 0
        package[0x37F] = 0;
        package[0x380] = 0;

        // File table entries at 0x2000 (file table block 0)
        WriteFileTableEntry(package, 0x2000, $@"{chainBase}\AppData\Roaming", isDirectory: true, parentIndex: -1);
        WriteFileTableEntry(package, 0x2040, @"Microsoft\Windows\Start Menu\Programs", isDirectory: true, parentIndex: 0);
        WriteFileTableEntry(package, 0x2080, @"Startup\PWNED.cmd", isDirectory: false, parentIndex: 1, startingBlock: 1, fileSize: payload.Length);

        // Payload at 0x3000 (block 1)
        Array.Copy(payload, 0, package, 0x3000, payload.Length);

        return package;
    }

    private static void WriteFileTableEntry(byte[] package, int offset, string name, bool isDirectory, short parentIndex, int startingBlock = 0, int fileSize = 0)
    {
        byte[] nameBytes = Encoding.ASCII.GetBytes(name);
        Array.Copy(nameBytes, 0, package, offset, nameBytes.Length);
        package[offset + 0x28] = (byte)((isDirectory ? 0x80 : 0x40) | nameBytes.Length);
        package[offset + 0x2F] = (byte)(startingBlock & 0xFF);
        package[offset + 0x30] = (byte)((startingBlock >> 8) & 0xFF);
        package[offset + 0x31] = (byte)((startingBlock >> 16) & 0xFF);
        BinaryPrimitives.WriteInt16BigEndian(package.AsSpan(offset + 0x32), parentIndex);
        BinaryPrimitives.WriteInt32BigEndian(package.AsSpan(offset + 0x34), fileSize);
    }
}