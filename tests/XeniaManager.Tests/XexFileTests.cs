using System.Reflection;
using XeniaManager.Core.Files;
using XeniaManager.Core.Logging;

namespace XeniaManager.Tests;

/// <summary>
/// Unit tests for the XexFile class.
/// Tests cover loading, parsing, validation, and property access for XEX (Xbox Executable) files.
/// </summary>
[TestFixture]
public class XexFileTests
{
    private string _assetsFolder = string.Empty;
    private string _testXexPath = string.Empty;

    /// <summary>
    /// Sets up the test environment before each test.
    /// Initializes paths to the assets folder and test XEX file.
    /// </summary>
    [SetUp]
    public void Setup()
    {
        // Get the path to the Assets directory
        string assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        _assetsFolder = Path.Combine(assemblyLocation, "Assets");

        // Path to test XEX file (if present)
        _testXexPath = Path.Combine(_assetsFolder, "test.xex"); // Replace it with the proper path
    }

    /// <summary>
    /// Tests that loading a non-existent file throws FileNotFoundException.
    /// </summary>
    [Test]
    public void Load_NonexistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        string nonexistentPath = Path.Combine(_assetsFolder, "nonexistent.xex");

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => XexFile.Load(nonexistentPath));
    }

    /// <summary>
    /// Tests that loading a local XEX file parses successfully and extracts valid TitleID and MediaID.
    /// </summary>
    [Test]
    public void Load_LocalXexFile_ParsesSuccessfully()
    {
        // Arrange - Skip if the test file is not present
        Assume.That(File.Exists(_testXexPath), Is.True, $"Test XEX file not found at {_testXexPath}. Place a test.xex file in the Assets folder to run this test.");

        // Act
        XexFile xex = XexFile.Load(_testXexPath);

        // Assert
        Assert.That(xex, Is.Not.Null);
        Assert.That(xex.IsValid, Is.True, $"XEX parsing failed: {xex.ValidationError}");

        // Verify parsed structures are populated
        Assert.That(xex.Header.Magic, Is.Not.Null, "Header magic should not be null");
        Assert.That(xex.SecurityInfo.ImageSize, Is.GreaterThan(0), "SecurityInfo should have valid image size");
        Assert.That(xex.Execution, Is.Not.Null, "Execution should be parsed");

        // Verify string properties
        Assert.That(xex.TitleId, Is.Not.Null.And.Not.Empty, "TitleID should not be null or empty");
        Assert.That(xex.MediaId, Is.Not.Null.And.Not.Empty, "MediaID should not be null or empty");

        // Verify execution info values
        Assert.That(xex.Execution.Value.TitleId, Is.GreaterThan(0), "TitleId should be non-zero");
        Assert.That(xex.Execution.Value.MediaId, Is.GreaterThan(0), "MediaId should be non-zero");

        // Log the parsed values for verification
        Logger.Info<XexFileTests>($"TitleID: {xex.TitleId} (0x{xex.Execution.Value.TitleId:X8})");
        Logger.Info<XexFileTests>($"MediaID: {xex.MediaId} (0x{xex.Execution.Value.MediaId:X8})");
    }

    /// <summary>
    /// Tests that loading a file with invalid magic bytes returns an invalid XexFile with a validation error.
    /// </summary>
    [Test]
    public void Load_InvalidMagic_ReturnsInvalidXexFile()
    {
        // Arrange - Create a file with invalid magic
        string tempPath = Path.Combine(Path.GetTempPath(), $"test_invalid_{Guid.NewGuid()}.xex");
        try
        {
            File.WriteAllBytes(tempPath, [0x00, 0x01, 0x02, 0x03, 0x04, 0x05]);

            // Act
            XexFile xex = XexFile.Load(tempPath);

            // Assert
            Assert.That(xex, Is.Not.Null);
            Assert.That(xex.IsValid, Is.False);
            Assert.That(xex.ValidationError, Is.Not.Null.And.Not.Empty);
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

    /// <summary>
    /// Tests that parsing data that is too short for a XEX header returns an invalid XexFile.
    /// </summary>
    [Test]
    public void FromBytes_TooShortData_ReturnsInvalidXexFile()
    {
        // Arrange - Data too short for XEX header
        byte[] tooShortData = [0x58, 0x45, 0x58, 0x32, 0x00, 0x01]; // Only 6 bytes

        // Act
        XexFile xex = XexFile.FromBytes(tooShortData);

        // Assert
        Assert.That(xex, Is.Not.Null);
        Assert.That(xex.IsValid, Is.False);
        Assert.That(xex.ValidationError, Is.Not.Null.And.Not.Empty);
    }

    /// <summary>
    /// Tests that parsing valid XEX byte data extracts correct TitleID and MediaID.
    /// </summary>
    [Test]
    public void FromBytes_ValidXexData_ParsesSuccessfully()
    {
        // Arrange - Skip if the test file is not present
        Assume.That(File.Exists(_testXexPath), Is.True, $"Test XEX file not found at {_testXexPath}. Place a test.xex file in the Assets folder to run this test.");

        byte[] xexData = File.ReadAllBytes(_testXexPath);

        // Act
        XexFile xex = XexFile.FromBytes(xexData);

        // Assert
        Assert.That(xex, Is.Not.Null);
        Assert.That(xex.IsValid, Is.True, $"XEX parsing failed: {xex.ValidationError}");

        // Verify parsed structures are populated
        Assert.That(xex.Header.Magic, Is.Not.Null, "Header magic should not be null");
        Assert.That(xex.SecurityInfo.ImageSize, Is.GreaterThan(0), "SecurityInfo should have valid image size");
        Assert.That(xex.Execution, Is.Not.Null, "Execution should be parsed");

        // Verify string properties
        Assert.That(xex.TitleId, Is.Not.Null.And.Not.Empty, "TitleID should not be null or empty");
        Assert.That(xex.MediaId, Is.Not.Null.And.Not.Empty, "MediaID should not be null or empty");

        // Verify execution info values
        Assert.That(xex.Execution.Value.TitleId, Is.GreaterThan(0), "TitleId should be non-zero");
        Assert.That(xex.Execution.Value.MediaId, Is.GreaterThan(0), "MediaId should be non-zero");

        // Log the parsed values for verification
        Logger.Info<XexFileTests>($"TitleID: {xex.TitleId} (0x{xex.Execution.Value.TitleId:X8})");
        Logger.Info<XexFileTests>($"MediaID: {xex.MediaId} (0x{xex.Execution.Value.MediaId:X8})");
    }

    /// <summary>
    /// Tests that all properties are correctly populated when loading a local XEX file.
    /// </summary>
    [Test]
    public void Load_LocalXexFile_HasExpectedProperties()
    {
        // Arrange - Skip if the test file is not present
        Assume.That(File.Exists(_testXexPath), Is.True, $"Test XEX file not found at {_testXexPath}. Place a test.xex file in the Assets folder to run this test.");

        // Act
        XexFile xex = XexFile.Load(_testXexPath);

        // Assert
        Assert.That(xex.IsValid, Is.True, $"XEX parsing failed: {xex.ValidationError}");

        // Verify parsed structures are populated
        Assert.That(xex.Header.Magic, Is.Not.Null, "Header magic should not be null");
        Assert.That(xex.SecurityInfo.ImageSize, Is.GreaterThan(0), "SecurityInfo should have valid image size");
        Assert.That(xex.Execution, Is.Not.Null, "Execution should be parsed");

        // Verify header properties
        Assert.That(GetString(xex.Header.Magic), Is.EqualTo("XEX2"), "Header magic should be XEX2");
        Assert.That(xex.Header.HeaderDirectoryEntryCount, Is.GreaterThan(0), "Header should have directory entries");

        // Verify security info properties
        Assert.That(xex.SecurityInfo.ImageSize, Is.GreaterThan(0), "Image size should be non-zero");
        Assert.That(xex.SecurityInfo.ImageInfo.GameRegion, Is.GreaterThanOrEqualTo(0), "Game region should be valid");

        // Verify string properties are accessible and contain valid hex strings
        Assert.That(xex.TitleId, Is.Not.Null.And.Not.Empty, "TitleID should not be null or empty");
        Assert.That(xex.MediaId, Is.Not.Null.And.Not.Empty, "MediaID should not be null or empty");

        // Verify the format is correct (8-character hex string)
        Assert.That(xex.TitleId.Length, Is.EqualTo(8), "TitleID should be 8 characters");
        Assert.That(xex.MediaId.Length, Is.EqualTo(8), "MediaID should be 8 characters");

        // Verify execution info properties
        Assert.That(xex.Execution.Value.TitleId, Is.GreaterThanOrEqualTo(0), "TitleId should be non-negative");
        Assert.That(xex.Execution.Value.MediaId, Is.GreaterThanOrEqualTo(0), "MediaId should be non-negative");

        // Verify consistency between string and uint value properties
        Assert.That(xex.TitleId, Is.EqualTo($"{xex.Execution.Value.TitleId:X8}"), "TitleId should match Execution.TitleId formatted as hex");
        Assert.That(xex.MediaId, Is.EqualTo($"{xex.Execution.Value.MediaId:X8}"), "MediaId should match Execution.MediaId formatted as hex");

        // Output for debugging
        Logger.Info<XexFileTests>($"TitleID: {xex.TitleId} (0x{xex.Execution.Value.TitleId:X8})");
        Logger.Info<XexFileTests>($"MediaID: {xex.MediaId} (0x{xex.Execution.Value.MediaId:X8})");
        Logger.Info<XexFileTests>($"Image Size: 0x{xex.SecurityInfo.ImageSize:X8}");
        Logger.Info<XexFileTests>($"Game Region: 0x{xex.SecurityInfo.ImageInfo.GameRegion:X8}");
    }

    /// <summary>
    /// Helper method to convert byte array to ASCII string (mirrors XexFile.GetString).
    /// </summary>
    private static string GetString(byte[] bytes)
    {
        return System.Text.Encoding.ASCII.GetString(bytes).Trim('\0');
    }
}