using XeniaManager.Core.Logging;
using XeniaManager.Core.Models.Files.Shortcut;
using XeniaManager.Core.Models.Game;
using XeniaManager.Core.Utilities.Paths;

namespace XeniaManager.Core.Manage;

/// <summary>
/// Manages the creation of Windows shortcuts for games in the Xenia Manager library.
/// Handles shortcut file creation with custom icons and launch arguments.
/// </summary>
public class ShortcutManager
{
    /// <summary>
    /// Creates a Windows desktop shortcut for the specified game.
    /// The shortcut launches Xenia Manager with the game title as an argument.
    /// </summary>
    /// <param name="game">The game information containing title, artwork, and other metadata.</param>
    /// <param name="directory">
    /// The directory where the shortcut will be created.
    /// If null or empty, defaults to the user's Desktop folder.
    /// </param>
    /// <exception cref="Exception">Thrown when the shortcut creation fails.</exception>
    public static void CreateShortcut(Game game, string? directory = null)
    {
        Logger.Trace<ShortcutManager>($"Starting CreateShortcut operation for game: '{game.Title}' ({game.GameId})");

        // Determine the target directory
        if (string.IsNullOrEmpty(directory))
        {
            directory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            Logger.Debug<ShortcutManager>($"No directory specified, using default Desktop folder: {directory}");
        }
        else
        {
            Logger.Debug<ShortcutManager>($"Using specified directory: {directory}");
        }

        // Build the shortcut path
        string shortcutPath = Path.Combine(directory, $"{game.Title}.lnk");
        Logger.Debug<ShortcutManager>($"Shortcut path will be: {shortcutPath}");

        // Get the working directory
        string workingDirectory = AppPathResolver.GetFullPath(Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
        Logger.Trace<ShortcutManager>($"Working directory set to: {workingDirectory}");

        // Create the shell link object
        Logger.Info<ShortcutManager>($"Creating Windows shortcut for game: '{game.Title}'");
        IShellLink link = (IShellLink)new ShellLink();

        // Set the target path (XeniaManager.exe)
        string targetPath = Environment.ProcessPath ?? AppPathResolver.GetFullPath("XeniaManager.exe");
        link.SetPath(targetPath);
        Logger.Debug<ShortcutManager>($"Shortcut target path set to: {targetPath}");

        // Set the launch arguments (game title)
        string arguments = $@"""{game.Title}""";
        link.SetArguments(arguments);
        Logger.Debug<ShortcutManager>($"Shortcut arguments set to: {arguments}");

        // Set the working directory
        link.SetWorkingDirectory(workingDirectory);
        Logger.Debug<ShortcutManager>($"Shortcut working directory set to: {workingDirectory}");

        // Set the icon location
        string iconLocation = AppPathResolver.GetFullPath(game.Artwork.Icon);
        link.SetIconLocation(iconLocation, 0);
        Logger.Debug<ShortcutManager>($"Shortcut icon location set to: {iconLocation}");

        // Save the shortcut file
        IPersistFile file = (IPersistFile)link;
        file.Save(shortcutPath, false);
        Logger.Info<ShortcutManager>($"Successfully created desktop shortcut at: {shortcutPath}");
        Logger.Trace<ShortcutManager>("CreateShortcut operation completed successfully");
    }
}