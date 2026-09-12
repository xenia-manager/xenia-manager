namespace XeniaManager.Files.Browsing;

/// <summary>
/// A single file or directory entry in a browsable game container (ISO, ZAR, STFS, SVOD, or loose directory).
/// </summary>
public sealed class GameFileNode
{
    /// <summary>
    /// Gets the entry name within its parent directory (e.g., <c>default.xex</c>).
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the full path from the container root using <c>/</c> separators (e.g., <c>game/data.bin</c>).
    /// </summary>
    public required string FullPath { get; init; }

    /// <summary>
    /// Gets whether this entry is a file (true) or a directory (false).
    /// </summary>
    public required bool IsFile { get; init; }

    /// <summary>
    /// Gets the file size in bytes (0 for directories).
    /// </summary>
    public ulong Size { get; init; }
}