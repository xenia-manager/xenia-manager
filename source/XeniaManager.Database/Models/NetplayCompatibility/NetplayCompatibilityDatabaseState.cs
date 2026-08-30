namespace XeniaManager.Database.Models.NetplayCompatibility;

/// <summary>
/// Internal state container for the netplay compatibility database.
/// Encapsulates all mutable state for the netplay compatibility database.
/// </summary>
internal sealed class NetplayCompatibilityDatabaseState
{
    /// <summary>
    /// Gets the set of normalized (uppercase) title IDs for fast existence checks and iteration.
    /// </summary>
    public HashSet<string> TitleIds { get; } = [];

    /// <summary>
    /// Gets the mapping of normalized title IDs to their corresponding NetplayCompatibilityEntry objects.
    /// Enables O(1) lookup of netplay compatibility information by title ID.
    /// </summary>
    public Dictionary<string, NetplayCompatibilityEntry> TitleIdGameMap { get; } = new Dictionary<string, NetplayCompatibilityEntry>();

    /// <summary>
    /// Gets or sets the filtered list of games matching the current search query.
    /// Contains the full NetplayCompatibilityEntry objects for display purposes.
    /// </summary>
    public List<NetplayCompatibilityEntry> FilteredDatabase { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether the database has been loaded from the remote source.
    /// Prevents redundant loading operations.
    /// </summary>
    public bool IsLoaded { get; set; }
}