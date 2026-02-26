namespace XeniaManager.Core.Models.Database.Patches;

/// <summary>
/// Internal state container for a patch database variant.
/// Encapsulates all mutable states for either Canary or Netplay patch databases.
/// </summary>
internal sealed class PatchDatabaseState
{
    /// <summary>
    /// Gets the set of normalized (uppercase) patch names for fast existence checks and iteration.
    /// </summary>
    public HashSet<string> PatchNames { get; } = [];

    /// <summary>
    /// Gets the mapping of normalized patch names to their corresponding PatchInfo objects.
    /// Enables O(1) lookup of patch information by name.
    /// </summary>
    public Dictionary<string, PatchInfo> PatchNameMap { get; } = new Dictionary<string, PatchInfo>();

    /// <summary>
    /// Gets or sets the filtered list of patches matching the current search query.
    /// Contains the full PatchInfo objects for display purposes.
    /// </summary>
    public List<PatchInfo> FilteredDatabase { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether the database has been loaded from the remote source.
    /// Prevents redundant loading operations.
    /// </summary>
    public bool IsLoaded { get; set; }
}