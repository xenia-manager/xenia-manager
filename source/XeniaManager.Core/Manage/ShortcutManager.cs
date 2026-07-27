using System.Diagnostics;
using Microsoft.Win32;
using SkiaSharp;
using XeniaManager.Core.Constants;
using XeniaManager.Core.Files;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Models;
using XeniaManager.Core.Models.Files.Shortcut;
using XeniaManager.Core.Models.Files.SteamShortcuts;
using XeniaManager.Core.Models.Files.Vdf;
using XeniaManager.Core.Models.Game;
using XeniaManager.Core.Utilities;

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

    /// <summary>
    /// Creates a Steam shortcut for the specified game using a specific Steam user ID.
    /// </summary>
    /// <param name="game">The game to create a Steam shortcut for.</param>
    /// <param name="steamUserId">The SteamID32 (or SteamID64) of the user to create the shortcut for.</param>
    /// <param name="restartSteam">Whether to restart Steam after adding the shortcut. Default is true.</param>
    /// <exception cref="Exception">Thrown when Steam is not found or shortcut creation fails.</exception>
    public static void CreateSteamShortcut(Game game, string steamUserId, bool restartSteam = true)
    {
        Logger.Trace<ShortcutManager>($"Starting CreateSteamShortcut operation for game: '{game.Title}' ({game.GameId}), user: {steamUserId}");

        // Find Steam installation
        string steamInstallPath = FindSteamInstallPath()
                                  ?? throw new Exception("Steam installation not found");
        Logger.Info<ShortcutManager>($"Steam installation found at: {steamInstallPath}");

        // Find the logged-in users file
        string loggedInUsersFilePath = Path.Combine(steamInstallPath, "config", "loginusers.vdf");
        if (!File.Exists(loggedInUsersFilePath))
        {
            throw new Exception("Steam loginusers.vdf file not found");
        }
        Logger.Debug<ShortcutManager>($"Steam loginusers file found: {loggedInUsersFilePath}");

        // Load loginusers
        Logger.Info<ShortcutManager>("Loading Steam loginusers.vdf file");
        VdfFile loggedUsersFile = VdfFile.Load(loggedInUsersFilePath);

        CreateSteamShortcutInternal(game, steamInstallPath, loggedUsersFile, steamUserId, restartSteam);
    }

    /// <summary>
    /// Shared logic for creating a Steam shortcut with a known user ID.
    /// </summary>
    private static void CreateSteamShortcutInternal(Game game, string steamInstallPath, VdfFile loggedUsersFile, string userId, bool restartSteam)
    {
        // Find the userdata directory (try 32-bit ID first, then 64-bit)
        string? userDataDirectory = FindSteamUserDataDirectory(steamInstallPath, loggedUsersFile, userId);
        if (userDataDirectory == null)
        {
            throw new Exception("Steam user data directory not found");
        }
        Logger.Debug<ShortcutManager>($"Steam userdata directory found: {userDataDirectory}");

        // Load or create a shortcuts file
        string shortcutsFilePath = Path.Combine(userDataDirectory, "config", "shortcuts.vdf");
        SteamShortcutsFile shortcutsFile = File.Exists(shortcutsFilePath)
            ? SteamShortcutsFile.Load(shortcutsFilePath)
            : SteamShortcutsFile.Create();

        // Check if a shortcut with this game title already exists
        if (shortcutsFile.Shortcuts.Any(s => s.AppName == game.Title))
        {
            Logger.Info<ShortcutManager>($"Steam shortcut already exists for game: '{game.Title}'");
            Logger.Trace<ShortcutManager>("CreateSteamShortcut operation completed - shortcut already exists");
            return;
        }

        // Create and add the game shortcut
        (SteamShortcut gameShortcut, uint appId) = CreateSteamShortcutFromGame(game);
        shortcutsFile.AddShortcut(gameShortcut);
        shortcutsFile.Save(shortcutsFilePath);

        // Copy artwork to the Steam grid folder
        string gridFolder = Path.Combine(userDataDirectory, "config", "grid");
        CopyGameArtworkToGridFolder(game, gridFolder, appId);

        Logger.Info<ShortcutManager>($"Successfully created Steam shortcut for: '{game.Title}'");

        if (restartSteam)
        {
            RestartSteam();
        }

        Logger.Trace<ShortcutManager>("CreateSteamShortcut operation completed successfully");
    }

    /// <summary>
    /// Retrieves all Steam user accounts from the loginusers.vdf file.
    /// </summary>
    /// <returns>A list of SteamUser objects representing all accounts found.</returns>
    /// <exception cref="Exception">Thrown when Steam is not found or loginusers.vdf cannot be loaded.</exception>
    public static List<SteamUser> GetAllSteamUsers()
    {
        Logger.Trace<ShortcutManager>("Starting GetAllSteamUsers");

        // Find Steam installation
        string steamInstallPath = FindSteamInstallPath()
                                  ?? throw new Exception("Steam installation not found");
        Logger.Info<ShortcutManager>($"Steam installation found at: {steamInstallPath}");

        // Find the logged-in users file
        string loggedInUsersFilePath = Path.Combine(steamInstallPath, "config", "loginusers.vdf");
        if (!File.Exists(loggedInUsersFilePath))
        {
            throw new Exception("Steam loginusers.vdf file not found");
        }

        // Load loginusers
        VdfFile loggedUsersFile = VdfFile.Load(loggedInUsersFilePath);

        List<SteamUser> users = ParseSteamUsers(loggedUsersFile);
        Logger.Info<ShortcutManager>($"Found {users.Count} Steam user(s)");
        return users;
    }

    /// <summary>
    /// Parses Steam user accounts from a loaded VDF file.
    /// </summary>
    /// <param name="loginUsersFile">The loaded loginusers.vdf file.</param>
    /// <returns>A list of SteamUser objects.</returns>
    private static List<SteamUser> ParseSteamUsers(VdfFile loginUsersFile)
    {
        if (loginUsersFile.Root == null)
        {
            return [];
        }

        List<SteamUser> users = [];
        foreach (VdfNode userNode in loginUsersFile.Root.Children)
        {
            string steamId64 = userNode.Key;
            string? steamId32 = SteamId64To32(steamId64);
            string personaName = userNode.GetValue("PersonaName") ?? "Unknown";
            string? accountName = userNode.GetValue("AccountName");

            users.Add(new SteamUser
            {
                SteamId64 = steamId64,
                SteamId32 = steamId32,
                PersonaName = personaName,
                AccountName = accountName
            });
        }

        return users;
    }

    /// <summary>
    /// Finds the Steam userdata directory for the most recent user.
    /// Tries SteamID32 first, then falls back to SteamID64 if needed.
    /// </summary>
    /// <param name="steamInstallPath">The Steam installation path.</param>
    /// <param name="loggedUsersFile">The loaded loginusers.vdf file.</param>
    /// <param name="userId32">The SteamID32 of the user.</param>
    /// <returns>The userdata directory path if found; otherwise, null.</returns>
    private static string? FindSteamUserDataDirectory(string steamInstallPath, VdfFile loggedUsersFile, string userId32)
    {
        // Try 32-bit ID first
        string userDataDirectory32 = Path.Combine(steamInstallPath, "userdata", userId32);
        if (Directory.Exists(userDataDirectory32))
        {
            Logger.Debug<ShortcutManager>($"Steam userdata directory found (32-bit ID): {userDataDirectory32}");
            return userDataDirectory32;
        }

        // Fallback to 64-bit ID
        Logger.Debug<ShortcutManager>($"32-bit ID directory not found, searching for 64-bit ID directory");
        string? userId64 = FindSteamUserId64(loggedUsersFile, userId32);
        if (userId64 != null)
        {
            string userDataDirectory64 = Path.Combine(steamInstallPath, "userdata", userId64);
            if (Directory.Exists(userDataDirectory64))
            {
                Logger.Debug<ShortcutManager>($"Steam userdata directory found (64-bit ID): {userDataDirectory64}");
                return userDataDirectory64;
            }
        }

        return null;
    }

    /// <summary>
    /// Creates a SteamShortcut object from a Game object.
    /// Configures the executable path, working directory, launch arguments, and icon.
    /// </summary>
    /// <param name="game">The game to create a Steam shortcut for.</param>
    /// <returns>A tuple containing the configured SteamShortcut and its AppId.</returns>
    private static (SteamShortcut shortcut, uint appId) CreateSteamShortcutFromGame(Game game)
    {
        Logger.Trace<ShortcutManager>($"Creating SteamShortcut from game: '{game.Title}'");

        string exePath = AppPaths.ManagerExecutable;
        string startDir = AppPathResolver.BaseDirectory();
        string launchOptions = $@"""{game.Title}""";

        Logger.Debug<ShortcutManager>($"Executable: {exePath}");
        Logger.Debug<ShortcutManager>($"Start directory: {startDir}");
        Logger.Debug<ShortcutManager>($"Launch options: {launchOptions}");

        SteamShortcut shortcut = new SteamShortcut
        {
            AppName = game.Title,
            Exe = exePath,
            StartDir = startDir,
            LaunchOptions = launchOptions,
            AllowOverlay = true,
            AllowDesktopConfig = true,
            IsHidden = false,
            OpenVR = false
        };

        // Set icon if available
        string? iconPath = GetGameIconPath(game);
        if (iconPath != null)
        {
            shortcut.Icon = iconPath;
            Logger.Debug<ShortcutManager>($"Shortcut icon: {iconPath}");
        }

        // Compute and set AppId
        uint appId = shortcut.ComputeAppId();
        shortcut.SetAppIdFromUint(appId);
        Logger.Debug<ShortcutManager>($"Computed AppId: {appId}");

        return (shortcut, appId);
    }

    /// <summary>
    /// Gets the full path to the game's icon file if it exists.
    /// </summary>
    /// <param name="game">The game to get the icon for.</param>
    /// <returns>The full icon path if it exists; otherwise, null.</returns>
    private static string? GetGameIconPath(Game game)
    {
        if (string.IsNullOrEmpty(game.Artwork.Icon))
        {
            return null;
        }

        string iconPath = AppPathResolver.GetFullPath(game.Artwork.Icon);
        return File.Exists(iconPath) ? iconPath : null;
    }

    /// <summary>
    /// Copies game artwork to the Steam grid folder for display in Steam Library.
    /// Copies boxart as cover ("appid"p.png) and background as the hero ("appid"_hero.png).
    /// </summary>
    /// <param name="game">The game to copy artwork for.</param>
    /// <param name="gridFolder">The Steam grid folder path.</param>
    /// <param name="appId">The Steam AppId for the shortcut.</param>
    private static void CopyGameArtworkToGridFolder(Game game, string gridFolder, uint appId)
    {
        Logger.Trace<ShortcutManager>($"Copying game artwork to grid folder: {gridFolder}");

        try
        {
            // Ensure grid folder exists
            if (!Directory.Exists(gridFolder))
            {
                Directory.CreateDirectory(gridFolder);
                Logger.Debug<ShortcutManager>($"Created grid folder: {gridFolder}");
            }

            // Copy boxart as cover (<appid>p.png)
            if (!string.IsNullOrEmpty(game.Artwork.Boxart))
            {
                string boxartPath = AppPathResolver.GetFullPath(game.Artwork.Boxart);
                if (File.Exists(boxartPath))
                {
                    string coverPath = Path.Combine(gridFolder, $"{appId}p.png");
                    ArtworkManager.ResizeArtwork(boxartPath, coverPath, 600, 900, ArtworkManager.ResizeMode.Stretch, SKEncodedImageFormat.Png);
                    Logger.Info<ShortcutManager>($"Copied boxart to Steam grid cover: {coverPath}");
                }
                else
                {
                    Logger.Debug<ShortcutManager>($"Boxart file not found: {boxartPath}");
                }
            }

            // Copy background as hero ("appid"0_hero.png)
            if (!string.IsNullOrEmpty(game.Artwork.Background))
            {
                string backgroundPath = AppPathResolver.GetFullPath(game.Artwork.Background);
                if (File.Exists(backgroundPath))
                {
                    string heroPath = Path.Combine(gridFolder, $"{appId}_hero.png");
                    ArtworkManager.ResizeArtwork(backgroundPath, heroPath, 3840, 1240, ArtworkManager.ResizeMode.Fill, SKEncodedImageFormat.Png);
                    Logger.Info<ShortcutManager>($"Copied background to Steam grid hero: {heroPath}");
                }
                else
                {
                    Logger.Debug<ShortcutManager>($"Background file not found: {backgroundPath}");
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Warning<ShortcutManager>($"Failed to copy game artwork to grid folder: {ex.Message}");
        }
    }

    /// <summary>
    /// Searches for the Steam installation path on the system.
    /// Checks registry keys and default installation directories in order of preference.
    /// </summary>
    /// <returns>The Steam installation path if found; otherwise, null.</returns>
    private static string? FindSteamInstallPath()
    {
        Logger.Trace<ShortcutManager>("Starting FindSteamInstallPath search");

        if (OperatingSystem.IsWindows())
        {
            // 1. Check the 64-bit registry view
            Logger.Debug<ShortcutManager>("Checking 64-bit registry for Steam installation");
            using RegistryKey? key64 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).OpenSubKey(@"SOFTWARE\Valve\Steam");
            string? path = key64?.GetValue("InstallPath") as string;
            if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
            {
                Logger.Info<ShortcutManager>($"Found Steam installation: {path}");
                return path;
            }
            Logger.Debug<ShortcutManager>("Steam not found in 64-bit registry");

            // 2. Fallback to the 32-bit registry view
            Logger.Debug<ShortcutManager>("Checking 32-bit registry for Steam installation");
            using RegistryKey? key32 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey(@"SOFTWARE\Valve\Steam");
            path = key32?.GetValue("InstallPath") as string;
            if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
            {
                Logger.Info<ShortcutManager>($"Found Steam installation: {path}");
                return path;
            }
            Logger.Debug<ShortcutManager>("Steam not found in 32-bit registry");

            // 3. Default locations: Program Files (x86)
            Logger.Debug<ShortcutManager>("Checking default Program Files (x86) location");
            string? progFilesX86 = Environment.GetEnvironmentVariable("ProgramFiles(x86)");
            if (!string.IsNullOrEmpty(progFilesX86))
            {
                string defaultX86 = Path.Combine(progFilesX86, "Steam");
                if (Directory.Exists(defaultX86))
                {
                    Logger.Info<ShortcutManager>($"Found Steam installation: {defaultX86}");
                    return defaultX86;
                }
            }
            Logger.Debug<ShortcutManager>("Steam not found in Program Files (x86)");

            // 4. Default locations: Program Files
            Logger.Debug<ShortcutManager>("Checking default Program Files location");
            string? progFiles = Environment.GetEnvironmentVariable("ProgramFiles");
            if (!string.IsNullOrEmpty(progFiles))
            {
                string default64 = Path.Combine(progFiles, "Steam");
                if (Directory.Exists(default64))
                {
                    Logger.Info<ShortcutManager>($"Found Steam installation: {default64}");
                    return default64;
                }
            }
            Logger.Debug<ShortcutManager>("Steam not found in Program Files");
        }
        else
        {
            Logger.Debug<ShortcutManager>("Skipping Windows-specific search on non-Windows platform");
        }

        // TODO: Linux support?
        Logger.Trace<ShortcutManager>("FindSteamInstallPath search completed - no installation found");
        return null;
    }

    /// <summary>
    /// Finds the SteamID64 for a given SteamID32 by searching the loginusers.vdf file.
    /// </summary>
    /// <param name="loginUsersFile">The loaded loginusers.vdf file.</param>
    /// <param name="steamId32">The SteamID32 to search for.</param>
    /// <returns>The SteamID64 if found; otherwise, null.</returns>
    private static string? FindSteamUserId64(VdfFile loginUsersFile, string steamId32)
    {
        Logger.Trace<ShortcutManager>($"Finding SteamID64 for SteamID32: {steamId32}");

        if (loginUsersFile.Root == null)
        {
            return null;
        }

        foreach (VdfNode userNode in loginUsersFile.Root.Children)
        {
            string? convertedId32 = SteamId64To32(userNode.Key);
            if (convertedId32 == steamId32)
            {
                Logger.Debug<ShortcutManager>($"Found SteamID64: {userNode.Key} for SteamID32: {steamId32}");
                return userNode.Key;
            }
        }

        Logger.Debug<ShortcutManager>($"No SteamID64 found for SteamID32: {steamId32}");
        return null;
    }

    /// <summary>
    /// Converts a SteamID64 to SteamID32 format.
    /// SteamID64 base: 76561197960265728
    /// SteamID32 = SteamID64 - base
    /// </summary>
    /// <param name="steamId64">The SteamID64 to convert.</param>
    /// <returns>The SteamID32 as a string, or null if conversion fails.</returns>
    private static string? SteamId64To32(string steamId64)
    {
        if (!ulong.TryParse(steamId64, out ulong id))
        {
            Logger.Debug<ShortcutManager>($"Failed to parse SteamID64: {steamId64}");
            return null;
        }

        const ulong SteamId64Base = 76561197960265728UL;

        if (id < SteamId64Base)
        {
            Logger.Debug<ShortcutManager>($"SteamID64 {id} is less than base {SteamId64Base}, treating as SteamID32");
            return id.ToString();
        }

        uint id32 = (uint)(id - SteamId64Base);
        return id32.ToString();
    }

    /// <summary>
    /// Restarts the Steam client by killing all running Steam processes and relaunching Steam.
    /// Used after adding shortcuts to ensure Steam recognizes the new shortcuts.
    /// </summary>
    public static void RestartSteam()
    {
        Logger.Trace<ShortcutManager>("Starting Steam restart operation");

        // Kill running Steam processes
        Process[] steamProcesses = Process.GetProcessesByName("steam");
        Logger.Debug<ShortcutManager>($"Found {steamProcesses.Length} running Steam process(es)");

        foreach (Process process in steamProcesses)
        {
            try
            {
                Logger.Debug<ShortcutManager>($"Killing Steam process (PID: {process.Id})");
                process.Kill();
                process.WaitForExit();
                Logger.Debug<ShortcutManager>($"Steam process (PID: {process.Id}) terminated successfully");
            }
            catch (Exception ex)
            {
                Logger.Error<ShortcutManager>($"Failed to kill Steam process (PID: {process.Id}): {ex.Message}");
            }
        }

        if (steamProcesses.Length > 0)
        {
            Logger.Info<ShortcutManager>("Steam was running, restart Steam to apply changes");
            // Relaunch Steam
            try
            {
                Logger.Info<ShortcutManager>("Relaunching Steam client");
                Process.Start(new ProcessStartInfo
                {
                    FileName = "steam://open/main",
                    UseShellExecute = true
                });
                Logger.Info<ShortcutManager>("Steam client relaunch initiated successfully");
            }
            catch (Exception ex)
            {
                Logger.Error<ShortcutManager>($"Failed to relaunch Steam: {ex.Message}");
            }
        }
        else
        {
            Logger.Info<ShortcutManager>("Steam was not running, no need to restart");
        }

        Logger.Trace<ShortcutManager>("Steam restart operation completed");
    }
}