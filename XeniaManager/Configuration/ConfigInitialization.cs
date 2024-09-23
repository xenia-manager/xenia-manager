using System;
using System.Reflection;

// Imported
using Newtonsoft.Json;
using Serilog;

namespace XeniaManager
{
    public static class ConfigurationManager
    {
        /// <summary>
        /// Instance of the loaded configuration file
        /// </summary>
        public static Configuration AppConfig { get; set; }

        /// <summary>
        /// Path towards configuration file
        /// </summary>
        private static string ConfigurationFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");

        /// <summary>
        /// Initializes a new configuration file
        /// </summary>
        public static void InitializeNewConfiguration()
        {
            AppConfig = new Configuration();
            AppConfig.Manager = new UpdateInfo
            {
                Version = $"{Assembly.GetExecutingAssembly().GetName().Version.Major}.{Assembly.GetExecutingAssembly().GetName().Version.Minor}.{Assembly.GetExecutingAssembly().GetName().Version.Build}",
                ReleaseDate = null,
                LastUpdateCheckDate = DateTime.Now
            };
        }

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
            AppConfig = JsonConvert.DeserializeObject<Configuration>(File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json")));
            Log.Information("Configuration file loaded");
        }

        /// <summary>
        /// Saves the configuration file
        /// </summary>
        public static void SaveConfigurationFile()
        {
            File.WriteAllText(ConfigurationFilePath, JsonConvert.SerializeObject(AppConfig, Formatting.Indented));
        }
    }
}
