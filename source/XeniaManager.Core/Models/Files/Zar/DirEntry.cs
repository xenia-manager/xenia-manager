namespace XeniaManager.Core.Models.Files.Zar;

/// <summary>
/// Represents a single entry in a directory listing, returned by ZarFile.ListDirectory.
/// </summary>
public class DirEntry
{
    /// <summary>
    /// Gets or sets the name of the file or directory.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether this entry is a file (true) or a directory (false).
    /// </summary>
    public bool IsFile { get; set; }

    /// <summary>
    /// Gets or sets the file size in bytes (0 for directories).
    /// </summary>
    public ulong Size { get; set; }
}