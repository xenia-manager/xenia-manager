namespace XeniaManager.Core.Models.Files.Gpd;

/// <summary>
/// Represents the type of achievement based on the Flags field.
/// These types categorize achievements by their completion criteria.
/// </summary>
public enum AchievementType : byte
{
    /// <summary>
    /// Completion-based achievement (complete the game, finish a level, etc.).
    /// </summary>
    Completion = 1,

    /// <summary>
    /// Leveling-based achievement (reach level X, accumulate points, etc.).
    /// </summary>
    Leveling = 2,

    /// <summary>
    /// Unlock-based achievement (unlock a character, weapon, etc.).
    /// </summary>
    Unlock = 3,

    /// <summary>
    /// Event-based achievement (perform a specific action, win a match, etc.).
    /// </summary>
    Event = 4,

    /// <summary>
    /// Tournament-based achievement (win a tournament, place in competition, etc.).
    /// </summary>
    Tournament = 5,

    /// <summary>
    /// Checkpoint-based achievement (reach a milestone, complete a chapter, etc.).
    /// </summary>
    Checkpoint = 6,

    /// <summary>
    /// Other achievement types that don't fit the standard categories.
    /// </summary>
    Other = 7
}
