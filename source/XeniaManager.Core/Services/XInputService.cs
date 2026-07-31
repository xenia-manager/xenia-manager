using System.Linq;
using System.Runtime.InteropServices;
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
    ToggleView
}

/// <summary>
/// Polls an Xbox controller (via XInput) and raises navigation events
/// (Up/Down/Left/Right/Confirm/Back/Info/Menu/ToggleView) suitable for driving UI focus,
/// similar to how a console dashboard is navigated.
///
/// Navigation events are only delivered to the current "focus owner" (see
/// <see cref="PushNavigationContext"/>/<see cref="PopNavigationContext"/>), so
/// only one part of the UI reacts to the controller at a time. This mirrors how
/// opening a modal dialog should "take over" input from whatever's behind it.
///
/// EXPERIMENTAL: this is a first proof-of-concept limited to controller
/// slot 0 (the first connected controller) and to D-Pad + left stick +
/// A/B/X/Y/View buttons. Windows-only, since Xenia Manager itself only targets
/// Windows (see XeniaManager.csproj OutputType=WinExe).
/// </summary>
public class XInputService : IDisposable
{
    private const int PollIntervalMs = 66; // ~15 polls/sec, responsive without hammering the CPU
    private const int RepeatDelayMs = 400; // Delay before a held direction starts repeating
    private const int RepeatIntervalMs = 150; // Interval between repeats while a direction is held
    private const short StickDeadzone = 8000; // Same order of magnitude as XINPUT_GAMEPAD_LEFT_THUMB_DEADZONE

    /// <summary>
    /// Reference to the current singleton instance, set by the DI container when it's
    /// constructed. Lets static code that doesn't have access to the DI container (e.g.
    /// <see cref="Launcher"/>, which lives in XeniaManager.Core and predates this service)
    /// still pause/resume controller polling around launching a game. Null until the app's
    /// service provider has actually created the singleton instance.
    /// </summary>
    public static XInputService? Current { get; private set; }

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

    // Previous button state, used to detect edge-triggered presses for Confirm/Back
    // (buttons should fire once per press, not repeat while held)
    private bool _previousAPressed;
    private bool _previousBPressed;
    private bool _previousXPressed;
    private bool _previousYPressed;
    private bool _previousViewPressed;

    public XInputService()
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
    /// Fires when controller connection state changes (slot 0 only).
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
            Logger.Debug<XInputService>($"Navigation context pushed: {owner.GetType().Name} (stack depth: {_navigationContextStack.Count})");
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
                Logger.Warning<XInputService>($"PopNavigationContext called for {owner.GetType().Name} but the context stack is empty");
                return;
            }

            if (!ReferenceEquals(_navigationContextStack.Peek(), owner))
            {
                // Defensive: this can happen if contexts are popped out of order (e.g. a bug
                // elsewhere, or a dialog closed unexpectedly). Log it but don't leave the stack
                // in a broken state - the caller's context is still removed if present anywhere.
                Logger.Warning<XInputService>($"PopNavigationContext called for {owner.GetType().Name}, but it's not the top of the stack. Removing it anyway.");
                object[] remaining = _navigationContextStack.Where(c => !ReferenceEquals(c, owner)).Reverse().ToArray();
                _navigationContextStack.Clear();
                foreach (object c in remaining)
                {
                    _navigationContextStack.Push(c);
                }
                return;
            }

            _navigationContextStack.Pop();
            Logger.Debug<XInputService>($"Navigation context popped: {owner.GetType().Name} (stack depth: {_navigationContextStack.Count})");
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
            Logger.Info<XInputService>("Pausing XInput polling for external process (e.g. Xenia launching)");
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
            Logger.Info<XInputService>("Resuming XInput polling after external process exited");
            Start();
        }
    }

    /// <summary>
    /// Starts polling for controller input.
    /// </summary>
    public void Start()
    {
        lock (_lock)
        {
            if (_isRunning)
            {
                Logger.Trace<XInputService>("XInputService is already running, skipping duplicate start");
                return;
            }

            Logger.Info<XInputService>("Starting XInput polling service");
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
                Logger.Trace<XInputService>("XInputService is not running, nothing to stop");
                return;
            }

            Logger.Info<XInputService>("Stopping XInput polling service");
            _pollTimer?.Dispose();
            _pollTimer = null;
            _isRunning = false;
            _heldAction = null;
        }
    }

    /// <summary>
    /// Polls controller slot 0 and raises navigation events based on its current state.
    /// Runs on the Timer's thread pool thread; event handlers subscribing from the UI
    /// are responsible for marshalling back to the UI thread if needed.
    /// </summary>
    private void Poll(object? state)
    {
        try
        {
            int result = NativeMethods.XInputGetState(0, out XInputState state0);
            bool isConnected = result == 0; // ERROR_SUCCESS

            if (isConnected != _wasConnected)
            {
                _wasConnected = isConnected;
                Logger.Info<XInputService>($"Controller slot 0 connection state changed: {(isConnected ? "connected" : "disconnected")}");
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
                return;
            }

            XInputGamepad gamepad = state0.Gamepad;

            // Resolve the current directional intent (D-Pad takes priority over the stick)
            ControllerNavigationAction? currentDirection = ResolveDirection(gamepad);

            // Edge-triggered buttons (fire once per press, not while held)
            bool aPressed = (gamepad.wButtons & XInputButtons.A) != 0;
            bool bPressed = (gamepad.wButtons & XInputButtons.B) != 0;
            bool xPressed = (gamepad.wButtons & XInputButtons.X) != 0;
            bool yPressed = (gamepad.wButtons & XInputButtons.Y) != 0;
            // Note: the physical button XInput calls "Back" is labeled "View" on Xbox
            // One/Series controllers - it's the small button left of center, distinct
            // from our logical ControllerNavigationAction.Back (mapped to the B face button).
            bool viewPressed = (gamepad.wButtons & XInputButtons.Back) != 0;

            if (aPressed && !_previousAPressed)
            {
                Logger.Debug<XInputService>("Controller: A pressed -> Confirm");
                NavigationActionTriggered?.Invoke(this, ControllerNavigationAction.Confirm);
            }

            if (bPressed && !_previousBPressed)
            {
                Logger.Debug<XInputService>("Controller: B pressed -> Back");
                NavigationActionTriggered?.Invoke(this, ControllerNavigationAction.Back);
            }

            if (xPressed && !_previousXPressed)
            {
                Logger.Debug<XInputService>("Controller: X pressed -> Info");
                NavigationActionTriggered?.Invoke(this, ControllerNavigationAction.Info);
            }

            if (yPressed && !_previousYPressed)
            {
                Logger.Debug<XInputService>("Controller: Y pressed -> Menu");
                NavigationActionTriggered?.Invoke(this, ControllerNavigationAction.Menu);
            }

            if (viewPressed && !_previousViewPressed)
            {
                Logger.Debug<XInputService>("Controller: View pressed -> ToggleView");
                NavigationActionTriggered?.Invoke(this, ControllerNavigationAction.ToggleView);
            }

            _previousAPressed = aPressed;
            _previousBPressed = bPressed;
            _previousXPressed = xPressed;
            _previousYPressed = yPressed;
            _previousViewPressed = viewPressed;

            HandleDirectionalRepeat(currentDirection);
        }
        catch (Exception ex)
        {
            // Defensive: a polling failure should never crash the app or stop future polls
            Logger.Error<XInputService>("Unexpected error while polling XInput state");
            Logger.LogExceptionDetails<XInputService>(ex);
        }
    }

    /// <summary>
    /// Handles the "press once, then repeat while held" behavior for directional navigation,
    /// so holding the D-Pad/stick doesn't fire dozens of events per second, but still lets
    /// the user scroll through a long list by holding a direction.
    /// </summary>
    private void HandleDirectionalRepeat(ControllerNavigationAction? currentDirection)
    {
        DateTime now = DateTime.UtcNow;

        if (currentDirection == null)
        {
            _heldAction = null;
            return;
        }

        if (_heldAction != currentDirection)
        {
            // New direction: fire immediately, then wait RepeatDelayMs before auto-repeating
            _heldAction = currentDirection;
            _heldSince = now;
            _lastRepeatFired = now;
            Logger.Debug<XInputService>($"Controller: direction -> {currentDirection}");
            NavigationActionTriggered?.Invoke(this, currentDirection.Value);
            return;
        }

        // Same direction still held: check if it's time to repeat
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
    private static ControllerNavigationAction? ResolveDirection(XInputGamepad gamepad)
    {
        // D-Pad takes priority over the analog stick
        if ((gamepad.wButtons & XInputButtons.DPadUp) != 0)
        {
            return ControllerNavigationAction.Up;
        }
        if ((gamepad.wButtons & XInputButtons.DPadDown) != 0)
        {
            return ControllerNavigationAction.Down;
        }
        if ((gamepad.wButtons & XInputButtons.DPadLeft) != 0)
        {
            return ControllerNavigationAction.Left;
        }
        if ((gamepad.wButtons & XInputButtons.DPadRight) != 0)
        {
            return ControllerNavigationAction.Right;
        }

        // Fall back to the left analog stick, past the deadzone
        short x = gamepad.sThumbLX;
        short y = gamepad.sThumbLY;

        if (Math.Abs(x) < StickDeadzone && Math.Abs(y) < StickDeadzone)
        {
            return null;
        }

        // Whichever axis has the larger magnitude "wins", so diagonal stick tilts
        // resolve to a single clear direction instead of firing two at once
        if (Math.Abs(x) > Math.Abs(y))
        {
            return x > 0 ? ControllerNavigationAction.Right : ControllerNavigationAction.Left;
        }

        return y > 0 ? ControllerNavigationAction.Up : ControllerNavigationAction.Down;
    }

    /// <summary>
    /// Disposes the service by stopping polling.
    /// </summary>
    public void Dispose()
    {
        Logger.Trace<XInputService>("Disposing XInputService");
        Stop();
        if (ReferenceEquals(Current, this))
        {
            Current = null;
        }
        GC.SuppressFinalize(this);
    }

    // --- XInput native interop -------------------------------------------------

    [Flags]
    private enum XInputButtons : ushort
    {
        DPadUp = 0x0001,
        DPadDown = 0x0002,
        DPadLeft = 0x0004,
        DPadRight = 0x0008,
        Start = 0x0010,
        Back = 0x0020,
        LeftThumb = 0x0040,
        RightThumb = 0x0080,
        LeftShoulder = 0x0100,
        RightShoulder = 0x0200,
        A = 0x1000,
        B = 0x2000,
        X = 0x4000,
        Y = 0x8000
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct XInputGamepad
    {
        public XInputButtons wButtons;
        public byte bLeftTrigger;
        public byte bRightTrigger;
        public short sThumbLX;
        public short sThumbLY;
        public short sThumbRX;
        public short sThumbRY;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct XInputState
    {
        public uint dwPacketNumber;
        public XInputGamepad Gamepad;
    }

    /// <summary>
    /// Direct P/Invoke bindings to XInput1_4.dll (bundled with Windows 8+), avoiding
    /// an extra NuGet dependency for this experimental feature.
    /// </summary>
    private static class NativeMethods
    {
        [DllImport("xinput1_4.dll", EntryPoint = "XInputGetState")]
        public static extern int XInputGetState(int dwUserIndex, out XInputState pState);
    }
}
