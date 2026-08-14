namespace XeniaManager.BigScreen.Models;

/// <summary>
/// Sort order for the achievements list; X cycles through the options.
/// </summary>
public enum AchievementSort
{
    /// <summary>
    /// Achieved (unlocked) achievements first, in original order.
    /// </summary>
    Achieved,

    /// <summary>
    /// By gamerscore awarded, highest first.
    /// </summary>
    GamerscoreAwarded,

    /// <summary>
    /// Alphabetically by achievement name.
    /// </summary>
    Alphabetical,
}
