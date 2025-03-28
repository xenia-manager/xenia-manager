// Imported
using Microsoft.Win32;
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
    /// Setup of Xenia on install
    /// </summary>
    public static void CanarySetup()
    {
        RegistrySetup(); // Setup registry to remove the popup on the first launch
        Logger.Info("Creating a configuration file for usage of Xenia Canary");

        Settings.EmulatorSettings.Canary = new EmulatorInfo()
        {

        };
    }
}