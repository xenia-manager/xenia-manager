namespace XeniaManager.Files.Browsing;

/// <summary>
/// Read-only browsing over a game container (ISO, ZAR, STFS, SVOD, or loose directory).
/// Paths use <c>/</c> separators; lookup is case-insensitive; <c>""</c> addresses the root.
/// </summary>
public interface IGameFileSource : IDisposable
{
    /// <summary>
    /// Gets a flat list of all files in the container with their full paths and sizes.
    /// Empty when the container has no browsable tree.
    /// </summary>
    IReadOnlyList<GameFileNode> Files { get; }

    /// <summary>
    /// Lists the immediate children of a directory by path.
    /// </summary>
    /// <param name="path">The directory path (e.g., "game" or "" for root).</param>
    /// <returns>A list of child entries, or null if the path does not exist or is a file.</returns>
    List<GameFileNode>? ListDirectory(string path);

    /// <summary>
    /// Reads the full contents of a file identified by its path within the container.
    /// </summary>
    /// <param name="path">The path to the file (e.g., "default.xex").</param>
    /// <returns>The complete file data, or null if the file was not found or is a directory.</returns>
    byte[]? ReadFile(string path);

    /// <summary>
    /// Finds the preferred XEX full path (<c>default.xex</c> first, then any <c>.xex</c>).
    /// </summary>
    /// <returns>The XEX full path, or null when the container holds no XEX.</returns>
    string? FindDefaultXexPath();

    /// <summary>
    /// Gets a short human-readable format label for this container (e.g., "ISO", "Loose").
    /// </summary>
    string FormatName { get; }

    /// <summary>
    /// Gets a longer description of the container format.
    /// </summary>
    string FormatDescription { get; }
}