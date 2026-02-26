namespace XeniaManager.Core.Models.Files.Patches;

/// <summary>
/// Represents a media ID with optional comment (e.g., redump link and region info).
/// </summary>
public class MediaIdEntry
{
    /// <summary>
    /// The media ID in uppercase hex (e.g., "2B7A1346").
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Optional comment associated with the media ID (e.g., "Disc (Europe, Asia): http://redump.org/disc/84331").
    /// </summary>
    public string? Comment { get; set; }

    /// <summary>
    /// Whether this media ID was commented out in the original file.
    /// </summary>
    public bool IsCommented { get; set; }

    /// <summary>
    /// Creates a new instance of MediaIdEntry.
    /// </summary>
    public MediaIdEntry()
    {
    }

    /// <summary>
    /// Creates a new instance of MediaIdEntry with the specified parameters.
    /// </summary>
    /// <param name="id">The media ID in uppercase hex.</param>
    /// <param name="comment">Optional comment associated with the media ID.</param>
    /// <param name="isCommented">Whether this media ID was commented out in the original file.</param>
    public MediaIdEntry(string id, string? comment = null, bool isCommented = false)
    {
        Id = id;
        Comment = comment;
        IsCommented = isCommented;
    }
}