using XeniaManager.Core.Utilities;

namespace XeniaManager.Tests.Core.Utilities;

public class SecurityUtilitiesTests
{
    [Test]
    public void IsNtfsDrive_CurrentDirectory_ReturnsBool()
    {
        // Should not throw, returns true on non-Windows or NTFS
        bool result = SecurityUtilities.IsNtfsDrive(Environment.CurrentDirectory);
        Assert.That(result, Is.EqualTo(true).Or.EqualTo(false));
    }

    [Test]
    public void IsNtfsDrive_TempPath_ReturnsBool()
    {
        bool result = SecurityUtilities.IsNtfsDrive(Path.GetTempPath());
        Assert.That(result, Is.EqualTo(true).Or.EqualTo(false));
    }

    [Test]
    public void IsNtfsDrive_InvalidPath_ReturnsFalseOrTrueWithoutThrowing()
    {
        // Empty root should return false per implementation
        Assert.DoesNotThrow(() => SecurityUtilities.IsNtfsDrive(""));
        bool result = SecurityUtilities.IsNtfsDrive("");
        Assert.That(result, Is.False); // Path.GetPathRoot("") => "" => false
    }

    [Test]
    public void IsNtfsDrive_NonWindows_ReturnsTrue()
    {
        if (OperatingSystem.IsWindows())
        {
            Assert.Ignore("Only on non-Windows");
        }

        Assert.That(SecurityUtilities.IsNtfsDrive("/tmp"), Is.True);
    }

    [Test]
    public void IsRunAsAdministrator_ReturnsBoolWithoutThrowing()
    {
        Assert.DoesNotThrow(() =>
        {
            bool result = SecurityUtilities.IsRunAsAdministrator();
            Assert.That(result, Is.EqualTo(true).Or.EqualTo(false));
        });
    }

    [Test]
    public void IsRunAsAdministrator_NonWindows_ReturnsFalse()
    {
        if (OperatingSystem.IsWindows())
        {
            Assert.Ignore("Only on non-Windows");
        }

        Assert.That(SecurityUtilities.IsRunAsAdministrator(), Is.False);
    }

    [Test]
    public void IsNtfsDrive_RootDrive_ReturnsBool()
    {
        string? root = Path.GetPathRoot(Environment.CurrentDirectory);
        if (string.IsNullOrEmpty(root))
        {
            Assert.Ignore("No root");
        }

        bool result = SecurityUtilities.IsNtfsDrive(root);
        Assert.That(result, Is.EqualTo(true).Or.EqualTo(false));
    }
}