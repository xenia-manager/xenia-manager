using System;

// Imported
using Serilog;

namespace XeniaManager.Logging
{
    public partial class Logger
    {
        /// <summary>
        /// Used for cleaning all of the old log files (Older than 7 days)
        /// </summary>
        public static void Cleanup()
        {
            // Check if Logs folder even exists before continuing with cleanup
            // This is to prevent crashing
            if (!Directory.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Logs")))
            {
                return;
            }

            // Cleaning of old Log files
            string[] logFiles = Directory.GetFiles(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Logs"), "Log-*.txt");
            DateTime currentTime = DateTime.UtcNow;
            Log.Information("Looking for old log files to clean");
            foreach (string logFile in logFiles)
            {
                FileInfo fileInfo = new FileInfo(logFile);
                if (fileInfo.CreationTimeUtc < currentTime - TimeSpan.FromDays(7))
                {
                    Log.Information($"Deleting {fileInfo.Name}");
                    fileInfo.Delete();
                }
            }
            Log.Information("Old log files cleaned");
        }
    }
}
