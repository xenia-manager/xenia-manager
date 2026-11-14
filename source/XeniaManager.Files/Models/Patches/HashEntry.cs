namespace XeniaManager.Files.Models.Patches;

/// <summary>
/// Represents one entry in the module hash array, including entries that are
/// commented out in the source file.
/// </summary>
public class HashEntry
{
    /// <summary>
    /// The module hash in uppercase hex.
    /// </summary>
    public string Hash { get; set; } = string.Empty;

    /// <summary>
    /// Optional inline comment associated with the hash.
    /// </summary>
    public string? Comment { get; set; }

    /// <summary>
    /// Whether this hash is commented out in the source file.
    /// </summary>
    public bool IsCommented { get; set; }

    /// <summary>
    /// Creates a new instance of HashEntry.
    /// </summary>
    public HashEntry()
    {
    }

    /// <summary>
    /// Creates a new instance of HashEntry with the specified parameters.
    /// </summary>
    public HashEntry(string hash, string? comment = null, bool isCommented = false)
    {
        Hash = hash;
        Comment = comment;
        IsCommented = isCommented;
    }
}