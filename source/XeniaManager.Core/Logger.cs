using System.Runtime.InteropServices;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.File;

namespace XeniaManager.Core
{
    // Extension method to check for console argument
    public static class ArgumentExtensions
    {
        public static bool HasConsoleArgument(this string[] args)
        {
            return args != null &&
                   (args.Contains("-console", StringComparer.OrdinalIgnoreCase) ||
                    args.Contains("--console", StringComparer.OrdinalIgnoreCase));
        }
    }

    public static class Logger
    {
        // Variables
        private static ILogger? _logger { get; set; }

        private static bool _consoleVisible = false;

        [DllImport("kernel32.dll")]
        static extern bool AllocConsole();

        [DllImport("kernel32.dll")]
        static extern bool AttachConsole(int processId);

        [DllImport("kernel32.dll")]
        static extern bool FreeConsole();

        // Functions
        /// <summary>
        /// Initializes logger
        /// </summary>
        public static void Initialize(bool showConsole = false)
        {
            // Ensure log directory exists
            string logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            Directory.CreateDirectory(logDirectory);

            // Initialize Serilog with the configuration
            _logger = new LoggerConfiguration()
                // Set minimum log level
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File(
                path: Path.Combine(logDirectory, "Log-.txt"), // Path where the logfile gets saved
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7
                )
                .CreateLogger();

            Log.Logger = _logger;

            if (showConsole)
            {
                ShowConsole();
            }
        }

        /// <summary>
        /// Shows the console window
        /// </summary>
        public static void ShowConsole()
        {
            if (_consoleVisible) return;

            try
            {
                // Try to attach to parent console, if it exists
                AttachConsole(-1);

                // If that fails, allocate a new console
                AllocConsole();

                _consoleVisible = true;
                Info("Console enabled for debugging.");
            }
            catch (Exception ex)
            {
                Error(ex, "Failed to show console");
            }
        }

        /// <summary>
        /// Writes an informational log message
        /// </summary>
        public static void Info(string message, params object[] propertyValues)
        {
            _logger?.Information(message, propertyValues);
        }

        /// <summary>
        /// Writes a debug log message
        /// </summary>
        public static void Debug(string message, params object[] propertyValues)
        {
            _logger?.Debug(message, propertyValues);
        }

        /// <summary>
        /// Writes a warning log message
        /// </summary>
        public static void Warning(string message, params object[] propertyValues)
        {
            _logger?.Warning(message, propertyValues);
        }

        /// <summary>
        /// Writes an error log message
        /// </summary>
        public static void Error(string message, params object[] propertyValues)
        {
            _logger?.Error(message, propertyValues);
        }

        /// <summary>
        /// Writes an error log message with an exception
        /// </summary>
        public static void Error(Exception ex, string? message = null)
        {
            if (message == null)
            {
                _logger?.Error(ex, ex.Message);
            }
            else
            {
                _logger?.Error(ex, message);
            }
        }

        /// <summary>
        /// Closes and flushes the logger
        /// </summary>
        public static void Shutdown()
        {
            Log.CloseAndFlush();
            FreeConsole();
        }
    }
}
