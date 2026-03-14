using System.Reflection;
using XeniaManager.Core.Files;

namespace XeniaManager.Tests;

[TestFixture]
public class HeaderFileTests
{
    private string _testHeaderFile308Path = string.Empty;
    private string _testHeaderFile328Path = string.Empty;
    private string _testHeaderFile332Path = string.Empty;

    [SetUp]
    public void Setup()
    {
        // Assembly Location
        string assemblyLocation = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "Assets");

        _testHeaderFile308Path = Path.Combine(assemblyLocation, @"TestHeaderFile (308).header");
        _testHeaderFile328Path = Path.Combine(assemblyLocation, @"TestHeaderFile (328).header");
        _testHeaderFile332Path = Path.Combine(assemblyLocation, @"TestHeaderFile (332).header");
    }

    [Test]
    public void Load_ValidHeaderFile308_ReturnsHeaderFile()
    {
        // Skip if the test file doesn't exist
        if (!File.Exists(_testHeaderFile308Path))
        {
            Assert.Ignore("Header test file (308) not found");
            return;
        }

        // Act
        HeaderFile header = HeaderFile.Load(_testHeaderFile308Path);

        // Assert
        Assert.That(header, Is.Not.Null);
        Assert.That(header.HeaderSize, Is.EqualTo(308));
    }

    [Test]
    public void Load_ValidHeaderFile328_ReturnsHeaderFile()
    {
        // Skip if the test file doesn't exist
        if (!File.Exists(_testHeaderFile328Path))
        {
            Assert.Ignore("Header test file (328) not found");
            return;
        }

        // Act
        HeaderFile header = HeaderFile.Load(_testHeaderFile328Path);

        // Assert
        Assert.That(header, Is.Not.Null);
        Assert.That(header.HeaderSize, Is.EqualTo(328));
    }

    [Test]
    public void Load_ValidHeaderFile332_ReturnsHeaderFile()
    {
        // Skip if the test file doesn't exist
        if (!File.Exists(_testHeaderFile332Path))
        {
            Assert.Ignore("Header test file (332) not found");
            return;
        }

        // Act
        HeaderFile header = HeaderFile.Load(_testHeaderFile332Path);

        // Assert
        Assert.That(header, Is.Not.Null);
        Assert.That(header.HeaderSize, Is.EqualTo(332));
    }

    [TestCaseSource(nameof(GetHeaderTestFiles))]
    public void Load_SaveAndReload_PreservesData(string headerFilePath)
    {
        // Skip if the test file doesn't exist
        if (!File.Exists(headerFilePath))
        {
            Assert.Ignore($"Header test file not found: {headerFilePath}");
            return;
        }

        // Arrange
        string tempFilePath = Path.Combine(Path.GetTempPath(), $"test_header_{Guid.NewGuid()}.header");

        try
        {
            // Load original
            HeaderFile originalHeader = HeaderFile.Load(headerFilePath);

            // Act - Save to new file
            originalHeader.Save(tempFilePath);

            // Reload the saved file
            HeaderFile reloadedHeader = HeaderFile.Load(tempFilePath);

            // Assert - Compare original and reloaded
            Assert.That(reloadedHeader, Is.Not.Null);
            Assert.That(reloadedHeader.HeaderSize, Is.EqualTo(originalHeader.HeaderSize));
        }
        finally
        {
            // Cleanup
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
    }

    private static string[] GetHeaderTestFiles()
    {
        return
        [
            Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "Assets", @"TestHeaderFile (308).header"),
            Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "Assets", @"TestHeaderFile (328).header"),
            Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "Assets", @"TestHeaderFile (332).header")
        ];
    }
}