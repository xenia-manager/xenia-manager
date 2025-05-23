using System.Text.Json;
using Tomlyn.Model;

namespace XeniaManager.Core.Game;

/// <summary>
/// Changes the current Xenia configuration file and saves changes to the original file
/// </summary>
public static class ConfigManager
{
    // Variables
    /// <summary>
    /// Paths for Xenia Versions (where the configuration file needs to be so it's loaded in and the stock Xenia configuration file)
    /// </summary>
    private static readonly Dictionary<XeniaVersion, (string DefaultConfigLocation, string ConfigLocation)> _configLocations = new Dictionary<XeniaVersion, (string DefaultConfigLocation, string ConfigLocation)>
    {
        {
            XeniaVersion.Canary,
            (Path.Combine(Constants.DirectoryPaths.Base, Constants.Xenia.Canary.DefaultConfigLocation), Path.Combine(Constants.DirectoryPaths.Base, Constants.Xenia.Canary.ConfigLocation))
        }
        // TODO: Mousehook/Netplay support for ConfigManager (DefaultConfigLocation/ConfigLocation)
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
        // Tries to grab the configuration paths from the dictionary
        if (!_configLocations.TryGetValue(xeniaVersion, out (string DefaultConfigLocation, string ConfigLocation) configPaths))
        {
            // Throw NotImplementedException if it can't find the paths
            throw new NotImplementedException($"Unsupported emulator version: {xeniaVersion}");
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

    /// <summary>
    /// Saves the configuration file currently in use to its original location
    /// </summary>
    /// <param name="configurationFile">Original location of currently in use configuration file</param>
    /// <param name="xeniaVersion">Xenia version</param>
    /// <exception cref="NotImplementedException">Currently not implemented</exception>
    public static void SaveConfigurationFile(string configurationFile, XeniaVersion xeniaVersion)
    {
        // Tries to grab the configuration paths from the dictionary
        if (!_configLocations.TryGetValue(xeniaVersion, out (string DefaultConfigLocation, string ConfigLocation) configPaths))
        {
            throw new NotImplementedException($"Unsupported emulator version: {xeniaVersion}");
        }

        Logger.Debug($"Emulator configuration file location: {configPaths.DefaultConfigLocation}");
        Logger.Debug($"Game configuration file location: {configurationFile}");

        // Copies currently in use configuration file to its original location so changes are saved
        if (File.Exists(configPaths.DefaultConfigLocation))
        {
            File.Copy(configPaths.DefaultConfigLocation, configurationFile, true);
        }
    }

    private static async Task<JsonElement?> FetchOptimizedSettings(string titleId)
    {
        try
        {
            string url = @$"https://raw.githubusercontent.com/xenia-manager/Optimized-Settings/main/Settings/{titleId}.json";
            using (HttpClientService client = new HttpClientService())
            {
                return JsonSerializer.Deserialize<JsonElement>(await client.GetAsync(url));
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
            return null;
        }
    }

    public static async Task<JsonElement?> SearchForOptimizedSettings(Game game)
    {
        // Search by TitleID
        JsonElement? optimizedSettings = null;
        try
        {
            optimizedSettings = await FetchOptimizedSettings(game.GameId);
        }
        catch (Exception) { }
        if (optimizedSettings.HasValue)
        {
            return optimizedSettings;
        }
        else
        {
            // Search by AlternativeID
            if (game.AlternativeIDs.Count > 0)
            {
                foreach (string alternativeId in game.AlternativeIDs)
                {
                    try
                    {
                        optimizedSettings = await FetchOptimizedSettings(alternativeId);
                    }
                    catch (Exception) { }
                    if (optimizedSettings.HasValue)
                    {
                        return optimizedSettings;
                    }
                }
            }
        }

        return null;
    }

    public static string OptimizeSettings(TomlTable currentSettings, JsonElement optimizedSettings)
    {
        string changedSettings = string.Empty;
        // Iterate through each section (top-level properties)
        foreach (JsonProperty optimizedSection in optimizedSettings.EnumerateObject())
        {
            if (currentSettings.TryGetValue(optimizedSection.Name, out object value))
            {
                if (value is TomlTable settingSection)
                {
                    foreach (JsonProperty optimizedSetting in optimizedSection.Value.EnumerateObject())
                    {
                        if (settingSection.ContainsKey(optimizedSetting.Name))
                        {
                            Logger.Info($"{optimizedSetting.Name} {settingSection[optimizedSetting.Name]} -> {optimizedSetting.Value.ToObject()}");
                            changedSettings += $"{optimizedSetting.Name} = {optimizedSetting.Value}\n";
                            settingSection[optimizedSetting.Name] = optimizedSetting.Value.ToObject();
                        }
                        else
                        {
                            Logger.Warning($"Setting {optimizedSetting.Name} not found in current settings");
                        }
                    }
                }
            }
            else
            {
                Logger.Warning($"{optimizedSection.Name} not found in current settings");
            }
        }
        return changedSettings;
    }
}