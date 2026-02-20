namespace XeniaManager.Core.Models.Database.Xbox;

/// <summary>
/// Internal state container for the Xbox marketplace database.
/// Encapsulates all mutable state for the Xbox database.
/// </summary>
internal sealed class XboxDatabaseState
{
    /// <summary>
    /// Gets the set of normalized (uppercase) title IDs for fast existence checks and iteration.
    /// </summary>
    public HashSet<string> TitleIds { get; } = [];

    /// <summary>
    /// Gets the mapping of normalized title IDs to their corresponding GameInfo objects.
    /// Enables O(1) lookup of game information by title ID.
    /// Multiple IDs (main ID and alternative IDs) can map to the same GameInfo object.
    /// </summary>
    public Dictionary<string, GameInfo> TitleIdGameMap { get; } = new Dictionary<string, GameInfo>();

    /// <summary>
    /// Gets or sets the filtered list of games matching the current search query.
    /// Contains the full GameInfo objects for display purposes.
    /// </summary>
    public List<GameInfo> FilteredDatabase { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether the database has been loaded from the remote source.
    /// Prevents redundant loading operations.
    /// </summary>
    public bool IsLoaded { get; set; }
}
