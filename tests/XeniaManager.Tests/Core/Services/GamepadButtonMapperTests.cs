using SDL;
using XeniaManager.Core.Models;
using XeniaManager.Core.Services;

namespace XeniaManager.Tests.Core.Services;

public class GamepadButtonMapperTests
{
    [Test]
    public void Map_South_ReturnsA() => Assert.That(GamepadButtonMapper.Map(SDL_GamepadButton.SDL_GAMEPAD_BUTTON_SOUTH), Is.EqualTo(GamepadButton.A));

    [Test]
    public void Map_East_ReturnsB() => Assert.That(GamepadButtonMapper.Map(SDL_GamepadButton.SDL_GAMEPAD_BUTTON_EAST), Is.EqualTo(GamepadButton.B));

    [Test]
    public void Map_West_ReturnsX() => Assert.That(GamepadButtonMapper.Map(SDL_GamepadButton.SDL_GAMEPAD_BUTTON_WEST), Is.EqualTo(GamepadButton.X));

    [Test]
    public void Map_North_ReturnsY() => Assert.That(GamepadButtonMapper.Map(SDL_GamepadButton.SDL_GAMEPAD_BUTTON_NORTH), Is.EqualTo(GamepadButton.Y));

    [Test]
    public void Map_DpadLeft_ReturnsDpadLeft() =>
        Assert.That(GamepadButtonMapper.Map(SDL_GamepadButton.SDL_GAMEPAD_BUTTON_DPAD_LEFT), Is.EqualTo(GamepadButton.DpadLeft));

    [Test]
    public void Map_DpadRight_ReturnsDpadRight() =>
        Assert.That(GamepadButtonMapper.Map(SDL_GamepadButton.SDL_GAMEPAD_BUTTON_DPAD_RIGHT), Is.EqualTo(GamepadButton.DpadRight));

    [Test]
    public void Map_DpadUp_ReturnsDpadUp() =>
        Assert.That(GamepadButtonMapper.Map(SDL_GamepadButton.SDL_GAMEPAD_BUTTON_DPAD_UP), Is.EqualTo(GamepadButton.DpadUp));

    [Test]
    public void Map_DpadDown_ReturnsDpadDown() =>
        Assert.That(GamepadButtonMapper.Map(SDL_GamepadButton.SDL_GAMEPAD_BUTTON_DPAD_DOWN), Is.EqualTo(GamepadButton.DpadDown));

    [Test]
    public void Map_LeftShoulder_ReturnsLeftShoulder() => Assert.That(GamepadButtonMapper.Map(SDL_GamepadButton.SDL_GAMEPAD_BUTTON_LEFT_SHOULDER),
        Is.EqualTo(GamepadButton.LeftShoulder));

    [Test]
    public void Map_RightShoulder_ReturnsRightShoulder() => Assert.That(GamepadButtonMapper.Map(SDL_GamepadButton.SDL_GAMEPAD_BUTTON_RIGHT_SHOULDER),
        Is.EqualTo(GamepadButton.RightShoulder));

    [Test]
    public void Map_Back_ReturnsView() => Assert.That(GamepadButtonMapper.Map(SDL_GamepadButton.SDL_GAMEPAD_BUTTON_BACK), Is.EqualTo(GamepadButton.View));

    [Test]
    public void Map_Start_ReturnsStart() => Assert.That(GamepadButtonMapper.Map(SDL_GamepadButton.SDL_GAMEPAD_BUTTON_START), Is.EqualTo(GamepadButton.Start));

    [Test]
    public void Map_Unmapped_ReturnsNull()
    {
        // Trigger buttons not navigation-relevant
        Assert.That(GamepadButtonMapper.Map(SDL_GamepadButton.SDL_GAMEPAD_BUTTON_LEFT_STICK), Is.Null);
        Assert.That(GamepadButtonMapper.Map(SDL_GamepadButton.SDL_GAMEPAD_BUTTON_RIGHT_STICK), Is.Null);
        Assert.That(GamepadButtonMapper.Map((SDL_GamepadButton)999), Is.Null);
    }

    [Test]
    public void AxisDeadzone_Is16000() => Assert.That(GamepadButtonMapper.AxisDeadzone, Is.EqualTo((short)16000));

    [Test]
    public void Map_AllNavigationButtons_MapToNonNull()
    {
        SDL_GamepadButton[] navigation =
        [
            SDL_GamepadButton.SDL_GAMEPAD_BUTTON_SOUTH,
            SDL_GamepadButton.SDL_GAMEPAD_BUTTON_EAST,
            SDL_GamepadButton.SDL_GAMEPAD_BUTTON_WEST,
            SDL_GamepadButton.SDL_GAMEPAD_BUTTON_NORTH,
            SDL_GamepadButton.SDL_GAMEPAD_BUTTON_DPAD_LEFT,
            SDL_GamepadButton.SDL_GAMEPAD_BUTTON_DPAD_RIGHT,
            SDL_GamepadButton.SDL_GAMEPAD_BUTTON_DPAD_UP,
            SDL_GamepadButton.SDL_GAMEPAD_BUTTON_DPAD_DOWN,
            SDL_GamepadButton.SDL_GAMEPAD_BUTTON_LEFT_SHOULDER,
            SDL_GamepadButton.SDL_GAMEPAD_BUTTON_RIGHT_SHOULDER,
            SDL_GamepadButton.SDL_GAMEPAD_BUTTON_BACK,
            SDL_GamepadButton.SDL_GAMEPAD_BUTTON_START
        ];
        foreach (SDL_GamepadButton btn in navigation)
        {
            Assert.That(GamepadButtonMapper.Map(btn), Is.Not.Null, $"{btn} should map");
        }
    }
}