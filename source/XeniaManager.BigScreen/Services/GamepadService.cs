using System;
using System.Linq;
using Avalonia.Threading;
using SDL;
using XeniaManager.BigScreen.Models;
using XeniaManager.Core.Logging;

namespace XeniaManager.BigScreen.Services;

/// <summary>
/// Polls the SDL3 gamepad subsystem on the UI thread and raises button presses.
/// D-pad, left-stick and bumper input all normalize onto the D-pad values.
/// Fails gracefully when SDL can't be initialized (e.g. no native runtime).
/// </summary>
public class GamepadService : IGamepadService
{
    /// <summary>
    /// Raised on the UI thread when a navigation-relevant button is pressed.
    /// </summary>
    public event Action<GamepadButton>? ButtonPressed;

    /// <summary>
    /// Stick axis magnitude beyond which a direction counts as pressed (0-32767).
    /// </summary>
    private const short AxisDeadzone = 16000;

    /// <summary>
    /// Stick X direction currently held (left or right).
    /// </summary>
    private bool _stickLeftHeld;
    private bool _stickRightHeld;

    /// <summary>
    /// Stick Y direction currently held (up or down).
    /// </summary>
    private bool _stickUpHeld;
    private bool _stickDownHeld;

    private readonly DispatcherTimer? _pollTimer;

    /// <summary>
    /// The opened gamepad handle; gamepad button/axis events only flow while open.
    /// </summary>
    private unsafe SDL_Gamepad* _gamepad;

    /// <summary>
    /// Joystick ID of the currently open gamepad.
    /// </summary>
    private SDL_JoystickID _gamepadWhich;

    /// <summary>
    /// Whether SDL initialized successfully and polling is active.
    /// </summary>
    public bool IsActive { get; }

    /// <summary>
    /// Raised when the connection or battery state changes.
    /// </summary>
    public event Action? StateChanged;

    /// <summary>
    /// Whether a gamepad is currently open.
    /// </summary>
    public bool IsConnected { get; private set; }

    /// <summary>
    /// Battery percentage (0-100), or -1 when unknown/no battery.
    /// </summary>
    public int BatteryPercent { get; private set; } = -1;

    /// <summary>
    /// Whether the gamepad battery is currently charging.
    /// </summary>
    public bool IsCharging { get; private set; }

    /// <summary>
    /// How often the gamepad battery state is queried.
    /// </summary>
    private static readonly TimeSpan BatteryPollInterval = TimeSpan.FromSeconds(5);

    private DispatcherTimer? _batteryTimer;

    public unsafe GamepadService()
    {
        try
        {
            Logger.Info<GamepadService>("Initializing SDL gamepad subsystem");
            if (!SDL3.SDL_Init(SDL_InitFlags.SDL_INIT_GAMEPAD))
            {
                Logger.Warning<GamepadService>($"SDL gamepad init failed: {SDL3.SDL_GetError()}");
                return;
            }

            Logger.Info<GamepadService>("SDL gamepad init succeeded");

            // Deliver gamepad events even while the window isn't focused
            bool hintSet = SDL3.SDL_SetHint("SDL_JOYSTICK_ALLOW_BACKGROUND_EVENTS", "1");
            Logger.Debug<GamepadService>($"Set SDL_JOYSTICK_ALLOW_BACKGROUND_EVENTS hint: {(hintSet ? "ok" : "failed")}");

            // Report which gamepads SDL sees right now, opening the first one so
            // button/axis events flow (SDL3 only delivers them for open gamepads)
            SDLArray<SDL_JoystickID>? gamepads = SDL3.SDL_GetGamepads();
            if (gamepads != null)
            {
                int count = 0;
                foreach (SDL_JoystickID id in gamepads)
                {
                    Logger.Info<GamepadService>($"Gamepad[{count}] joystick ID: {id}");
                    count++;
                    if (_gamepad == null)
                    {
                        OpenGamepad(id);
                    }
                }

                Logger.Info<GamepadService>($"SDL sees {count} connected gamepad(s)");
            }
            else
            {
                Logger.Warning<GamepadService>("SDL_GetGamepads returned null");
            }

            _pollTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
            _pollTimer.Tick += (_, _) => PollEvents();
            _pollTimer.Start();
            IsActive = true;
            Logger.Info<GamepadService>($"Poll timer started ({_pollTimer.Interval.TotalMilliseconds}ms)");

            _batteryTimer = new DispatcherTimer { Interval = BatteryPollInterval };
            _batteryTimer.Tick += (_, _) => PollBattery();
            _batteryTimer.Start();
            PollBattery();
            Logger.Info<GamepadService>($"Battery timer started ({BatteryPollInterval.TotalSeconds}s)");
        }
        catch (Exception ex)
        {
            Logger.Error<GamepadService>("Failed to initialize SDL gamepad input");
            Logger.LogExceptionDetails<GamepadService>(ex);
        }
    }

    /// <summary>
    /// Maps an SDL gamepad button to a BigScreen button and raises it.
    /// </summary>
    private void HandleButtonDown(SDL_GamepadButtonEvent e)
    {
        SDL_GamepadButton sdlButton = e.Button;
        Logger.Trace<GamepadService>($"Button down: {sdlButton} (raw {e.button}, gamepad {e.which})");

        GamepadButton? mapped = sdlButton switch
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
            _ => null,
        };

        if (mapped == null)
        {
            Logger.Trace<GamepadService>($"Button {sdlButton} not mapped, ignoring");
            return;
        }

        Logger.Debug<GamepadService>($"Raising {mapped.Value} (from {sdlButton})");
        ButtonPressed?.Invoke(mapped.Value);
    }

    /// <summary>
    /// Tracks one stick axis, raising a press only when a direction is newly
    /// entered (no repeat while held, no press when returning to center).
    /// </summary>
    private void TrackAxis(
        ref bool negativeHeld,
        ref bool positiveHeld,
        short value,
        GamepadButton negativeButton,
        GamepadButton positiveButton)
    {
        if (value < -AxisDeadzone)
        {
            if (!negativeHeld && !positiveHeld)
            {
                Logger.Debug<GamepadService>($"Stick crossed negative deadzone (value {value}), raising {negativeButton}");
                ButtonPressed?.Invoke(negativeButton);
            }

            negativeHeld = true;
            positiveHeld = false;
        }
        else if (value > AxisDeadzone)
        {
            if (!positiveHeld && !negativeHeld)
            {
                Logger.Debug<GamepadService>($"Stick crossed positive deadzone (value {value}), raising {positiveButton}");
                ButtonPressed?.Invoke(positiveButton);
            }

            positiveHeld = true;
            negativeHeld = false;
        }
        else
        {
            if (negativeHeld || positiveHeld)
            {
                Logger.Trace<GamepadService>($"Stick returned to center (value {value})");
            }

            negativeHeld = false;
            positiveHeld = false;
        }
    }

    /// <summary>
    /// Tracks the left stick axes, raising a press only when a direction is
    /// newly entered (no repeat while held, no press when returning to center).
    /// </summary>
    private void HandleAxisMotion(SDL_GamepadAxisEvent e)
    {
        SDL_GamepadAxis axis = e.Axis;
        short value = e.value;
        Logger.Trace<GamepadService>($"Axis motion: {axis} (raw {e.axis}, value {value}, gamepad {e.which})");

        switch (axis)
        {
            case SDL_GamepadAxis.SDL_GAMEPAD_AXIS_LEFTX:
                TrackAxis(ref _stickLeftHeld, ref _stickRightHeld, value, GamepadButton.DpadLeft, GamepadButton.DpadRight);
                break;
            case SDL_GamepadAxis.SDL_GAMEPAD_AXIS_LEFTY:
                TrackAxis(ref _stickUpHeld, ref _stickDownHeld, value, GamepadButton.DpadUp, GamepadButton.DpadDown);
                break;
            default:
                Logger.Trace<GamepadService>($"Axis {axis} not tracked, ignoring");
                break;
        }
    }

    /// <summary>
    /// Queries the open gamepad's battery state and raises <see cref="StateChanged"/>
    /// when it changed.
    /// </summary>
    private unsafe void PollBattery()
    {
        if (_gamepad == null)
        {
            return;
        }

        int percent;
        SDL_PowerState state = SDL3.SDL_GetGamepadPowerInfo(_gamepad, &percent);
        Logger.Trace<GamepadService>($"Battery poll: state={state}, percent={percent}");

        int newPercent = percent;
        bool newCharging = state is SDL_PowerState.SDL_POWERSTATE_CHARGING or SDL_PowerState.SDL_POWERSTATE_CHARGED;
        if (state is SDL_PowerState.SDL_POWERSTATE_NO_BATTERY or SDL_PowerState.SDL_POWERSTATE_UNKNOWN
            or SDL_PowerState.SDL_POWERSTATE_ERROR)
        {
            newPercent = -1;
        }

        if (newPercent != BatteryPercent || newCharging != IsCharging)
        {
            BatteryPercent = newPercent;
            IsCharging = newCharging;
            StateChanged?.Invoke();
        }
    }

    /// <summary>
    /// Opens a gamepad by joystick ID so SDL delivers its button/axis events.
    /// </summary>
    private unsafe void OpenGamepad(SDL_JoystickID id)
    {
        _gamepad = SDL3.SDL_OpenGamepad(id);
        if (_gamepad == null)
        {
            Logger.Warning<GamepadService>($"Failed to open gamepad {id}: {SDL3.SDL_GetError()}");
            return;
        }

        _gamepadWhich = id;
        IsConnected = true;
        PollBattery();
        Logger.Info<GamepadService>($"Opened gamepad {id}: {SDL3.SDL_GetGamepadName(_gamepad)}");
        StateChanged?.Invoke();
    }

    /// <summary>
    /// Closes the currently open gamepad, if any.
    /// </summary>
    private unsafe void CloseGamepad()
    {
        if (_gamepad != null)
        {
            Logger.Info<GamepadService>($"Closing gamepad {_gamepadWhich}");
            SDL3.SDL_CloseGamepad(_gamepad);
            _gamepad = null;
            _gamepadWhich = 0;
            IsConnected = false;
            BatteryPercent = -1;
            IsCharging = false;
            StateChanged?.Invoke();
        }
    }

    /// <summary>
    /// Drains all pending SDL events and raises normalized button presses.
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
                    Logger.Trace<GamepadService>($"Button up: {ev.gbutton.Button} (gamepad {ev.gbutton.which})");
                    break;
                case SDL_EventType.SDL_EVENT_GAMEPAD_AXIS_MOTION:
                    HandleAxisMotion(ev.gaxis);
                    break;
                case SDL_EventType.SDL_EVENT_GAMEPAD_ADDED:
                    Logger.Info<GamepadService>($"Gamepad added: joystick ID {ev.gdevice.which}");
                    if (_gamepad == null)
                    {
                        OpenGamepad(ev.gdevice.which);
                    }

                    break;
                case SDL_EventType.SDL_EVENT_GAMEPAD_REMOVED:
                    Logger.Info<GamepadService>($"Gamepad removed: joystick ID {ev.gdevice.which}");
                    if (ev.gdevice.which == _gamepadWhich)
                    {
                        CloseGamepad();
                    }

                    break;
                default:
                    Logger.Trace<GamepadService>($"Ignored event: {ev.Type}");
                    break;
            }
        }

        if (eventsProcessed > 0)
        {
            Logger.Trace<GamepadService>($"Poll cycle processed {eventsProcessed} event(s)");
        }
    }

    /// <summary>
    /// Shuts down the SDL gamepad subsystem.
    /// </summary>
    public void Dispose()
    {
        Logger.Info<GamepadService>("Shutting down SDL gamepad subsystem");
        _pollTimer?.Stop();
        _batteryTimer?.Stop();
        CloseGamepad();
        SDL3.SDL_Quit();
        GC.SuppressFinalize(this);
    }
}
