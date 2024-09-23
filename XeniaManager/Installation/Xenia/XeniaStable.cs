using System;

// Imported
using Serilog;

namespace XeniaManager.Installation
{
    public static partial class InstallationManager
    {
        /// <summary>
        /// Function that sets up Xenia Stable
        /// </summary>
        public static void XeniaStableSetup()
        {
            Log.Information("Creating a configuration file for usage of Xenia Stable");
            ConfigurationManager.AppConfig.XeniaStable = new EmulatorInfo
            {
                EmulatorLocation = @"Xenia Stable\",
                ExecutableLocation = @"Xenia Stable\xenia.exe",
                ConfigurationFileLocation = @"Xenia Stable\xenia.config.toml",
                Version = InstallationManager.tagName,
                ReleaseDate = InstallationManager.releaseDate,
                LastUpdateCheckDate = DateTime.Now
            };
            Log.Information("Saving changes to the configuration file");
            ConfigurationManager.SaveConfigurationFile();

            // Add portable.txt so the Xenia Emulator is in portable mode
            if (!File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Xenia Stable\portable.txt")))
            {
                File.Create(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Xenia Stable\portable.txt"));
            }

            // Add "config" directory for storing game specific configuration files
            if (!Directory.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Xenia Stable\config")))
            {
                Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Xenia Stable\config"));
            }

            // Generate Xenia Stable Configuration file
            GenerateConfigFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigurationManager.AppConfig.XeniaStable.ExecutableLocation), Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigurationManager.AppConfig.XeniaStable.ExecutableLocation));
        }
    }
}
