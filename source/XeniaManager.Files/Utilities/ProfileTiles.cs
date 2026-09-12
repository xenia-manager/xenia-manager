using XeniaManager.Logging;

namespace XeniaManager.Files.Utilities;

/// <summary>
/// Loose profile tile PNGs (gamer pictures) stored next to a profile's GPDs:
/// <c>tile_64.png</c>, <c>tile_32.png</c>, <c>pp_64/32.png</c>, <c>avtr_64/32.png</c>.
/// Only the 64/32 gamer tiles are preloaded and validated.
/// </summary>
public class ProfileTiles
{
    /// <summary>
    /// Large gamer tile filename.
    /// </summary>
    public const string GamerTileLarge = "tile_64.png";

    /// <summary>
    /// Small gamer tile filename.
    /// </summary>
    public const string GamerTileSmall = "tile_32.png";

    /// <summary>
    /// Large personal tile filename.
    /// </summary>
    public const string PersonalTileLarge = "pp_64.png";

    /// <summary>
    /// Small personal tile filename.
    /// </summary>
    public const string PersonalTileSmall = "pp_32.png";

    /// <summary>
    /// Large avatar tile filename.
    /// </summary>
    public const string AvatarTileLarge = "avtr_64.png";

    /// <summary>
    /// Small avatar tile filename.
    /// </summary>
    public const string AvatarTileSmall = "avtr_32.png";

    /// <summary>
    /// Expected dimension of large tiles.
    /// </summary>
    public const int LargeSize = 64;

    /// <summary>
    /// Expected dimension of small tiles.
    /// </summary>
    public const int SmallSize = 32;

    /// <summary>
    /// Known tile filenames checked by <see cref="LoadAllTiles"/>.
    /// </summary>
    public static readonly string[] KnownFileNames =
    [
        GamerTileLarge, GamerTileSmall,
        PersonalTileLarge, PersonalTileSmall,
        AvatarTileLarge, AvatarTileSmall
    ];

    /// <summary>
    /// Reads PNG dimensions from the IHDR chunk without decoding the image.
    /// </summary>
    /// <param name="png">The raw PNG bytes.</param>
    /// <param name="width">The image width on success.</param>
    /// <param name="height">The image height on success.</param>
    /// <returns>True when the signature and IHDR chunk parse, false otherwise.</returns>
    public static bool TryGetPngDimensions(byte[] png, out int width, out int height)
    {
        width = 0;
        height = 0;
        // Signature (8) + length (4) + "IHDR" (4) + width (4) + height (4).
        if (png.Length < 24)
        {
            return false;
        }

        if (png[0] != 0x89 || png[1] != 0x50 || png[2] != 0x4E || png[3] != 0x47 ||
            png[4] != 0x0D || png[5] != 0x0A || png[6] != 0x1A || png[7] != 0x0A)
        {
            return false;
        }

        if (png[12] != (byte)'I' || png[13] != (byte)'H' || png[14] != (byte)'D' || png[15] != (byte)'R')
        {
            return false;
        }

        width = (png[16] << 24) | (png[17] << 16) | (png[18] << 8) | png[19];
        height = (png[20] << 24) | (png[21] << 16) | (png[22] << 8) | png[23];
        return width > 0 && height > 0;
    }

    /// <summary>
    /// Checks PNG signature plus exact square dimensions (only 64x64 or 32x32 tiles are accepted).
    /// </summary>
    /// <param name="png">The raw PNG bytes.</param>
    /// <param name="expectedSize">The expected width and height.</param>
    /// <returns>True when the PNG is well-formed with the expected dimensions.</returns>
    public static bool IsValidTile(byte[] png, int expectedSize)
    {
        return TryGetPngDimensions(png, out int width, out int height)
               && width == expectedSize && height == expectedSize;
    }

    /// <summary>
    /// Loads a tile PNG from a profile directory. Never throws; null when missing or invalid.
    /// </summary>
    /// <param name="profileDir">The profile directory (the folder holding <c>FFFE07D1.gpd</c>).</param>
    /// <param name="fileName">The tile filename (see <see cref="KnownFileNames"/>).</param>
    /// <returns>The PNG bytes, or null when the file is missing or not a PNG.</returns>
    public static byte[]? LoadTile(string profileDir, string fileName)
    {
        try
        {
            string path = Path.Combine(profileDir, fileName);
            if (!File.Exists(path))
            {
                return null;
            }

            byte[] data = File.ReadAllBytes(path);
            if (!TryGetPngDimensions(data, out _, out _))
            {
                Logger.Warning<ProfileTiles>($"Tile '{fileName}' is not a valid PNG");
                return null;
            }

            return data;
        }
        catch (Exception ex)
        {
            Logger.Trace<ProfileTiles>($"LoadTile failed for '{fileName}': {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Loads every known tile PNG present in a profile directory.
    /// </summary>
    /// <param name="profileDir">The profile directory (the folder holding <c>FFFE07D1.gpd</c>).</param>
    /// <returns>Filename/data pairs for each tile found. Never throws.</returns>
    public static IReadOnlyList<(string FileName, byte[] Data)> LoadAllTiles(string profileDir)
    {
        List<(string FileName, byte[] Data)> tiles = [];
        foreach (string fileName in KnownFileNames)
        {
            byte[]? data = LoadTile(profileDir, fileName);
            if (data != null)
            {
                tiles.Add((fileName, data));
            }
        }

        return tiles;
    }

    /// <summary>
    /// Writes a tile PNG into a profile directory after validating its dimensions.
    /// </summary>
    /// <param name="profileDir">The profile directory (created when missing).</param>
    /// <param name="fileName">The tile filename (see <see cref="KnownFileNames"/>).</param>
    /// <param name="png">The PNG bytes to write.</param>
    /// <param name="expectedSize">The expected width and height (64 or 32).</param>
    /// <exception cref="InvalidDataException">Thrown when the PNG is malformed, mis-sized, or escapes the profile directory.</exception>
    public static void SaveTile(string profileDir, string fileName, byte[] png, int expectedSize)
    {
        if (!IsValidTile(png, expectedSize))
        {
            throw new InvalidDataException($"Tile '{fileName}' is not a valid {expectedSize}x{expectedSize} PNG");
        }

        Directory.CreateDirectory(profileDir);
        string safePath;
        try
        {
            safePath = ArchiveExtractor.GetSafeEntryOutputPath(Path.GetFullPath(profileDir), fileName);
        }
        catch (IOException ex)
        {
            throw new InvalidDataException($"Tile filename '{fileName}' escapes the profile directory: {ex.Message}", ex);
        }

        File.WriteAllBytes(safePath, png);
        Logger.Info<ProfileTiles>($"Saved tile '{fileName}' ({png.Length} bytes)");
    }
}