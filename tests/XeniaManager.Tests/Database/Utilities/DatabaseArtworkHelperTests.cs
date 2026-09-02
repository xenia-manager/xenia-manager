using SkiaSharp;
using XeniaManager.Database.Utilities;

namespace XeniaManager.Tests.Database.Utilities;

public class DatabaseArtworkHelperTests
{
    [TestCase("https://raw.githubusercontent.com/xenia-manager/x360db/refs/heads/main/titles/00000000/artwork/icon.png", "icon.png")]
    [TestCase("https://raw.githubusercontent.com/xenia-manager/x360db/refs/heads/main/titles/00000000/artwork/boxart.jpg", "boxart.jpg")]
    public void ParseArtworkFileNameFromUrl_VariousUrls_ReturnsExpected(string url, string expected)
    {
        string? result = DatabaseArtworkHelper.ParseArtworkFileNameFromUrl(url);
        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void ParseArtworkFileNameFromUrl_WithLgAndNumber_RemovesSuffix()
    {
        string url = "https://raw.githubusercontent.com/xenia-manager/x360db/refs/heads/main/titles/00000000/artwork/cover_lg999.webp";
        string? result = DatabaseArtworkHelper.ParseArtworkFileNameFromUrl(url);
        Assert.That(result, Is.EqualTo("cover_.webp"));
    }

    [Test]
    public void ParseArtworkFileNameFromUrl_WithSmAndNumber_RemovesSuffix()
    {
        string url = "https://raw.githubusercontent.com/xenia-manager/x360db/refs/heads/main/titles/00000000/artwork/cover_sm12.png";
        Assert.That(DatabaseArtworkHelper.ParseArtworkFileNameFromUrl(url), Is.EqualTo("cover_.png"));
    }

    [Test]
    public void ParseArtworkFileNameFromUrl_LowercasesResult()
    {
        string url = "https://raw.githubusercontent.com/xenia-manager/x360db/refs/heads/main/titles/00000000/artwork/IMAGE_LG123.PNG";
        string? result = DatabaseArtworkHelper.ParseArtworkFileNameFromUrl(url);
        Assert.That(result, Is.EqualTo("image_.png"));
    }

    [Test]
    public void ParseArtworkFileNameFromUrl_NoSlash_ReturnsNull()
    {
        Assert.That(DatabaseArtworkHelper.ParseArtworkFileNameFromUrl("noSlash"), Is.Null);
        Assert.That(DatabaseArtworkHelper.ParseArtworkFileNameFromUrl(""), Is.Null);
    }

    [Test]
    public void ParseArtworkFileNameFromUrl_NoDot_ReturnsNull()
    {
        string url = "https://raw.githubusercontent.com/xenia-manager/x360db/refs/heads/main/titles/00000000/artwork/noextension";
        Assert.That(DatabaseArtworkHelper.ParseArtworkFileNameFromUrl(url), Is.Null);
    }

    [Test]
    public void ParseArtworkFileNameFromUrl_TrailingSlash_ReturnsNull()
    {
        string url = "https://raw.githubusercontent.com/xenia-manager/x360db/refs/heads/main/titles/00000000/artwork/";
        Assert.That(DatabaseArtworkHelper.ParseArtworkFileNameFromUrl(url), Is.Null);
    }

    [Test]
    public void ParseArtworkFileNameFromUrl_NullOrEmpty_DoesNotThrow()
    {
        // Empty covered; null would throw but method expects string, we test empty
        Assert.DoesNotThrow(() => DatabaseArtworkHelper.ParseArtworkFileNameFromUrl(""));
    }

    [TestCase("image.jpg", SKEncodedImageFormat.Jpeg)]
    [TestCase("image.jpeg", SKEncodedImageFormat.Jpeg)]
    [TestCase("image.png", SKEncodedImageFormat.Png)]
    [TestCase("image.bmp", SKEncodedImageFormat.Bmp)]
    [TestCase("image.webp", SKEncodedImageFormat.Webp)]
    [TestCase("image.JPG", SKEncodedImageFormat.Jpeg)]
    [TestCase("image.PNG", SKEncodedImageFormat.Png)]
    public void InferImageFormatFromFileName_SupportedExtensions_ReturnsFormat(string fileName, SKEncodedImageFormat expected)
    {
        SKEncodedImageFormat? result = DatabaseArtworkHelper.InferImageFormatFromFileName(fileName);
        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void InferImageFormatFromFileName_Ico_ReturnsIco()
    {
        Assert.That(DatabaseArtworkHelper.InferImageFormatFromFileName("icon.ico"), Is.EqualTo(SKEncodedImageFormat.Ico));
        Assert.That(DatabaseArtworkHelper.InferImageFormatFromFileName("ICON.ICO"), Is.EqualTo(SKEncodedImageFormat.Ico));
    }

    [Test]
    public void InferImageFormatFromFileName_Unknown_ReturnsNull()
    {
        Assert.That(DatabaseArtworkHelper.InferImageFormatFromFileName("file.txt"), Is.Null);
        Assert.That(DatabaseArtworkHelper.InferImageFormatFromFileName("file"), Is.Null);
        Assert.That(DatabaseArtworkHelper.InferImageFormatFromFileName("file.tiff"), Is.Null);
    }

    [Test]
    public void InferImageFormatFromFileName_WithPath_ReturnsFormat()
    {
        Assert.That(DatabaseArtworkHelper.InferImageFormatFromFileName("/tmp/image.png"), Is.EqualTo(SKEncodedImageFormat.Png));
        Assert.That(DatabaseArtworkHelper.InferImageFormatFromFileName(@"C:\images\cover.jpg"), Is.EqualTo(SKEncodedImageFormat.Jpeg));
    }
}