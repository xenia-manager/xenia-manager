using System.Linq;
using SDL;
using XeniaManager.Core.Logging;

namespace XeniaManager.Core.Services;

/// <summary>
/// Logical navigation directions/actions derived from raw controller input,
/// decoupled from the specific button/stick that triggered them.
/// </summary>
public enum ControllerNavigationAction
{
    Up,
    Down,
    Left,
    Right,
    Confirm,
    Back,
    /// <summary>X button: show/hide the focused item's info popup.</summary>
    Info,
    /// <summary>Y button: open the focused item's context menu (same as right-click).</summary>
    Menu,
    /// <summary>View button: toggle between grid and list view.</summary>
    ToggleView,
    /// <summary>Left shoulder (LB): switch to the previous tab, on pages that have tabs.</summary>
    PreviousTab,
    /// <summary>Right shoulder (RB): switch to the next tab, on pages that have tabs.</summary>
    NextTab
}

/// <summary>
/// Polls a game controller (via SDL3, through ppy.SDL3-CS) and raises navigation events
/// (Up/Down/Left/Right/Confirm/Back/Info/Menu/ToggleView/PreviousTab/NextTab) suitable for
/// driving UI focus, similar to how a console dashboard is navigated.
///
/// Navigation events are only delivered to the current "focus owner" (see
/// <see cref="PushNavigationContext"/>/<see cref="PopNavigationContext"/>), so
/// only one part of the UI reacts to the controller at a time. This mirrors how
/// opening a modal dialog should "take over" input from whatever's behind it.
///
/// EXPERIMENTAL: this is a first proof-of-concept limited to the first connected
/// controller and to D-Pad + left stick + A/B/X/Y/View/LB/RB buttons. Uses SDL3 (via
/// ppy.SDL3-CS, switched from the SDL2-based Silk.NET.SDL) so this also works
/// under Wine/Proton on Linux, not just native Windows, on a non-EOL SDL version.
/// </summary>
public unsafe class GamepadService : IDisposable
{
    private const int PollIntervalMs = 66; // ~15 polls/sec, responsive without hammering the CPU
    private const int RepeatDelayMs = 400; // Delay before a held direction starts repeating
    private const int RepeatIntervalMs = 150; // Interval between repeats while a direction is held
    private const short StickDeadzone = 8000; // Same order of magnitude XInput used; SDL axis range is also [-32768, 32767]

    /// <summary>
    /// Reference to the current singleton instance, set by the DI container when it's
    /// constructed. Lets static code that doesn't have access to the DI container (e.g.
    /// <see cref="Launcher"/>, which lives in XeniaManager.Core and predates this service)
    /// still pause/resume controller polling around launching a game. Null until the app's
    /// service provider has actually created the singleton instance.
    /// </summary>
    public static GamepadService? Current { get; private set; }

    // ppy.SDL3-CS exposes SDL as a static class (SDL3) rather than an instance API like
    // Silk.NET.SDL did, so there's no equivalent of the old "_sdl" handle - this flag tracks
    // whether SDL_Init succeeded instead.
    private bool _sdlInitialized;
    private SDL_Gamepad* _controller;
    private Timer? _pollTimer;
    private bool _isRunning;
    private readonly Lock _lock = new Lock();

    // Stack of active navigation contexts (e.g. "Library", "DiscSelectionDialog").
    // Only the top of the stack receives navigation events, so opening a modal
    // dialog naturally takes over from whatever's behind it, and closing it
    // returns control to the previous context automatically.
    private readonly Stack<object> _navigationContextStack = new Stack<object>();

    // Tracks which single navigation action is currently "held" for repeat purposes.
    // Only one direction repeats at a time to keep navigation predictable.
    private ControllerNavigationAction? _heldAction;
    private DateTime _heldSince;
    private DateTime _lastRepeatFired;

    // Previous button state, used to detect edge-triggered presses for Confirm/Back/etc.
    // (buttons should fire once per press, not repeat while held)
    private bool _previousAPressed;
    private bool _previousBPressed;
    private bool _previousXPressed;
    private bool _previousYPressed;
    private bool _previousViewPressed;
    private bool _previousLeftShoulderPressed;
    private bool _previousRightShoulderPressed;

    public GamepadService()
    {
        Current = this;
    }

    /// <summary>
    /// Fires when a navigation action (Up/Down/Left/Right/Confirm/Back) is triggered,
    /// but only for the currently active navigation context (see <see cref="PushNavigationContext"/>).
    /// Directional actions repeat while held (after an initial delay); Confirm/Back fire once per press.
    /// </summary>
    public event EventHandler<ControllerNavigationAction>? NavigationActionTriggered;

    /// <summary>
    /// Fires when controller connection state changes.
    /// </summary>
    public event EventHandler<bool>? ControllerConnectionChanged;

    private bool _wasConnected;

    /// <summary>
    /// Gets whether the polling timer is currently running.
    /// </summary>
    public bool IsRunning => _isRunning;

    /// <summary>
    /// Registers <paramref name="owner"/> as the current navigation context, so it starts
    /// receiving <see cref="NavigationActionTriggered"/> events (any previous context, e.g.
    /// the Library page, stops receiving them until this one is popped). Call this when a
    /// controller-navigable dialog/page becomes active.
    /// </summary>
    /// <param name="owner">A unique reference identifying the caller, e.g. "this". Used only for
    /// stack bookkeeping/logging, not compared by value.</param>
    public void PushNavigationContext(object owner)
    {
        lock (_lock)
        {
            _navigationContextStack.Push(owner);
            Logger.Debug<GamepadService>($"Navigation context pushed: {owner.GetType().Name} (stack depth: {_navigationContextStack.Count})");
        }
    }

    /// <summary>
    /// Unregisters <paramref name="owner"/> from the navigation context stack. Must be called
    /// with the same owner passed to <see cref="PushNavigationContext"/>, when the dialog/page
    /// is closed/deactivated, so control returns to whatever context was active before it.
    /// </summary>
    public void PopNavigationContext(object owner)
    {
        lock (_lock)
        {
            if (_navigationContextStack.Count == 0)
            {
                Logger.Warning<GamepadService>($"PopNavigationContext called for {owner.GetType().Name} but the context stack is empty");
                return;
            }

            if (!ReferenceEquals(_navigationContextStack.Peek(), owner))
            {
                // Defensive: this can happen if contexts are popped out of order (e.g. a bug
                // elsewhere, or a dialog closed unexpectedly). Log it but don't leave the stack
                // in a broken state - the caller's context is still removed if present anywhere.
                Logger.Warning<GamepadService>($"PopNavigationContext called for {owner.GetType().Name}, but it's not the top of the stack. Removing it anyway.");
                object[] remaining = _navigationContextStack.Where(c => !ReferenceEquals(c, owner)).Reverse().ToArray();
                _navigationContextStack.Clear();
                foreach (object c in remaining)
                {
                    _navigationContextStack.Push(c);
                }
                return;
            }

            _navigationContextStack.Pop();
            Logger.Debug<GamepadService>($"Navigation context popped: {owner.GetType().Name} (stack depth: {_navigationContextStack.Count})");
        }
    }

    /// <summary>
    /// Whether <paramref name="owner"/> is currently the active (topmost) navigation context,
    /// i.e. whether it should currently be receiving navigation events.
    /// </summary>
    public bool IsActiveNavigationContext(object owner)
    {
        lock (_lock)
        {
            return _navigationContextStack.Count > 0 && ReferenceEquals(_navigationContextStack.Peek(), owner);
        }
    }

    // Remembers whether polling was actually running when PauseForExternalProcess was called,
    // so ResumeAfterExternalProcess only restarts it if it makes sense to (and doesn't start
    // polling if the user had the feature disabled in the first place).
    private bool _wasRunningBeforePause;

    /// <summary>
    /// Temporarily stops controller polling, intended to be called right before launching an
    /// external process (i.e. Xenia itself) that should have exclusive access to the
    /// controller. Xenia Manager's own navigation has no business reading the controller
    /// while the emulator is actually running and using it for gameplay.
    /// Safe to call even if polling isn't currently running.
    /// </summary>
    public void PauseForExternalProcess()
    {
        _wasRunningBeforePause = IsRunning;
        if (_wasRunningBeforePause)
        {
            Logger.Info<GamepadService>("Pausing gamepad polling for external process (e.g. Xenia launching)");
            Stop();
        }
    }

    /// <summary>
    /// Resumes controller polling after <see cref="PauseForExternalProcess"/>, but only if it
    /// was actually running beforehand (respects the user's setting rather than force-enabling it).
    /// </summary>
    public void ResumeAfterExternalProcess()
    {
        if (_wasRunningBeforePause)
        {
            Logger.Info<GamepadService>("Resuming gamepad polling after external process exited");
            Start();
        }
    }

    /// <summary>
    /// Starts polling for controller input. Initializes SDL3's gamepad subsystem on first use;
    /// if initialization fails (e.g. missing native SDL3 library), logs a warning and leaves
    /// the service inert rather than throwing, so a missing/broken SDL3 doesn't crash the app
    /// for users who don't use a controller anyway.
    /// </summary>
    public void Start()
    {
        lock (_lock)
        {
            if (_isRunning)
            {
                Logger.Trace<GamepadService>("GamepadService is already running, skipping duplicate start");
                return;
            }

            if (!_sdlInitialized && !TryInitializeSdl())
            {
                return;
            }

            Logger.Info<GamepadService>("Starting gamepad polling service");
            _isRunning = true;
            _pollTimer = new Timer(Poll, null, 0, PollIntervalMs);
        }
    }

    /// <summary>
    /// Stops polling for controller input and cleans up resources.
    /// </summary>
    public void Stop()
    {
        lock (_lock)
        {
            if (!_isRunning)
            {
                Logger.Trace<GamepadService>("GamepadService is not running, nothing to stop");
                return;
            }

            Logger.Info<GamepadService>("Stopping gamepad polling service");
            _pollTimer?.Dispose();
            _pollTimer = null;
            _isRunning = false;
            _heldAction = null;
        }
    }

    /// <summary>
    /// Initializes SDL3's gamepad subsystem via ppy.SDL3-CS. Returns false (and logs a
    /// warning, not an error) if it fails, since a missing native SDL3 library shouldn't be
    /// treated as a hard failure - the feature is opt-in and experimental.
    /// </summary>
    private bool TryInitializeSdl()
    {
        try
        {
            // Unlike SDL2's SDL_INIT_GAMECONTROLLER, SDL3's SDL_INIT_GAMEPAD does NOT imply
            // SDL_INIT_JOYSTICK - without requesting both explicitly, SDL_GetGamepads() and
            // SDL_GetJoysticks() silently return zero devices (no error, no exception), which
            // made the controller appear to simply not exist during the SDL3 migration.
            if (!SDL3.SDL_Init(SDL_InitFlags.SDL_INIT_GAMEPAD | SDL_InitFlags.SDL_INIT_JOYSTICK))
            {
                Logger.Warning<GamepadService>($"SDL3 gamepad subsystem initialization failed: {SDL3.SDL_GetError()}");
                return false;
            }

            _sdlInitialized = true;
            Logger.Info<GamepadService>("SDL3 gamepad subsystem initialized successfully");
            return true;
        }
        catch (Exception ex)
        {
            // Most likely cause: the native SDL3 library isn't present/loadable on this system
            Logger.Warning<GamepadService>("Failed to initialize SDL3 (native library may be missing) - gamepad navigation will be unavailable");
            Logger.LogExceptionDetails<GamepadService>(ex);
            _sdlInitialized = false;
            return false;
        }
    }

    /// <summary>
    /// Polls the first connected game controller and raises navigation events based on its
    /// current state. Runs on the Timer's thread pool thread; event handlers subscribing from
    /// the UI are responsible for marshalling back to the UI thread if needed.
    /// </summary>
    private void Poll(object? state)
    {
        if (!_sdlInitialized)
        {
            return;
        }

        try
        {
            // SDL3 only refreshes gamepad/joystick state (including detecting newly-connected
            // devices) when the event queue is pumped - unlike SDL2, which kept this working
            // transparently for polling-style code like ours. Since we poll on a Timer rather
            // than running an SDL event loop, this has to be done explicitly every poll or
            // device discovery/button/axis reads below would return stale (usually all-zero,
            // or "no controller found") state.
            SDL3.SDL_PumpEvents();
            SDL3.SDL_UpdateGamepads();

            RefreshControllerConnection();

            bool isConnected = _controller != null;

            if (isConnected != _wasConnected)
            {
                _wasConnected = isConnected;
                Logger.Info<GamepadService>($"Controller connection state changed: {(isConnected ? "connected" : "disconnected")}");
                ControllerConnectionChanged?.Invoke(this, isConnected);
            }

            if (!isConnected)
            {
                _heldAction = null;
                _previousAPressed = false;
                _previousBPressed = false;
                _previousXPressed = false;
                _previousYPressed = false;
                _previousViewPressed = false;
                _previousLeftShoulderPressed = false;
                _previousRightShoulderPressed = false;
                return;
            }

            // Resolve the current directional intent (D-Pad takes priority over the stick)
            ControllerNavigationAction? currentDirection = ResolveDirection();

            // Edge-triggered buttons (fire once per press, not while held). SDL3 renamed the
            // face buttons from their SDL2 Xbox-labeled names (A/B/X/Y) to layout-agnostic
            // positional names - SOUTH/EAST/WEST/NORTH map to A/B/X/Y respectively on an
            // Xbox-style gamepad.
            bool aPressed = GetButton(SDL_GamepadButton.SDL_GAMEPAD_BUTTON_SOUTH);
            bool bPressed = GetButton(SDL_GamepadButton.SDL_GAMEPAD_BUTTON_EAST);
            bool xPressed = GetButton(SDL_GamepadButton.SDL_GAMEPAD_BUTTON_WEST);
            bool yPressed = GetButton(SDL_GamepadButton.SDL_GAMEPAD_BUTTON_NORTH);
            // The small button left of center on Xbox One/Series controllers is labeled "View"
            // but SDL (like XInput before it) still calls it "Back" for historical reasons -
            // distinct from our logical ControllerNavigationAction.Back (mapped to the B face button).
            bool viewPressed = GetButton(SDL_GamepadButton.SDL_GAMEPAD_BUTTON_BACK);
            bool leftShoulderPressed = GetButton(SDL_GamepadButton.SDL_GAMEPAD_BUTTON_LEFT_SHOULDER);
            bool rightShoulderPressed = GetButton(SDL_GamepadButton.SDL_GAMEPAD_BUTTON_RIGHT_SHOULDER);

            if (aPressed && !_previousAPressed)
            {
                Logger.Debug<GamepadService>("Controller: A pressed -> Confirm");
                NavigationActionTriggered?.Invoke(this, ControllerNavigationAction.Confirm);
            }

            if (bPressed && !_previousBPressed)
            {
                Logger.Debug<GamepadService>("Controller: B pressed -> Back");
                NavigationActionTriggered?.Invoke(this, ControllerNavigationAction.Back);
            }

            if (xPressed && !_previousXPressed)
            {
                Logger.Debug<GamepadService>("Controller: X pressed -> Info");
                NavigationActionTriggered?.Invoke(this, ControllerNavigationAction.Info);
            }

            if (yPressed && !_previousYPressed)
            {
                Logger.Debug<GamepadService>("Controller: Y pressed -> Menu");
                NavigationActionTriggered?.Invoke(this, ControllerNavigationAction.Menu);
            }

            if (viewPressed && !_previousViewPressed)
            {
                Logger.Debug<GamepadService>("Controller: View pressed -> ToggleView");
                NavigationActionTriggered?.Invoke(this, ControllerNavigationAction.ToggleView);
            }

            if (leftShoulderPressed && !_previousLeftShoulderPressed)
            {
                Logger.Debug<GamepadService>("Controller: LB pressed -> PreviousTab");
                NavigationActionTriggered?.Invoke(this, ControllerNavigationAction.PreviousTab);
            }

            if (rightShoulderPressed && !_previousRightShoulderPressed)
            {
                Logger.Debug<GamepadService>("Controller: RB pressed -> NextTab");
                NavigationActionTriggered?.Invoke(this, ControllerNavigationAction.NextTab);
            }

            _previousAPressed = aPressed;
            _previousBPressed = bPressed;
            _previousXPressed = xPressed;
            _previousYPressed = yPressed;
            _previousViewPressed = viewPressed;
            _previousLeftShoulderPressed = leftShoulderPressed;
            _previousRightShoulderPressed = rightShoulderPressed;

            HandleDirectionalRepeat(currentDirection);
        }
        catch (Exception ex)
        {
            // Defensive: a polling failure should never crash the app or stop future polls
            Logger.Error<GamepadService>("Unexpected error while polling gamepad state");
            Logger.LogExceptionDetails<GamepadService>(ex);
        }
    }

    /// <summary>
    /// Detects if the currently-open controller (if any) got disconnected, and/or opens the
    /// first available game controller if none is currently open. Limited to a single
    /// controller for this first pass (unlike shazzaam7's reference implementation, which
    /// tracked up to 4) to keep the scope of this experimental feature small; multi-controller
    /// support can be added later if needed.
    /// </summary>
    private void RefreshControllerConnection()
    {
        if (!_sdlInitialized)
        {
            return;
        }

        // Close the current controller if it's no longer attached
        if (_controller != null && !SDL3.SDL_GamepadConnected(_controller))
        {
            SDL3.SDL_CloseGamepad(_controller);
            _controller = null;
        }

        if (_controller != null)
        {
            return;
        }

        // SDL_GetGamepads() already returns only gamepad-capable joysticks (unlike SDL2, where
        // we had to enumerate all joysticks and filter with IsGameController ourselves).
        using SDLArray<SDL_JoystickID>? gamepads = SDL3.SDL_GetGamepads();
        if (gamepads == null || gamepads.Count == 0)
        {
            return;
        }

        _controller = SDL3.SDL_OpenGamepad(gamepads[0]);
    }

    private bool GetButton(SDL_GamepadButton button)
    {
        return _sdlInitialized && _controller != null && SDL3.SDL_GetGamepadButton(_controller, button);
    }

    /// <summary>
    /// Handles the "press once, then repeat while held" behavior for directional navigation,
    /// so holding the D-Pad/stick doesn't fire dozens of events per second, but still lets
    /// the user scroll through a long list by holding a direction.
    /// </summary>
    // How long a newly-detected direction change is treated with suspicion (ignored in favor
    // of the previous direction) if it comes very soon after the direction was last confirmed.
    // This exists because a physical analog stick isn't perfectly stable: holding it in one
    // direction can still produce tiny fluctuations on the other axis, which - right at the
    // deadzone boundary - can flip ResolveDirection()'s result back and forth between two
    // directions (or null) many times per second. Without this, HandleDirectionalRepeat sees
    // each flip as "a new direction" and keeps resetting the repeat timer, so held input never
    // actually repeats (this was reported as "holding left doesn't keep scrolling, but right
    // does" - consistent with stick drift/jitter being direction- and even controller-specific).
    //
    // _lastDirectionSeenAt is refreshed on every poll where the held direction is still being
    // reported, not just when it was first detected - otherwise the guard only covers the first
    // 120ms of a hold, and a single jittery reading anytime after that wipes _heldAction and
    // restarts the direction as if freshly pressed, which never accumulates enough time to reach
    // RepeatDelayMs and so never repeats (this is what was actually happening for "left").
    private const int DirectionJitterGuardMs = 120;
    private DateTime _lastDirectionSeenAt;

    private void HandleDirectionalRepeat(ControllerNavigationAction? currentDirection)
    {
        DateTime now = DateTime.UtcNow;

        if (currentDirection == null)
        {
            // A "no direction" reading this soon after the direction was last confirmed is also
            // treated as possible jitter (stick passing through/near center) rather than an
            // intentional release, as long as we're still within the guard window.
            if (_heldAction != null && (now - _lastDirectionSeenAt).TotalMilliseconds < DirectionJitterGuardMs)
            {
                return;
            }

            _heldAction = null;
            return;
        }

        if (_heldAction != currentDirection)
        {
            // If the held direction was confirmed very recently and we're now seeing a
            // different (or no) direction within the jitter guard window, this looks like
            // stick noise rather than an intentional direction change - keep the previous
            // direction active instead of restarting the repeat timer from scratch.
            bool withinJitterWindow = (now - _lastDirectionSeenAt).TotalMilliseconds < DirectionJitterGuardMs;
            if (_heldAction != null && withinJitterWindow)
            {
                return;
            }

            // New direction: fire immediately, then wait RepeatDelayMs before auto-repeating
            _heldAction = currentDirection;
            _heldSince = now;
            _lastRepeatFired = now;
            _lastDirectionSeenAt = now;
            Logger.Debug<GamepadService>($"Controller: direction -> {currentDirection}");
            NavigationActionTriggered?.Invoke(this, currentDirection.Value);
            return;
        }

        // Same direction still held/confirmed again this poll
        _lastDirectionSeenAt = now;

        bool pastInitialDelay = (now - _heldSince).TotalMilliseconds >= RepeatDelayMs;
        bool pastRepeatInterval = (now - _lastRepeatFired).TotalMilliseconds >= RepeatIntervalMs;

        if (pastInitialDelay && pastRepeatInterval)
        {
            _lastRepeatFired = now;
            NavigationActionTriggered?.Invoke(this, currentDirection.Value);
        }
    }

    /// <summary>
    /// Resolves the current directional input from the D-Pad (priority) or the left analog stick.
    /// Returns null if no direction is currently pressed/tilted past the deadzone.
    /// </summary>
    // Once an axis (horizontal or vertical) "wins" the comparison below, it stays preferred
    // until the stick returns to center or the other axis overtakes it by a wide, decisive
    // margin - not just by a tiny amount. This (axis "locking" with hysteresis) is what
    // actually fixes holding the stick in one direction being unreliable: without it, a
    // physical stick's natural micro-jitter on the "losing" axis can occasionally tip the
    // simple magnitude comparison the other way, flipping the resolved direction back and
    // forth many times per second even though the user isn't moving the stick at all.
    private const short AxisLockMargin = 6000;
    private bool _lastWinningAxisWasHorizontal;

    private ControllerNavigationAction? ResolveDirection()
    {
        if (!_sdlInitialized || _controller == null)
        {
            return null;
        }

        // D-Pad takes priority over the analog stick
        if (GetButton(SDL_GamepadButton.SDL_GAMEPAD_BUTTON_DPAD_UP))
        {
            return ControllerNavigationAction.Up;
        }
        if (GetButton(SDL_GamepadButton.SDL_GAMEPAD_BUTTON_DPAD_DOWN))
        {
            return ControllerNavigationAction.Down;
        }
        if (GetButton(SDL_GamepadButton.SDL_GAMEPAD_BUTTON_DPAD_LEFT))
        {
            return ControllerNavigationAction.Left;
        }
        if (GetButton(SDL_GamepadButton.SDL_GAMEPAD_BUTTON_DPAD_RIGHT))
        {
            return ControllerNavigationAction.Right;
        }

        // Fall back to the left analog stick, past the deadzone
        short x = SDL3.SDL_GetGamepadAxis(_controller, SDL_GamepadAxis.SDL_GAMEPAD_AXIS_LEFTX);
        short y = SDL3.SDL_GetGamepadAxis(_controller, SDL_GamepadAxis.SDL_GAMEPAD_AXIS_LEFTY);

        // Widened to int before Math.Abs: SDL's axis range is the asymmetric short range
        // [-32768, 32767], and a stick pushed fully to one side reports exactly short.MinValue.
        // Math.Abs(short) throws OverflowException for short.MinValue specifically (its
        // positive equivalent, 32768, doesn't fit back in a short) - this was silently aborting
        // every poll while the stick was held at full deflection in that direction, which is
        // exactly why holding left (but not right) stopped producing any navigation at all.
        int xAbs = Math.Abs((int)x);
        int yAbs = Math.Abs((int)y);
        bool xPastDeadzone = xAbs >= StickDeadzone;
        bool yPastDeadzone = yAbs >= StickDeadzone;

        if (!xPastDeadzone && !yPastDeadzone)
        {
            // Stick is back at/near center: nothing "wins" anymore, reset the lock so the
            // next push starts from a clean comparison
            return null;
        }

        // Whichever axis has the larger magnitude "wins", so diagonal stick tilts resolve to
        // a single clear direction instead of firing two at once - but with hysteresis: if the
        // previously-winning axis is still past the deadzone, it keeps winning unless the
        // other axis now exceeds it by AxisLockMargin, not just by any amount.
        bool horizontalWins;
        if (_lastWinningAxisWasHorizontal && xPastDeadzone)
        {
            horizontalWins = yAbs < xAbs + AxisLockMargin;
        }
        else if (!_lastWinningAxisWasHorizontal && yPastDeadzone)
        {
            horizontalWins = xAbs > yAbs + AxisLockMargin;
        }
        else
        {
            // No previous lock to honor (or the previously-winning axis fell back under the
            // deadzone) - fall back to a plain magnitude comparison
            horizontalWins = xAbs > yAbs;
        }

        _lastWinningAxisWasHorizontal = horizontalWins;

        if (horizontalWins)
        {
            return x > 0 ? ControllerNavigationAction.Right : ControllerNavigationAction.Left;
        }

        // SDL's Y axis follows screen coordinates: positive means DOWN, not up (this is the
        // opposite of what "y > 0 means up" would intuitively suggest, and differs from a
        // standard math/cartesian Y axis - a real controller confirmed this the hard way,
        // pushing up was moving the selection down and vice versa before this fix)
        return y > 0 ? ControllerNavigationAction.Down : ControllerNavigationAction.Up;
    }

    /// <summary>
    /// Disposes the service by stopping polling, closing the open controller (if any), and
    /// shutting down the SDL3 gamepad subsystem.
    /// </summary>
    public void Dispose()
    {
        Logger.Trace<GamepadService>("Disposing GamepadService");
        Stop();

        if (_sdlInitialized)
        {
            if (_controller != null)
            {
                SDL3.SDL_CloseGamepad(_controller);
                _controller = null;
            }

            SDL3.SDL_QuitSubSystem(SDL_InitFlags.SDL_INIT_GAMEPAD | SDL_InitFlags.SDL_INIT_JOYSTICK);
            _sdlInitialized = false;
        }

        if (ReferenceEquals(Current, this))
        {
            Current = null;
        }

        GC.SuppressFinalize(this);
    }
}
