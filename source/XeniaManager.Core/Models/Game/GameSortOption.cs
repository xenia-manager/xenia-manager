namespace XeniaManager.Core.Models.Game;

/// <summary>
/// Represents the different sorting options available for organizing games in the library.
/// </summary>
public enum GameSortOption
{
    /// <summary>
    /// Sort games alphabetically by title.
    /// </summary>
    Title,

    /// <summary>
    /// Sort games by total playtime.
    /// </summary>
    Playtime,

    /// <summary>
    /// Sort games by compatibility status with the emulator.
    /// </summary>
    Compatibility,

    /// <summary>
    /// Sort games by their unique game identifier.
    /// </summary>
    GameId,

    /// <summary>
    /// Sort games by their media identifier.
    /// </summary>
    MediaId,

    /// <summary>
    /// Sort games by the Xenia emulator version.
    /// </summary>
    XeniaVersion
}