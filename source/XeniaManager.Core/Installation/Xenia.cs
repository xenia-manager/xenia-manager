using System.Collections.Specialized;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Windows;

// Imported
using Microsoft.Win32;
using Octokit;
using XeniaManager.Core.Downloader;
using XeniaManager.Core.Game;
using XeniaManager.Core.Settings;

namespace XeniaManager.Core.Installation;

/// <summary>
/// Manages setup, installation and removal of Xenia Emulator
/// </summary>
public static class Xenia
{
    // Functions
    /// <summary>
    /// Function that sets up the registry key and removes the popup on the first launch of Xenia
    /// </summary>
    private static void RegistrySetup()
    {
        const string registryPath = @"Software\Xenia";
        const string valueName = "XEFLAGS";
        const long valueData = 1;

        using var key = Registry.CurrentUser.CreateSubKey(registryPath);

        if (key.GetValue(valueName) is null)
        {
            key.SetValue(valueName, valueData, RegistryValueKind.QWord);
            Logger.Info("XEFLAGS registry value created successfully.");
        }
        else
        {
            Logger.Warning("XEFLAGS registry value already exists.");
        }
    }

    /// <summary>
    /// Puts the emulator in the portable mode and creates necessary directories
    /// </summary>
    /// <param name="emulatorLocation">Location of the emulator</param>
    private static void SetupEmulatorDirectory(string emulatorLocation)
    {
        // Add portable.txt so the Xenia Emulator is in portable mode
        if (!File.Exists(Path.Combine(emulatorLocation, "portable.txt")))
        {
            File.Create(Path.Combine(emulatorLocation, "portable.txt"));
        }

        // Add "config" directory for storing game specific configuration files
        Directory.CreateDirectory(Path.Combine(emulatorLocation, "config"));

        // Add "patches" directory for storing game specific configuration files
        Directory.CreateDirectory(Path.Combine(emulatorLocation, "patches"));
    }

    private static void SetupContentFolder(string emulatorContentFolder)
    {
        Logger.Info("Creating a symbolic link for the content folder to the unified content folder");
        Directory.CreateDirectory(Constants.DirectoryPaths.EmulatorContent);
        Logger.Debug($"Unified Content folder: {Constants.DirectoryPaths.EmulatorContent}");
        Logger.Debug($"Emulator Content Folder: {emulatorContentFolder}");
        if (Directory.Exists(emulatorContentFolder))
        {
            Directory.Delete(emulatorContentFolder, true);
        }
        Directory.CreateSymbolicLink(emulatorContentFolder, Constants.DirectoryPaths.EmulatorContent);

        DirectoryInfo linkInfo = new DirectoryInfo(emulatorContentFolder);
        if ((linkInfo.Attributes & FileAttributes.ReparsePoint) != 0)
        {
            Logger.Info("Verified: Symbolic link created successfully.");
        }
        else
        {
            Logger.Error("Failed to verify symbolic link.");
            throw new Exception("Symbolic Link creation process failed.");
        }
    }

    /// <summary>
    /// Generates configuration file and profile if needed
    /// </summary>
    /// <param name="executableLocation">Location to the Xenia executable</param>
    /// <param name="configLocation">Location to the Xenia configuration file</param>
    /// <param name="generateProfile">true if we want to also generate a Profile</param>
    public static void GenerateConfigFile(string executableLocation, string configLocation, bool generateProfile = false)
    {
        Logger.Info("Generating config file by launching the emulator.");
        // Setup process
        Process xenia = new Process();
        xenia.StartInfo.FileName = executableLocation;
        xenia.StartInfo.WorkingDirectory = Path.GetDirectoryName(xenia.StartInfo.FileName);
        if (!xenia.Start())
        {
            throw new Exception("Failed to start emulator.");
        }

        Logger.Info("Xenia launched successfully.");
        Logger.Info("Waiting for configuration file to be generated");
        while (!File.Exists(configLocation) || new FileInfo(configLocation).Length < 20 * 1024)
        {
            Task.Delay(100);
        }

        Logger.Info("Waiting for emulator to close");
        if (generateProfile)
        {
            // Wait for user to manually close Xenia
            xenia.WaitForExit();
        }
        else
        {
            // Close Xenia automatically
            if (xenia.CloseMainWindow())
            {
                if (!xenia.WaitForExit(5000))
                {
                    xenia.Kill();
                }
            }
            else
            {
                xenia.Kill();
            }
        }
    }

    /// <summary>
    /// Sets up Xenia Canary
    /// </summary>
    /// <param name="canaryInfo">Xenia Managers emulator section of the configuration file</param>
    /// <param name="releaseVersion">Current version of installed Xenia Canary</param>
    /// <param name="releaseDate">Release date of currently installed Xenia Canary</param>
    public static EmulatorInfo CanarySetup(string releaseVersion, DateTime releaseDate, bool unifiedContentFolder = false)
    {
        // Setup registry to remove the popup on the first launch
        RegistrySetup();

        Logger.Info("Creating a configuration file for usage of Xenia Canary");
        EmulatorInfo canaryInfo = new EmulatorInfo()
        {
            EmulatorLocation = Constants.Xenia.Canary.EmulatorDir,
            ExecutableLocation = Constants.Xenia.Canary.ExecutableLocation,
            ConfigLocation = Constants.Xenia.Canary.DefaultConfigLocation,
            Version = releaseVersion,
            ReleaseDate = releaseDate,
        };

        // Setup emulator directory
        SetupEmulatorDirectory(Path.Combine(Constants.DirectoryPaths.Base, canaryInfo.EmulatorLocation));

        if (unifiedContentFolder)
        {
            SetupContentFolder(Path.Combine(Constants.DirectoryPaths.Base, Constants.Xenia.Canary.EmulatorDir, "content"));
        }

        // Generate configuration file and profile
        GenerateConfigFile(Path.Combine(Constants.DirectoryPaths.Base, canaryInfo.ExecutableLocation),
            Path.Combine(Constants.DirectoryPaths.Base, canaryInfo.ConfigLocation), true);

        // Move the configuration file to config directory
        if (!File.Exists(Path.Combine(Constants.DirectoryPaths.Base, canaryInfo.ConfigLocation)))
        {
            throw new Exception("Could not find Xenia Canary config file.");
        }

        Logger.Info("Moving the configuration file to config folder");
        File.Move(Path.Combine(Constants.DirectoryPaths.Base, canaryInfo.ConfigLocation),
            Path.Combine(Constants.DirectoryPaths.Base, Constants.Xenia.Canary.ConfigLocation));

        // Updating the path since the default configuration file is stored inside the config directory
        canaryInfo.ConfigLocation = Constants.Xenia.Canary.ConfigLocation;
        ConfigManager.ChangeConfigurationFile(Path.Combine(Constants.DirectoryPaths.Base, canaryInfo.ConfigLocation), XeniaVersion.Canary);

        // Return info about the Xenia Canary
        return canaryInfo;
    }

    public static async Task<bool> UpdateCanary(EmulatorInfo canaryInfo, IProgress<double>? downloadProgress = null)
    {
        Release latestRelease = await Github.GetLatestRelease(XeniaVersion.Canary);
        ReleaseAsset asset = latestRelease.Assets.FirstOrDefault(a => a.Name.Contains("windows", StringComparison.OrdinalIgnoreCase));

        if (asset == null)
        {
            throw new Exception("Windows build asset missing in the release");
        }

        Logger.Info("Downloading the latest Xenia Canary build");
        DownloadManager downloadManager = new DownloadManager();
        if (downloadProgress != null)
        {
            downloadManager.ProgressChanged += (progress) => { downloadProgress.Report(progress); };
        }

        await downloadManager.DownloadAndExtractAsync(asset.BrowserDownloadUrl, "xenia.zip", Path.Combine(Constants.DirectoryPaths.Base, Constants.Xenia.Canary.EmulatorDir));

        // Parse version
        string? version = latestRelease.TagName;
        if (string.IsNullOrEmpty(version) || version.Length != 7)
        {
            string releaseTitle = latestRelease.Name;
            if (!string.IsNullOrEmpty(releaseTitle))
            {
                if (releaseTitle.Contains('_'))
                {
                    version = releaseTitle.Substring(0, releaseTitle.IndexOf('_'));
                }
                else if (releaseTitle.Length == 7)
                {
                    version = releaseTitle;
                }
            }
        }

        // Update settings
        canaryInfo.Version = version;
        canaryInfo.ReleaseDate = latestRelease.CreatedAt.UtcDateTime;
        canaryInfo.LastUpdateCheckDate = DateTime.Now;
        canaryInfo.UpdateAvailable = false;
        return true;
    }

    /// <summary>
    /// Removes the selected Xenia version from the system
    /// </summary>
    /// <param name="emulatorInfo">Xenia version that will be uninstalled</param>
    /// <param name="xeniaVersion">Xenia Version</param>
    /// <exception cref="NotImplementedException">Missing implementation for other versions of Xenia</exception>
    public static EmulatorInfo Uninstall(XeniaVersion xeniaVersion)
    {
        string emulatorLocation = xeniaVersion switch
        {
            XeniaVersion.Canary => Constants.Xenia.Canary.EmulatorDir,
            _ => throw new NotImplementedException($"Xenia {xeniaVersion} is not implemented.")
        };

        // Delete Xenia folder
        Logger.Info($"Deleting Xenia {xeniaVersion} folder: {emulatorLocation}");
        if (Directory.Exists(Path.Combine(Constants.DirectoryPaths.Base, emulatorLocation)))
        {
            Directory.Delete(Path.Combine(Constants.DirectoryPaths.Base, emulatorLocation), true);
        }

        // Remove all games using the selected Xenia Version
        foreach (var game in GameManager.Games.ToList())
        {
            if (game.XeniaVersion == xeniaVersion)
            {
                GameManager.RemoveGame(game);
            }
        }

        // Remove the emulator from the settings
        return null;
    }

    /// <summary>
    /// Checks for Xenia Emulator updates
    /// </summary>
    /// <param name="emulatorInfo">Info about the Xenia version we're checking updates for</param>
    /// <param name="xeniaVersion">Xenia version that is being checked for updates</param>
    /// <returns>True and latest release if there is a newer release; otherwise false and null</returns>
    public static async Task<(bool updateAvailable, Release latestRelease)> CheckForUpdates(EmulatorInfo emulatorInfo, XeniaVersion xeniaVersion)
    {
        // Grab latest release
        Release release = await Github.GetLatestRelease(XeniaVersion.Canary);

        // Compare currently installed version and the latest one
        switch (xeniaVersion)
        {
            case XeniaVersion.Canary:
                string? latestVersion = release.TagName;
                if (string.IsNullOrEmpty(latestVersion))
                {
                    Logger.Warning("Couldn't find the version for the latest release of Xenia Canary");
                    return (false, null);
                }

                // Checking if we got proper version number
                if (latestVersion.Length != 7)
                {
                    // Parsing version number from title
                    string releaseTitle = release.Name;
                    if (!string.IsNullOrEmpty(releaseTitle))
                    {
                        // Checking if the title has an underscore
                        if (releaseTitle.Contains('_'))
                        {
                            // Everything before the underscore is version number
                            latestVersion = releaseTitle.Substring(0, releaseTitle.IndexOf('_'));
                        }
                        else if (releaseTitle.Length == 7)
                        {
                            latestVersion = releaseTitle;
                        }
                    }
                }

                Logger.Info($"Latest version of Xenia Canary: {latestVersion}");

                // Comparing 2 versions
                if (!string.Equals(latestVersion, emulatorInfo.Version, StringComparison.OrdinalIgnoreCase))
                {
                    Logger.Info("Xenia Canary has a new update");
                    emulatorInfo.LastUpdateCheckDate = DateTime.Now; // Update the update check
                    emulatorInfo.UpdateAvailable = true;
                    return (true, release);
                }
                else
                {
                    Logger.Info("No updates available");
                }

                break;
            // TODO: Implement Mousehook/Netplay check for updates
            case XeniaVersion.Mousehook:
                break;
            case XeniaVersion.Netplay:
                break;
        }

        emulatorInfo.LastUpdateCheckDate = DateTime.Now; // Update the update check
        emulatorInfo.UpdateAvailable = false;
        return (false, null);
    }

    public static void ExportLogs(XeniaVersion xeniaVersion)
    {
        Logger.Info($"Exporting Xenia {xeniaVersion} logs to desktop");
        string logLocation = xeniaVersion switch
        {
            XeniaVersion.Canary => Path.Combine(Constants.DirectoryPaths.Base, Constants.Xenia.Canary.EmulatorDir, "xenia.log"),
            _ => throw new NotImplementedException($"Xenia {xeniaVersion} is not implemented.")
        };
        string destination = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"xenia_{xeniaVersion.ToString().ToLower()}.log");
        Logger.Debug(logLocation);
        Logger.Debug(destination);
        if (!File.Exists(logLocation))
        {
            Logger.Error("Could not find Xenia log file");
            throw new IOException("Xenia Log file not found");
        }
        File.Copy(logLocation, destination, true);
        Logger.Info($"Xenia {xeniaVersion} log exported to desktop");
    }
}