using System.Reflection;
using System.Security.Cryptography;
using System.Windows.Media.Imaging;

// Imported
using ImageMagick;

namespace XeniaManager.Core.Game;

/// <summary>
/// Everything related to loading and caching artwork when in use
/// </summary>
public static class ArtworkManager
{
    /// <summary>
    /// Converts the artworkData into an actual file.
    /// </summary>
    /// <param name="artworkData">The artwork data in byte array format.</param>
    /// <param name="savePath">The file path where the converted artwork will be saved.</param>
    /// <param name="format">The format to which the artwork will be converted.</param>
    /// <param name="width">The width of the converted artwork file.</param>
    /// <param name="height">The height of the converted artwork file.</param>
    public static void ConvertArtwork(byte[] artworkData, string savePath, MagickFormat format, uint width, uint height)
    {
        using MemoryStream memoryStream = new MemoryStream(artworkData);
        using MagickImage image = new MagickImage(memoryStream);
        image.Resize(width, height);
        image.Format = format;
        image.Write(savePath);
    }

    public static void ConvertArtwork(byte[] artworkData, string savePath, MagickFormat format)
    {
        using MemoryStream memoryStream = new MemoryStream(artworkData);
        using MagickImage image = new MagickImage(memoryStream);
        image.Format = format;
        image.Write(savePath);
    }

    /// <summary>
    /// Converts an image file to a specified format and resizes it.
    /// </summary>
    /// <param name="filePath">Path to the input image file.</param>
    /// <param name="savePath">Path where the converted image will be saved.</param>
    /// <param name="format">Target format of the converted image.</param>
    /// <param name="width">Width of the converted image.</param>
    /// <param name="height">Height of the converted image.</param>
    public static void ConvertArtwork(string filePath, string savePath, MagickFormat format, uint width, uint height)
    {
        MagickFormat currentFormat = Path.GetExtension(filePath).ToLower() switch
        {
            ".jpg" or ".jpeg" => MagickFormat.Jpeg,
            ".png" => MagickFormat.Png,
            ".ico" => MagickFormat.Ico,
            _ => throw new NotSupportedException($"Unsupported file extension: {Path.GetExtension(filePath)}")
        };
        using FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        using MagickImage image = new MagickImage(fileStream, currentFormat);
        image.Resize(width, height);
        image.Format = format;
        image.Write(savePath);
    }

    public static void ConvertArtwork(string filePath, string savePath, MagickFormat format)
    {
        MagickFormat currentFormat = Path.GetExtension(filePath).ToLower() switch
        {
            ".jpg" or ".jpeg" => MagickFormat.Jpeg,
            ".png" => MagickFormat.Png,
            ".ico" => MagickFormat.Ico,
            _ => throw new NotSupportedException($"Unsupported file extension: {Path.GetExtension(filePath)}")
        };
        using FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        using MagickImage image = new MagickImage(fileStream, currentFormat);
        image.Format = format;
        image.Write(savePath);
    }

    /// <summary>
    /// Copies Artwork from the ResourceStream into the destination folder
    /// </summary>
    /// <param name="artwork">Name of the artwork in the ResourceStream</param>
    /// <param name="destination">Destination where the artwork will be copied to</param>
    /// <param name="format">Format of the copied artwork</param>
    /// <param name="width">Width of the copied artwork</param>
    /// <param name="height">Height of the artwork</param>
    /// <exception cref="ArgumentException"></exception>
    public static void LocalArtwork(string artwork, string destination, MagickFormat format = MagickFormat.Ico, uint width = 150, uint height = 207)
    {
        // Loading in assembly manifest
        Assembly assembly = Assembly.GetExecutingAssembly();
        using Stream? resourceStream = assembly.GetManifestResourceStream(artwork);
        // Checking if that artwork name is in the ResourceStream
        if (resourceStream == null)
        {
            throw new ArgumentException("Invalid artwork file");
        }

        // Loading the ResourceStream into MemoryStream
        using MemoryStream memoryStream = new MemoryStream();
        // Copying the embedded artwork into memoryStream
        resourceStream.CopyTo(memoryStream);

        // Export the image from memory into directory
        ConvertArtwork(memoryStream.ToArray(), destination, format, width, height);
    }

    public static void LocalArtwork(string artwork, string destination, MagickFormat format = MagickFormat.Ico)
    {
        // Loading in assembly manifest
        Assembly assembly = Assembly.GetExecutingAssembly();
        using Stream? resourceStream = assembly.GetManifestResourceStream(artwork);
        // Checking if that artwork name is in the ResourceStream
        if (resourceStream == null)
        {
            throw new ArgumentException("Invalid artwork file");
        }

        // Loading the ResourceStream into MemoryStream
        using MemoryStream memoryStream = new MemoryStream();
        // Copying the embedded artwork into memoryStream
        resourceStream.CopyTo(memoryStream);

        // Export the image from memory into directory
        ConvertArtwork(memoryStream.ToArray(), destination, format);
    }

    private static string ComputeMd5Hash(byte[] data)
    {
        byte[] hash = MD5.HashData(data);
        return Convert.ToHexString(hash);
    }

    /// <summary>
    /// Looks up the cached version of the artwork in the `Cache` folder
    /// </summary>
    /// <param name="artworkLocation">Location of the original artwork file</param>
    /// <returns>Path to the cached version of the artwork, otherwise null</returns>
    private static string FindCachedArtwork(string artworkLocation)
    {
        // Creates `Cache` directory if it doesn't exist
        Directory.CreateDirectory(Constants.DirectoryPaths.Cache);

        byte[] originalBytes = File.ReadAllBytes(artworkLocation);
        string originalHash = ComputeMd5Hash(originalBytes);

        // Scan cache directory
        foreach (string cachedPath in Directory.EnumerateFiles(Constants.DirectoryPaths.Cache))
        {
            // Skip if somehow pointing at the same file
            if (Path.GetFullPath(cachedPath).Equals(Path.GetFullPath(artworkLocation), StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            byte[] cachedBytes = File.ReadAllBytes(cachedPath);
            if (ComputeMd5Hash(cachedBytes) == originalHash)
            {
                return cachedPath;
            }
        }

        return null;
    }

    /// <summary>
    /// Loads the cached artwork or caches it and then loads the cached artwork
    /// </summary>
    /// <param name="artworkLocation">Location to the original artwork file</param>
    /// <returns>Loaded cached artwork as BitmapImage</returns>
    public static BitmapImage CacheLoadArtwork(string artworkLocation)
    {
        // Looks up for the cached artwork
        string cachedArtwork = FindCachedArtwork(artworkLocation);

        // If there is no cached version of the artwork, cache it and load the cached artwork
        if (cachedArtwork == null)
        {
            Logger.Debug("Couldn't find cached artwork");
            Logger.Debug("Creating new cached artwork");
            string cachedArtworkName = $"{Path.GetRandomFileName().Replace(".", "")[..8]}{Path.GetExtension(artworkLocation)}";
            string cachedPath = Path.Combine(Constants.DirectoryPaths.Cache, cachedArtworkName);
            File.Copy(artworkLocation, cachedPath);
            cachedArtwork = cachedPath;
        }

        // Load the cached artwork
        Logger.Debug($"Cached artwork name: {Path.GetFileName(cachedArtwork)}");
        BitmapImage cachedArtworkimage = new BitmapImage();
        cachedArtworkimage.BeginInit();
        cachedArtworkimage.UriSource = new Uri(cachedArtwork);
        cachedArtworkimage.CacheOption = BitmapCacheOption.OnDemand;
        cachedArtworkimage.EndInit();

        return cachedArtworkimage;
    }

    public static BitmapImage PreloadImage(string artworkLocation)
    {
        return CacheLoadArtwork(artworkLocation);
    }
}