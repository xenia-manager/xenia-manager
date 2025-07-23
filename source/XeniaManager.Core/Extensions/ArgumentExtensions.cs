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
            Game.Game? matchingGame = GameManager.Games.FirstOrDefault(game => string.Equals(game.Title, argument, StringComparison.OrdinalIgnoreCase));
            if (matchingGame != null)
            {
                return matchingGame;
            }
        }
        return null;
    }
}