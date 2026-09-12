namespace XeniaManager.Files.Models.Spa;

/// <summary>
/// Stats-view type, decoded from the low nibble of a view's flags.
/// </summary>
public enum ViewType : uint
{
    /// <summary>
    /// Leaderboard view.
    /// </summary>
    Leaderboard = 0,

    /// <summary>
    /// Context-by-property view.
    /// </summary>
    ContextByProperty = 1,

    /// <summary>
    /// Context-by-context view.
    /// </summary>
    ContextByContext = 2
}