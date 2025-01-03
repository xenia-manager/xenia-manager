using System;

// Imported
using Serilog;
using Tomlyn.Model;
using Tomlyn;

namespace XeniaManager.Installation
{
    public partial class Xenia
    {
        /// <summary>
        /// Function that sets up Xenia Netplay
        /// </summary>
        public void NetplaySetup()
        {
            RegistrySetup(); // Setup Registry to remove the popup on the first launch
            Log.Information("Creating a configuration file for usage of Xenia Netplay");
            ConfigurationManager.AppConfig.XeniaNetplay = new EmulatorInfo
            {
                EmulatorLocation = @"Emulators\Xenia Netplay\",
                ExecutableLocation = @"Emulators\Xenia Netplay\xenia_canary_netplay.exe",
                ConfigurationFileLocation = @"Emulators\Xenia Netplay\xenia-canary-netplay.config.toml",
                Version = InstallationManager.TagName,
                ReleaseDate = InstallationManager.ReleaseDate,
                LastUpdateCheckDate = DateTime.Now
            };
            Log.Information("Saving changes to the configuration file");
            ConfigurationManager.SaveConfigurationFile();

            // Add portable.txt so the Xenia Emulator is in portable mode
            if (!File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                    @"Emulators\Xenia Netplay\portable.txt")))
            {
                File.Create(
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Emulators\Xenia Netplay\portable.txt"));
            }

            // Add "config" directory for storing game specific configuration files
            Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                @"Emulators\Xenia Netplay\config"));

            // Add "patches" directory for storing game specific patch files
            Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                @"Emulators\Xenia Netplay\patches"));

            // Generate Xenia Netplay Configuration file
            InstallationManager.GenerateConfigFile(
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                    ConfigurationManager.AppConfig.XeniaNetplay.ExecutableLocation),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                    ConfigurationManager.AppConfig.XeniaNetplay.ConfigurationFileLocation));

            // Move the configuration file to a new location
            if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                    ConfigurationManager.AppConfig.XeniaNetplay.ConfigurationFileLocation)))
            {
                Log.Information("Moving the configuration file so we can create a Symbolic Link to it");
                File.Move(
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                        ConfigurationManager.AppConfig.XeniaNetplay.ConfigurationFileLocation),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                        ConfigurationManager.AppConfig.XeniaNetplay.EmulatorLocation,
                        @"config\xenia-canary-netplay.config.toml"));
                ConfigurationManager.AppConfig.XeniaNetplay.ConfigurationFileLocation =
                    @"Emulators\Xenia Netplay\config\xenia-canary-netplay.config.toml";
                Log.Information("Creating Symbolic Link for the Xenia Netplay configuration file");
                GameManager.ChangeConfigurationFile(
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                        ConfigurationManager.AppConfig.XeniaNetplay.ConfigurationFileLocation),
                    EmulatorVersion.Netplay);
            }

            ConfigurationManager.SaveConfigurationFile(); // Save changes
        }
    }
}