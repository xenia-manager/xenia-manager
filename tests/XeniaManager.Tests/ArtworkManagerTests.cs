using System.Reflection;
using Avalonia.Media.Imaging;
using SkiaSharp;
using XeniaManager.Core.Manage;

namespace XeniaManager.Tests;

[TestFixture]
public class ArtworkManagerTests
{
    private string _testArtworkDirectory = string.Empty;
    private string _tempOutputDirectory = string.Empty;

    [SetUp]
    public void Setup()
    {
        // Get the directory of the currently executing assembly (the test assembly)
        string assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        
        // Navigate up from bin/Debug/net10.0/ to the solution root, then to the source directory
        string solutionRoot = Path.GetFullPath(Path.Combine(assemblyLocation, "..", "..", "..", "..", ".."));
        _testArtworkDirectory = Path.Combine(solutionRoot, "source", "XeniaManager.Core", "Assets", "Artwork");
        
        // Create a temporary output directory for test results
        _tempOutputDirectory = Path.Combine(assemblyLocation, "TempArtworkTests");
        Directory.CreateDirectory(_tempOutputDirectory);
        
        // Verify that the artwork directory exists
        Assert.That(Directory.Exists(_testArtworkDirectory), Is.True, $"Test artwork directory does not exist at {_testArtworkDirectory}");
    }

    [TearDown]
    public void Teardown()
    {
        // Clean up the temporary output directory
        if (Directory.Exists(_tempOutputDirectory))
        {
            Directory.Delete(_tempOutputDirectory, true);
        }
    }

    [Test]
    public void ConvertArtwork_WithByteArrayAndResize_Succeeds()
    {
        // Arrange
        string imagePath = Path.Combine(_testArtworkDirectory, "Boxart.jpg");
        byte[] imageData = File.ReadAllBytes(imagePath);
        string outputPath = Path.Combine(_tempOutputDirectory, "converted_resized.png");

        // Act
        ArtworkManager.ConvertArtwork(imageData, outputPath, SKEncodedImageFormat.Png, 100, 100);

        // Assert
        Assert.That(File.Exists(outputPath), Is.True);
        using SKBitmap? bitmap = SKBitmap.Decode(outputPath);
        Assert.That(bitmap.Width, Is.EqualTo(100));
        Assert.That(bitmap.Height, Is.EqualTo(100));
    }

    [Test]
    public void ConvertArtwork_WithByteArrayWithoutResize_Succeeds()
    {
        // Arrange
        string imagePath = Path.Combine(_testArtworkDirectory, "Icon.png");
        byte[] imageData = File.ReadAllBytes(imagePath);
        string outputPath = Path.Combine(_tempOutputDirectory, "converted_original.png");

        // Act
        ArtworkManager.ConvertArtwork(imageData, outputPath, SKEncodedImageFormat.Png);

        // Assert
        Assert.That(File.Exists(outputPath), Is.True);
    }

    [Test]
    public void ConvertArtwork_WithFilePathAndResize_Succeeds()
    {
        // Arrange
        string inputPath = Path.Combine(_testArtworkDirectory, "Background.jpg");
        string outputPath = Path.Combine(_tempOutputDirectory, "converted_from_file_resized.jpg");

        // Act
        ArtworkManager.ConvertArtwork(inputPath, outputPath, SKEncodedImageFormat.Jpeg, 200, 150);

        // Assert
        Assert.That(File.Exists(outputPath), Is.True);
        using SKBitmap? bitmap = SKBitmap.Decode(outputPath);
        Assert.That(bitmap.Width, Is.EqualTo(200));
        Assert.That(bitmap.Height, Is.EqualTo(150));
    }

    [Test]
    public void ConvertArtwork_WithFilePathWithoutResize_Succeeds()
    {
        // Arrange
        string inputPath = Path.Combine(_testArtworkDirectory, "Icon.png");
        string outputPath = Path.Combine(_tempOutputDirectory, "converted_from_file_original.png");

        // Act
        ArtworkManager.ConvertArtwork(inputPath, outputPath, SKEncodedImageFormat.Png);

        // Assert
        Assert.That(File.Exists(outputPath), Is.True);
    }

    [Test]
    public void ConvertToIcon_WithByteArrayStandardSizes_Succeeds()
    {
        // Arrange
        string imagePath = Path.Combine(_testArtworkDirectory, "Icon.png");
        byte[] imageData = File.ReadAllBytes(imagePath);
        string outputPath = Path.Combine(_tempOutputDirectory, "icon_standard.ico");

        // Act
        ArtworkManager.ConvertToIcon(imageData, outputPath);

        // Assert
        Assert.That(File.Exists(outputPath), Is.True);
        // Note: We can't easily verify ICO contents without additional libraries, 
        // but the method should complete without throwing
    }

    [Test]
    public void ConvertToIcon_WithByteArrayCustomSizes_Succeeds()
    {
        // Arrange
        string imagePath = Path.Combine(_testArtworkDirectory, "Boxart.jpg");
        byte[] imageData = File.ReadAllBytes(imagePath);
        string outputPath = Path.Combine(_tempOutputDirectory, "icon_custom.ico");
        int[] customSizes = { 16, 32, 64 };

        // Act
        ArtworkManager.ConvertToIcon(imageData, outputPath, customSizes);

        // Assert
        Assert.That(File.Exists(outputPath), Is.True);
        // Note: We can't easily verify ICO contents without additional libraries, 
        // but the method should complete without throwing
    }

    [Test]
    public void ConvertToIcon_WithFilePathStandardSizes_Succeeds()
    {
        // Arrange
        string inputPath = Path.Combine(_testArtworkDirectory, "Icon.png");
        string outputPath = Path.Combine(_tempOutputDirectory, "icon_file_standard.ico");

        // Act
        ArtworkManager.ConvertToIcon(inputPath, outputPath);

        // Assert
        Assert.That(File.Exists(outputPath), Is.True);
        // Note: We can't easily verify ICO contents without additional libraries, 
        // but the method should complete without throwing
    }

    [Test]
    public void ConvertToIcon_WithFilePathCustomSizes_Succeeds()
    {
        // Arrange
        string inputPath = Path.Combine(_testArtworkDirectory, "Background.jpg");
        string outputPath = Path.Combine(_tempOutputDirectory, "icon_file_custom.ico");
        int[] customSizes = { 24, 48, 96 };

        // Act
        ArtworkManager.ConvertToIcon(inputPath, outputPath, customSizes);

        // Assert
        Assert.That(File.Exists(outputPath), Is.True);
        // Note: We can't easily verify ICO contents without additional libraries, 
        // but the method should complete without throwing
    }

    [Test]
    [Ignore("Requires Avalonia runtime initialization")]
    public void CacheLoadArtwork_WithValidPath_Succeeds()
    {
        // Arrange
        string imagePath = Path.Combine(_testArtworkDirectory, "Icon.png");

        // Act & Assert - This method should complete without throwing
        // Note: We can't test the actual Bitmap creation in a test environment without Avalonia initialization
        Assert.DoesNotThrow(() => 
        {
            Bitmap result = ArtworkManager.CacheLoadArtwork(imagePath);
        });
    }

    [Test]
    [Ignore("Requires Avalonia runtime initialization")]
    public void PreloadImage_WithValidPath_Succeeds()
    {
        // Arrange
        string imagePath = Path.Combine(_testArtworkDirectory, "Boxart.jpg");

        // Act & Assert - This method should complete without throwing
        // Note: We can't test the actual Bitmap creation in a test environment without Avalonia initialization
        Assert.DoesNotThrow(() => 
        {
            Bitmap result = ArtworkManager.PreloadImage(imagePath);
        });
    }

    [Test]
    public void ValidateSourceExtension_WithSupportedExtension_DoesNotThrow()
    {
        // Arrange
        string imagePath = Path.Combine(_testArtworkDirectory, "Icon.png");

        // Act & Assert - Should not throw
        Assert.DoesNotThrow(() => 
        {
            // We can't directly test the private ValidateSourceExtension method,
            // but we can test that public methods that use it work correctly
            string outputPath = Path.Combine(_tempOutputDirectory, "validation_test.png");
            ArtworkManager.ConvertArtwork(imagePath, outputPath, SKEncodedImageFormat.Png);
        });
    }

    [Test]
    public void UnsupportedFileExtension_ThrowsNotSupportedException()
    {
        // Arrange
        string unsupportedPath = Path.Combine(_tempOutputDirectory, "test.xyz");
        File.WriteAllText(unsupportedPath, "dummy content");
        string outputPath = Path.Combine(_tempOutputDirectory, "should_fail.png");

        // Act & Assert
        Assert.Throws<NotSupportedException>(() => 
        {
            ArtworkManager.ConvertArtwork(unsupportedPath, outputPath, SKEncodedImageFormat.Png);
        });

        File.Delete(unsupportedPath);
    }
}