namespace XeniaManager.Files.Models.Iso;

/// <summary>
/// Represents a single file or directory in an Xbox ISO (GDFX) filesystem.
/// Returned by <see cref="Files.IsoFile"/> browsing APIs.
/// </summary>
public sealed class GdfxEntry
{
    /// <summary>
    /// Gets the entry name within its parent directory (e.g., <c>default.xex</c>).
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the full path from the disc root using <c>/</c> separators (e.g., <c>game/data.bin</c>).
    /// </summary>
    public string FullPath { get; init; } = string.Empty;

    /// <summary>
    /// Gets whether this entry is a file (true) or a directory (false).
    /// </summary>
    public bool IsFile { get; init; }

    /// <summary>
    /// Gets the file size in bytes (0 for directories).
    /// </summary>
    public ulong Size { get; init; }

    /// <summary>
    /// Gets the raw GDFX start sector (add <c>XgdInformation.BaseSector</c> for the absolute sector).
    /// Only meaningful for files.
    /// </summary>
    public uint Sector { get; init; }
}