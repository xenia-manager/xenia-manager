using System;

// Imported
using Newtonsoft.Json;
using Serilog;

namespace XeniaManager
{
    public static class Configuration
    {
        /// <summary>
        /// Instance of the loaded configuration file
        /// </summary>
        public static Config config { get; set; }

        private static string ConfigurationFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");

        /// <summary>
        /// Loads the configuration file
        /// </summary>
        public static void LoadConfigurationFile()
        {
            // Check if the configuration file exists, if it doesn't, don't continue with loading of configuration file
            if (!File.Exists(ConfigurationFilePath))
            {
                Log.Warning("Couldn't find configuration file");
                return;
            }

            Log.Information("Loading configuration file");
            config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json")));
            Log.Information("Configuration file loaded");
        }

        /// <summary>
        /// Saves the configuration file
        /// </summary>
        public static void SaveConfigurationFile()
        {
            File.WriteAllText(ConfigurationFilePath, JsonConvert.SerializeObject(config, Formatting.Indented));
        }
    }
}
