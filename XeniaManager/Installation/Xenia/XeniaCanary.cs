using System;

// Imported
using Serilog;

namespace XeniaManager.Installation
{
    public static partial class InstallationManager
    {
        /// <summary>
        /// Function that sets up Xenia Canary
        /// </summary>
        public static void XeniaCanarySetup()
        {
            Log.Information("Creating a configuration file for usage of Xenia Canary");
            ConfigurationManager.AppConfig.XeniaCanary = new EmulatorInfo
            {
                EmulatorLocation = @"Xenia Canary\",
                ExecutableLocation = @"Xenia Canary\xenia_canary.exe",
                ConfigurationFileLocation = @"Xenia Canary\xenia-canary.config.toml",
                Version = InstallationManager.tagName,
                ReleaseDate = InstallationManager.releaseDate,
                LastUpdateCheckDate = DateTime.Now
            };
            Log.Information("Saving changes to the configuration file");
            ConfigurationManager.SaveConfigurationFile();

            // Add portable.txt so the Xenia Emulator is in portable mode
            if (!File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Xenia Canary\portable.txt")))
            {
                File.Create(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Xenia Canary\portable.txt"));
            }

            // Add "config" directory for storing game specific configuration files
            if (!Directory.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Xenia Canary\config")))
            {
                Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Xenia Canary\config"));
            }

            // Generate Xenia Canary Configuration file
            GenerateConfigFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigurationManager.AppConfig.XeniaCanary.ExecutableLocation), Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigurationManager.AppConfig.XeniaCanary.ExecutableLocation));
        }
    }
}
