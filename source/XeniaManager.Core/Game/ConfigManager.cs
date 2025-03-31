namespace XeniaManager.Core.Game;

public static class ConfigManager
{
    // Variables
    private static readonly Dictionary<XeniaVersion, (string DefaultConfigLocation, string ConfigLocation)> _configLocations = new()
    {
        {
            XeniaVersion.Canary,
            (Path.Combine(Constants.BaseDir, Constants.Xenia.Canary.DefaultConfigLocation), Path.Combine(Constants.BaseDir, Constants.Xenia.Canary.ConfigLocation))
        }
    };
    
    // Functions
    /// <summary>
    /// Changes the current configuration file Xenia uses
    /// </summary>
    /// <param name="configurationFile">New configuration file</param>
    /// <param name="xeniaVersion">Xenia version we're switching configuration files</param>
    /// <exception cref="Exception"></exception>
    public static bool ChangeConfigurationFile(string configurationFile, XeniaVersion xeniaVersion)
    {
        if (!_configLocations.TryGetValue(xeniaVersion, out (string DefaultConfigLocation, string ConfigLocation) configPaths))
        {
            throw new Exception($"Unsupported emulator version: {xeniaVersion}");
        }

        try
        {
            // Delete the original file
            if (File.Exists(configPaths.DefaultConfigLocation))
            {
                File.Delete(configPaths.DefaultConfigLocation);
            }

            // Ensure the game configuration file exists, create if missing
            if (!File.Exists(configurationFile))
            {
                Logger.Warning($"Configuration file '{configurationFile}' is missing. Creating a new one from default.");
                File.Copy(configPaths.ConfigLocation, configurationFile);
            }
            
            // Copy the file to the location of Xenia's configuration file
            File.Copy(configurationFile, configPaths.DefaultConfigLocation, true);
            return true;
        }
        catch (Exception ex)
        {
            throw new Exception($"{ex.Message}\n{ex}");
        }
    }

    public static void SaveConfigurationFile(string gameConfigurationFile, XeniaVersion xeniaVersion)
    {
        if (!_configLocations.TryGetValue(xeniaVersion, out (string DefaultConfigLocation, string ConfigLocation) configPaths))
        {
            throw new Exception($"Unsupported emulator version: {xeniaVersion}");
        }

        Logger.Debug($"Emulator configuration file location: {configPaths.DefaultConfigLocation}");
        Logger.Debug($"Game configuration file location: {gameConfigurationFile}");
        if (File.Exists(configPaths.DefaultConfigLocation))
        {
            File.Copy(configPaths.DefaultConfigLocation, gameConfigurationFile, true);
        }
    }
}