using SDL;

namespace XeniaManager.Core.Models;

/// <summary>
/// Live status snapshot of a connected gamepad.
/// </summary>
/// <param name="Id">SDL joystick instance ID (stable while connected).</param>
/// <param name="Name">Human-readable gamepad name.</param>
/// <param name="Guid">Device GUID (hex) - stable across reconnects for the same model.</param>
/// <param name="BatteryPercent">Battery percentage (0-100), or -1 when unknown.</param>
/// <param name="IsCharging">Whether the battery is currently charging.</param>
/// <param name="IsPrimary">Whether this gamepad drives navigation input.</param>
public record GamepadInfo(
    SDL_JoystickID Id,
    string Name,
    string Guid,
    int BatteryPercent,
    bool IsCharging,
    bool IsPrimary);