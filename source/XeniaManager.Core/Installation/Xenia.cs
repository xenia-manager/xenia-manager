using System.Diagnostics;

// Imported
using Microsoft.Win32;
using XeniaManager.Core.Game;
using XeniaManager.Core.Settings;

namespace XeniaManager.Core.Installation;

public static class Xenia
{
    // Variables
    
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
    /// <param name="emulatorLocation"></param>
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

    /// <summary>
    /// Generates configuration file and profile if needed
    /// </summary>
    /// <param name="executableLocation">Location to the Xenia executable</param>
    /// <param name="configLocation">Location to the Xenia configuration file</param>
    /// <param name="generateProfile">true if we want to also generate a Profile</param>
    private static void GenerateConfigFile(string executableLocation, string configLocation, bool generateProfile = false)
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
    
    public static void CanarySetup(EmulatorSettings emulatorSettings, string releaseVersion, DateTime releaseDate)
    {
        // Setup registry to remove the popup on the first launch
        RegistrySetup();
        
        Logger.Info("Creating a configuration file for usage of Xenia Canary");
        emulatorSettings.Canary = new EmulatorInfo()
        {
            EmulatorLocation = Constants.Xenia.Canary.EmulatorDir,
            ExecutableLocation = Constants.Xenia.Canary.ExecutableLocation,
            ConfigLocation = Constants.Xenia.Canary.ConfigLocation,
            Version = releaseVersion,
            ReleaseDate = releaseDate,
        };  
        
        // Setup emulator directory
        SetupEmulatorDirectory(Path.Combine(Constants.BaseDir, emulatorSettings.Canary.EmulatorLocation));
        
        // Generate configuration file and profile
        GenerateConfigFile(Path.Combine(Constants.BaseDir, emulatorSettings.Canary.ExecutableLocation), Path.Combine(Constants.BaseDir, emulatorSettings.Canary.ConfigLocation), true);
            
        // Move the configuration file to config directory
        if (File.Exists(Path.Combine(Constants.BaseDir, emulatorSettings.Canary.ConfigLocation)))
        {
            Logger.Info("Moving the configuration file to config folder");
            File.Move(Path.Combine(Constants.BaseDir, emulatorSettings.Canary.ConfigLocation), Path.Combine(Constants.BaseDir, Constants.Xenia.Canary.DefaultConfigLocation));
            
            // Updating the path since the default configuration file is stored inside the config directory
            emulatorSettings.Canary.ConfigLocation = Constants.Xenia.Canary.DefaultConfigLocation;
            ConfigManager.ChangeConfigurationFile(Path.Combine(Constants.BaseDir, emulatorSettings.Canary.ConfigLocation), XeniaVersion.Canary);
        }
    }

    public static void Uninstall(EmulatorSettings settings,XeniaVersion xeniaVersion)
    {
        string emulatorLocation = xeniaVersion switch
        {
            XeniaVersion.Canary => Constants.Xenia.Canary.EmulatorDir,
            _ => throw new Exception("Unknown Xenia version.")
        };
        
        // Delete Xenia folder
        Logger.Info($"Deleting Xenia {xeniaVersion} folder: {emulatorLocation}");
        if (Directory.Exists(Path.Combine(Constants.BaseDir, emulatorLocation)))
        {
            Directory.Delete(Path.Combine(Constants.BaseDir, emulatorLocation), true);
        }
        
        // TODO: Remove all games using this Xenia
        
        // Remove the emulator from the settings
        switch (xeniaVersion)
        {
            case XeniaVersion.Canary:
                settings.Canary = null;
                break;
            case XeniaVersion.Mousehook:
                settings.Mousehook = null;
                break;
            case XeniaVersion.Netplay:
                settings.Netplay = null;
                break;;
        }
    }
}