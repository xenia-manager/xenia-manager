namespace XeniaManager.BigScreen.Models;

/// <summary>
/// Gamepad buttons relevant to BigScreen navigation.
/// Stick and bumper input is normalized onto the D-pad values by the service.
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
}
