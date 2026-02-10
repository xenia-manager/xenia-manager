using System.Runtime.CompilerServices;
using System.Text;
using NLog;
using NLog.Config;
using NLog.Targets;
using XeniaManager.Core.Constants;

namespace XeniaManager.Core.Logging;

/// <summary>
/// Provides centralized logging functionality with support for console and file output,
/// colored console messages, and type-based logging with automatic class/method name resolution.
/// </summary>
public static class Logger
{
    // Properties
    private static readonly NLog.Logger _logger;
    private static readonly LoggingConfiguration _config;

    // Constructor
    static Logger()
    {
        _config = new LoggingConfiguration();

        // Console target (colored)
        ColoredConsoleTarget consoleTarget = new ColoredConsoleTarget("console")
        {
            Layout = @"[${longdate:format=HH\:mm\:ss.fff}][${level:uppercase=true:format=FirstCharacter}] ${message}"
        };
        consoleTarget.RowHighlightingRules.Add(new ConsoleRowHighlightingRule
        {
            Condition = "level == LogLevel.Warn",
            ForegroundColor = ConsoleOutputColor.Yellow
        });
        consoleTarget.RowHighlightingRules.Add(new ConsoleRowHighlightingRule
        {
            Condition = "level == LogLevel.Error",
            ForegroundColor = ConsoleOutputColor.Red
        });
        consoleTarget.RowHighlightingRules.Add(new ConsoleRowHighlightingRule
        {
            Condition = "level == LogLevel.Fatal",
            ForegroundColor = ConsoleOutputColor.DarkRed
        });
        _config.AddTarget(consoleTarget);
        _config.AddRule(LogLevel.Trace, LogLevel.Fatal, consoleTarget);

        // File target
        FileTarget fileTarget = new FileTarget("file")
        {
            FileName = $"{AppPaths.LogsDirectory}/Log-${{shortdate}}.log",
            Layout = @"[${longdate:format=HH\:mm\:ss.fff}][${level:uppercase=true:format=FirstCharacter}] ${message}",
            KeepFileOpen = false,
            Encoding = Encoding.UTF8
        };
        _config.AddTarget(fileTarget);
        _config.AddRule(LogLevel.Trace, LogLevel.Fatal, fileTarget);

        LogManager.Configuration = _config;
        _logger = LogManager.GetCurrentClassLogger();
    }

    // Functions
    /// <summary>
    /// Dynamically updates the minimum logging level for all configured logging targets.
    /// </summary>
    /// <param name="level">The minimum log level to capture (Trace, Debug, Info, Warn, Error, Fatal).</param>
    public static void SetLogLevel(LogLevel level)
    {
        IList<LoggingRule> rules = _config.LoggingRules;

        foreach (LoggingRule rule in rules)
        {
            rule.SetLoggingLevels(level, LogLevel.Fatal);
        }

        LogManager.ReconfigExistingLoggers();
        _logger.Info($"Logging level updated: {level}");
    }

    /// <summary>
    /// Flushes NLog's LogManager, ensuring logs are written before potential crash
    /// </summary>
    public static void Flush()
    {
        LogManager.Flush();
    }

    /// <summary>
    /// Extracts a clean type name from a generic type parameter, handling generic types
    /// and nested classes by removing generic arity markers and including parent class names.
    /// </summary>
    /// <typeparam name="T">The type to extract the name from.</typeparam>
    /// <returns>A formatted type name without generic markers (e.g., "MyClass" instead of "MyClass`1").</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string GetTypeName<T>()
    {
        Type type = typeof(T);

        // Handle generic types - remove `1, `2, etc.
        string typeName = type.Name;
        int backtickIndex = typeName.IndexOf('`');
        if (backtickIndex > 0)
        {
            typeName = typeName.Substring(0, backtickIndex);
        }

        // Handle nested types - include parent class
        if (type is not { IsNested: true, DeclaringType: not null })
        {
            return typeName;
        }
        string declaringName = type.DeclaringType.Name;
        int declaringBacktick = declaringName.IndexOf('`');
        if (declaringBacktick > 0)
        {
            declaringName = declaringName.Substring(0, declaringBacktick);
        }
        return $"{declaringName}.{typeName}";
    }

    // Type-based logging (class name only)
    /// <summary>
    /// Logs a trace-level message with the specified type's class name as a prefix.
    /// </summary>
    /// <typeparam name="T">The type to use for class name resolution.</typeparam>
    /// <param name="message">The message to log.</param>
    public static void Trace<T>(string message)
    {
        _logger.Trace($"[{GetTypeName<T>()}] {message}");
    }

    /// <summary>
    /// Logs a debug-level message with the specified type's class name as a prefix.
    /// </summary>
    /// <typeparam name="T">The type to use for class name resolution.</typeparam>
    /// <param name="message">The message to log.</param>
    public static void Debug<T>(string message)
    {
        _logger.Debug($"[{GetTypeName<T>()}] {message}");
    }

    /// <summary>
    /// Logs an info-level message with the specified type's class name as a prefix.
    /// </summary>
    /// <typeparam name="T">The type to use for class name resolution.</typeparam>
    /// <param name="message">The message to log.</param>
    public static void Info<T>(string message)
    {
        _logger.Info($"[{GetTypeName<T>()}] {message}");
    }

    /// <summary>
    /// Logs a warning-level message with the specified type's class name as a prefix.
    /// </summary>
    /// <typeparam name="T">The type to use for class name resolution.</typeparam>
    /// <param name="message">The message to log.</param>
    public static void Warning<T>(string message)
    {
        _logger.Warn($"[{GetTypeName<T>()}] {message}");
    }

    /// <summary>
    /// Logs an error-level message with the specified type's class name as a prefix.
    /// </summary>
    /// <typeparam name="T">The type to use for class name resolution.</typeparam>
    /// <param name="message">The message to log.</param>
    public static void Error<T>(string message)
    {
        _logger.Error($"[{GetTypeName<T>()}] {message}");
    }

    /// <summary>
    /// Logs a fatal-level message with the specified type's class name as a prefix.
    /// </summary>
    /// <typeparam name="T">The type to use for class name resolution.</typeparam>
    /// <param name="message">The message to log.</param>
    public static void Fatal<T>(string message)
    {
        _logger.Fatal($"[{GetTypeName<T>()}] {message}");
    }

    // Type-based logging WITH method names
    /// <summary>
    /// Logs a trace-level message with the specified type's class name and calling method name as a prefix.
    /// </summary>
    /// <typeparam name="T">The type to use for class name resolution.</typeparam>
    /// <param name="message">The message to log.</param>
    /// <param name="methodName">The name of the calling method (automatically captured via CallerMemberName).</param>
    public static void Trace<T>(string message, [CallerMemberName] string? methodName = null)
    {
        string className = GetTypeName<T>();
        string prefix = string.IsNullOrEmpty(methodName) ? className : $"{className}.{methodName}";
        _logger.Trace($"[{prefix}] {message}");
    }

    /// <summary>
    /// Logs a debug-level message with the specified type's class name and calling method name as a prefix.
    /// </summary>
    /// <typeparam name="T">The type to use for class name resolution.</typeparam>
    /// <param name="message">The message to log.</param>
    /// <param name="methodName">The name of the calling method (automatically captured via CallerMemberName).</param>
    public static void Debug<T>(string message, [CallerMemberName] string? methodName = null)
    {
        string className = GetTypeName<T>();
        string prefix = string.IsNullOrEmpty(methodName) ? className : $"{className}.{methodName}";
        _logger.Debug($"[{prefix}] {message}");
    }

    /// <summary>
    /// Logs an info-level message with the specified type's class name and calling method name as a prefix.
    /// </summary>
    /// <typeparam name="T">The type to use for class name resolution.</typeparam>
    /// <param name="message">The message to log.</param>
    /// <param name="methodName">The name of the calling method (automatically captured via CallerMemberName).</param>
    public static void Info<T>(string message, [CallerMemberName] string? methodName = null)
    {
        string className = GetTypeName<T>();
        string prefix = string.IsNullOrEmpty(methodName) ? className : $"{className}.{methodName}";
        _logger.Info($"[{prefix}] {message}");
    }

    /// <summary>
    /// Logs a warning-level message with the specified type's class name and calling method name as a prefix.
    /// </summary>
    /// <typeparam name="T">The type to use for class name resolution.</typeparam>
    /// <param name="message">The message to log.</param>
    /// <param name="methodName">The name of the calling method (automatically captured via CallerMemberName).</param>
    public static void Warning<T>(string message, [CallerMemberName] string? methodName = null)
    {
        string className = GetTypeName<T>();
        string prefix = string.IsNullOrEmpty(methodName) ? className : $"{className}.{methodName}";
        _logger.Warn($"[{prefix}] {message}");
    }

    /// <summary>
    /// Logs an error-level message with the specified type's class name and calling method name as a prefix.
    /// </summary>
    /// <typeparam name="T">The type to use for class name resolution.</typeparam>
    /// <param name="message">The message to log.</param>
    /// <param name="methodName">The name of the calling method (automatically captured via CallerMemberName).</param>
    public static void Error<T>(string message, [CallerMemberName] string? methodName = null)
    {
        string className = GetTypeName<T>();
        string prefix = string.IsNullOrEmpty(methodName) ? className : $"{className}.{methodName}";
        _logger.Error($"[{prefix}] {message}");
    }

    /// <summary>
    /// Logs a fatal-level message with the specified type's class name and calling method name as a prefix.
    /// </summary>
    /// <typeparam name="T">The type to use for class name resolution.</typeparam>
    /// <param name="message">The message to log.</param>
    /// <param name="methodName">The name of the calling method (automatically captured via CallerMemberName).</param>
    public static void Fatal<T>(string message, [CallerMemberName] string? methodName = null)
    {
        string className = GetTypeName<T>();
        string prefix = string.IsNullOrEmpty(methodName) ? className : $"{className}.{methodName}";
        _logger.Fatal($"[{prefix}] {message}");
    }

    /// <summary>
    /// Logs comprehensive exception details including type, message, stack trace, inner exceptions,
    /// and optionally system/environment information.
    /// </summary>
    /// <typeparam name="T">The type to use for class name resolution in log prefixes.</typeparam>
    /// <param name="ex">The exception to log.</param>
    /// <param name="includeEnvironmentInfo">Whether to include system information (machine name, OS version, etc.).</param>
    public static void LogExceptionDetails<T>(Exception ex, bool includeEnvironmentInfo = true)
    {
        string className = GetTypeName<T>();
        _logger.Error($"[{className}] ===== Exception Report Start =====");
        _logger.Error($"[{className}] Timestamp (UTC): {DateTime.UtcNow:O}");

        LogExceptionWithDepth(ex, className);

        if (includeEnvironmentInfo)
        {
            _logger.Error($"[{className}] === System Information ===");
            _logger.Error($"[{className}] Machine Name: {Environment.MachineName}");
            _logger.Error($"[{className}] OS Version: {Environment.OSVersion}");
            _logger.Error($"[{className}] .NET Runtime: {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");
            _logger.Error($"[{className}] Process Architecture: {System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture}");
            _logger.Error($"[{className}] Current Directory: {Environment.CurrentDirectory}");
        }

        _logger.Error($"[{className}] ===== Exception Report End =====");
    }

    // Original logging (for backward compatibility - DEPRECATED)
    /// <summary>
    /// Logs a trace-level message without type-based prefixes.
    /// </summary>
    /// <param name="message">The message to log.</param>
    [Obsolete("Use Logger.Trace<T>(message) instead for class-specific logging")]
    public static void Trace(string message) => _logger.Trace(message);

    /// <summary>
    /// Logs a debug-level message without type-based prefixes.
    /// </summary>
    /// <param name="message">The message to log.</param>
    [Obsolete("Use Logger.Debug<T>(message) instead for class-specific logging")]
    public static void Debug(string message) => _logger.Debug(message);

    /// <summary>
    /// Logs an info-level message without type-based prefixes.
    /// </summary>
    /// <param name="message">The message to log.</param>
    [Obsolete("Use Logger.Info<T>(message) instead for class-specific logging")]
    public static void Info(string message) => _logger.Info(message);

    /// <summary>
    /// Logs a warning-level message without type-based prefixes.
    /// </summary>
    /// <param name="message">The message to log.</param>
    [Obsolete("Use Logger.Warning<T>(message) instead for class-specific logging")]
    public static void Warning(string message) => _logger.Warn(message);

    /// <summary>
    /// Logs an error-level message without type-based prefixes.
    /// </summary>
    /// <param name="message">The message to log.</param>
    [Obsolete("Use Logger.Error<T>(message) instead for class-specific logging")]
    public static void Error(string message) => _logger.Error(message);

    /// <summary>
    /// Logs a fatal-level message without type-based prefixes.
    /// </summary>
    /// <param name="message">The message to log.</param>
    [Obsolete("Use Logger.Fatal<T>(message) instead for class-specific logging")]
    public static void Fatal(string message) => _logger.Fatal(message);

    /// <summary>
    /// Logs comprehensive exception details without type-based prefixes.
    /// </summary>
    /// <param name="ex">The exception to log.</param>
    /// <param name="includeEnvironmentInfo">Whether to include system information (machine name, OS version, etc.).</param>
    [Obsolete("Use Logger.LogExceptionDetails<T>(ex) instead for class-specific logging")]
    public static void LogExceptionDetails(Exception ex, bool includeEnvironmentInfo = true)
    {
        _logger.Error("===== Exception Report Start =====");
        _logger.Error($"Timestamp (UTC): {DateTime.UtcNow:O}");

        LogExceptionWithDepth(ex);

        if (includeEnvironmentInfo)
        {
            _logger.Error("=== System Information ===");
            _logger.Error($"Machine Name: {Environment.MachineName}");
            _logger.Error($"OS Version: {Environment.OSVersion}");
            _logger.Error($".NET Runtime: {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");
            _logger.Error($"Process Architecture: {System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture}");
            _logger.Error($"Current Directory: {Environment.CurrentDirectory}");
        }

        _logger.Error("===== Exception Report End =====");
    }

    /// <summary>
    /// Recursively logs exception details including message, stack trace, and inner exceptions
    /// with hierarchical indentation based on exception depth.
    /// </summary>
    /// <param name="ex">The exception to log.</param>
    /// <param name="className">Optional class name prefix for log entries.</param>
    /// <param name="depth">The current depth level in the exception hierarchy (used for indentation).</param>
    private static void LogExceptionWithDepth(Exception ex, string? className = null, int depth = 0)
    {
        while (true)
        {
            string indent = new string(' ', depth * 2);
            string prefix = className != null ? $"[{className}] " : "";

            _logger.Error($"{prefix}{indent}Exception Level: {depth}");
            _logger.Error($"{prefix}{indent}Type: {ex.GetType().FullName}");
            _logger.Error($"{prefix}{indent}Message: {ex.Message}");
            _logger.Error($"{prefix}{indent}Source: {ex.Source}");
            _logger.Error($"{prefix}{indent}HResult: {ex.HResult}");
            if (ex.HelpLink != null)
            {
                _logger.Error($"{prefix}{indent}Help Link: {ex.HelpLink}");
            }

            if (ex.Data.Count > 0)
            {
                _logger.Error($"{prefix}{indent}Data:");
                foreach (object? key in ex.Data.Keys)
                {
                    _logger.Error($"{prefix}{indent}  {key}: {ex.Data[key]}");
                }
            }

            if (!string.IsNullOrWhiteSpace(ex.StackTrace))
            {
                _logger.Error($"{prefix}{indent}StackTrace:");
                foreach (string line in ex.StackTrace.Split(Environment.NewLine))
                {
                    _logger.Error($"{prefix}{indent}  {line}");
                }
            }

            if (ex.TargetSite != null)
            {
                _logger.Error($"{prefix}{indent}TargetSite: {ex.TargetSite}");
            }

            if (ex.InnerException != null)
            {
                _logger.Error($"{prefix}{indent}--- Inner Exception ---");
                ex = ex.InnerException;
                depth = depth + 1;
                continue;
            }
            break;
        }
    }

    /// <summary>
    /// Flushes all pending log messages and shuts down the logging system.
    /// Should be called before application exit to ensure all logs are written.
    /// </summary>
    public static void Shutdown()
    {
        LogManager.Shutdown();
    }
}