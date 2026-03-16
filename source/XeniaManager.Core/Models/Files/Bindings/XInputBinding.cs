namespace XeniaManager.Core.Models.Files.Bindings;

/// <summary>
/// Enumeration of Xbox 360 controller inputs that can be bound to keyboard keys.
/// Based on Xenia's kXInputButtons mapping.
/// </summary>
public enum XInputBinding
{
    // D-Pad
    [BindingName("Up")] Up,
    [BindingName("Down")] Down,
    [BindingName("Left")] Left,
    [BindingName("Right")] Right,

    // Menu buttons
    [BindingName("Start")] Start,
    [BindingName("Back")] Back,

    // Stick presses
    [BindingName("LS")] LS,
    [BindingName("RS")] RS,

    // Bumpers
    [BindingName("LB")] LB,
    [BindingName("RB")] RB,

    // Face buttons
    [BindingName("A")] A,
    [BindingName("B")] B,
    [BindingName("X")] X,
    [BindingName("Y")] Y,

    // Triggers
    [BindingName("LT")] LT,
    [BindingName("RT")] RT,

    // Left stick directions
    [BindingName("LS-Up")] LS_Up,
    [BindingName("LS-Down")] LS_Down,
    [BindingName("LS-Left")] LS_Left,
    [BindingName("LS-Right")] LS_Right,

    // Right stick directions
    [BindingName("RS-Up")] RS_Up,
    [BindingName("RS-Down")] RS_Down,
    [BindingName("RS-Left")] RS_Left,
    [BindingName("RS-Right")] RS_Right,

    // Special bindings
    [BindingName("Modifier")] Modifier,

    // Weapon slots
    [BindingName("weapon1")] Weapon1,
    [BindingName("weapon2")] Weapon2,
    [BindingName("weapon3")] Weapon3,
    [BindingName("weapon4")] Weapon4,
    [BindingName("weapon5")] Weapon5,
    [BindingName("weapon6")] Weapon6,
    [BindingName("weapon7")] Weapon7,
    [BindingName("weapon8")] Weapon8,
    [BindingName("weapon9")] Weapon9,
    [BindingName("weapon10")] Weapon10
}