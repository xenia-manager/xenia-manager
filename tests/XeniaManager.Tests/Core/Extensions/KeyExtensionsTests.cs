using Avalonia.Input;
using XeniaManager.Core.Extensions;
using XeniaManager.Files.Models.Bindings;

namespace XeniaManager.Tests.Core.Extensions;

public class KeyExtensionsTests
{
    [Test]
    public void ToVirtualKeyCode_A_ReturnsA() => Assert.That(Key.A.ToVirtualKeyCode(), Is.EqualTo(VirtualKeyCode.A));

    [Test]
    public void ToVirtualKeyCode_Z_ReturnsZ() => Assert.That(Key.Z.ToVirtualKeyCode(), Is.EqualTo(VirtualKeyCode.Z));

    [Test]
    public void ToVirtualKeyCode_D0_ReturnsD0() => Assert.That(Key.D0.ToVirtualKeyCode(), Is.EqualTo(VirtualKeyCode.D0));

    [Test]
    public void ToVirtualKeyCode_D9_ReturnsD9() => Assert.That(Key.D9.ToVirtualKeyCode(), Is.EqualTo(VirtualKeyCode.D9));

    [Test]
    public void ToVirtualKeyCode_LeftShift_ReturnsLShift() => Assert.That(Key.LeftShift.ToVirtualKeyCode(), Is.EqualTo(VirtualKeyCode.LShift));

    [Test]
    public void ToVirtualKeyCode_RightShift_ReturnsRShift() => Assert.That(Key.RightShift.ToVirtualKeyCode(), Is.EqualTo(VirtualKeyCode.RShift));

    [Test]
    public void ToVirtualKeyCode_LeftCtrl_ReturnsLControl() => Assert.That(Key.LeftCtrl.ToVirtualKeyCode(), Is.EqualTo(VirtualKeyCode.LControl));

    [Test]
    public void ToVirtualKeyCode_Escape_ReturnsEscape() => Assert.That(Key.Escape.ToVirtualKeyCode(), Is.EqualTo(VirtualKeyCode.Escape));

    [Test]
    public void ToVirtualKeyCode_Space_ReturnsSpace() => Assert.That(Key.Space.ToVirtualKeyCode(), Is.EqualTo(VirtualKeyCode.Space));

    [Test]
    public void ToVirtualKeyCode_Left_ReturnsLeft() => Assert.That(Key.Left.ToVirtualKeyCode(), Is.EqualTo(VirtualKeyCode.Left));

    [Test]
    public void ToVirtualKeyCode_F1_ReturnsF1() => Assert.That(Key.F1.ToVirtualKeyCode(), Is.EqualTo(VirtualKeyCode.F1));

    [Test]
    public void ToVirtualKeyCode_F12_ReturnsF12() => Assert.That(Key.F12.ToVirtualKeyCode(), Is.EqualTo(VirtualKeyCode.F12));

    [Test]
    public void ToVirtualKeyCode_NumPad0_ReturnsNumpad0() => Assert.That(Key.NumPad0.ToVirtualKeyCode(), Is.EqualTo(VirtualKeyCode.Numpad0));

    [Test]
    public void ToVirtualKeyCode_Add_ReturnsNumpadAdd() => Assert.That(Key.Add.ToVirtualKeyCode(), Is.EqualTo(VirtualKeyCode.NumpadAdd));

    [Test]
    public void ToVirtualKeyCode_Oem1_ReturnsOem1() => Assert.That(Key.Oem1.ToVirtualKeyCode(), Is.EqualTo(VirtualKeyCode.Oem1));

    [Test]
    public void ToVirtualKeyCode_PrintScreen_ReturnsNone() => Assert.That(Key.PrintScreen.ToVirtualKeyCode(), Is.EqualTo(VirtualKeyCode.None));

    [Test]
    public void ToVirtualKeyCode_Scroll_ReturnsNone() => Assert.That(Key.Scroll.ToVirtualKeyCode(), Is.EqualTo(VirtualKeyCode.None));

    [Test]
    public void ToVirtualKeyCode_Pause_ReturnsNone() => Assert.That(Key.Pause.ToVirtualKeyCode(), Is.EqualTo(VirtualKeyCode.None));

    [Test]
    public void ToVirtualKeyCode_Unmapped_ReturnsNull()
    {
        // Find a Key value not in switch - use 0 or large value
        Key unmapped = (Key)9999;
        Assert.That(unmapped.ToVirtualKeyCode(), Is.Null);
    }

    [Test]
    public void ToVirtualKeyCode_AllMappedKeys_HaveNoNullExceptSpecial()
    {
        // Spot check letters
        foreach (Key key in new[]
                 {
                     Key.A, Key.B, Key.C, Key.M, Key.Z
                 })
        {
            Assert.That(key.ToVirtualKeyCode(), Is.Not.Null, $"{key} should map");
        }
    }

    [Test]
    public void ToVirtualKeyCode_F24_ReturnsF24() => Assert.That(Key.F24.ToVirtualKeyCode(), Is.EqualTo(VirtualKeyCode.F24));
}