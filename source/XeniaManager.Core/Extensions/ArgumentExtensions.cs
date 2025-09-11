// Imported Libraries
using Serilog;
using XeniaManager.Core.Game;

namespace XeniaManager.Core.Extensions;

/// <summary>
/// Extension method to launch arguments
/// </summary>
public static class ArgumentExtensions
{
    /// <summary>
    /// Checks for launch arguments to show console
    /// </summary>
    /// <param name="args">Launch Arguments</param>
    /// <returns>true if console argument is present, otherwise false</returns>
    public static bool HasConsoleArgument(this string[] args)
    {
        return args != null &&
               (args.Contains("-console", StringComparer.OrdinalIgnoreCase) ||
                args.Contains("--console", StringComparer.OrdinalIgnoreCase));
    }

    private static bool IsConsoleArgument(string argument)
    {
        return argument.Equals("-console", StringComparison.OrdinalIgnoreCase) ||
               argument.Equals("--console", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks for game launch arguments and launches the game accordingly
    /// </summary>
    /// <param name="args">Launch Arguments</param>
    public static Game.Game? CheckLaunchArguments(this string[] args)
    {
        // Guard: no args or no games
        if (args == null || args.Length < 1 || GameManager.Games.Count == 0)
        {
            return null;
        }

        // Check arguments for games
        foreach (string argument in args)
        {
            if (IsConsoleArgument(argument))
            {
                continue;
            }

            Log.Information($"Current Argument: {argument}");

            Game.Game? matchingGame;

            if (File.Exists(argument))
            {
                Log.Information($"Argument is a filename and file exists. Attempting game match via game filename");
                matchingGame = GameManager.Games.FirstOrDefault(game => string.Equals(game.FileLocations.Game, argument, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                Log.Information($"Argument is either not a filename or the file is not available. Attempting game match via game title");
                matchingGame = GameManager.Games.FirstOrDefault(game => string.Equals(game.Title, argument, StringComparison.OrdinalIgnoreCase));
            }

            if (matchingGame != null)
            {
                Log.Information($"Game match found: {matchingGame.Title}");
                return matchingGame;
            }
        }
        Log.Information($"No match found. Showing xenia-manager library.");

        return null;
    }
}