using System.Runtime.InteropServices;

// Imported
using Serilog;

namespace XeniaManager.Logging
{
    public partial class Logger
    {
        /// <summary>
        /// Shows the console when called
        /// </summary>
        [DllImport("Kernel32")]
        public static extern void AllocConsole();

        /// <summary>
        /// Initializes logger
        /// </summary>
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
