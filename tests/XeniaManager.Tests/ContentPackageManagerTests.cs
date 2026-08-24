using XeniaManager.Core.Manage;

namespace XeniaManager.Tests;

[TestFixture]
public class ContentPackageManagerTests
{
    private string _tempDirectory = string.Empty;
    private string _sourcePackagePath = string.Empty;
    private byte[] _packageData = [];

    [SetUp]
    public void Setup()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"xenia_manager_pkg_tests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDirectory);

        // The installer copies package bytes as-is without parsing them,
        // so a synthetic payload is enough to verify the copy behavior.
        _packageData = new byte[3 * 1024 * 1024 + 123];
        new Random(1234).NextBytes(_packageData);
        _sourcePackagePath = Path.Combine(_tempDirectory, "SourceDLC.live");
        File.WriteAllBytes(_sourcePackagePath, _packageData);
    }

    [TearDown]
    public void Teardown()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }

    [Test]
    public void Install_CopiesPackageIntoContentTypeFolder_KeepingOriginalFileName()
    {
        // Act
        ContentPackageManager.InstallPackageAsFile(_sourcePackagePath, _tempDirectory, "4D5309C9", "00009000");

        // Assert
        string destinationPath = Path.Combine(_tempDirectory, "4D5309C9", "00009000", "SourceDLC.live");
        Assert.That(File.Exists(destinationPath), Is.True, "Package file was not copied to the content type folder");
        Assert.That(File.ReadAllBytes(destinationPath), Is.EqualTo(_packageData), "Copied package content differs from the source");
    }

    [Test]
    public void Install_NormalizesTitleIdAndContentTypeToUppercaseFolders()
    {
        // Act
        ContentPackageManager.InstallPackageAsFile(_sourcePackagePath, _tempDirectory, "4d5309c9", "00009000");

        // Assert
        Assert.That(File.Exists(Path.Combine(_tempDirectory, "4D5309C9", "00009000", "SourceDLC.live")), Is.True);
    }

    [Test]
    public void Install_OverwritesExistingDestinationFile()
    {
        // Arrange
        string destinationFolder = Path.Combine(_tempDirectory, "4D5309C9", "00009000");
        Directory.CreateDirectory(destinationFolder);
        string destinationPath = Path.Combine(destinationFolder, "SourceDLC.live");
        File.WriteAllBytes(destinationPath, [1, 2, 3]);

        // Act
        ContentPackageManager.InstallPackageAsFile(_sourcePackagePath, _tempDirectory, "4D5309C9", "00009000");

        // Assert
        Assert.That(File.ReadAllBytes(destinationPath), Is.EqualTo(_packageData), "Existing package was not overwritten");
    }

    [Test]
    public void Install_MissingSourceFile_ThrowsFileNotFoundException()
    {
        // Arrange
        string missingPath = Path.Combine(_tempDirectory, "missing.live");

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() =>
            ContentPackageManager.InstallPackageAsFile(missingPath, _tempDirectory, "4D5309C9", "00009000"));
    }

    [Test]
    public void Install_ReportsProgressEndingWithTotalByteCount()
    {
        // Arrange
        (long Copied, long Total) lastProgress = (0, 0);

        // Act
        ContentPackageManager.InstallPackageAsFile(_sourcePackagePath, _tempDirectory, "4D5309C9", "00009000",
            (copied, total) => lastProgress = (copied, total));

        // Assert
        long expectedTotal = new FileInfo(_sourcePackagePath).Length;
        Assert.That(lastProgress.Total, Is.EqualTo(expectedTotal));
        Assert.That(lastProgress.Copied, Is.EqualTo(expectedTotal));
    }
}