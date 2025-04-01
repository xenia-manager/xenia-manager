using System.Reflection;
using System.Security.Cryptography;
using System.Windows.Media.Imaging;
using ImageMagick;
using Microsoft.Extensions.Logging;

namespace XeniaManager.Core.Game;

public static class ArtworkManager
{
    public static void LocalArtwork(string artwork, string destination, MagickFormat format = MagickFormat.Ico, uint width = 150, uint height = 207)
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        using (Stream resourceStream = assembly.GetManifestResourceStream(artwork))
        {
            if (resourceStream == null)
            {
                throw new ArgumentException("Invalid artwork file");
            }

            using (MemoryStream memoryStream = new MemoryStream())
            {
                // Copying the embedded artwork into memoryStream
                resourceStream.CopyTo(memoryStream);
                memoryStream.Position = 0; // Resetting the memoryStream position

                // Export the image from memory into directory
                using (MagickImage magickImage = new MagickImage(memoryStream))
                {
                    // Resize the image to specified dimensions (This stretches the image)
                    magickImage.Resize(width, height);

                    // Convert to specific format
                    magickImage.Format = format;

                    // Export the image
                    magickImage.Write(destination);
                }
            }
        }
    }

    /// <summary>
    /// Compares arrays between game icon bytes and cached icon bytes
    /// </summary>
    /// <param name="OriginalIconBytes">Array of bytes of the original icon</param>
    /// <param name="CachedIconBytes">Array of bytes of the cached icon</param>
    /// <returns>True if they match, otherwise false</returns>
    private static bool ByteArraysAreEqual(byte[] OriginalIconBytes, byte[] CachedIconBytes)
    {
        // Compare lengths
        if (OriginalIconBytes.Length != CachedIconBytes.Length)
        {
            return false;
        }

        // Compare each byte
        for (int i = 0; i < OriginalIconBytes.Length; i++)
        {
            if (OriginalIconBytes[i] != CachedIconBytes[i])
            {
                return false;
            }
        }

        return true;
    }
    
    private static string FindCachedArtwork(string artworkLocation)
    {
        Directory.CreateDirectory(Constants.CacheDir);
        // Read the artwork file once
        byte[] artworkFileBytes = File.ReadAllBytes(artworkLocation);

        // Compute hash for the artwork file
        byte[] artworkFileHash;
        using (var md5 = MD5.Create())
        {
            artworkFileHash = md5.ComputeHash(artworkFileBytes);
        }

        // Get all files in the directory
        string[] files = Directory.GetFiles(Constants.CacheDir);

        foreach (string filePath in files)
        {
            // Skip comparing the icon file against itself
            if (string.Equals(filePath, artworkLocation, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Read the current file
            byte[] currentFileBytes = File.ReadAllBytes(filePath);

            if (ByteArraysAreEqual(artworkFileBytes, currentFileBytes))
            {
                return filePath;
            }
        }

        // If no identical file is found, return null or handle as needed
        return null;
    }

    public static BitmapImage CacheLoadArtwork(string artworkLocation)
    {
        string cachedArtwork = FindCachedArtwork(artworkLocation);
        if (cachedArtwork == null)
        {
            Logger.Debug("Couldn't find cached artwork");
            Logger.Debug("Creating new cached artwork");
            string cachedArtworkName = $"{Path.GetRandomFileName().Replace(".", "").Substring(0, 8)}.ico";
            File.Copy(artworkLocation, Path.Combine(Constants.CacheDir, cachedArtworkName));
            Logger.Debug($"Cached artwork name: {cachedArtworkName}");
            return new BitmapImage(new Uri(Path.Combine(Constants.CacheDir, cachedArtworkName)));
        }
        else
        {
            Logger.Info("Artwork has already been cached");
            Logger.Debug($"Cached artwork name: {Path.GetFileName(cachedArtwork)}");
            return new BitmapImage(new Uri(cachedArtwork));
        }
    }
}