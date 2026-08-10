using System;
using XeniaManager.BigScreen.Models;

namespace XeniaManager.BigScreen.Services;

/// <summary>
/// Abstraction over the SDL gamepad subsystem: navigation button presses and
/// live controller status (connection, battery).
/// </summary>
public interface IGamepadService
{
    /// <summary>
    /// Raised on the UI thread when a navigation-relevant button is pressed.
    /// </summary>
    event Action<GamepadButton>? ButtonPressed;

    /// <summary>
    /// Raised when the connection or battery state changes.
    /// </summary>
    event Action? StateChanged;

    /// <summary>
    /// Whether SDL initialised successfully and polling is active.
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// Whether a gamepad is currently open.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Battery percentage (0-100), or -1 when unknown/no battery.
    /// </summary>
    int BatteryPercent { get; }

    /// <summary>
    /// Whether the gamepad battery is currently charging.
    /// </summary>
    bool IsCharging { get; }
}
