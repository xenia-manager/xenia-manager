using Serilog;

namespace XeniaManager.Logging
{
    public partial class Logger
    {
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
