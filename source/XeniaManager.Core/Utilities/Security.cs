using System.Security.Principal;

namespace XeniaManager.Core.Utilities;
public static class Security
{
    public static bool IsRunAsAdministrator()
    {
        using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
        {
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }
}
