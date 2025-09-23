using System.Diagnostics;
using File = System.IO.File;
using System.Runtime.InteropServices;
using System.Text;

// Imported Libraries
using Microsoft.Win32;
using SteamKit2;
using XeniaManager.Core.Constants;
using XeniaManager.Core.Constants.Emulators;
using XeniaManager.Core.Enum;

namespace XeniaManager.Core.Game;

public static class Shortcut
{
    // Variables
    private static string _steamPath { get; set; }
    
    // COM interfaces for shell link functionality
    [ComImport]
    [Guid("00021401-0000-0000-C000-000000000046")]
    private class ShellLink
    {
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("000214F9-0000-0000-C000-000000000046")]
    private interface IShellLink
    {
        void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, out IntPtr pfd, int fFlags);
        void GetIDList(out IntPtr ppidl);
        void SetIDList(IntPtr pidl);
        void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);
        void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
        void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);
        void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
        void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);
        void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
        void GetHotkey(out short pwHotkey);
        void SetHotkey(short wHotkey);
        void GetShowCmd(out int piShowCmd);
        void SetShowCmd(int iShowCmd);
        void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cchIconPath, out int piIcon);
        void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
        void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, int dwReserved);
        void Resolve(IntPtr hwnd, int fFlags);
        void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("0000010B-0000-0000-C000-000000000046")]
    private interface IPersistFile
    {
        void GetCurFile([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile);
        void IsDirty();
        void Load([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, int dwMode);
        void Save([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, [MarshalAs(UnmanagedType.Bool)] bool fRemember);
        void SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string pszFileName);
    }
    
    // Functions
    /// <summary>
    /// Creates a shortcut and puts it on the desktop for the certain game
    /// </summary>
    /// <param name="shortcutName">Name of the game</param>
    /// <param name="targetPath">Target towards the executable</param>
    /// <param name="workingDirectory">Working directory</param>
    /// <param name="gameTitle">Name of the game that we're launching</param>
    /// <param name="iconPath">Icon used for the shortcut</param>
    public static void DesktopShortcut(Game game)
    {
        CreateShortcut(game, DirectoryPaths.Desktop);
    }

    public static void CreateShortcut(Game game, string directory)
    {
        string shortcutPath = Path.Combine(directory, $"{game.Title}.lnk");
        string workingDirectory = string.Empty;
        Logger.Debug($"Creating shortcut for {game.Title} ({shortcutPath})");
        switch (game.XeniaVersion)
        {
            case XeniaVersion.Canary:
                workingDirectory = Path.Combine(DirectoryPaths.Base, XeniaCanary.EmulatorDir);
                break;
            case XeniaVersion.Mousehook:
                workingDirectory = Path.Combine(DirectoryPaths.Base, XeniaMousehook.EmulatorDir);
                break;
            case XeniaVersion.Netplay:
                workingDirectory = Path.Combine(DirectoryPaths.Base, XeniaNetplay.EmulatorDir);
                break;
            default:
                throw new NotImplementedException($"Xenia {game.XeniaVersion} is not implemented");
        }

        // Create the shortcut using Shell32 interfaces
        IShellLink link = (IShellLink)new ShellLink();
        link.SetPath(Path.Combine(Constants.DirectoryPaths.Base, "XeniaManager.exe"));
        link.SetArguments($@"""{game.Title}""");
        link.SetWorkingDirectory(workingDirectory);

        if (game.Artwork.Icon != null)
        {
            link.SetIconLocation(Path.Combine(Constants.DirectoryPaths.Base, game.Artwork.Icon), 0);
        }

        // Save the shortcut
        IPersistFile file = (IPersistFile)link;
        file.Save(shortcutPath, false);
    }

    /// <summary>
    /// Attempts to locate the Steam installation directory.
    /// </summary>
    /// <returns>
    /// The full path to the Steam folder (where steam.exe resides), or
    /// null if it cannot be found.
    /// </returns>
    public static string? FindSteamInstallPath()
    {
        // Check if we already found a Steam installation path
        if (_steamPath != null)
        {
            Logger.Info($"Steam installation path: {_steamPath}");
            return _steamPath;
        }
        
        // 1. Check the 64-bit registry view
        using var key64 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).OpenSubKey(@"SOFTWARE\Valve\Steam");
        string path = key64?.GetValue("InstallPath") as string;
        if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
        {
            Logger.Info($"Found Steam installation: {path}");
            _steamPath = path;
            return _steamPath;
        }

        // 2. Fallback to the 32-bit registry view
        using var key32 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey(@"SOFTWARE\Valve\Steam");
        path = key32?.GetValue("InstallPath") as string;
        if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
        {
            Logger.Info($"Found Steam installation: {path}");
            _steamPath = path;
            return _steamPath;
        }

        // 3. Default locations: Program Files (x86) then Program Files
        string? progFilesX86 = Environment.GetEnvironmentVariable("ProgramFiles(x86)");
        if (!string.IsNullOrEmpty(progFilesX86))
        {
            string defaultX86 = Path.Combine(progFilesX86, "Steam");
            if (Directory.Exists(defaultX86))
            {
                Logger.Info($"Found Steam installation: {defaultX86}");
                _steamPath = defaultX86;
                return _steamPath;
            }   
        }

        string? progFiles = Environment.GetEnvironmentVariable("ProgramFiles");
        if (!string.IsNullOrEmpty(progFiles))
        {
            string default64 = Path.Combine(progFiles, "Steam");
            if (Directory.Exists(default64))
            {
                Logger.Info($"Found Steam installation: {default64}");
                _steamPath = default64;
                return _steamPath;
            }
        }

        // 4. Not found
        Logger.Warning("Couldn't find Steam installation.");
        return null;
    }
    
    
    /// <summary>
    /// Terminates any running Steam processes and restarts Steam.
    /// </summary>
    private static void RestartSteam()
    {
        // Kill running Steam processes
        foreach (var process in Process.GetProcessesByName("steam"))
        {
            try
            {
                process.Kill();
                process.WaitForExit();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error terminating Steam process: {ex.Message}");
            }
        }

        // Relaunch Steam
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "steam://open/main",
                UseShellExecute = true
            });
            Logger.Info("Steam restarted successfully.");
        }
        catch (Exception ex)
        {
            Logger.Error($"Error starting Steam: {ex.Message}");
        }
    }

    public static void SteamShortcut(Game game)
    {
        string steamPath = FindSteamInstallPath();
        if (steamPath == null)
        {
            Logger.Error("Steam installation path not found");
            throw new Exception("Steam installation path not found");
        }

        string userDataDir = Path.Combine(steamPath, "userdata");
        if (!Directory.Exists(userDataDir))
        {
            Logger.Error("Couldn't find Steam user data directory");
            throw new Exception("Couldn't find Steam user data directory");
        }
        
        string[] userDirs = Directory.GetDirectories(userDataDir);
        Random r = new Random();
        foreach (string userDir in userDirs)
        {
            string configDir = Path.Combine(userDir, "config");
            if (!Directory.Exists(configDir))
            {
                Logger.Warning("Couldn't find Steam user config directory");
                Logger.Info("Creating user config directory");
                Directory.CreateDirectory(configDir);
            }
            
            string shortcutsFile = Path.Combine(configDir, "shortcuts.vdf");
            KeyValue root;
            if (File.Exists(shortcutsFile))
            {
                using var stream = File.OpenRead(shortcutsFile);
                root = new KeyValue();
                root.TryReadAsBinary(stream);
            }
            else
            {
                root = new KeyValue("shortcuts");
            }

            int newIndex = root.Children.Count;
            KeyValue newShortcut = new KeyValue(newIndex.ToString());

            newShortcut["appid"] = new KeyValue("appname", r.Next(1, int.MaxValue).ToString());
            newShortcut["appname"] = new KeyValue("appname", game.Title);
            newShortcut["Exe"] = new KeyValue("Exe", $"\"{Constants.FilePaths.ManagerExecutable}\"");
            newShortcut["StartDir"] = new KeyValue("StartDir", Constants.DirectoryPaths.Base);
            newShortcut["icon"] = new KeyValue("icon", Path.Combine(Constants.DirectoryPaths.Base, game.Artwork.Icon));
            newShortcut["ShortcutPath"] = new KeyValue("ShortcutPath", string.Empty);
            newShortcut["LaunchOptions"] = new KeyValue("LaunchOptions", $@"""{game.Title}""");
            newShortcut["IsHidden"] = new KeyValue("IsHidden", "0");
            newShortcut["AllowDesktopConfig"] = new KeyValue("AllowDesktopConfig", "1");
            newShortcut["AllowOverlay"] = new KeyValue("AllowOverlay", "1");
            newShortcut["OpenVR"] = new KeyValue("OpenVR", "0");
            newShortcut["Devkit"] = new KeyValue("Devkit", "0");
            newShortcut["DevkitGameID"] = new KeyValue("DevkitGameID", string.Empty);
            newShortcut["DevkitOverrideAppID"] = new KeyValue("DevkitOverrideAppID", string.Empty);
            newShortcut["LastPlayTime"] = new KeyValue("LastPlayTime", "0");
            newShortcut["FlatpakAppID"] = new KeyValue("FlatpakAppID", string.Empty);
            newShortcut["tags"] = new KeyValue("tags", string.Empty);
            
            root.Children.Add(newShortcut);
            Directory.CreateDirectory(configDir);
            root.SaveToFile(shortcutsFile, true);
            Logger.Info($"Added shortcut for user: {Path.GetFileName(userDir)} (AppID: {newShortcut["appid"]})");
        }
        RestartSteam();
    }
}