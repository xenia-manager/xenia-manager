using XeniaManager.Core.Utilities;

namespace XeniaManager.Tests.Core.Utilities;

public class PlatformUtilitiesTests
{
    [Test]
    public void IsNativeWindows_ReturnsBoolWithoutThrowing()
    {
        Assert.DoesNotThrow(() =>
        {
            bool result = PlatformUtilities.IsNativeWindows();
            Assert.That(result, Is.EqualTo(true).Or.EqualTo(false));
        });
    }

    [Test]
    public void IsNativeWindows_NonWindows_ReturnsFalse()
    {
        if (OperatingSystem.IsWindows())
        {
            Assert.Ignore("Only verify on non-Windows CI");
        }

        Assert.That(PlatformUtilities.IsNativeWindows(), Is.False);
    }

    [Test]
    public void IsNativeWindows_WithWineEnv_ReturnsFalse()
    {
        if (!OperatingSystem.IsWindows())
        {
            Assert.Ignore("Wine check only on Windows");
        }

        string? originalWine = Environment.GetEnvironmentVariable("WINEPREFIX");
        string? originalProton = Environment.GetEnvironmentVariable("PROTONPATH");
        try
        {
            Environment.SetEnvironmentVariable("WINEPREFIX", "/fake/wine");
            Assert.That(PlatformUtilities.IsNativeWindows(), Is.False);
        }
        finally
        {
            Environment.SetEnvironmentVariable("WINEPREFIX", originalWine);
            Environment.SetEnvironmentVariable("PROTONPATH", originalProton);
        }
    }

    [Test]
    public void IsNativeWindows_WithProtonEnv_ReturnsFalse()
    {
        if (!OperatingSystem.IsWindows())
        {
            Assert.Ignore("Proton check only on Windows");
        }

        string? originalWine = Environment.GetEnvironmentVariable("WINEPREFIX");
        string? originalProton = Environment.GetEnvironmentVariable("PROTONPATH");
        try
        {
            Environment.SetEnvironmentVariable("WINEPREFIX", null);
            Environment.SetEnvironmentVariable("PROTONPATH", "/fake/proton");
            Assert.That(PlatformUtilities.IsNativeWindows(), Is.False);
        }
        finally
        {
            Environment.SetEnvironmentVariable("WINEPREFIX", originalWine);
            Environment.SetEnvironmentVariable("PROTONPATH", originalProton);
        }
    }

    [Test]
    public void IsNativeWindows_WithoutWineOrProton_OnWindows_MayReturnTrue()
    {
        if (!OperatingSystem.IsWindows())
        {
            Assert.Ignore("Windows only");
        }

        string? origWine = Environment.GetEnvironmentVariable("WINEPREFIX");
        string? origProton = Environment.GetEnvironmentVariable("PROTONPATH");
        try
        {
            Environment.SetEnvironmentVariable("WINEPREFIX", null);
            Environment.SetEnvironmentVariable("PROTONPATH", null);
            bool result = PlatformUtilities.IsNativeWindows();
            // On native Windows without wine/proton, should be true
            Assert.That(result, Is.True);
        }
        finally
        {
            Environment.SetEnvironmentVariable("WINEPREFIX", origWine);
            Environment.SetEnvironmentVariable("PROTONPATH", origProton);
        }
    }
}