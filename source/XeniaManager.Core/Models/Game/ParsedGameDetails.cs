namespace XeniaManager.Core.Models.Game;

/// <summary>
/// Represents game details parsed from Xenia emulator output
/// Used during game scanning to extract title, title ID, and media ID
/// </summary>
public class ParsedGameDetails
{
    /// <summary>
    /// The game title extracted from Xenia output
    /// </summary>
    public string Title { get; set; } = "Not found";

    /// <summary>
    /// The title ID extracted from Xenia output
    /// </summary>
    public string TitleId { get; set; } = "00000000";

    /// <summary>
    /// The media ID extracted from Xenia output
    /// </summary>
    public string MediaId { get; set; } = "00000000";

    /// <summary>
    /// Gets a value indicating whether valid game details were extracted
    /// </summary>
    public bool IsValid => Title != "Not found" || TitleId != "00000000";
}