﻿// Imported
using Serilog;

namespace XeniaManager.Installation
{
    public partial class Xenia
    {
        /// <summary>
        /// Function that sets up Xenia Canary
        /// </summary>
        public void CanarySetup()
        {
            RegistrySetup(); // Setup Registry to remove the popup on the first launch
            Log.Information("Creating a configuration file for usage of Xenia Canary");
            ConfigurationManager.AppConfig.XeniaCanary = new EmulatorInfo
            {
                EmulatorLocation = @"Emulators\Xenia Canary\",
                ExecutableLocation = @"Emulators\Xenia Canary\xenia_canary.exe",
                ConfigurationFileLocation = @"Emulators\Xenia Canary\xenia-canary.config.toml",
                Version = InstallationManager.TagName,
                ReleaseDate = InstallationManager.ReleaseDate,
                LastUpdateCheckDate = DateTime.Now
            };
            Log.Information("Saving changes to the configuration file");
            ConfigurationManager.SaveConfigurationFile();

            // Add portable.txt so the Xenia Emulator is in portable mode
            if (!File.Exists(
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Emulators\Xenia Canary\portable.txt")))
            {
                File.Create(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                    @"Emulators\Xenia Canary\portable.txt"));
            }

            // Add "config" directory for storing game specific configuration files
            Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                @"Emulators\Xenia Canary\config"));

            // Add "patches" directory for storing game specific patch files
            Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                @"Emulators\Xenia Canary\patches"));

            // Generate Xenia Canary Configuration file
            InstallationManager.GenerateConfigFileAndProfile(
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                    ConfigurationManager.AppConfig.XeniaCanary.ExecutableLocation),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                    ConfigurationManager.AppConfig.XeniaCanary.ConfigurationFileLocation));

            // Move the configuration file to a new location
            if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                    ConfigurationManager.AppConfig.XeniaCanary.ConfigurationFileLocation)))
            {
                Log.Information("Moving the configuration file so we can create a Symbolic Link to it");
                File.Move(
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                        ConfigurationManager.AppConfig.XeniaCanary.ConfigurationFileLocation),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                        ConfigurationManager.AppConfig.XeniaCanary.EmulatorLocation,
                        @"config\xenia-canary.config.toml"));
                ConfigurationManager.AppConfig.XeniaCanary.ConfigurationFileLocation =
                    @"Emulators\Xenia Canary\config\xenia-canary.config.toml";
                Log.Information("Creating Symbolic Link for the Xenia Canary");
                GameManager.ChangeConfigurationFile(
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                        ConfigurationManager.AppConfig.XeniaCanary.ConfigurationFileLocation), EmulatorVersion.Canary);
            }

            ConfigurationManager.SaveConfigurationFile(); // Save changes
        }
    }
}