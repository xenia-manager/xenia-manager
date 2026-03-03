using System.Security.Principal;
using XeniaManager.Core.Logging;

namespace XeniaManager.Core.Utilities;

/// <summary>
/// Provides utility methods for security-related operations such as checking administrator privileges.
/// </summary>
public class SecurityUtilities
{
    /// <summary>
    /// Checks whether the current process is running with administrator privileges.
    /// This method only works on Windows platforms.
    /// </summary>
    /// <returns>True if the process is running as administrator on Windows, false otherwise.</returns>
    public static bool IsRunAsAdministrator()
    {
        Logger.Trace<SecurityUtilities>("Checking if process is running as administrator");

        if (!OperatingSystem.IsWindows())
        {
            Logger.Debug<SecurityUtilities>("Operating system is not Windows, returning false");
            return false;
        }

        using WindowsIdentity identity = WindowsIdentity.GetCurrent();
        WindowsPrincipal principal = new WindowsPrincipal(identity);
        bool isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);

        Logger.Info<SecurityUtilities>($"Process is running {(isAdmin ? "as" : "without")} administrator privileges");
        return isAdmin;
    }
}