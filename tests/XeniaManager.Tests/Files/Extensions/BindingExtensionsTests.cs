using XeniaManager.Files.Extensions;
using XeniaManager.Files.Models.Bindings;

namespace XeniaManager.Tests.Files.Extensions;

public class BindingExtensionsTests
{
    [Test]
    public void ToBindingString_WithAttribute_ReturnsName()
    {
        // VirtualKeyCode.A has [BindingName("A")]
        string result = VirtualKeyCode.A.ToBindingString();
        Assert.That(result, Is.EqualTo("A"));
    }

    [Test]
    public void ToBindingString_WithAlternative_ReturnsPrimaryName()
    {
        // LButton has [BindingName("LMouse", "LClick")]
        string result = VirtualKeyCode.LButton.ToBindingString();
        Assert.That(result, Is.EqualTo("LMouse"));
    }

    [Test]
    public void ToBindingString_None_ReturnsNone()
    {
        string result = VirtualKeyCode.None.ToBindingString();
        Assert.That(result, Is.EqualTo("None"));
    }

    [Test]
    public void ToXeniaKey_None_ReturnsNull()
    {
        string? result = VirtualKeyCode.None.ToXeniaKey();
        Assert.That(result, Is.Null);
    }

    [Test]
    public void ToXeniaKey_ValidKey_ReturnsName()
    {
        string? result = VirtualKeyCode.A.ToXeniaKey();
        Assert.That(result, Is.EqualTo("A"));
    }

    [Test]
    public void ToXeniaKey_WithAttribute_ReturnsName()
    {
        string? result = VirtualKeyCode.LButton.ToXeniaKey();
        Assert.That(result, Is.EqualTo("LMouse"));
    }

    [Test]
    public void ParseFromBindingString_PrimaryName_ReturnsEnum()
    {
        VirtualKeyCode? result = BindingExtensions.ParseFromBindingString<VirtualKeyCode>("A");
        Assert.That(result, Is.EqualTo(VirtualKeyCode.A));
    }

    [Test]
    public void ParseFromBindingString_AlternativeName_ReturnsEnum()
    {
        VirtualKeyCode? result = BindingExtensions.ParseFromBindingString<VirtualKeyCode>("LClick");
        Assert.That(result, Is.EqualTo(VirtualKeyCode.LButton));
    }

    [Test]
    public void ParseFromBindingString_CaseSensitive_ReturnsNullForWrongCase()
    {
        VirtualKeyCode? result = BindingExtensions.ParseFromBindingString<VirtualKeyCode>("a");
        Assert.That(result, Is.Null);
    }

    [Test]
    public void ParseFromBindingString_Unknown_ReturnsNull()
    {
        VirtualKeyCode? result = BindingExtensions.ParseFromBindingString<VirtualKeyCode>("NotAKey");
        Assert.That(result, Is.Null);
    }

    [Test]
    public void ParseFromBindingString_Empty_ReturnsNull()
    {
        VirtualKeyCode? result = BindingExtensions.ParseFromBindingString<VirtualKeyCode>("");
        Assert.That(result, Is.Null);
    }

    [Test]
    public void ToBindingString_XInputBinding_ReturnsName()
    {
        // XInputBinding values also have BindingName
        string result = XInputBinding.A.ToBindingString();
        Assert.That(result, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void ParseFromBindingString_XInputBinding_RoundTrip()
    {
        string name = XInputBinding.A.ToBindingString();
        XInputBinding? parsed = BindingExtensions.ParseFromBindingString<XInputBinding>(name);
        Assert.That(parsed, Is.EqualTo(XInputBinding.A));
    }

    [Test]
    public void ToXeniaKey_AllNonNone_HaveMapping()
    {
        foreach (VirtualKeyCode code in Enum.GetValues<VirtualKeyCode>())
        {
            if (code == VirtualKeyCode.None)
            {
                continue;
            }

            string? key = code.ToXeniaKey();
            Assert.That(key, Is.Not.Null, $"{code} should have Xenia key");
        }
    }
}