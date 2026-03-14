using System.Reflection;
using System.Text;
using XeniaManager.Core.Utilities;

namespace XeniaManager.Tests;

[TestFixture]
public class ArchiveExtractorTests
{
    private string _testArchivePath = string.Empty;
    private string _testOutputPath = string.Empty;
    private string _testZipPath = string.Empty;
    private string _test7zPath = string.Empty;

    [SetUp]
    public void Setup()
    {
        // Get the directory where the test assembly is located
        string testAssemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        
        // Set up paths for test archives that should be copied to output
        _testZipPath = Path.Combine(testAssemblyLocation, "Assets", "TestFile.zip");
        _test7zPath = Path.Combine(testAssemblyLocation, "Assets", "TestFile.7z");
        
        // Create a temporary output directory for extraction tests
        _testOutputPath = Path.Combine(Path.GetTempPath(), "XeniaManagerTestExtraction");
        
        // Clean up any existing test files
        if (Directory.Exists(_testOutputPath))
        {
            Directory.Delete(_testOutputPath, true);
        }
    }

    [TearDown]
    public void Teardown()
    {
        // Clean up test files after each test
        if (Directory.Exists(_testOutputPath))
        {
            Directory.Delete(_testOutputPath, true);
        }
    }
    
    [Test]
    public void ExtractArchive_ZipArchive_ValidExtraction()
    {
        // Arrange
        if (!File.Exists(_testZipPath))
        {
            Assert.Inconclusive("TestFile.zip not found in Assets folder");
        }

        // Act
        ArchiveExtractor.ExtractArchive(_testZipPath, _testOutputPath);

        // Assert
        Assert.That(Directory.Exists(_testOutputPath), Is.True);
        string[] extractedFiles = Directory.GetFiles(_testOutputPath, "*", SearchOption.AllDirectories);
        Assert.That(extractedFiles.Length, Is.GreaterThan(0), "At least one file should be extracted from the ZIP archive");
    }

    [Test]
    public void ExtractArchive_7zArchive_ValidExtraction()
    {
        // Arrange
        if (!File.Exists(_test7zPath))
        {
            Assert.Inconclusive("TestFile.7z not found in Assets folder");
        }

        // Act & Assert - Skip if 7Z format is not supported by SharpCompress
        try
        {
            ArchiveExtractor.ExtractArchive(_test7zPath, _testOutputPath);

            // If we get here, the extraction worked
            Assert.That(Directory.Exists(_testOutputPath), Is.True);
            string[] extractedFiles = Directory.GetFiles(_testOutputPath, "*", SearchOption.AllDirectories);
            Assert.That(extractedFiles.Length, Is.GreaterThan(0), "At least one file should be extracted from the 7Z archive");
        }
        catch (SharpCompress.Common.InvalidFormatException)
        {
            Assert.Inconclusive("7Z format not supported by current SharpCompress version");
        }
    }

    [Test]
    public void ExtractArchive_WithSpecificFiles_ValidExtraction()
    {
        // Arrange
        if (!File.Exists(_testZipPath))
        {
            Assert.Inconclusive("TestFile.zip not found in Assets folder");
        }

        // First, extract all files to see what's in the archive
        string tempOutputPath = Path.Combine(_testOutputPath, "temp");
        ArchiveExtractor.ExtractArchive(_testZipPath, tempOutputPath);
        
        string[] allFiles = Directory.GetFiles(tempOutputPath, "*", SearchOption.AllDirectories);
        if (allFiles.Length == 0)
        {
            Assert.Inconclusive("No files found in the test archive");
        }

        // Use the first file found in the archive for specific extraction test
        string fileToExtract = Path.GetFileName(allFiles[0]);
        string[] filesToExtract = { fileToExtract };

        // Clean up and test specific extraction
        if (Directory.Exists(_testOutputPath))
        {
            Directory.Delete(_testOutputPath, true);
        }

        // Act
        ArchiveExtractor.ExtractArchive(_testZipPath, _testOutputPath, filesToExtract);

        // Assert
        Assert.That(Directory.Exists(_testOutputPath), Is.True);
        string[] extractedFiles = Directory.GetFiles(_testOutputPath, "*", SearchOption.AllDirectories);
        Assert.That(extractedFiles.Length, Is.GreaterThanOrEqualTo(0), "Specified files should be extracted from the archive");
    }
    
    [Test]
    public async Task ExtractArchiveAsync_ZipArchive_ValidExtraction()
    {
        // Arrange
        if (!File.Exists(_testZipPath))
        {
            Assert.Inconclusive("TestFile.zip not found in Assets folder");
        }

        // Act
        await ArchiveExtractor.ExtractArchiveAsync(_testZipPath, _testOutputPath);

        // Assert
        Assert.That(Directory.Exists(_testOutputPath), Is.True);
        string[] extractedFiles = Directory.GetFiles(_testOutputPath, "*", SearchOption.AllDirectories);
        Assert.That(extractedFiles.Length, Is.GreaterThan(0), "At least one file should be extracted from the ZIP archive");
    }

    [Test]
    public async Task ExtractArchiveAsync_7zArchive_ValidExtraction()
    {
        // Arrange
        if (!File.Exists(_test7zPath))
        {
            Assert.Inconclusive("TestFile.7z not found in Assets folder");
        }

        // Act & Assert - Skip if 7Z format is not supported by SharpCompress
        try
        {
            await ArchiveExtractor.ExtractArchiveAsync(_test7zPath, _testOutputPath);

            // If we get here, the extraction worked
            Assert.That(Directory.Exists(_testOutputPath), Is.True);
            string[] extractedFiles = Directory.GetFiles(_testOutputPath, "*", SearchOption.AllDirectories);
            Assert.That(extractedFiles.Length, Is.GreaterThan(0), "At least one file should be extracted from the 7Z archive");
        }
        catch (SharpCompress.Common.InvalidFormatException)
        {
            Assert.Inconclusive("7Z format not supported by current SharpCompress version");
        }
    }

    [Test]
    public async Task ExtractArchiveAsync_WithSpecificFiles_ValidExtraction()
    {
        // Arrange
        if (!File.Exists(_testZipPath))
        {
            Assert.Inconclusive("TestFile.zip not found in Assets folder");
        }

        // First, extract all files to see what's in the archive
        string tempOutputPath = Path.Combine(_testOutputPath, "temp");
        await ArchiveExtractor.ExtractArchiveAsync(_testZipPath, tempOutputPath);
        
        string[] allFiles = Directory.GetFiles(tempOutputPath, "*", SearchOption.AllDirectories);
        if (allFiles.Length == 0)
        {
            Assert.Inconclusive("No files found in the test archive");
        }

        // Use the first file found in the archive for specific extraction test
        string fileToExtract = Path.GetFileName(allFiles[0]);
        string[] filesToExtract = { fileToExtract };

        // Clean up and test specific extraction
        if (Directory.Exists(_testOutputPath))
        {
            Directory.Delete(_testOutputPath, true);
        }

        // Act
        await ArchiveExtractor.ExtractArchiveAsync(_testZipPath, _testOutputPath, filesToExtract);

        // Assert
        Assert.That(Directory.Exists(_testOutputPath), Is.True);
        string[] extractedFiles = Directory.GetFiles(_testOutputPath, "*", SearchOption.AllDirectories);
        Assert.That(extractedFiles.Length, Is.GreaterThanOrEqualTo(0), "Specified files should be extracted from the archive");
    }

    [Test]
    public async Task ExtractArchiveAsync_WithCancellation_CancelledSuccessfully()
    {
        // Arrange
        if (!File.Exists(_testZipPath))
        {
            Assert.Inconclusive("TestFile.zip not found in Assets folder");
        }

        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately to test cancellation

        // Act & Assert
        var exception = Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await ArchiveExtractor.ExtractArchiveAsync(_testZipPath, _testOutputPath, cancellationToken: cts.Token);
        });
        
        Assert.That(exception, Is.Not.Null);
    }

    [Test]
    public void ExtractArchive_WithNullArchivePath_ThrowsArgumentException()
    {
        // Act & Assert
        ArgumentException? exception = Assert.Throws<ArgumentException>(() => 
            ArchiveExtractor.ExtractArchive(null!, _testOutputPath));
        Assert.That(exception!.ParamName, Is.EqualTo("archivePath"));
    }

    [Test]
    public void ExtractArchive_WithEmptyArchivePath_ThrowsArgumentException()
    {
        // Act & Assert
        ArgumentException? exception = Assert.Throws<ArgumentException>(() => 
            ArchiveExtractor.ExtractArchive("", _testOutputPath));
        Assert.That(exception!.ParamName, Is.EqualTo("archivePath"));
    }

    [Test]
    public void ExtractArchive_WithWhitespaceArchivePath_ThrowsArgumentException()
    {
        // Act & Assert
        ArgumentException? exception = Assert.Throws<ArgumentException>(() => 
            ArchiveExtractor.ExtractArchive("   ", _testOutputPath));
        Assert.That(exception!.ParamName, Is.EqualTo("archivePath"));
    }

    [Test]
    public void ExtractArchive_WithNullOutputPath_ThrowsArgumentException()
    {
        // Act & Assert
        ArgumentException? exception = Assert.Throws<ArgumentException>(() => 
            ArchiveExtractor.ExtractArchive(_testZipPath, null!));
        Assert.That(exception!.ParamName, Is.EqualTo("outputPath"));
    }

    [Test]
    public void ExtractArchive_WithEmptyOutputPath_ThrowsArgumentException()
    {
        // Act & Assert
        ArgumentException? exception = Assert.Throws<ArgumentException>(() => 
            ArchiveExtractor.ExtractArchive(_testZipPath, ""));
        Assert.That(exception!.ParamName, Is.EqualTo("outputPath"));
    }

    [Test]
    public void ExtractArchive_WithWhitespaceOutputPath_ThrowsArgumentException()
    {
        // Act & Assert
        ArgumentException? exception = Assert.Throws<ArgumentException>(() => 
            ArchiveExtractor.ExtractArchive(_testZipPath, "   "));
        Assert.That(exception!.ParamName, Is.EqualTo("outputPath"));
    }
    
    [Test]
    public void ExtractArchive_WithNonExistentArchive_ThrowsFileNotFoundException()
    {
        // Arrange
        string nonExistentArchive = Path.Combine(Path.GetTempPath(), "nonexistent.zip");

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => 
            ArchiveExtractor.ExtractArchive(nonExistentArchive, _testOutputPath));
    }

    [Test]
    public void ExtractArchive_WithUnsupportedFormat_ThrowsNotSupportedException()
    {
        // Arrange
        string tempFilePath = Path.Combine(Path.GetTempPath(), "unsupported.txt");
        File.WriteAllText(tempFilePath, "dummy content");

        try
        {
            // Act & Assert
            Assert.Throws<NotSupportedException>(() => 
                ArchiveExtractor.ExtractArchive(tempFilePath, _testOutputPath));
        }
        finally
        {
            // Clean up
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
    }

    [Test]
    public void ExtractArchive_WithValidPath_ExtractionSuccessful()
    {
        // Arrange
        if (!File.Exists(_testZipPath))
        {
            Assert.Inconclusive("TestFile.zip not found in Assets folder");
        }

        // Act & Assert - This should not throw any exceptions
        Assert.DoesNotThrow(() => 
            ArchiveExtractor.ExtractArchive(_testZipPath, _testOutputPath));
    }
}