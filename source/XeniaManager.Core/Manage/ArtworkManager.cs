using System.Collections.Concurrent;
using System.IO.Hashing;
using System.Reflection;
using Avalonia.Media.Imaging;
using SkiaSharp;
using XeniaManager.Core.Constants;
using XeniaManager.Core.Logging;

namespace XeniaManager.Core.Manage;

/// <summary>
/// Manages artwork processing, conversion, and caching for the Xenia Manager application.
/// Handles image format conversions, resizing, ICO encoding, and embedded resource extraction.
/// </summary>
public class ArtworkManager
{
    /// <summary>
    /// In-memory cache mapping artwork file paths to their cached file metadata.
    /// Key: Full path of original artwork file
    /// Value: CacheEntry containing cached file path (hash-based), hash, file size, and last modified time
    /// This prevents redundant file reads and hash computations for the same artwork.
    /// </summary>
    private static readonly ConcurrentDictionary<string, CacheEntry> ArtworkCache = new ConcurrentDictionary<string, CacheEntry>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Represents a cache entry with metadata for quick validation.
    /// </summary>
    private record CacheEntry(string CachedPath, string Hash, long FileSize, DateTime LastModifiedTime);

    /// <summary>
    /// Dictionary mapping file extensions to their corresponding SkiaSharp encoded image formats.
    /// Used to validate and determine the output format for image conversions.
    /// </summary>
    private static readonly Dictionary<string, SKEncodedImageFormat> SupportedExtensions = new Dictionary<string, SKEncodedImageFormat>(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = SKEncodedImageFormat.Jpeg,
        [".jpeg"] = SKEncodedImageFormat.Jpeg,
        [".png"] = SKEncodedImageFormat.Png,
        [".webp"] = SKEncodedImageFormat.Webp,
        [".bmp"] = SKEncodedImageFormat.Bmp,
        [".gif"] = SKEncodedImageFormat.Gif,
    };

    /// <summary>
    /// Default quality setting used for image encoding operations (JPEG/WebP).
    /// Value ranges from 0-100, where 100 is the highest quality.
    /// </summary>
    private const int DefaultQuality = 100;

    /// <summary>
    /// Resizes a source bitmap to the specified dimensions using high-quality filtering.
    /// </summary>
    /// <param name="source">The original bitmap to resize.</param>
    /// <param name="width">The target width for the resized bitmap.</param>
    /// <param name="height">The target height for the resized bitmap.</param>
    /// <returns>A new SKBitmap instance with the specified dimensions.</returns>
    private static SKBitmap ResizeBitmap(SKBitmap source, int width, int height)
    {
        Logger.Trace<ArtworkManager>($"Starting ResizeBitmap operation from {source.Width}x{source.Height} to {width}x{height}");
        SKBitmap resized = new SKBitmap(width, height, source.ColorType, source.AlphaType);
        source.ScalePixels(resized, SKFilterQuality.High);
        Logger.Debug<ArtworkManager>($"Successfully resized bitmap to {width}x{height}");
        Logger.Trace<ArtworkManager>("ResizeBitmap operation completed successfully");
        return resized;
    }

    /// <summary>
    /// Encodes a bitmap to the specified format and saves it to the given path.
    /// </summary>
    /// <param name="bitmap">The bitmap to encode.</param>
    /// <param name="savePath">The file path where the encoded image will be saved.</param>
    /// <param name="format">The target image format for encoding.</param>
    /// <param name="quality">The quality level for lossy formats (JPEG/WebP), defaults to 95.</param>
    /// <exception cref="InvalidOperationException">Thrown when SkiaSharp fails to encode the image.</exception>
    private static void EncodeTo(SKBitmap bitmap, string savePath, SKEncodedImageFormat format, int quality = DefaultQuality)
    {
        Logger.Trace<ArtworkManager>($"Starting EncodeTo operation for bitmap {bitmap.Width}x{bitmap.Height} to {savePath} (Format: {format}, Quality: {quality})");
        using SKImage? image = SKImage.FromBitmap(bitmap);
        using SKData data = image.Encode(format, quality)
                            ?? throw new InvalidOperationException(
                                $"SkiaSharp failed to encode image as {format}.");
        using FileStream stream = File.Create(savePath);
        data.SaveTo(stream);
        Logger.Info<ArtworkManager>($"Successfully encoded and saved image to {savePath}");
        Logger.Trace<ArtworkManager>("EncodeTo operation completed successfully");
    }

    /// <summary>
    /// Validates that the file extension of the provided file path is supported for processing.
    /// </summary>
    /// <param name="filePath">The path to the file whose extension needs validation.</param>
    /// <exception cref="NotSupportedException">Thrown when the file extension is not in the supported list.</exception>
    private static void ValidateSourceExtension(string filePath)
    {
        Logger.Trace<ArtworkManager>($"Starting ValidateSourceExtension operation for {filePath}");
        string ext = Path.GetExtension(filePath);
        if (!SupportedExtensions.ContainsKey(ext))
        {
            Logger.Error<ArtworkManager>($"Unsupported file extension: '{ext}'. Supported: {string.Join(", ", SupportedExtensions.Keys)}");
            throw new NotSupportedException($"Unsupported file extension: '{ext}'. " +
                                            $"Supported: {string.Join(", ", SupportedExtensions.Keys)}");
        }
        Logger.Debug<ArtworkManager>($"File extension '{ext}' is supported");
        Logger.Trace<ArtworkManager>("ValidateSourceExtension operation completed successfully");
    }

    /// <summary>
    /// Converts raw image bytes to the specified format with resizing.
    /// </summary>
    /// <param name="artworkData">The raw image data to convert.</param>
    /// <param name="savePath">The path where the converted image will be saved.</param>
    /// <param name="format">The target image format for conversion.</param>
    /// <param name="width">The target width for the resized image.</param>
    /// <param name="height">The target height for the resized image.</param>
    /// <param name="quality">The quality level for lossy formats (JPEG/WebP), defaults to 95.</param>
    /// <exception cref="InvalidOperationException">Thrown when the image data cannot be decoded.</exception>
    public static void ConvertArtwork(byte[] artworkData, string savePath, SKEncodedImageFormat format, int width, int height, int quality = DefaultQuality)
    {
        Logger.Trace<ArtworkManager>($"Starting ConvertArtwork operation with raw image data to {savePath} (Format: {format}, Size: {width}x{height}, Quality: {quality})");

        using SKBitmap original = SKBitmap.Decode(artworkData)
                                  ?? throw new InvalidOperationException("Failed to decode image data.");
        Logger.Debug<ArtworkManager>($"Successfully decoded image data with dimensions {original.Width}x{original.Height}");

        using SKBitmap resized = ResizeBitmap(original, width, height);
        Logger.Debug<ArtworkManager>($"Successfully resized image to {width}x{height}");

        EncodeTo(resized, savePath, format, quality);
        Logger.Info<ArtworkManager>($"Successfully converted and saved image to {savePath}");
        Logger.Trace<ArtworkManager>("ConvertArtwork operation completed successfully");
    }

    /// <summary>
    /// Converts raw image bytes to the specified format without resizing.
    /// </summary>
    /// <param name="artworkData">The raw image data to convert.</param>
    /// <param name="savePath">The path where the converted image will be saved.</param>
    /// <param name="format">The target image format for conversion.</param>
    /// <param name="quality">The quality level for lossy formats (JPEG/WebP), defaults to 95.</param>
    /// <exception cref="InvalidOperationException">Thrown when the image data cannot be decoded.</exception>
    public static void ConvertArtwork(byte[] artworkData, string savePath, SKEncodedImageFormat format, int quality = DefaultQuality)
    {
        Logger.Trace<ArtworkManager>($"Starting ConvertArtwork operation with raw image data to {savePath} (Format: {format}, Quality: {quality})");

        using SKBitmap original = SKBitmap.Decode(artworkData)
                                  ?? throw new InvalidOperationException("Failed to decode image data.");
        Logger.Debug<ArtworkManager>($"Successfully decoded image data with dimensions {original.Width}x{original.Height}");

        EncodeTo(original, savePath, format, quality);
        Logger.Info<ArtworkManager>($"Successfully converted and saved image to {savePath}");
        Logger.Trace<ArtworkManager>("ConvertArtwork operation completed successfully");
    }

    /// <summary>
    /// Converts an image file to the specified format with resizing.
    /// </summary>
    /// <param name="filePath">The path to the source image file.</param>
    /// <param name="savePath">The path where the converted image will be saved.</param>
    /// <param name="format">The target image format for conversion.</param>
    /// <param name="width">The target width for the resized image.</param>
    /// <param name="height">The target height for the resized image.</param>
    /// <param name="quality">The quality level for lossy formats (JPEG/WebP), defaults to 95.</param>
    /// <exception cref="InvalidOperationException">Thrown when the image file cannot be decoded.</exception>
    /// <exception cref="NotSupportedException">Thrown when the source file extension is not supported.</exception>
    public static void ConvertArtwork(string filePath, string savePath, SKEncodedImageFormat format, int width, int height, int quality = DefaultQuality)
    {
        Logger.Trace<ArtworkManager>($"Starting ConvertArtwork operation with file {filePath} to {savePath} (Format: {format}, Size: {width}x{height}, Quality: {quality})");

        ValidateSourceExtension(filePath);
        Logger.Debug<ArtworkManager>($"Source file extension validated: {Path.GetExtension(filePath)}");

        using SKBitmap original = SKBitmap.Decode(filePath)
                                  ?? throw new InvalidOperationException(
                                      $"Failed to decode image: {filePath}");
        Logger.Debug<ArtworkManager>($"Successfully decoded image file {filePath} with dimensions {original.Width}x{original.Height}");

        using SKBitmap resized = ResizeBitmap(original, width, height);
        Logger.Debug<ArtworkManager>($"Successfully resized image to {width}x{height}");

        EncodeTo(resized, savePath, format, quality);
        Logger.Info<ArtworkManager>($"Successfully converted and saved image to {savePath}");
        Logger.Trace<ArtworkManager>("ConvertArtwork operation completed successfully");
    }

    /// <summary>
    /// Converts an image file to the specified format without resizing.
    /// </summary>
    /// <param name="filePath">The path to the source image file.</param>
    /// <param name="savePath">The path where the converted image will be saved.</param>
    /// <param name="format">The target image format for conversion.</param>
    /// <param name="quality">The quality level for lossy formats (JPEG/WebP), defaults to 95.</param>
    /// <exception cref="InvalidOperationException">Thrown when the image file cannot be decoded.</exception>
    /// <exception cref="NotSupportedException">Thrown when the source file extension is not supported.</exception>
    public static void ConvertArtwork(string filePath, string savePath, SKEncodedImageFormat format, int quality = DefaultQuality)
    {
        Logger.Trace<ArtworkManager>($"Starting ConvertArtwork operation with file {filePath} to {savePath} (Format: {format}, Quality: {quality})");

        ValidateSourceExtension(filePath);
        Logger.Debug<ArtworkManager>($"Source file extension validated: {Path.GetExtension(filePath)}");

        using SKBitmap original = SKBitmap.Decode(filePath)
                                  ?? throw new InvalidOperationException(
                                      $"Failed to decode image: {filePath}");
        Logger.Debug<ArtworkManager>($"Successfully decoded image file {filePath} with dimensions {original.Width}x{original.Height}");

        EncodeTo(original, savePath, format, quality);
        Logger.Info<ArtworkManager>($"Successfully converted and saved image to {savePath}");
        Logger.Trace<ArtworkManager>("ConvertArtwork operation completed successfully");
    }

    /// <summary>
    /// Converts raw image bytes to a .ico file with standard sizes (16, 32, 48, 256).
    /// Used for Windows shortcut icon creation.
    /// </summary>
    /// <param name="artworkData">The raw image data to convert to an icon.</param>
    /// <param name="savePath">The path where the .ico file will be saved.</param>
    public static void ConvertToIcon(byte[] artworkData, string savePath)
    {
        Logger.Trace<ArtworkManager>($"Starting ConvertToIcon operation with raw image data to {savePath} (Standard sizes)");
        IcoEncoder.Encode(artworkData, savePath);
        Logger.Info<ArtworkManager>($"Successfully converted and saved icon to {savePath}");
        Logger.Trace<ArtworkManager>("ConvertToIcon operation completed successfully");
    }

    /// <summary>
    /// Converts raw image bytes to a .ico file with custom sizes.
    /// </summary>
    /// <param name="artworkData">The raw image data to convert to an icon.</param>
    /// <param name="savePath">The path where the .ico file will be saved.</param>
    /// <param name="sizes">An array of pixel sizes for each icon frame (e.g., 16, 32, 48, 256).</param>
    public static void ConvertToIcon(byte[] artworkData, string savePath, int[] sizes)
    {
        Logger.Trace<ArtworkManager>($"Starting ConvertToIcon operation with raw image data to {savePath} (Custom sizes: {string.Join(", ", sizes)})");
        IcoEncoder.Encode(artworkData, savePath, sizes);
        Logger.Info<ArtworkManager>($"Successfully converted and saved icon to {savePath}");
        Logger.Trace<ArtworkManager>("ConvertToIcon operation completed successfully");
    }

    /// <summary>
    /// Converts an image file to a .ico file with standard sizes (16, 32, 48, 256).
    /// </summary>
    /// <param name="filePath">The path to the source image file.</param>
    /// <param name="savePath">The path where the .ico file will be saved.</param>
    public static void ConvertToIcon(string filePath, string savePath)
    {
        Logger.Trace<ArtworkManager>($"Starting ConvertToIcon operation with file {filePath} to {savePath} (Standard sizes)");
        IcoEncoder.Encode(filePath, savePath);
        Logger.Info<ArtworkManager>($"Successfully converted and saved icon to {savePath}");
        Logger.Trace<ArtworkManager>("ConvertToIcon operation completed successfully");
    }

    /// <summary>
    /// Converts an image file to a .ico file with custom sizes.
    /// </summary>
    /// <param name="filePath">The path to the source image file.</param>
    /// <param name="savePath">The path where the .ico file will be saved.</param>
    /// <param name="sizes">An array of pixel sizes for each icon frame (e.g., 16, 32, 48, 256).</param>
    public static void ConvertToIcon(string filePath, string savePath, int[] sizes)
    {
        Logger.Trace<ArtworkManager>($"Starting ConvertToIcon operation with file {filePath} to {savePath} (Custom sizes: {string.Join(", ", sizes)})");
        IcoEncoder.Encode(filePath, savePath, sizes);
        Logger.Info<ArtworkManager>($"Successfully converted and saved icon to {savePath}");
        Logger.Trace<ArtworkManager>("ConvertToIcon operation completed successfully");
    }

    /// <summary>
    /// Extracts an embedded resource and converts it to the specified format with resizing.
    /// </summary>
    /// <param name="artwork">The name of the embedded resource to extract.</param>
    /// <param name="destination">The path where the converted image will be saved.</param>
    /// <param name="width">The target width for the resized image, defaults to 150.</param>
    /// <param name="height">The target height for the resized image, defaults to 207.</param>
    /// <param name="format">The target image format for conversion, defaults to PNG.</param>
    public static void LocalArtwork(string artwork, string destination, int width = 150, int height = 207, SKEncodedImageFormat format = SKEncodedImageFormat.Png)
    {
        Logger.Trace<ArtworkManager>($"Starting LocalArtwork operation with embedded resource '{artwork}' to {destination} (Format: {format}, Size: {width}x{height})");
        byte[] resourceBytes = ReadEmbeddedResource(artwork);
        Logger.Debug<ArtworkManager>($"Successfully extracted embedded resource '{artwork}', size: {resourceBytes.Length} bytes");
        ConvertArtwork(resourceBytes, destination, format, width, height);
        Logger.Info<ArtworkManager>($"Successfully processed and saved embedded resource to {destination}");
        Logger.Trace<ArtworkManager>("LocalArtwork operation completed successfully");
    }

    /// <summary>
    /// Extracts an embedded resource and converts it to the specified format without resizing.
    /// </summary>
    /// <param name="artwork">The name of the embedded resource to extract.</param>
    /// <param name="destination">The path where the converted image will be saved.</param>
    /// <param name="format">The target image format for conversion, defaults to PNG.</param>
    public static void LocalArtwork(string artwork, string destination, SKEncodedImageFormat format = SKEncodedImageFormat.Png)
    {
        Logger.Trace<ArtworkManager>($"Starting LocalArtwork operation with embedded resource '{artwork}' to {destination} (Format: {format})");
        byte[] resourceBytes = ReadEmbeddedResource(artwork);
        Logger.Debug<ArtworkManager>($"Successfully extracted embedded resource '{artwork}', size: {resourceBytes.Length} bytes");
        ConvertArtwork(resourceBytes, destination, format);
        Logger.Info<ArtworkManager>($"Successfully processed and saved embedded resource to {destination}");
        Logger.Trace<ArtworkManager>("LocalArtwork operation completed successfully");
    }

    /// <summary>
    /// Extracts an embedded resource as a .ico file with standard icon sizes.
    /// </summary>
    /// <param name="artwork">The name of the embedded resource to extract.</param>
    /// <param name="destination">The path where the .ico file will be saved.</param>
    public static void LocalArtworkAsIcon(string artwork, string destination)
    {
        Logger.Trace<ArtworkManager>($"Starting LocalArtworkAsIcon operation with embedded resource '{artwork}' to {destination} (Standard icon sizes)");
        byte[] resourceBytes = ReadEmbeddedResource(artwork);
        Logger.Debug<ArtworkManager>($"Successfully extracted embedded resource '{artwork}', size: {resourceBytes.Length} bytes");
        IcoEncoder.Encode(resourceBytes, destination);
        Logger.Info<ArtworkManager>($"Successfully processed and saved embedded resource as icon to {destination}");
        Logger.Trace<ArtworkManager>("LocalArtworkAsIcon operation completed successfully");
    }

    /// <summary>
    /// Reads an embedded resource from the executing assembly as a byte array.
    /// </summary>
    /// <param name="resourceName">The name of the embedded resource to read.</param>
    /// <returns>The embedded resource data as a byte array.</returns>
    /// <exception cref="ArgumentException">Thrown when the embedded resource is not found.</exception>
    private static byte[] ReadEmbeddedResource(string resourceName)
    {
        Logger.Trace<ArtworkManager>($"Starting ReadEmbeddedResource operation for '{resourceName}'");

        Assembly assembly = Assembly.GetExecutingAssembly();
        using Stream? resourceStream = assembly.GetManifestResourceStream(resourceName);
        if (resourceStream is null)
        {
            string[] available = assembly.GetManifestResourceNames();
            Logger.Error<ArtworkManager>($"Embedded resource '{resourceName}' not found. Available: [{string.Join(", ", available)}]");
            throw new ArgumentException($"Embedded resource '{resourceName}' not found. " +
                                        $"Available: [{string.Join(", ", available)}]",
                nameof(resourceName));
        }

        using MemoryStream memoryStream = new MemoryStream();
        resourceStream.CopyTo(memoryStream);
        byte[] result = memoryStream.ToArray();
        Logger.Debug<ArtworkManager>($"Successfully read embedded resource '{resourceName}', size: {result.Length} bytes");
        Logger.Trace<ArtworkManager>("ReadEmbeddedResource operation completed successfully");
        return result;
    }

    /// <summary>
    /// Computes an MD5 hash for the given byte array.
    /// Used for comparing artwork files to determine if they are identical.
    /// </summary>
    /// <param name="data">The byte array to hash.</param>
    /// <returns>The XXHash3 hash as a hexadecimal string (16 chars).</returns>
    private static string ComputeHash(byte[] data)
    {
        Logger.Trace<ArtworkManager>($"Starting ComputeXxHash operation for data of size {data.Length} bytes");
        // XxHash3 provides a 64-bit (8 bytes) hash, which is 16 hex characters
        // Much faster than MD5 while maintaining good collision resistance for file caching
        byte[] hash = XxHash3.Hash(data);
        string result = Convert.ToHexString(hash).ToLowerInvariant();
        Logger.Debug<ArtworkManager>($"Computed XXHash3 hash: {result}");
        Logger.Trace<ArtworkManager>("ComputeXxHash operation completed successfully");
        return result;
    }

    /// <summary>
    /// Finds a cached version of the artwork if it exists and hasn't changed.
    /// Uses an in-memory cache with metadata validation to avoid redundant file reads.
    /// Optimization: Uses file size and last modified time for quick validation before computing hash.
    /// Uses XXHash3 for fast hashing and hash-based filenames for O(1) lookup.
    /// </summary>
    /// <param name="artworkLocation">The path to the original artwork file.</param>
    /// <returns>The path to the cached artwork if found and unchanged, otherwise null.</returns>
    private static string? FindCachedArtwork(string artworkLocation)
    {
        Logger.Trace<ArtworkManager>($"Starting FindCachedArtwork operation for {artworkLocation}");

        Directory.CreateDirectory(AppPaths.ImageCacheDirectory);
        Logger.Debug<ArtworkManager>($"Cache directory ensured: {AppPaths.ImageCacheDirectory}");

        string fullPath = Path.GetFullPath(artworkLocation);

        // Get current file metadata for quick validation
        FileInfo fileInfo = new FileInfo(artworkLocation);
        long currentFileSize = fileInfo.Length;
        DateTime currentLastModified = fileInfo.LastWriteTimeUtc;

        // Check in-memory cache first
        if (ArtworkCache.TryGetValue(fullPath, out CacheEntry? cachedEntry))
        {
            // Verify the cached file still exists
            if (File.Exists(cachedEntry.CachedPath))
            {
                // Quick validation using file metadata (no file read needed)
                if (currentFileSize == cachedEntry.FileSize &&
                    currentLastModified == cachedEntry.LastModifiedTime)
                {
                    Logger.Info<ArtworkManager>($"Found matching cached artwork at: {cachedEntry.CachedPath} (metadata validation passed)");
                    Logger.Trace<ArtworkManager>("FindCachedArtwork operation completed successfully");
                    return cachedEntry.CachedPath;
                }

                // Metadata changed, verify with hash
                Logger.Debug<ArtworkManager>($"File metadata changed (old: {cachedEntry.FileSize} bytes @ {cachedEntry.LastModifiedTime}, new: {currentFileSize} bytes @ {currentLastModified}), verifying with hash");
                byte[] originalBytes = File.ReadAllBytes(artworkLocation);
                string currentHash = ComputeHash(originalBytes);

                if (currentHash == cachedEntry.Hash)
                {
                    Logger.Info<ArtworkManager>($"Found matching cached artwork at: {cachedEntry.CachedPath} (hash validation passed)");
                    // Update metadata in cache
                    ArtworkCache[fullPath] = cachedEntry with { FileSize = currentFileSize, LastModifiedTime = currentLastModified };
                    Logger.Trace<ArtworkManager>("FindCachedArtwork operation completed successfully");
                    return cachedEntry.CachedPath;
                }
                else
                {
                    Logger.Debug<ArtworkManager>($"Original artwork has changed (old hash: {cachedEntry.Hash}, new hash: {currentHash}), removing from cache");
                    ArtworkCache.TryRemove(fullPath, out _);
                }
            }
            else
            {
                Logger.Debug<ArtworkManager>($"Cached file no longer exists: {cachedEntry.CachedPath}, removing from cache");
                ArtworkCache.TryRemove(fullPath, out _);
            }
        }

        // Not in cache or cache is stale, compute hash and check if the hash-named file exists
        byte[] originalBytesForHash = File.ReadAllBytes(artworkLocation);
        string originalHash = ComputeHash(originalBytesForHash);
        Logger.Debug<ArtworkManager>($"Original artwork XXHash3 computed: {originalHash}");

        // Direct hash-based filename lookup (O(1) operation - no scanning needed!)
        string hashBasedFilename = $"{originalHash}{Path.GetExtension(artworkLocation)}";
        string hashBasedPath = Path.Combine(AppPaths.ImageCacheDirectory, hashBasedFilename);

        if (File.Exists(hashBasedPath))
        {
            Logger.Info<ArtworkManager>($"Found matching cached artwork via hash-based filename: {hashBasedPath}");
            // Add to the path-based cache with metadata
            ArtworkCache[fullPath] = new CacheEntry(hashBasedPath, originalHash, currentFileSize, currentLastModified);
            Logger.Trace<ArtworkManager>("FindCachedArtwork operation completed successfully");
            return hashBasedPath;
        }

        Logger.Info<ArtworkManager>($"No matching cached artwork found for: {artworkLocation}");
        Logger.Trace<ArtworkManager>("FindCachedArtwork operation completed successfully");
        return null;
    }

    /// <summary>
    /// Loads artwork from the cache, copying it there if it doesn't exist or has changed.
    /// This helps prevent file locking issues when the original artwork is in use elsewhere.
    /// Uses a multi-level caching strategy: in-memory Bitmap cache → file path cache → disk cache.
    /// Optimization: Reads the file once, then uses the bytes for both hashing and caching.
    /// Uses XXHash3 hash as filename for O(1) lookup and automatic deduplication.
    /// </summary>
    /// <param name="artworkLocation">The path to the original artwork file.</param>
    /// <returns>A Bitmap instance loaded from the cached artwork.</returns>
    public static Bitmap CacheLoadArtwork(string artworkLocation)
    {
        Logger.Trace<ArtworkManager>($"Starting CacheLoadArtwork operation for {artworkLocation}");

        string? cachedArtwork = FindCachedArtwork(artworkLocation);

        if (cachedArtwork is null)
        {
            // Read the file once for both hashing and caching (optimization for large libraries)
            byte[] artworkBytes = File.ReadAllBytes(artworkLocation);
            string hash = ComputeHash(artworkBytes);

            // Get file metadata for cache entry
            FileInfo fileInfo = new FileInfo(artworkLocation);
            long fileSize = fileInfo.Length;
            DateTime lastModified = fileInfo.LastWriteTimeUtc;

            // Use hash as filename for O(1) lookup and automatic deduplication
            string hashBasedFilename = $"{hash}{Path.GetExtension(artworkLocation)}";
            string cachedPath = Path.Combine(AppPaths.ImageCacheDirectory, hashBasedFilename);

            // Check if the file was created between our check and now (race condition handling)
            if (!File.Exists(cachedPath))
            {
                File.WriteAllBytes(cachedPath, artworkBytes);
                Logger.Info<ArtworkManager>($"Created new hash-based cache file: {cachedPath}");
            }
            else
            {
                Logger.Info<ArtworkManager>($"Hash-based cache file already exists (deduplication): {cachedPath}");
            }

            // Add to the cache with full metadata
            string fullPath = Path.GetFullPath(artworkLocation);
            ArtworkCache[fullPath] = new CacheEntry(cachedPath, hash, fileSize, lastModified);

            cachedArtwork = cachedPath;
            Logger.Info<ArtworkManager>($"Copied artwork to cache: {cachedPath} (XXHash3 filename optimization)");
        }
        else
        {
            Logger.Debug<ArtworkManager>($"Using existing cached artwork: {cachedArtwork}");
        }

        // Load bitmap from disk
        Logger.Trace<ArtworkManager>($"Loading bitmap from disk: {cachedArtwork}");
        Bitmap result = new Bitmap(cachedArtwork);

        Logger.Info<ArtworkManager>($"Successfully loaded artwork from cache: {cachedArtwork}");
        Logger.Trace<ArtworkManager>("CacheLoadArtwork operation completed successfully");
        return result;
    }

    /// <summary>
    /// Preloads an image from the specified location using the caching mechanism.
    /// This is a convenience method that wraps CacheLoadArtwork.
    /// </summary>
    /// <param name="artworkLocation">The path to the artwork file to preload.</param>
    /// <returns>A Bitmap instance loaded from the cached artwork.</returns>
    public static Bitmap PreloadImage(string artworkLocation)
    {
        Logger.Trace<ArtworkManager>($"Starting PreloadImage operation for {artworkLocation}");
        Bitmap result = CacheLoadArtwork(artworkLocation);
        Logger.Info<ArtworkManager>($"Successfully preloaded image from {artworkLocation}");
        Logger.Trace<ArtworkManager>("PreloadImage operation completed successfully");
        return result;
    }

    /// <summary>
    /// Clears all in-memory artwork caches.
    /// This does not delete cached files from the disk, only removes the memory references.
    /// Useful when freeing memory or when a full cache refresh is needed.
    /// </summary>
    public static void ClearCache()
    {
        Logger.Info<ArtworkManager>("Clearing all in-memory artwork caches");
        ArtworkCache.Clear();
        Logger.Debug<ArtworkManager>("All in-memory artwork caches cleared successfully");
    }

    /// <summary>
    /// Removes a specific artwork entry from all in-memory caches.
    /// This does not delete the cached file from the disk.
    /// </summary>
    /// <param name="artworkLocation">The path to the original artwork file to remove from the cache.</param>
    public static void RemoveFromCache(string artworkLocation)
    {
        string fullPath = Path.GetFullPath(artworkLocation);
        if (ArtworkCache.TryRemove(fullPath, out CacheEntry? cachedEntry))
        {
            Logger.Debug<ArtworkManager>($"Removed artwork from all in-memory caches: {artworkLocation}");
        }
        else
        {
            Logger.Trace<ArtworkManager>($"Artwork not found in in-memory caches: {artworkLocation}");
        }
    }

    /// <summary>
    /// Gets statistics about the current state of all artwork caches.
    /// Useful for debugging and monitoring cache performance.
    /// </summary>
    /// <returns>A string containing cache statistics.</returns>
    public static string GetCacheStatistics()
    {
        int artworkCacheCount = ArtworkCache.Count;

        // Calculate total cache size
        long totalCacheSize = ArtworkCache.Values.Sum(e => e.FileSize);
        string sizeString = totalCacheSize > 1024 * 1024
            ? $"{totalCacheSize / (1024 * 1024)} MB"
            : $"{totalCacheSize / 1024} KB";

        return $"Artwork Cache: {artworkCacheCount} entries ({sizeString})";
    }

    /// <summary>
    /// Clears cached image files from the disk that are not currently referenced in the in-memory caches.
    /// This helps free up disk space by removing orphaned cache files that are no longer in use.
    /// Should be called after the library is fully loaded to ensure all active artwork is cached.
    /// </summary>
    /// <returns>The number of orphaned cache files that were deleted.</returns>
    public static int ClearUnusedCachedArtwork()
    {
        Logger.Info<ArtworkManager>("Starting ClearUnusedCachedArtwork operation to remove orphaned cache files");

        if (!Directory.Exists(AppPaths.ImageCacheDirectory))
        {
            Logger.Debug<ArtworkManager>("Image cache directory does not exist, nothing to clean up");
            return 0;
        }

        // Collect all cached file paths from in-memory caches
        HashSet<string> activeCachedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (CacheEntry cachedEntry in ArtworkCache.Values)
        {
            activeCachedPaths.Add(cachedEntry.CachedPath);
        }

        Logger.Debug<ArtworkManager>($"Found {activeCachedPaths.Count} active cached file paths in memory");

        int deletedCount = 0;
        string[] cacheFiles = Directory.GetFiles(AppPaths.ImageCacheDirectory);

        Logger.Debug<ArtworkManager>($"Scanning {cacheFiles.Length} files in cache directory");

        foreach (string cacheFile in cacheFiles)
        {
            // If the file is not in the active cache, it's orphaned and can be deleted
            if (!activeCachedPaths.Contains(cacheFile))
            {
                try
                {
                    File.Delete(cacheFile);
                    deletedCount++;
                    Logger.Debug<ArtworkManager>($"Deleted orphaned cache file: {cacheFile}");
                }
                catch (Exception ex)
                {
                    Logger.Warning<ArtworkManager>($"Failed to delete orphaned cache file {cacheFile}");
                    Logger.LogExceptionDetails<ArtworkManager>(ex);
                }
            }
            else
            {
                Logger.Trace<ArtworkManager>($"Keeping active cache file: {cacheFile}");
            }
        }

        Logger.Info<ArtworkManager>($"ClearUnusedCachedArtwork operation completed. Deleted {deletedCount} orphaned cache files out of {cacheFiles.Length} total files");
        return deletedCount;
    }
}

/// <summary>
/// Minimal ICO encoder using SkiaSharp.
/// Supports embedding one or more PNG frames into a .ico container.
/// Handles the creation of Windows-compatible icon files from various image sources.
/// </summary>
public class IcoEncoder
{
    /// <summary>
    /// Standard icon sizes used for Windows shortcuts.
    /// These sizes provide compatibility with various Windows interface elements.
    /// </summary>
    private static readonly int[] DefaultSizes = [16, 32, 48, 256];

    /// <summary>
    /// Creates a .ico file from raw image bytes with the standard icon sizes (16, 32, 48, 256).
    /// </summary>
    /// <param name="imageData">Source image bytes (any format SkiaSharp can decode).</param>
    /// <param name="savePath">Destination .ico file path.</param>
    /// <exception cref="InvalidOperationException">Thrown when the source image cannot be decoded.</exception>
    public static void Encode(byte[] imageData, string savePath)
    {
        Logger.Trace<IcoEncoder>($"Starting Encode operation with raw image data to {savePath} (Standard sizes)");
        Encode(imageData, savePath, DefaultSizes);
        Logger.Info<IcoEncoder>($"Successfully encoded and saved icon to {savePath}");
        Logger.Trace<IcoEncoder>("Encode operation completed successfully");
    }

    /// <summary>
    /// Creates a .ico file from raw image bytes with custom sizes.
    /// </summary>
    /// <param name="imageData">Source image bytes.</param>
    /// <param name="savePath">Destination .ico file path.</param>
    /// <param name="sizes">Array of pixel sizes for each icon frame (e.g., 16, 32, 48, 256).</param>
    /// <exception cref="InvalidOperationException">Thrown when the source image cannot be decoded.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when any size is not between 1 and 256.</exception>
    /// <exception cref="ArgumentException">Thrown when no sizes are specified.</exception>
    public static void Encode(byte[] imageData, string savePath, int[] sizes)
    {
        Logger.Trace<IcoEncoder>($"Starting Encode operation with raw image data to {savePath} (Custom sizes: {string.Join(", ", sizes)})");
        using SKBitmap source = SKBitmap.Decode(imageData)
                                ?? throw new InvalidOperationException("Failed to decode source image.");
        Logger.Debug<IcoEncoder>($"Successfully decoded source image with dimensions {source.Width}x{source.Height}");
        Encode(source, savePath, sizes);
        Logger.Info<IcoEncoder>($"Successfully encoded and saved icon to {savePath}");
        Logger.Trace<IcoEncoder>("Encode operation completed successfully");
    }

    /// <summary>
    /// Creates a .ico file from an image file path with the standard icon sizes.
    /// </summary>
    /// <param name="filePath">Path to the source image file.</param>
    /// <param name="savePath">Destination .ico file path.</param>
    /// <exception cref="InvalidOperationException">Thrown when the source image cannot be decoded.</exception>
    public static void Encode(string filePath, string savePath)
    {
        Logger.Trace<IcoEncoder>($"Starting Encode operation with file {filePath} to {savePath} (Standard sizes)");
        Encode(filePath, savePath, DefaultSizes);
        Logger.Info<IcoEncoder>($"Successfully encoded and saved icon to {savePath}");
        Logger.Trace<IcoEncoder>("Encode operation completed successfully");
    }

    /// <summary>
    /// Creates a .ico file from an image file path with custom sizes.
    /// </summary>
    /// <param name="filePath">Path to the source image file.</param>
    /// <param name="savePath">Destination .ico file path.</param>
    /// <param name="sizes">Array of pixel sizes for each icon frame.</param>
    /// <exception cref="InvalidOperationException">Thrown when the source image cannot be decoded.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when any size is not between 1 and 256.</exception>
    /// <exception cref="ArgumentException">Thrown when no sizes are specified.</exception>
    public static void Encode(string filePath, string savePath, int[] sizes)
    {
        Logger.Trace<IcoEncoder>($"Starting Encode operation with file {filePath} to {savePath} (Custom sizes: {string.Join(", ", sizes)})");
        using SKBitmap source = SKBitmap.Decode(filePath)
                                ?? throw new InvalidOperationException($"Failed to decode image: {filePath}");
        Logger.Debug<IcoEncoder>($"Successfully decoded source image {filePath} with dimensions {source.Width}x{source.Height}");
        Encode(source, savePath, sizes);
        Logger.Info<IcoEncoder>($"Successfully encoded and saved icon to {savePath}");
        Logger.Trace<IcoEncoder>("Encode operation completed successfully");
    }

    /// <summary>
    /// Creates a .ico file from an SKBitmap source with the specified sizes.
    /// This method implements the ICO file format specification by creating a container
    /// that holds multiple PNG-encoded frames at different resolutions.
    ///
    /// ICO file format:
    /// ┌──────────────────────────────┐
    /// │ ICONDIR (6 bytes)            │
    /// │   Reserved: 0               │
    /// │   Type: 1 (icon)            │
    /// │   Count: number of images    │
    /// ├──────────────────────────────┤
    /// │ ICONDIRENTRY[] (16 bytes ea) │
    /// │   Width, Height, Colors      │
    /// │   Data size, Data offset     │
    /// ├──────────────────────────────┤
    /// │ PNG data for frame 0         │
    /// │ PNG data for frame 1         │
    /// │ ...                          │
    /// └──────────────────────────────┘
    /// </summary>
    /// <param name="source">The source bitmap to encode into the ICO file.</param>
    /// <param name="savePath">The path where the .ico file will be saved.</param>
    /// <param name="sizes">Array of pixel sizes for each icon frame.</param>
    /// <exception cref="ArgumentNullException">Thrown when sizes array is null.</exception>
    /// <exception cref="ArgumentException">Thrown when no sizes are specified.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when any size is not between 1 and 256.</exception>
    /// <exception cref="InvalidOperationException">Thrown when encoding fails.</exception>
    private static void Encode(SKBitmap source, string savePath, int[] sizes)
    {
        Logger.Trace<IcoEncoder>($"Starting Encode operation for bitmap {source.Width}x{source.Height} to {savePath} (Sizes: {string.Join(", ", sizes)})");

        ArgumentNullException.ThrowIfNull(sizes);
        if (sizes.Length == 0)
        {
            Logger.Error<IcoEncoder>("No sizes specified for ICO encoding");
            throw new ArgumentException("At least one size must be specified.", nameof(sizes));
        }

        Logger.Debug<IcoEncoder>($"Encoding ICO with {sizes.Length} different sizes: {string.Join(", ", sizes)}");

        // Generate PNG data for each size
        List<byte[]> frames = new List<byte[]>(sizes.Length);
        foreach (int size in sizes)
        {
            if (size is < 1 or > 256)
            {
                Logger.Error<IcoEncoder>($"Icon size must be between 1 and 256, got {size}");
                throw new ArgumentOutOfRangeException(nameof(sizes), $"Icon size must be between 1 and 256, got {size}.");
            }

            Logger.Debug<IcoEncoder>($"Processing icon size: {size}x{size}");
            using SKBitmap resized = new SKBitmap(size, size, source.ColorType, source.AlphaType);
            source.ScalePixels(resized, SKFilterQuality.High);

            using SKImage? image = SKImage.FromBitmap(resized);
            using SKData pngData = image.Encode(SKEncodedImageFormat.Png, 100)
                                   ?? throw new InvalidOperationException(
                                       $"Failed to encode {size}x{size} frame as PNG.");
            frames.Add(pngData.ToArray());
            Logger.Debug<IcoEncoder>($"Successfully encoded {size}x{size} frame, size: {pngData.ToArray().Length} bytes");
        }

        Logger.Debug<IcoEncoder>($"Writing ICO file to {savePath} with {frames.Count} frames");

        // Write an ICO file
        using FileStream output = File.Create(savePath);
        using BinaryWriter writer = new BinaryWriter(output);

        // "ICONDIR" header (6 bytes)
        writer.Write((ushort)0); // Reserved, must be 0
        writer.Write((ushort)1); // Type: 1 = ICO
        writer.Write((ushort)frames.Count); // Number of images
        Logger.Debug<IcoEncoder>($"Wrote ICO header with {frames.Count} images");

        // Calculate where PNG data starts (after header + all directory entries)
        int dataOffset = 6 + (frames.Count * 16);

        // "ICONDIRENTRY" for each frame (16 bytes each)
        for (int i = 0; i < frames.Count; i++)
        {
            byte widthByte = sizes[i] == 256 ? (byte)0 : (byte)sizes[i]; // 0 = 256px
            byte heightByte = sizes[i] == 256 ? (byte)0 : (byte)sizes[i]; // 0 = 256px

            writer.Write(widthByte); // Width
            writer.Write(heightByte); // Height
            writer.Write((byte)0); // Color palette count (0 = no palette)
            writer.Write((byte)0); // Reserved
            writer.Write((ushort)1); // Color planes
            writer.Write((ushort)32); // Bits per pixel
            writer.Write((uint)frames[i].Length); // PNG data size
            writer.Write((uint)dataOffset); // Offset to PNG data

            Logger.Debug<IcoEncoder>($"Wrote ICONDIRENTRY for frame {i}: {sizes[i]}x{sizes[i]}, size: {frames[i].Length} bytes, offset: {dataOffset}");
            dataOffset += frames[i].Length;
        }

        // PNG data for each frame
        foreach (byte[] frameData in frames)
        {
            writer.Write(frameData);
        }

        Logger.Info<IcoEncoder>($"Successfully created ICO file at {savePath} with {frames.Count} frames");
        Logger.Trace<IcoEncoder>("Encode operation completed successfully");
    }
}