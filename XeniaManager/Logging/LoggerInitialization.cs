using System.Runtime.InteropServices;

// Imported
using Serilog;

namespace XeniaManager.Logging
{
    public partial class Logger
    {
        // This is needed for Console to show up when using argument -console
        [DllImport("Kernel32")]
        public static extern void AllocConsole();

        public static void InitializeLogger()
        {
            // Initialize Serilog with the configuration
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.File("Logs/Log-.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();
        }
    }
}
