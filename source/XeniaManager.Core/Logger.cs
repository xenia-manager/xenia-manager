﻿using System.Runtime.InteropServices;

// Imported Libraries
using Serilog;
using Serilog.Core;
using Serilog.Events;
using XeniaManager.Core.Utilities;

namespace XeniaManager.Core;

/// <summary>
/// Customized Serilog Logger
/// </summary>
public static class Logger
{
    // Variables
    private static ILogger? _logger { get; set; }

    private static LoggingLevelSwitch _levelSwitch = new(LogEventLevel.Verbose);

    private static bool _consoleVisible = false;

    // Functions
    /// <summary>
    /// Initializes logger
    /// </summary>
    public static void Initialize(bool showConsole = false)
    {
        // Ensure log directory exists
        Directory.CreateDirectory(Constants.DirectoryPaths.Logs);

        // Initialize Serilog with the configuration
        _logger = new LoggerConfiguration()
            // Set minimum log level
            .MinimumLevel.ControlledBy(_levelSwitch)
            .WriteTo.Console()
            .WriteTo.File(
            path: Path.Combine(Constants.DirectoryPaths.Logs, "Log-.txt"), // Path where the logfile gets saved
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 7
            )
            .CreateLogger();

        Log.Logger = _logger;

        if (showConsole)
        {
            ConsoleHelper.ShowConsole();
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
#if DEBUG
        _logger?.Debug(message, propertyValues);
#endif
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
    /// Changes the level of debugging
    /// </summary>
    /// <param name="level"></param>
    public static void SetMinimumLevel(LogEventLevel level)
    {
        _levelSwitch.MinimumLevel = level;
    }

    /// <summary>
    /// Closes and flushes the logger
    /// </summary>
    public static void Shutdown()
    {
        Log.CloseAndFlush();
        ConsoleHelper.HideConsole();
    }
}