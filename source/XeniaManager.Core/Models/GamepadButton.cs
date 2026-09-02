namespace XeniaManager.Core.Models;

/// <summary>
/// Gamepad buttons relevant to UI navigation.
/// Stick and bumper input is normalised onto the D-pad values by the service.
/// </summary>
public enum GamepadButton
{
    DpadLeft,
    DpadRight,
    DpadUp,
    DpadDown,
    A,
    B,
    X,
    Y,
    LeftShoulder,
    RightShoulder,
    View,
    Start
}