namespace XeniaManager.BigScreen.Models;

/// <summary>
/// Navigation-relevant actions produced by either input source (keyboard or gamepad).
/// The active screen or modal decides what each command does.
/// </summary>
public enum NavigationCommand
{
    MoveLeft,
    MoveRight,
    MoveUp,
    MoveDown,
    Activate,
    Back,
    CycleSort,
    ToggleView,
    Start,
}