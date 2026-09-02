using System.Collections.Generic;
using System;
using XeniaManager.Core.Models;

namespace XeniaManager.Core.Services;

/// <summary>
/// Abstraction over the SDL gamepad subsystem: navigation button presses and
/// live controller status (connection, battery). Supports multiple connected
/// gamepads with a single primary pad that drives input.
/// </summary>
public interface IGamepadInputService
{
    /// <summary>
    /// Raised on the UI thread when a navigation-relevant button is pressed
    /// on the primary gamepad.
    /// </summary>
    event Action<GamepadButton>? ButtonPressed;

    /// <summary>
    /// Raised when the connection, primary or battery state changes.
    /// </summary>
    event Action? StateChanged;

    /// <summary>
    /// Whether SDL initialised successfully and polling is active.
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// Whether any gamepad is currently connected.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Battery percentage (0-100) of the primary gamepad, or -1 when unknown/no battery.
    /// </summary>
    int BatteryPercent { get; }

    /// <summary>
    /// Whether the primary gamepad battery is currently charging.
    /// </summary>
    bool IsCharging { get; }

    /// <summary>
    /// All currently connected gamepads with live status.
    /// </summary>
    IReadOnlyList<GamepadInfo> ConnectedGamepads { get; }

    /// <summary>
    /// The gamepad that drives navigation input, or null when none is connected.
    /// </summary>
    GamepadInfo? PrimaryGamepad { get; }

    /// <summary>
    /// Sets the given gamepad as the primary input source.
    /// </summary>
    void SetPrimary(GamepadInfo gamepad);

    /// <summary>
    /// Restores the primary gamepad from a saved device GUID (hex string),
    /// falling back to the first connected pad when it isn't present.
    /// </summary>
    void SetPrimaryByGuid(string guidHex);

    /// <summary>
    /// Re-enumerates the connected gamepads: opens new ones, drops stale ones
    /// and restores the primary selection.
    /// </summary>
    void Rescan();

    /// <summary>
    /// Reloads the SDL game controller database (after an update).
    /// </summary>
    void ReloadMappings();
}