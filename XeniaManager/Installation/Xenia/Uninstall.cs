using System;

// Imported
using Serilog;

namespace XeniaManager.Installation
{
    public partial class Xenia
    {
        public void Uninstall(EmulatorVersion emulatorVersion)
        {
            string emulatorPath = emulatorVersion switch
            {
                EmulatorVersion.Canary => ConfigurationManager.AppConfig.XeniaCanary.EmulatorLocation,
                EmulatorVersion.Netplay => ConfigurationManager.AppConfig.XeniaNetplay.EmulatorLocation,
                _ => null
            };

            if (emulatorPath == null)
            {
                Log.Error("No Xenia folder found");
                return;
            }

            // Delete the folder containing Xenia
            Log.Information($"Deleting Xenia {emulatorVersion} folder");
            if (Directory.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, emulatorPath)))
            {
                Directory.Delete(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, emulatorPath), true);
            }

            // Remove all games using this version of the emulator
            GameManager.RemoveGames(emulatorVersion);

            switch (emulatorVersion)
            {
                case EmulatorVersion.Canary:
                    ConfigurationManager.AppConfig.XeniaCanary = null;
                    break;
                case EmulatorVersion.Netplay:
                    ConfigurationManager.AppConfig.XeniaNetplay = null;
                    break;
                default:
                    break;
            }

            ConfigurationManager.SaveConfigurationFile();
        }
    }
}
