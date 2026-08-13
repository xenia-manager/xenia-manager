using SDL;
using XeniaManager.Core.Models;

namespace XeniaManager.Core.Services;

/// <summary>
/// Pure mapping between SDL gamepad buttons and navigation buttons.
/// No SDL state - kept separate from the polling service so it's unit-testable.
/// </summary>
public static class GamepadButtonMapper
{
    /// <summary>
    /// Stick axis magnitude beyond which a direction counts as pressed (0-32767).
    /// </summary>
    public const short AxisDeadzone = 16000;

    /// <summary>
    /// Maps an SDL gamepad button to a navigation button, or null when the
    /// button isn't navigation-relevant.
    /// </summary>
    public static GamepadButton? Map(SDL_GamepadButton button)
    {
        return button switch
        {
            SDL_GamepadButton.SDL_GAMEPAD_BUTTON_SOUTH => GamepadButton.A,
            SDL_GamepadButton.SDL_GAMEPAD_BUTTON_EAST => GamepadButton.B,
            SDL_GamepadButton.SDL_GAMEPAD_BUTTON_WEST => GamepadButton.X,
            SDL_GamepadButton.SDL_GAMEPAD_BUTTON_NORTH => GamepadButton.Y,
            SDL_GamepadButton.SDL_GAMEPAD_BUTTON_DPAD_LEFT => GamepadButton.DpadLeft,
            SDL_GamepadButton.SDL_GAMEPAD_BUTTON_DPAD_RIGHT => GamepadButton.DpadRight,
            SDL_GamepadButton.SDL_GAMEPAD_BUTTON_DPAD_UP => GamepadButton.DpadUp,
            SDL_GamepadButton.SDL_GAMEPAD_BUTTON_DPAD_DOWN => GamepadButton.DpadDown,
            SDL_GamepadButton.SDL_GAMEPAD_BUTTON_LEFT_SHOULDER => GamepadButton.LeftShoulder,
            SDL_GamepadButton.SDL_GAMEPAD_BUTTON_RIGHT_SHOULDER => GamepadButton.RightShoulder,
            SDL_GamepadButton.SDL_GAMEPAD_BUTTON_BACK => GamepadButton.View,
            _ => null,
        };
    }
}
