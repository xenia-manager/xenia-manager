using XeniaManager.Core.Constants;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Manage;
using XeniaManager.Core.Models.Game;

namespace XeniaManager.Core.Utilities;

/// <summary>
/// Provides utility methods for parsing and working with command-line arguments.
/// </summary>
public class ArgumentParser
{
    /// <summary>
    /// Converts an array of string arguments to a case-insensitive hash set for efficient lookups.
    /// </summary>
    /// <param name="args">The array of string arguments to convert, or null to create an empty set.</param>
    /// <returns>A hash set containing the arguments with case-insensitive string comparison.</returns>
    public static HashSet<string> ToArgumentSet(string[]? args)
    {
        Logger.Debug<ArgumentParser>($"Converting {(args?.Length ?? 0)} arguments to a hash set");

        HashSet<string> result = args == null
            ? new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(args, StringComparer.OrdinalIgnoreCase);

        Logger.Debug<ArgumentParser>($"Created hash set with {result.Count} unique arguments");
        return result;
    }

    /// <summary>
    /// Checks if a specific argument exists in the provided array of arguments.
    /// </summary>
    /// <param name="args">The array of string arguments to search in, or null to search in an empty set.</param>
    /// <param name="arg">The argument to search for (case-insensitive).</param>
    /// <returns>True if the argument exists in the array, false otherwise.</returns>
    public static bool Contains(string[]? args, string arg)
    {
        Logger.Debug<ArgumentParser>($"Checking if argument '{arg}' exists in {(args?.Length ?? 0)} provided arguments");

        bool result = ToArgumentSet(args).Contains(arg);

        Logger.Debug<ArgumentParser>($"Argument '{arg}' {(result ? "found" : "not found")} in provided arguments");
        return result;
    }

    /// <summary>
    /// Checks if the desktop arguments contain a specific game title and returns the matching game.
    /// This method handles game titles that may be wrapped in quotes.
    /// </summary>
    /// <param name="args">The array of string arguments from the desktop shortcut.</param>
    /// <returns>The matching Game if found in GameManager.Games, null otherwise.</returns>
    public static Game? GetGameFromArgs(string[]? args)
    {
        // Try explicit flag first
        (int flagIndex, string? flagValue) = TryGetFlagValue(args, ArgumentFlags.Game);
        if (!string.IsNullOrWhiteSpace(flagValue))
        {
            string trimmed = flagValue.Trim('"');
            Game? match = GameManager.Games.FirstOrDefault(g => g.Title.Equals(trimmed, StringComparison.OrdinalIgnoreCase));
            if (match is { FileLocations.IsGamePathValid: true })
            {
                Logger.Debug<ArgumentParser>("Found game via --game flag '{0}'", trimmed);
                return match;
            }
        }
        // Fallback to positional search (original logic)
        Logger.Debug<ArgumentParser>($"Checking if desktop arguments contain a game title");

        if (args == null || args.Length == 0)
        {
            Logger.Debug<ArgumentParser>("No arguments provided");
            return null;
        }

        // Check if any argument matches a game title (with or without quotes)
        foreach (string arg in args)
        {
            string trimmedArg = arg.Trim('"');

            // Search for a matching game in GameManager.Games
            Game? matchingGame = GameManager.Games.FirstOrDefault(g => g.Title.Equals(trimmedArg, StringComparison.OrdinalIgnoreCase));

            if (matchingGame == null)
            {
                continue;
            }

            if (!matchingGame.FileLocations.IsGamePathValid)
            {
                Logger.Warning<ArgumentParser>($"Game '{matchingGame.Title}' ({matchingGame.GameId}) has an invalid path, skipping");
                continue;
            }
            Logger.Debug<ArgumentParser>($"Found game '{matchingGame.Title}' ({matchingGame.GameId}) in desktop arguments");
            return matchingGame;
        }

        Logger.Debug<ArgumentParser>("No matching game found in desktop arguments");
        return null;
    }

    /// <summary>
    /// Checks if the desktop arguments contain a specific config overrides and returns a config string.
    /// This method handles xenia overrides that may be wrapped in quotes.
    /// </summary>
    /// <param name="args">The array of string arguments from the desktop shortcut.</param>
    /// <returns>The xenia config string if found, null otherwise.</returns>
    public static string? GetConfigOverridesFromArgs(string[]? args)
    {
        Logger.Debug<ArgumentParser>($"Checking if desktop arguments contain config overrides");

        if (args == null || args.Length == 0)
        {
            Logger.Debug<ArgumentParser>("No arguments provided");
            return null;
        }
        // Flag based overrides first
        (int xeniaFlagIndex, string? xeniaFlagValue) = TryGetFlagValue(args, ArgumentFlags.XeniaArgs);
        if (!string.IsNullOrWhiteSpace(xeniaFlagValue))
        {
            return xeniaFlagValue;
        }

        string configOverrideArgs = "";

        // Collect any remaining -- arguments (excluding known flags)
        foreach (string arg in args)
        {
            string trimmedArg = arg.Trim('"');
            if (trimmedArg.StartsWith("--") &&
                !ArgumentFlags.Game.Any(f => trimmedArg.Equals(f, StringComparison.OrdinalIgnoreCase)) &&
                !ArgumentFlags.XeniaArgs.Any(f => trimmedArg.Equals(f, StringComparison.OrdinalIgnoreCase)))
            {
                configOverrideArgs += trimmedArg + " ";
            }
        }

        if (string.IsNullOrEmpty(configOverrideArgs))
        {
            Logger.Debug<ArgumentParser>("No override arguments provided");
            return null;
        }
        Logger.Debug<ArgumentParser>($"Found overrides '{configOverrideArgs}' in desktop arguments");
        return configOverrideArgs;
    }

    /// <summary>
    /// Helper to retrieve flag value
    /// </summary>
    /// <param name="args">Launch Arguments</param>
    /// <param name="flags">Array of flag aliases</param>
    /// <returns>Index where the flag is and it's values</returns>
    private static (int Index, string? Value) TryGetFlagValue(string[]? args, string[] flags)
    {
        if (args == null || args.Length == 0)
        {
            return (-1, null);
        }

        for (int i = 0; i < args.Length; i++)
        {
            foreach (string flag in flags)
            {
                if (args[i].Equals(flag, StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 < args.Length)
                    {
                        return (i, args[i + 1].Trim('"'));
                    }
                    return (i, null);
                }
            }
        }
        return (-1, null);
    }
}