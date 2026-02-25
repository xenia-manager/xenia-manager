using System.Reflection;
using XeniaManager.Core.Files;
using XeniaManager.Core.Models.Stfs;

namespace XeniaManager.Tests;

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
}