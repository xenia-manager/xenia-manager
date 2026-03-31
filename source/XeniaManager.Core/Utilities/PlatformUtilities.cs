using XeniaManager.Core.Logging;

namespace XeniaManager.Core.Utilities;

/// <summary>
/// Provides utility methods for platform detection and compatibility checks.
/// </summary>
public class PlatformUtilities
{
    /// <summary>
    /// Indicates whether the application is running on native Windows (not under Wine/Proton).
    /// </summary>
    /// <returns>True if running on native Windows; otherwise, false.</returns>
    public static bool IsNativeWindows()
    {
        Logger.Trace<PlatformUtilities>("Checking if running on native Windows");

        if (!OperatingSystem.IsWindows())
        {
            Logger.Debug<PlatformUtilities>("Operating system is not Windows, returning false");
            return false;
        }

        // Check if running under Wine/Proton
        bool isWine = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WINEPREFIX"))
                      || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PROTONPATH"));

        if (isWine)
        {
            Logger.Debug<PlatformUtilities>("Running under Wine/Proton, returning false");
            return false;
        }

        Logger.Debug<PlatformUtilities>("Running on native Windows");
        return true;
    }
}