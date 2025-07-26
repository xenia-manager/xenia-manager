using System.Runtime.InteropServices;

namespace XeniaManager.Core.Utilities;
public static class ConsoleHelper
{
    [DllImport("kernel32.dll")]
    static extern bool AllocConsole();

    [DllImport("kernel32.dll")]
    static extern bool FreeConsole();

    public static void ShowConsole()
    {
        if (!AllocConsole())
        {
            Logger.Warning("Failed to allocate console - console may already be allocated");
        }
    }

    public static void HideConsole()
    {
        if (!FreeConsole())
        {
            Logger.Warning("Failed to free console - no console may be allocated\"");
        }
    }
}
