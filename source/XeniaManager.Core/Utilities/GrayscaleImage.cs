using Avalonia.Media.Imaging;
using Avalonia.Platform;
using XeniaManager.Logging;

namespace XeniaManager.Core.Utilities;

/// <summary>
/// Renders achievement art in black and white for locked entries.
/// Unlocked entries keep their original colors; entries without art fall back
/// to the lock icons, handled by the callers.
/// </summary>
public sealed class GrayscaleImage
{
    private GrayscaleImage()
    {
    }

    /// <summary>
    /// ITU-R BT.601 luma (the same weights photo editors use for desaturation).
    /// </summary>
    public static byte ToGray(byte r, byte g, byte b) => (byte)(0.299 * r + 0.587 * g + 0.114 * b);

    /// <summary>
    /// Desaturates tightly-packed Bgra8888 pixels in place, preserving alpha.
    /// </summary>
    /// <param name="pixels">Pixel data as consecutive blue, green, red, alpha quadruples.</param>
    internal static void GrayscaleBgra8888(Span<byte> pixels)
    {
        for (int i = 0; i + 4 <= pixels.Length; i += 4)
        {
            byte gray = ToGray(pixels[i + 2], pixels[i + 1], pixels[i]);
            pixels[i] = gray;
            pixels[i + 1] = gray;
            pixels[i + 2] = gray;
        }
    }

    /// <summary>
    /// Decodes PNG art and returns its black-and-white version.
    /// </summary>
    /// <param name="pngData">The source PNG bytes.</param>
    /// <returns>The grayscale image, or null when decoding fails.</returns>
    public static WriteableBitmap? ToGrayscale(byte[] pngData)
    {
        try
        {
            if (pngData.Length == 0)
            {
                return null;
            }

            using MemoryStream stream = new MemoryStream(pngData);
            using Bitmap source = new Bitmap(stream);
            if (source.PixelSize.Width <= 0 || source.PixelSize.Height <= 0)
            {
                return null;
            }

            WriteableBitmap dest = new WriteableBitmap(source.PixelSize, source.Dpi, PixelFormats.Bgra8888, AlphaFormat.Premul);
            using (ILockedFramebuffer framebuffer = dest.Lock())
            {
                // Copies (and transcodes when needed) into Bgra8888.
                source.CopyPixels(framebuffer);
                unsafe
                {
                    byte* basePtr = (byte*)framebuffer.Address.ToPointer();
                    for (int y = 0; y < framebuffer.Size.Height; y++)
                    {
                        Span<byte> row = new Span<byte>(basePtr + y * framebuffer.RowBytes, framebuffer.Size.Width * 4);
                        GrayscaleBgra8888(row);
                    }
                }
            }

            return dest;
        }
        catch (Exception ex)
        {
            Logger.Warning<GrayscaleImage>($"Failed to convert image to grayscale: {ex.Message}");
            Logger.LogExceptionDetails<GrayscaleImage>(ex);
            return null;
        }
    }
}