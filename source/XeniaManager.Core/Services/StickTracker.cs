using XeniaManager.Core.Models;

namespace XeniaManager.Core.Services;

/// <summary>
/// Tracks one stick axis and raises a press only when a direction is newly
/// entered (no repeat while held, no press when returning to center).
/// Exposes the currently held direction so the gamepad service can drive
/// hold-repeat while the stick stays past the deadzone.
/// </summary>
public class StickTracker
{
    private bool _negativeHeld;
    private bool _positiveHeld;

    /// <summary>
    /// The direction currently held past the deadzone, or null when the
    /// axis is centered. Cleared when the stick returns to center.
    /// </summary>
    public GamepadButton? HeldButton { get; private set; }

    /// <summary>
    /// Feeds an axis value and returns the newly entered direction, or null
    /// when nothing should be raised (held, centered or below the deadzone).
    /// </summary>
    /// <param name="value">Raw axis value (-32768..32767).</param>
    /// <param name="negativeButton">Direction raised when the axis is negative.</param>
    /// <param name="positiveButton">Direction raised when the axis is positive.</param>
    public GamepadButton? Track(short value, GamepadButton negativeButton, GamepadButton positiveButton)
    {
        if (value < -GamepadButtonMapper.AxisDeadzone)
        {
            GamepadButton? result = !_negativeHeld && !_positiveHeld ? negativeButton : null;
            _negativeHeld = true;
            _positiveHeld = false;
            HeldButton = negativeButton;
            return result;
        }

        if (value > GamepadButtonMapper.AxisDeadzone)
        {
            GamepadButton? result = !_positiveHeld && !_negativeHeld ? positiveButton : null;
            _positiveHeld = true;
            _negativeHeld = false;
            HeldButton = positiveButton;
            return result;
        }

        _negativeHeld = false;
        _positiveHeld = false;
        HeldButton = null;
        return null;
    }
}