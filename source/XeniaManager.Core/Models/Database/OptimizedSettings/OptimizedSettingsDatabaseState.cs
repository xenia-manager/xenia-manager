namespace XeniaManager.Core.Models.Database.OptimizedSettings;

/// <summary>
/// Internal state container for the optimized settings database.
/// Encapsulates all mutable states for the optimized settings database.
/// </summary>
internal sealed class OptimizedSettingsDatabaseState
{
    /// <summary>
    /// Gets the set of normalized (uppercase) title IDs for fast existence checks and iteration.
    /// </summary>
    public HashSet<string> TitleIds { get; } = [];

    /// <summary>
    /// Gets the mapping of normalized title IDs to their corresponding OptimizedSettingsEntry objects.
    /// Enables O(1) lookup of optimized settings information by title ID.
    /// </summary>
    public Dictionary<string, OptimizedSettingsEntry> TitleIdGameMap { get; } = new Dictionary<string, OptimizedSettingsEntry>();

    /// <summary>
    /// Gets or sets the filtered list of entries matching the current search query.
    /// Contains the full OptimizedSettingsEntry objects for display purposes.
    /// </summary>
    public List<OptimizedSettingsEntry> FilteredDatabase { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether the database has been loaded from the remote source.
    /// Prevents redundant loading operations.
    /// </summary>
    public bool IsLoaded { get; set; }
}