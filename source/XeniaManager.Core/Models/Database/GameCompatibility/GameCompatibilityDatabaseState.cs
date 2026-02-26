namespace XeniaManager.Core.Models.Database.GameCompatibility;

/// <summary>
/// Internal state container for the game compatibility database.
/// Encapsulates all mutable state for the game compatibility database.
/// </summary>
internal sealed class GameCompatibilityDatabaseState
{
    /// <summary>
    /// Gets the set of normalized (uppercase) title IDs for fast existence checks and iteration.
    /// </summary>
    public HashSet<string> TitleIds { get; } = [];

    /// <summary>
    /// Gets the mapping of normalized title IDs to their corresponding GameCompatibilityEntry objects.
    /// Enables O(1) lookup of game compatibility information by title ID.
    /// </summary>
    public Dictionary<string, GameCompatibilityEntry> TitleIdGameMap { get; } = new Dictionary<string, GameCompatibilityEntry>();

    /// <summary>
    /// Gets or sets the filtered list of games matching the current search query.
    /// Contains the full GameCompatibilityEntry objects for display purposes.
    /// </summary>
    public List<GameCompatibilityEntry> FilteredDatabase { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether the database has been loaded from the remote source.
    /// Prevents redundant loading operations.
    /// </summary>
    public bool IsLoaded { get; set; }
}
