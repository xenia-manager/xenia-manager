using System.IO;
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

    /// <summary>
    /// Checks whether the given path is on an NTFS-formatted drive.
    /// On non-Windows systems, this always returns true since symbolic links are broadly supported.
    /// </summary>
    /// <param name="path">An absolute or relative path on the drive to check.</param>
    /// <returns>True if the drive is NTFS or the OS is not Windows; false otherwise.</returns>
    public static bool IsNtfsDrive(string path)
    {
        Logger.Trace<SecurityUtilities>("Checking if path is on an NTFS drive");

        if (!OperatingSystem.IsWindows())
        {
            Logger.Debug<SecurityUtilities>("Operating system is not Windows, skipping NTFS check");
            return true;
        }

        try
        {
            string? root = Path.GetPathRoot(path);
            if (string.IsNullOrEmpty(root))
            {
                Logger.Debug<SecurityUtilities>($"Could not determine drive root for path: {path}");
                return false;
            }

            DriveInfo drive = new DriveInfo(root);
            bool isNtfs = string.Equals(drive.DriveFormat, "NTFS", StringComparison.OrdinalIgnoreCase);

            Logger.Info<SecurityUtilities>($"Drive {root} is {(isNtfs ? "" : "not ")}NTFS (format: {drive.DriveFormat})");
            return isNtfs;
        }
        catch (Exception ex)
        {
            Logger.Error<SecurityUtilities>($"Failed to check drive format for path: {path}");
            Logger.LogExceptionDetails<SecurityUtilities>(ex);
            return false;
        }
    }
}