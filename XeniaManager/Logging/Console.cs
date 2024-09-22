using System.Runtime.InteropServices;

namespace XeniaManager.Logging
{
    public partial class Logger
    {
        // This is needed for Console to show up when using argument -console
        [DllImport("Kernel32")]
        public static extern void AllocConsole();
    }
}
