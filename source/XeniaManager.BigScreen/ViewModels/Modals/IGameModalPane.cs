using XeniaManager.BigScreen.Models;

namespace XeniaManager.BigScreen.ViewModels.Modals;

/// <summary>
/// A pane hosted inside the game modal that receives navigation input while
/// it is open.
/// </summary>
public interface IGameModalPane
{
    /// <summary>
    /// Handles a navigation command; returns true when the pane consumed it.
    /// </summary>
    bool HandleInput(NavigationCommand command);

    /// <summary>
    /// Called when the pane becomes the active column: selects its first item
    /// so exactly one element highlights (the nav list clears simultaneously).
    /// </summary>
    void OnPaneEntered();

    /// <summary>
    /// Called when the pane loses focus (returning to the nav list): clears its
    /// selection so exactly one element highlights.
    /// </summary>
    void OnPaneExited();
}
