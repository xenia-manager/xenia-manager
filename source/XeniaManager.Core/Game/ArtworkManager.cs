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
    /// Converts the artworkData into an actual file
    /// </summary>
    /// <param name="artworkData">Artwork as data</param>
    /// <param name="savePath">Path where artwork will be saved</param>
    /// <param name="format">Format of the artwork</param>
    /// <param name="width">Width of the artwork</param>
    /// <param name="height">Height of the artwork</param>
    public static void ConvertArtwork(byte[] artworkData, string savePath, MagickFormat format, uint width, uint height)
    {
        using (MemoryStream memoryStream = new MemoryStream(artworkData))
        {
            using (MagickImage image = new MagickImage(memoryStream))
            {
                image.Resize(width, height);
                image.Format = format;
                image.Write(savePath);
            }
        }
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
        using (Stream resourceStream = assembly.GetManifestResourceStream(artwork))
        {
            // Checking if that artwork name is in the ResourceStream
            if (resourceStream == null)
            {
                throw new ArgumentException("Invalid artwork file");
            }

            // Loading the ResourceStream into MemoryStream
            using (MemoryStream memoryStream = new MemoryStream())
            {
                // Copying the embedded artwork into memoryStream
                resourceStream.CopyTo(memoryStream);
                
                // Export the image from memory into directory
                ConvertArtwork(memoryStream.ToArray(), destination, format, width, height);
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
        // Compare lengths between 2 files
        if (OriginalIconBytes.Length != CachedIconBytes.Length)
        {
            // Return false if they don't match
            return false;
        }

        // Compare each byte between 2 files
        for (int i = 0; i < OriginalIconBytes.Length; i++)
        {
            if (OriginalIconBytes[i] != CachedIconBytes[i])
            {
                // Return false if they don't match
                return false;
            }
        }

        // If everything is the same, return true
        return true;
    }
    
    /// <summary>
    /// Looks up the cached version of the artwork in the `Cache` folder
    /// </summary>
    /// <param name="artworkLocation">Location of the original artwork file</param>
    /// <returns>Path to the cached version of the artwork, otherwise null</returns>
    private static string FindCachedArtwork(string artworkLocation)
    {
        // Creates `Cache` directory if it doesn't exist
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

        // Goes through all files `Cache` directory
        foreach (string filePath in files)
        {
            // Skip comparing the icon file against itself
            if (string.Equals(filePath, artworkLocation, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Read the current file
            byte[] currentFileBytes = File.ReadAllBytes(filePath);

            // Compare both files
            if (ByteArraysAreEqual(artworkFileBytes, currentFileBytes))
            {
                // If they are equal, return the file path
                return filePath;
            }
        }

        // If no identical file is found, return null or handle as needed
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
            string cachedArtworkName = $"{Path.GetRandomFileName().Replace(".", "").Substring(0, 8)}.ico";
            File.Copy(artworkLocation, Path.Combine(Constants.CacheDir, cachedArtworkName));
            Logger.Debug($"Cached artwork name: {cachedArtworkName}");
            return new BitmapImage(new Uri(Path.Combine(Constants.CacheDir, cachedArtworkName)));
        }
        // Load the cached artwork
        else
        {
            Logger.Info("Artwork has already been cached");
            Logger.Debug($"Cached artwork name: {Path.GetFileName(cachedArtwork)}");
            return new BitmapImage(new Uri(cachedArtwork));
        }
    }
}