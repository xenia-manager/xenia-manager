using SDL;
using XeniaManager.Logging;
using XeniaManager.Core.Models;

namespace XeniaManager.Core.Services;

/// <summary>
/// Tracks all open SDL gamepads: handles, live status and the primary
/// selection. All SDL pointer code lives here; the input service only
/// orchestrates events around it.
/// </summary>
internal sealed unsafe class GamepadDeviceCollection
{
    /// <summary>
    /// A single connected gamepad: SDL handle + live status.
    /// </summary>
    private sealed class GamepadHandle
    {
        /// <summary>
        /// SDL joystick instance ID (stable while connected).
        /// </summary>
        public SDL_JoystickID Id { get; init; }

        /// <summary>
        /// Open SDL gamepad handle.
        /// </summary>
        public SDL_Gamepad* Handle { get; init; }

        /// <summary>
        /// Human-readable gamepad name.
        /// </summary>
        public string Name { get; init; } = string.Empty;

        /// <summary>
        /// Battery percentage (0-100), or -1 when unknown/no battery.
        /// </summary>
        public int Battery { get; set; } = -1;

        /// <summary>
        /// Whether the battery is currently charging.
        /// </summary>
        public bool Charging { get; set; }
    }

    /// <summary>
    /// All currently open gamepads, keyed by joystick ID.
    /// </summary>
    private readonly Dictionary<SDL_JoystickID, GamepadHandle> _gamepads = [];

    /// <summary>
    /// The gamepad that drives navigation input (default = none).
    /// </summary>
    private SDL_JoystickID _primaryId;

    /// <summary>
    /// Raised when the connection, primary or battery state changes.
    /// </summary>
    public event Action? StateChanged;

    /// <summary>
    /// Whether any gamepad is currently open.
    /// </summary>
    public bool IsConnected
    {
        get
        {
            return _gamepads.Count > 0;
        }
    }

    /// <summary>
    /// The joystick ID driving navigation input (default = none).
    /// </summary>
    public SDL_JoystickID PrimaryId
    {
        get
        {
            return _primaryId;
        }
    }

    /// <summary>
    /// Battery percentage (0-100) of the primary pad, or -1 when unknown/no battery.
    /// </summary>
    public int PrimaryBattery
    {
        get
        {
            return GetPrimary()?.Battery ?? -1;
        }
    }

    /// <summary>
    /// Whether the primary pad battery is currently charging.
    /// </summary>
    public bool PrimaryCharging
    {
        get
        {
            return GetPrimary()?.Charging ?? false;
        }
    }

    /// <summary>
    /// Whether the given joystick ID is the current primary.
    /// </summary>
    public bool IsPrimary(SDL_JoystickID id) => id == _primaryId && _gamepads.ContainsKey(id);

    /// <summary>
    /// The handle for the primary gamepad, or null when none is connected.
    /// </summary>
    private GamepadHandle? GetPrimary() =>
        _primaryId != default && _gamepads.TryGetValue(_primaryId, out GamepadHandle? pad) ? pad : null;

    /// <summary>
    /// Builds a status snapshot of all connected gamepads.
    /// </summary>
    public IReadOnlyList<GamepadInfo> Snapshot()
    {
        List<GamepadInfo> list = new List<GamepadInfo>(_gamepads.Count);
        foreach (GamepadHandle pad in _gamepads.Values)
        {
            list.Add(new GamepadInfo(
                pad.Id,
                pad.Name,
                GetGuid(pad.Id),
                pad.Battery,
                pad.Charging,
                pad.Id == _primaryId));
        }

        return list;
    }

    /// <summary>
    /// The gamepad that drives navigation input, or null when none is connected.
    /// </summary>
    public GamepadInfo? Primary
    {
        get
        {
            return Snapshot().FirstOrDefault(g => g.IsPrimary);
        }
    }

    /// <summary>
    /// Sets the gamepad with the given joystick ID as primary.
    /// </summary>
    public void SetPrimary(SDL_JoystickID id)
    {
        if (!_gamepads.ContainsKey(id))
        {
            Logger.Warning<GamepadDeviceCollection>($"Cannot set primary: gamepad {id} is not connected");
            return;
        }

        Logger.Info<GamepadDeviceCollection>($"Primary gamepad set to {id}");
        _primaryId = id;
        StateChanged?.Invoke();
    }

    /// <summary>
    /// Restores the primary gamepad from a saved device GUID (hex string),
    /// falling back to the first connected pad when it isn't present.
    /// </summary>
    public void SetPrimaryByGuid(string guidHex)
    {
        try
        {
            string target = guidHex.Replace("-", string.Empty);
            foreach (GamepadHandle pad in _gamepads.Values)
            {
                if (GetGuid(pad.Id).Equals(target, StringComparison.OrdinalIgnoreCase))
                {
                    Logger.Info<GamepadDeviceCollection>($"Restored primary gamepad from GUID: {guidHex}");
                    SetPrimary(pad.Id);
                    return;
                }
            }

            Logger.Info<GamepadDeviceCollection>($"Saved primary GUID '{guidHex}' not connected, keeping current primary");
        }
        catch (Exception ex)
        {
            Logger.Warning<GamepadDeviceCollection>($"Failed to restore primary from GUID '{guidHex}'");
            Logger.LogExceptionDetails<GamepadDeviceCollection>(ex);
        }
    }

    /// <summary>
    /// Re-enumerates the connected gamepads: opens new ones, drops stale ones
    /// and restores the primary selection.
    /// </summary>
    public void Rescan()
    {
        Logger.Info<GamepadDeviceCollection>("Rescanning connected gamepads");
        using SDLArray<SDL_JoystickID>? gamepads = SDL3.SDL_GetGamepads();
        if (gamepads == null)
        {
            Logger.Warning<GamepadDeviceCollection>("Rescan: SDL_GetGamepads returned null");
            return;
        }

        HashSet<SDL_JoystickID> seen = [];
        foreach (SDL_JoystickID id in gamepads)
        {
            seen.Add(id);
        }

        foreach (SDL_JoystickID id in seen)
        {
            if (!_gamepads.ContainsKey(id))
            {
                Open(id);
            }
        }

        foreach (SDL_JoystickID id in _gamepads.Keys.ToList())
        {
            if (!seen.Contains(id))
            {
                Close(id);
            }
        }

        EnsurePrimary();
        StateChanged?.Invoke();
        Logger.Info<GamepadDeviceCollection>($"Rescan complete: {_gamepads.Count} gamepad(s) connected");
    }

    /// <summary>
    /// Opens a gamepad by joystick ID so SDL delivers its button/axis events.
    /// </summary>
    public void Open(SDL_JoystickID id)
    {
        if (_gamepads.ContainsKey(id))
        {
            return;
        }

        SDL_Gamepad* handle = SDL3.SDL_OpenGamepad(id);
        if (handle == null)
        {
            Logger.Warning<GamepadDeviceCollection>($"Failed to open gamepad {id}: {SDL3.SDL_GetError()}");
            return;
        }

        string name = SDL3.SDL_GetGamepadName(handle) ?? $"Gamepad {id}";
        _gamepads[id] = new GamepadHandle
        {
            Id = id,
            Handle = handle,
            Name = name
        };
        Logger.Info<GamepadDeviceCollection>($"Opened gamepad {id}: {name}");

        EnsurePrimary();
        StateChanged?.Invoke();
    }

    /// <summary>
    /// Closes and removes a gamepad, promoting another to primary if needed.
    /// </summary>
    public void Close(SDL_JoystickID id)
    {
        if (!_gamepads.TryGetValue(id, out GamepadHandle? pad))
        {
            return;
        }

        Logger.Info<GamepadDeviceCollection>($"Closing gamepad {id} ({pad.Name})");
        SDL3.SDL_CloseGamepad(pad.Handle);
        _gamepads.Remove(id);

        EnsurePrimary();
        StateChanged?.Invoke();
    }

    /// <summary>
    /// Closes every open gamepad (shutdown).
    /// </summary>
    public void CloseAll()
    {
        foreach (SDL_JoystickID id in _gamepads.Keys.ToList())
        {
            Close(id);
        }
    }

    /// <summary>
    /// Ensures a primary gamepad exists when pads are connected.
    /// </summary>
    private void EnsurePrimary()
    {
        if (_primaryId != default && _gamepads.ContainsKey(_primaryId))
        {
            return;
        }

        _primaryId = _gamepads.Keys.FirstOrDefault();
        if (_primaryId != default)
        {
            Logger.Info<GamepadDeviceCollection>($"Primary gamepad is now {_primaryId}");
        }
    }

    /// <summary>
    /// Queries every open gamepad's battery state and raises <see cref="StateChanged"/>
    /// when any of it changed.
    /// </summary>
    public void PollBattery()
    {
        bool changed = false;
        foreach (GamepadHandle pad in _gamepads.Values)
        {
            int percent;
            SDL_PowerState state = SDL3.SDL_GetGamepadPowerInfo(pad.Handle, &percent);
            Logger.Trace<GamepadDeviceCollection>($"Battery poll ({pad.Name}): state={state}, percent={percent}");

            int newPercent = percent;
            bool newCharging = state is SDL_PowerState.SDL_POWERSTATE_CHARGING or SDL_PowerState.SDL_POWERSTATE_CHARGED;
            if (state is SDL_PowerState.SDL_POWERSTATE_NO_BATTERY or SDL_PowerState.SDL_POWERSTATE_UNKNOWN
                or SDL_PowerState.SDL_POWERSTATE_ERROR)
            {
                newPercent = -1;
            }

            if (newPercent != pad.Battery || newCharging != pad.Charging)
            {
                pad.Battery = newPercent;
                pad.Charging = newCharging;
                changed = true;
            }
        }

        if (changed)
        {
            StateChanged?.Invoke();
        }
    }

    /// <summary>
    /// The device GUID (hex) for a gamepad, stable across reconnects for the same model.
    /// </summary>
    private static string GetGuid(SDL_JoystickID id) => SDL3.SDL_GetJoystickGUIDForID(id).ToString() ?? string.Empty;
}