using SkiaSharp;

namespace XeniaManager.Database.Utilities;

/// <summary>
/// Minimal artwork helpers duplicated from ArtworkManager to keep Database leaf-independent.
/// </summary>
internal static class DatabaseArtworkHelper
{
    private static readonly Dictionary<string, SKEncodedImageFormat> SupportedExtensions = new Dictionary<string, SKEncodedImageFormat>(StringComparer.OrdinalIgnoreCase)
    {
        { ".jpg", SKEncodedImageFormat.Jpeg },
        { ".jpeg", SKEncodedImageFormat.Jpeg },
        { ".png", SKEncodedImageFormat.Png },
        { ".bmp", SKEncodedImageFormat.Bmp },
        { ".webp", SKEncodedImageFormat.Webp }
    };

    public static string? ParseArtworkFileNameFromUrl(string url)
    {
        try
        {
            int lastSlashIndex = url.LastIndexOf('/');
            if (lastSlashIndex >= 0 && lastSlashIndex < url.Length - 1)
            {
                string fileName = url.Substring(lastSlashIndex + 1);
                if (fileName.Contains('.'))
                {
                    fileName = System.Text.RegularExpressions.Regex.Replace(fileName, @"(lg|sm)\d*\.(\w+)$", ".$2", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    return fileName.ToLowerInvariant();
                }
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    public static SKEncodedImageFormat? InferImageFormatFromFileName(string fileName)
    {
        string extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (extension == ".ico") return SKEncodedImageFormat.Ico;
        return SupportedExtensions.TryGetValue(extension, out SKEncodedImageFormat format) ? format : null;
    }

    public static void ConvertArtwork(byte[] artworkData, string savePath, SKEncodedImageFormat format)
    {
        using SKBitmap original = SKBitmap.Decode(artworkData) ?? throw new InvalidOperationException("Failed to decode image data.");
        EncodeTo(original, savePath, format);
    }

    public static void ConvertToIcon(byte[] artworkData, string savePath)
    {
        IcoEncoder.Encode(artworkData, savePath);
    }

    private static void EncodeTo(SKBitmap bitmap, string savePath, SKEncodedImageFormat format, int quality = 95)
    {
        if (format == SKEncodedImageFormat.Ico)
        {
            ConvertToIcon(SKImage.FromBitmap(bitmap).Encode(SKEncodedImageFormat.Png, 100)!.ToArray(), savePath);
            return;
        }
        using SKImage image = SKImage.FromBitmap(bitmap);
        using SKData data = image.Encode(format, quality) ?? throw new InvalidOperationException($"Failed to encode image as {format}");
        Directory.CreateDirectory(Path.GetDirectoryName(savePath) ?? ".");
        File.WriteAllBytes(savePath, data.ToArray());
    }
}

internal static class IcoEncoder
{
    private static readonly int[] DefaultSizes = [16, 32, 48, 256];

    public static void Encode(byte[] imageData, string savePath) => Encode(imageData, savePath, DefaultSizes);

    public static void Encode(byte[] imageData, string savePath, int[] sizes)
    {
        using SKBitmap source = SKBitmap.Decode(imageData) ?? throw new InvalidOperationException("Failed to decode source image.");
        Encode(source, savePath, sizes);
    }

    private static void Encode(SKBitmap source, string savePath, int[] sizes)
    {
        List<byte[]> frames = new List<byte[]>(sizes.Length);
        foreach (int size in sizes)
        {
            using SKBitmap resized = new SKBitmap(size, size, source.ColorType, source.AlphaType);
            source.ScalePixels(resized, SKFilterQuality.Medium);
            using SKImage image = SKImage.FromBitmap(resized);
            using SKData pngData = image.Encode(SKEncodedImageFormat.Png, 100) ?? throw new InvalidOperationException($"Failed to encode {size}x{size}");
            frames.Add(pngData.ToArray());
        }
        using FileStream output = File.Create(savePath);
        using BinaryWriter writer = new BinaryWriter(output);
        writer.Write((ushort)0);
        writer.Write((ushort)1);
        writer.Write((ushort)frames.Count);
        int dataOffset = 6 + (frames.Count * 16);
        for (int i = 0; i < frames.Count; i++)
        {
            byte w = sizes[i] == 256 ? (byte)0 : (byte)sizes[i];
            byte h = sizes[i] == 256 ? (byte)0 : (byte)sizes[i];
            writer.Write(w);
            writer.Write(h);
            writer.Write((byte)0);
            writer.Write((byte)0);
            writer.Write((ushort)1);
            writer.Write((ushort)32);
            writer.Write((uint)frames[i].Length);
            writer.Write((uint)dataOffset);
            dataOffset += frames[i].Length;
        }
        foreach (byte[] frame in frames) writer.Write(frame);
    }
}
