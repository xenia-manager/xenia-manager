namespace XeniaManager.BigScreen.Models;

/// <summary>
/// The game modal panes that can be opened from the options list.
/// </summary>
public enum GameModalPane
{
    /// <summary>
    /// Per-game achievement list from the profile GPD.
    /// </summary>
    Achievements,

    /// <summary>
    /// Installed title updates for the game.
    /// </summary>
    TitleUpdates,

    /// <summary>
    /// Installed marketplace content for the game.
    /// </summary>
    MarketplaceContent,

    /// <summary>
    /// Screenshots captured for the game.
    /// </summary>
    Screenshots,

    /// <summary>
    /// Downloaded patches for the game.
    /// </summary>
    Patches,

    /// <summary>
    /// The game's config file editor.
    /// </summary>
    Settings
}