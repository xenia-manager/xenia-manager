namespace XeniaManager.Core.Models.Patches;

/// <summary>
/// Represents the root structure of a patch TOML file.
/// Contains metadata about the game and all patch entries.
/// </summary>
public class PatchDocument
{
    /// <summary>
    /// The name of the game title (e.g., "Forza Horizon").
    /// </summary>
    public string TitleName { get; set; } = string.Empty;

    /// <summary>
    /// The title ID in uppercase hex (e.g., "4D5309C9").
    /// Format: AB-1234 becomes 4D5309C9.
    /// </summary>
    public string TitleId { get; set; } = string.Empty;

    /// <summary>
    /// The module hash in uppercase hex (e.g., "D48ABF1704CE5C4A").
    /// This is the hash of the executable file (e.g., default.xex).
    /// Can be a single hash or multiple hashes for different executables.
    /// </summary>
    public List<string> Hashes { get; set; } = [];

    /// <summary>
    /// Optional media IDs with their associated comments.
    /// Used to identify specific disc versions (Redump links).
    /// Media IDs are often commented out in patch files.
    /// </summary>
    public List<MediaIdEntry> MediaIds { get; set; } = [];

    /// <summary>
    /// List of all patch entries in this document.
    /// Each entry corresponds to a [[patch]] table.
    /// </summary>
    public List<PatchEntry> Patches { get; set; } = [];

    /// <summary>
    /// Creates a new instance of PatchDocument.
    /// </summary>
    public PatchDocument()
    {
    }

    /// <summary>
    /// Creates a new instance of PatchDocument with the specified parameters.
    /// </summary>
    /// <param name="titleName">The name of the game title.</param>
    /// <param name="titleId">The title ID in uppercase hex.</param>
    /// <param name="hash">The primary module hash in uppercase hex.</param>
    /// <param name="mediaIds">Optional list of media IDs (without comments).</param>
    public PatchDocument(string titleName, string titleId, string hash, List<string>? mediaIds = null)
    {
        TitleName = titleName;
        TitleId = titleId;
        Hashes = [hash];
        if (mediaIds != null)
        {
            MediaIds = mediaIds.Select(id => new MediaIdEntry(id)).ToList();
        }
    }

    /// <summary>
    /// Adds a patch entry to this document.
    /// </summary>
    /// <param name="patch">The patch entry to add.</param>
    public void AddPatch(PatchEntry patch)
    {
        Patches.Add(patch);
    }

    /// <summary>
    /// Adds a new patch entry with the specified parameters.
    /// </summary>
    /// <param name="name">The name of the patch.</param>
    /// <param name="author">The author of the patch.</param>
    /// <param name="isEnabled">Whether the patch is enabled.</param>
    /// <param name="description">Optional description of the patch.</param>
    /// <returns>The created PatchEntry for further modification.</returns>
    public PatchEntry AddPatch(string name, string author, bool isEnabled = false, string? description = null)
    {
        PatchEntry patch = new PatchEntry(name, author, isEnabled, description);
        Patches.Add(patch);
        return patch;
    }
}