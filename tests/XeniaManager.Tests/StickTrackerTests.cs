using XeniaManager.Core.Models;
using XeniaManager.Core.Services;

namespace XeniaManager.Tests;

[TestFixture]
public class StickTrackerTests
{
    [Test]
    public void Track_StickPushedPastDeadzone_ExposesHeldDirection()
    {
        StickTracker tracker = new();

        tracker.Track(20000, GamepadButton.DpadLeft, GamepadButton.DpadRight);

        Assert.That(tracker.HeldButton, Is.EqualTo(GamepadButton.DpadRight));
    }

    [Test]
    public void Track_StickReturnedToCenter_ClearsHeldDirection()
    {
        StickTracker tracker = new();
        tracker.Track(20000, GamepadButton.DpadLeft, GamepadButton.DpadRight);

        tracker.Track(0, GamepadButton.DpadLeft, GamepadButton.DpadRight);

        Assert.That(tracker.HeldButton, Is.Null);
    }

    [Test]
    public void Track_StickBelowDeadzone_NeverExposesHeldDirection()
    {
        StickTracker tracker = new();

        tracker.Track(-5000, GamepadButton.DpadLeft, GamepadButton.DpadRight);

        Assert.That(tracker.HeldButton, Is.Null);
    }

    [Test]
    public void Track_StickSwitchesDirectionDirectly_HeldDirectionFollows()
    {
        StickTracker tracker = new();
        tracker.Track(20000, GamepadButton.DpadLeft, GamepadButton.DpadRight);

        tracker.Track(-20000, GamepadButton.DpadLeft, GamepadButton.DpadRight);

        Assert.That(tracker.HeldButton, Is.EqualTo(GamepadButton.DpadLeft));
    }
}
