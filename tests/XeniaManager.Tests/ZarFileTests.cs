using System.Reflection;
using XeniaManager.Core.Files;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Models.Files.Zar;

namespace XeniaManager.Tests;

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
                File.Delete(tempPath);
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
                File.Delete(tempPath);
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
                File.Delete(tempPath);
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
}