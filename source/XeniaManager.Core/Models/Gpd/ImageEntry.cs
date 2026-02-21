using System.Text;
using XeniaManager.Core.Logging;

namespace XeniaManager.Core.Models.Gpd;

/// <summary>
/// Represents an image entry in a GPD file.
/// Image entries contain PNG image data (icons, gamer pictures, etc.).
/// </summary>
public class ImageEntry
{
    /// <summary>
    /// Gets whether this image entry is valid (successfully parsed).
    /// </summary>
    public bool IsValid { get; private set; } = true;

    /// <summary>
    /// Gets the error message if this entry is invalid.
    /// </summary>
    public string? ValidationError { get; private set; }

    /// <summary>
    /// The PNG image data.
    /// </summary>
    public byte[] ImageData { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// Gets the image ID from the associated entry table entry.
    /// </summary>
    public uint ImageId { get; set; }

    /// <summary>
    /// Gets whether the image data appears to be a valid PNG.
    /// </summary>
    public bool IsValidPng
    {
        get
        {
            if (ImageData.Length < 8)
            {
                return false;
            }

            // PNG signature: 89 50 4E 47 0D 0A 1A 0A
            return ImageData[0] == 0x89 &&
                   ImageData[1] == 0x50 &&
                   ImageData[2] == 0x4E &&
                   ImageData[3] == 0x47 &&
                   ImageData[4] == 0x0D &&
                   ImageData[5] == 0x0A &&
                   ImageData[6] == 0x1A &&
                   ImageData[7] == 0x0A;
        }
    }

    /// <summary>
    /// Creates a new ImageEntry from PNG data.
    /// </summary>
    /// <param name="pngData">The PNG image bytes.</param>
    /// <returns>A new ImageEntry instance.</returns>
    public static ImageEntry FromPngData(byte[] pngData)
    {
        return new ImageEntry { ImageData = pngData };
    }

    /// <summary>
    /// Creates a new ImageEntry from raw entry data.
    /// If the data is invalid, returns an invalid entry instead of throwing.
    /// </summary>
    /// <param name="data">The raw byte data containing the image entry.</param>
    /// <param name="offset">The offset to start reading from.</param>
    /// <param name="length">The total length of the entry data.</param>
    /// <returns>The parsed ImageEntry (can be invalid if data is corrupted).</returns>
    public static ImageEntry FromBytes(byte[] data, int offset, uint length)
    {
        Logger.Trace<ImageEntry>($"Parsing image entry from bytes at offset {offset}, length {length}");

        ImageEntry entry = new ImageEntry();

        try
        {
            if (data.Length < offset + length)
            {
                Logger.Error<ImageEntry>($"Data too short for image entry (expected {length}, got {data.Length - offset})");
                entry.IsValid = false;
                entry.ValidationError = $"Data too short for image entry (expected {length}, got {data.Length - offset})";
                return entry;
            }

            byte[] imageData = new byte[length];
            Array.Copy(data, offset, imageData, 0, length);
            entry.ImageData = imageData;
            entry.IsValid = true;

            Logger.Debug<ImageEntry>($"Image entry parsed ({length} bytes), Valid PNG: {entry.IsValidPng}");
        }
        catch (Exception ex)
        {
            Logger.Error<ImageEntry>($"Failed to parse image entry");
            Logger.LogExceptionDetails<ImageEntry>(ex);
            entry.IsValid = false;
            entry.ValidationError = $"Failed to parse image entry: {ex.Message}";
        }

        return entry;
    }

    /// <summary>
    /// Converts the image entry to a byte array.
    /// </summary>
    /// <returns>The image entry as a byte array.</returns>
    public byte[] ToBytes()
    {
        Logger.Trace<ImageEntry>($"Converting image (ID: {ImageId}) to bytes ({ImageData.Length} bytes)");
        return ImageData;
    }

    /// <summary>
    /// Returns a string representation of the image entry.
    /// </summary>
    public override string ToString() => $"Image (ID: {ImageId}, Size: {ImageData.Length} bytes, Valid PNG: {IsValidPng})";
}