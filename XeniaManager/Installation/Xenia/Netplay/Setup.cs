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
        /// This will set 'api_address' to the default one recommended by the Config Setup for Public Sessions
        /// </summary>
        private void NetplaySettingsAdjustment(string configLocation)
        {
            try
            {
                string configText = File.ReadAllText(configLocation);
                TomlTable configFile = Toml.Parse(configText).ToModel();
                foreach (var section in configFile)
                {
                    TomlTable sectionTable = section.Value as TomlTable;
                    if (sectionTable == null)
                    {
                        continue;
                    };
                    switch (section.Key)
                    {
                        case "Live":
                            // "api_address" setting
                            sectionTable["api_address"] = "https://xenia-netplay-2a0298c0e3f4.herokuapp.com";
                            break;
                        default:
                            break;
                    }
                }
                File.WriteAllText(configLocation, Toml.FromModel(configFile));
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
            }
        }

        /// <summary>
        /// Function that sets up Xenia Netplay
        /// </summary>
        public void NetplaySetup()
        {
            Log.Information("Creating a configuration file for usage of Xenia Netplay");
            ConfigurationManager.AppConfig.XeniaNetplay = new EmulatorInfo
            {
                EmulatorLocation = @"Xenia Netplay\",
                ExecutableLocation = @"Xenia Netplay\xenia_canary_netplay.exe",
                ConfigurationFileLocation = @"Xenia Netplay\xenia-canary-netplay.config.toml",
                Version = InstallationManager.tagName,
                ReleaseDate = InstallationManager.releaseDate,
                LastUpdateCheckDate = DateTime.Now
            };
            Log.Information("Saving changes to the configuration file");
            ConfigurationManager.SaveConfigurationFile();

            // Add portable.txt so the Xenia Emulator is in portable mode
            if (!File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Xenia Netplay\portable.txt")))
            {
                File.Create(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Xenia Netplay\portable.txt"));
            }

            // Add "config" directory for storing game specific configuration files
            if (!Directory.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Xenia Netplay\config")))
            {
                Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Xenia Netplay\config"));
            }

            // Generate Xenia Netplay Configuration file
            InstallationManager.GenerateConfigFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigurationManager.AppConfig.XeniaNetplay.ExecutableLocation), Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigurationManager.AppConfig.XeniaNetplay.ConfigurationFileLocation));

            // Adjust Netplay settings so they work out of the box
            NetplaySettingsAdjustment(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigurationManager.AppConfig.XeniaNetplay.ConfigurationFileLocation));
        }
    }
}
