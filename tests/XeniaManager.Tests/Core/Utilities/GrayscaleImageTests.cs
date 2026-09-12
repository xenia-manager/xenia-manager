using XeniaManager.Core.Utilities;

namespace XeniaManager.Tests.Core.Utilities;

public class GrayscaleImageTests
{
    [Test]
    public void ToGray_PrimaryColors_MatchesBt601Luma()
    {
        Assert.That(GrayscaleImage.ToGray(255, 0, 0), Is.EqualTo(76));
        Assert.That(GrayscaleImage.ToGray(0, 255, 0), Is.EqualTo(149));
        Assert.That(GrayscaleImage.ToGray(0, 0, 255), Is.EqualTo(29));
        Assert.That(GrayscaleImage.ToGray(255, 255, 255), Is.EqualTo(255));
        Assert.That(GrayscaleImage.ToGray(0, 0, 0), Is.EqualTo(0));
    }

    [Test]
    public void GrayscaleBgra8888_DesaturatesAndPreservesAlpha()
    {
        // Two BGRA pixels: opaque red and semi-transparent green.
        byte[] pixels = [0, 0, 255, 255, 0, 255, 0, 128];

        GrayscaleImage.GrayscaleBgra8888(pixels);

        Assert.That(pixels[0..3], Is.EquivalentTo(new byte[]
        {
            76, 76, 76
        }));
        Assert.That(pixels[3], Is.EqualTo(255));
        Assert.That(pixels[4..7], Is.EquivalentTo(new byte[]
        {
            149, 149, 149
        }));
        Assert.That(pixels[7], Is.EqualTo(128));
    }

    [Test]
    public void GrayscaleBgra8888_PartialPixel_IsIgnored()
    {
        byte[] pixels = [10, 20, 30, 40, 50];

        Assert.DoesNotThrow(() => GrayscaleImage.GrayscaleBgra8888(pixels));
        Assert.That(pixels, Is.EquivalentTo(new byte[]
        {
            21, 21, 21, 40, 50
        }));
    }

    [Test]
    public void ToGrayscale_EmptyData_ReturnsNull() => Assert.That(GrayscaleImage.ToGrayscale([]), Is.Null);

    [Test]
    public void ToGrayscale_InvalidData_ReturnsNull() => Assert.That(GrayscaleImage.ToGrayscale([0x00, 0x01, 0x02, 0x03]), Is.Null);
}