using System.Reflection;

// Imported
using Newtonsoft.Json;
using Serilog;

namespace XeniaManager
{
    public static partial class ConfigurationManager
    {
        /// <summary>
        /// Instance of the loaded configuration file
        /// </summary>
        public static Configuration AppConfig { get; set; }

        /// <summary>
        /// Simple check for clearing cache so it only happens once
        /// </summary>
        private static bool CacheCleared { get; set; } = false;

        /// <summary>
        /// Path towards configuration file
        /// </summary>
        private static readonly string ConfigurationFilePath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Config\config.json");

        /// <summary>
        /// Used for loading all the bindings from Bindings.ini
        /// </summary>
        public static MousehookBindings MousehookBindings = new MousehookBindings();

        /// <summary>
        /// Initializes a new configuration file
        /// </summary>
        public static void InitializeNewConfiguration()
        {
            AppConfig = new Configuration
            {
                Manager = new UpdateInfo
                {
                    Version =
                        $"{Assembly.GetExecutingAssembly().GetName().Version.Major}.{Assembly.GetExecutingAssembly().GetName().Version.Minor}.{Assembly.GetExecutingAssembly().GetName().Version.Build}",
                    ReleaseDate = null,
                    LastUpdateCheckDate = DateTime.Now
                }
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
            AppConfig = JsonConvert.DeserializeObject<Configuration>(File.ReadAllText(ConfigurationFilePath));
        }

        /// <summary>
        /// Saves the configuration file
        /// </summary>
        public static void SaveConfigurationFile()
        {
            File.WriteAllText(ConfigurationFilePath, JsonConvert.SerializeObject(AppConfig, Formatting.Indented));
        }

        /// <summary>
        /// Used to clear temporary files
        /// </summary>
        public static void ClearTemporaryFiles()
        {
            // Checking if cache has already been cleared
            if (CacheCleared)
            {
                return;
            }

            // Clearing icon cache
            Log.Information("Clearing Icon Cache");
            foreach (string filePath in Directory.GetFiles(
                         Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Cache"), "*",
                         SearchOption.AllDirectories))
            {
                try
                {
                    File.Delete(filePath);
                }
                catch (IOException)
                {
                    Log.Warning($"{Path.GetFileName(filePath)} won't get deleted since it's currently in use");
                }
                catch (Exception ex)
                {
                    Log.Error($"An error occurred: {ex.Message}");
                    break;
                }
            }

            // Clearing downloads folder
            Log.Information("Clearing downloads folder");
            foreach (string filePath in Directory.GetFiles(
                         Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Downloads"), "*",
                         SearchOption.AllDirectories))
            {
                try
                {
                    File.Delete(filePath);
                }
                catch (IOException)
                {
                    Log.Warning($"{Path.GetFileName(filePath)} won't get deleted since it's currently in use");
                }
                catch (Exception ex)
                {
                    Log.Error($"An error occurred: {ex.Message}");
                    break;
                }
            }

            CacheCleared = true; // This is to make sure clearing of temporary files happens only once per Xenia Manager
        }
    }
}