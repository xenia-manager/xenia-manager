using System.Reflection;
using XeniaManager.Core.Files;
using XeniaManager.Core.Logging;

namespace XeniaManager.Tests;

[TestFixture]
public class IsoFileTests
{
    private string _assetsFolder = string.Empty;
    private string _testIsoPath = string.Empty;

    [SetUp]
    public void Setup()
    {
        // Get the path to the Assets directory
        string assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        _assetsFolder = Path.Combine(assemblyLocation, "Assets");

        // Path to test the ISO file (if present)
        _testIsoPath = Path.Combine(_assetsFolder, "test.iso"); // Replace it with the proper path
    }

    [Test]
    public void Load_NonexistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        string nonexistentPath = Path.Combine(_assetsFolder, "nonexistent.iso");

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => IsoFile.Load(nonexistentPath));
    }

    [Test]
    public void Load_LocalIsoFile_ParsesSuccessfully()
    {
        // Arrange - Skip if the test file is not present
        Assume.That(File.Exists(_testIsoPath), Is.True, $"Test ISO file not found at {_testIsoPath}. Place a test.iso file in the Assets folder to run this test.");

        // Act
        using IsoFile iso = IsoFile.Load(_testIsoPath);

        // Assert
        Assert.That(iso, Is.Not.Null);
        Assert.That(iso.IsValid, Is.True, $"ISO parsing failed: {iso.ValidationError}");
        Assert.That(iso.XexFile, Is.Not.Null);
        Assert.That(iso.XexFile.IsValid, Is.True);
        Assert.That(iso.XexFile.Execution.HasValue, Is.True);
        Assert.That(iso.XexFile.Execution.Value.TitleId, Is.GreaterThan(0), "TitleID should be non-zero");
        Assert.That(iso.XexFile.Execution.Value.MediaId, Is.GreaterThan(0), "MediaID should be non-zero");

        // Log the parsed values for verification
        Logger.Info<IsoFileTests>($"TitleID: {iso.XexFile.TitleId}");
        Logger.Info<IsoFileTests>($"MediaID: {iso.XexFile.MediaId}");
    }

    [Test]
    public void Load_InvalidIsoFile_ReturnsInvalidIsoFile()
    {
        // Arrange - Create a file with invalid data
        string tempPath = Path.Combine(Path.GetTempPath(), $"test_invalid_{Guid.NewGuid()}.iso");
        try
        {
            File.WriteAllBytes(tempPath, [0x00, 0x01, 0x02, 0x03, 0x04, 0x05]);

            // Act
            using IsoFile iso = IsoFile.Load(tempPath);

            // Assert
            Assert.That(iso, Is.Not.Null);
            Assert.That(iso.IsValid, Is.False);
            Assert.That(iso.ValidationError, Is.Not.Null.And.Not.Empty);
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    [Test]
    public void FromBytes_IsNotSupported_ReturnsInvalidIsoFile()
    {
        // Arrange - Some dummy data
        byte[] dummyData = [0x00, 0x01, 0x02, 0x03];

        // Act
        using IsoFile iso = IsoFile.FromBytes(dummyData);

        // Assert
        Assert.That(iso, Is.Not.Null);
        Assert.That(iso.IsValid, Is.False);
        Assert.That(iso.ValidationError, Does.Contain("FromBytes is not supported"));
    }

    [Test]
    public void Load_LocalIsoFile_HasExpectedProperties()
    {
        // Arrange - Skip if the test file is not present
        Assume.That(File.Exists(_testIsoPath), Is.True, $"Test ISO file not found at {_testIsoPath}. Place a test.iso file in the Assets folder to run this test.");

        // Act
        using IsoFile iso = IsoFile.Load(_testIsoPath);

        // Assert
        Assert.That(iso.IsValid, Is.True, $"ISO parsing failed: {iso.ValidationError}");

        // Verify properties are accessible
        Assert.That(iso.XexFile, Is.Not.Null);
        Assert.That(iso.XexFile.Execution.HasValue, Is.True);
        Assert.That(iso.XexFile.Execution.Value.TitleId, Is.GreaterThanOrEqualTo(0));
        Assert.That(iso.XexFile.Execution.Value.MediaId, Is.GreaterThanOrEqualTo(0));
        Assert.That(iso.FilePath, Is.Not.Null.And.Not.Empty);

        // Output for debugging
        Logger.Info<IsoFileTests>($"TitleID: {iso.XexFile.TitleId}");
        Logger.Info<IsoFileTests>($"MediaID: {iso.XexFile.MediaId}");
    }

    [Test]
    public void Load_ValidIsoFile_HasXgdInformation()
    {
        // Arrange - Skip if the test file is not present
        Assume.That(File.Exists(_testIsoPath), Is.True, $"Test ISO file not found at {_testIsoPath}. Place a test.iso file in the Assets folder to run this test.");

        // Act
        using IsoFile iso = IsoFile.Load(_testIsoPath);

        // Assert
        if (iso.IsValid)
        {
            Assert.That(iso.XgdInformation, Is.Not.Null);
            Assert.That(iso.XgdInformation.BaseSector, Is.GreaterThanOrEqualTo(0));
            Assert.That(iso.XgdInformation.RootDirSector, Is.GreaterThanOrEqualTo(0));
            Assert.That(iso.XgdInformation.RootDirSize, Is.GreaterThanOrEqualTo(0));
        }
    }
}