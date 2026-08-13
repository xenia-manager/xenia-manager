using System;
using System.Collections.Generic;
using Avalonia.Threading;
using SDL;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Models;

namespace XeniaManager.Core.Services;

/// <summary>
/// Polls the SDL3 gamepad subsystem on the UI thread and raises button presses.
/// D-pad, left-stick and bumper input all normalise onto the D-pad values.
/// All connected gamepads are tracked (<see cref="GamepadDeviceCollection"/>);
/// navigation input flows from the primary pad only.
/// Fails gracefully when SDL can't be initialised (e.g. no native runtime).
/// </summary>
public class GamepadInputService : IGamepadInputService
{
    /// <summary>
    /// How often the gamepad event queue is drained.
    /// </summary>
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(50);

    /// <summary>
    /// How often the gamepad battery state is queried.
    /// </summary>
    private static readonly TimeSpan BatteryPollInterval = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Raised on the UI thread when a navigation-relevant button is pressed on the primary pad.
    /// </summary>
    public event Action<GamepadButton>? ButtonPressed;

    /// <summary>
    /// Raised when the connection, primary or battery state changes.
    /// </summary>
    public event Action? StateChanged;

    /// <summary>
    /// Whether SDL initialised successfully and polling is active.
    /// </summary>
    public bool IsActive { get; }

    /// <summary>
    /// Whether any gamepad is currently open.
    /// </summary>
    public bool IsConnected => _devices.IsConnected;

    /// <summary>
    /// Battery percentage (0-100) of the primary pad, or -1 when unknown/no battery.
    /// </summary>
    public int BatteryPercent => _devices.PrimaryBattery;

    /// <summary>
    /// Whether the primary pad battery is currently charging.
    /// </summary>
    public bool IsCharging => _devices.PrimaryCharging;

    /// <summary>
    /// All currently connected gamepads with live status.
    /// </summary>
    public IReadOnlyList<GamepadInfo> ConnectedGamepads => _devices.Snapshot();

    /// <summary>
    /// The gamepad that drives navigation input, or null when none is connected.
    /// </summary>
    public GamepadInfo? PrimaryGamepad => _devices.Primary;

    /// <summary>
    /// Left stick X-axis tracker (left/right).
    /// </summary>
    private readonly StickTracker _stickX = new();

    /// <summary>
    /// Left stick Y-axis tracker (up/down).
    /// </summary>
    private readonly StickTracker _stickY = new();

    /// <summary>
    /// All open gamepads + the primary selection.
    /// </summary>
    private readonly GamepadDeviceCollection _devices = new();

    private readonly DispatcherTimer? _pollTimer;
    private readonly DispatcherTimer? _batteryTimer;

    public GamepadInputService()
    {
        try
        {
            Logger.Info<GamepadInputService>("Initializing SDL gamepad subsystem");
            if (!SDL3.SDL_Init(SDL_InitFlags.SDL_INIT_GAMEPAD))
            {
                Logger.Warning<GamepadInputService>($"SDL gamepad init failed: {SDL3.SDL_GetError()}");
                return;
            }

            Logger.Info<GamepadInputService>("SDL gamepad init succeeded");

            // Deliver gamepad events even while the window isn't focused
            bool hintSet = SDL3.SDL_SetHint("SDL_JOYSTICK_ALLOW_BACKGROUND_EVENTS", "1");
            Logger.Debug<GamepadInputService>($"Set SDL_JOYSTICK_ALLOW_BACKGROUND_EVENTS hint: {(hintSet ? "ok" : "failed")}");

            _devices.StateChanged += () => StateChanged?.Invoke();

            // Report which gamepads SDL sees right now, opening every one so
            // button/axis/battery events flow (SDL3 only delivers them for open pads)
            SDLArray<SDL_JoystickID>? gamepads = SDL3.SDL_GetGamepads();
            if (gamepads != null)
            {
                int count = 0;
                foreach (SDL_JoystickID id in gamepads)
                {
                    Logger.Info<GamepadInputService>($"Gamepad[{count}] joystick ID: {id}");
                    count++;
                    _devices.Open(id);
                }

                Logger.Info<GamepadInputService>($"SDL sees {count} connected gamepad(s)");
            }
            else
            {
                Logger.Warning<GamepadInputService>("SDL_GetGamepads returned null");
            }

            _pollTimer = new DispatcherTimer { Interval = PollInterval };
            _pollTimer.Tick += (_, _) => PollEvents();
            _pollTimer.Start();
            IsActive = true;
            Logger.Info<GamepadInputService>($"Poll timer started ({_pollTimer.Interval.TotalMilliseconds}ms)");

            _batteryTimer = new DispatcherTimer { Interval = BatteryPollInterval };
            _batteryTimer.Tick += (_, _) => _devices.PollBattery();
            _batteryTimer.Start();
            _devices.PollBattery();
            Logger.Info<GamepadInputService>($"Battery timer started ({_batteryTimer.Interval.TotalSeconds}s)");
        }
        catch (Exception ex)
        {
            Logger.Error<GamepadInputService>("Failed to initialize SDL gamepad input");
            Logger.LogExceptionDetails<GamepadInputService>(ex);
        }
    }

    /// <summary>
    /// Sets the given gamepad as the primary input source.
    /// </summary>
    public void SetPrimary(GamepadInfo gamepad) => _devices.SetPrimary(gamepad.Id);

    /// <summary>
    /// Restores the primary gamepad from a saved device GUID (hex string),
    /// falling back to the first connected pad when it isn't present.
    /// </summary>
    public void SetPrimaryByGuid(string guidHex) => _devices.SetPrimaryByGuid(guidHex);

    /// <summary>
    /// Re-enumerates the connected gamepads: opens new ones, drops stale ones
    /// and restores the primary selection.
    /// </summary>
    public void Rescan()
    {
        if (IsActive)
        {
            _devices.Rescan();
        }
    }

    /// <summary>
    /// Reloads the SDL game controller database (after an update).
    /// </summary>
    public void ReloadMappings()
    {
        if (!IsActive)
        {
            return;
        }

        Logger.Info<GamepadInputService>("Reloading SDL game controller database");
        SDL3.SDL_ReloadGamepadMappings();
        Rescan();
    }

    /// <summary>
    /// Maps an SDL gamepad button to a navigation button and raises it
    /// (primary pad only).
    /// </summary>
    private void HandleButtonDown(SDL_GamepadButtonEvent e)
    {
        if (!_devices.IsPrimary(e.which))
        {
            Logger.Trace<GamepadInputService>($"Button on non-primary gamepad {e.which} ignored");
            return;
        }

        Logger.Trace<GamepadInputService>($"Button down: {e.Button} (raw {e.button}, gamepad {e.which})");
        GamepadButton? mapped = GamepadButtonMapper.Map(e.Button);
        if (mapped == null)
        {
            Logger.Trace<GamepadInputService>($"Button {e.Button} not mapped, ignoring");
            return;
        }

        Logger.Debug<GamepadInputService>($"Raising {mapped.Value} (from {e.Button})");
        ButtonPressed?.Invoke(mapped.Value);
    }

    /// <summary>
    /// Tracks the left stick axes, raising a press only when a direction is
    /// newly entered (no repeat while held, no press when returning to center).
    /// </summary>
    private void HandleAxisMotion(SDL_GamepadAxisEvent e)
    {
        if (!_devices.IsPrimary(e.which))
        {
            Logger.Trace<GamepadInputService>($"Axis on non-primary gamepad {e.which} ignored");
            return;
        }

        Logger.Trace<GamepadInputService>($"Axis motion: {e.Axis} (raw {e.axis}, value {e.value}, gamepad {e.which})");

        GamepadButton? pressed = e.Axis switch
        {
            SDL_GamepadAxis.SDL_GAMEPAD_AXIS_LEFTX => _stickX.Track(e.value, GamepadButton.DpadLeft, GamepadButton.DpadRight),
            SDL_GamepadAxis.SDL_GAMEPAD_AXIS_LEFTY => _stickY.Track(e.value, GamepadButton.DpadUp, GamepadButton.DpadDown),
            _ => null,
        };

        if (pressed == null)
        {
            if (e.Axis is not (SDL_GamepadAxis.SDL_GAMEPAD_AXIS_LEFTX or SDL_GamepadAxis.SDL_GAMEPAD_AXIS_LEFTY))
            {
                Logger.Trace<GamepadInputService>($"Axis {e.Axis} not tracked, ignoring");
            }

            return;
        }

        Logger.Debug<GamepadInputService>($"Stick crossed deadzone (value {e.value}), raising {pressed.Value}");
        ButtonPressed?.Invoke(pressed.Value);
    }

    /// <summary>
    /// Drains all pending SDL events and raises normalised button presses.
    /// </summary>
    private unsafe void PollEvents()
    {
        int eventsProcessed = 0;
        SDL_Event ev;
        while (SDL3.SDL_PollEvent(&ev))
        {
            eventsProcessed++;
            switch (ev.Type)
            {
                case SDL_EventType.SDL_EVENT_GAMEPAD_BUTTON_DOWN:
                    HandleButtonDown(ev.gbutton);
                    break;
                case SDL_EventType.SDL_EVENT_GAMEPAD_BUTTON_UP:
                    Logger.Trace<GamepadInputService>($"Button up: {ev.gbutton.Button} (gamepad {ev.gbutton.which})");
                    break;
                case SDL_EventType.SDL_EVENT_GAMEPAD_AXIS_MOTION:
                    HandleAxisMotion(ev.gaxis);
                    break;
                case SDL_EventType.SDL_EVENT_GAMEPAD_ADDED:
                    Logger.Info<GamepadInputService>($"Gamepad added: joystick ID {ev.gdevice.which}");
                    _devices.Open(ev.gdevice.which);
                    break;
                case SDL_EventType.SDL_EVENT_GAMEPAD_REMOVED:
                    Logger.Info<GamepadInputService>($"Gamepad removed: joystick ID {ev.gdevice.which}");
                    _devices.Close(ev.gdevice.which);
                    break;
                default:
                    Logger.Trace<GamepadInputService>($"Ignored event: {ev.Type}");
                    break;
            }
        }

        if (eventsProcessed > 0)
        {
            Logger.Trace<GamepadInputService>($"Poll cycle processed {eventsProcessed} event(s)");
        }
    }

    /// <summary>
    /// Shuts down the SDL gamepad subsystem.
    /// </summary>
    public void Dispose()
    {
        Logger.Info<GamepadInputService>("Shutting down SDL gamepad subsystem");
        _pollTimer?.Stop();
        _batteryTimer?.Stop();
        _devices.CloseAll();
        SDL3.SDL_Quit();
        GC.SuppressFinalize(this);
    }
}
