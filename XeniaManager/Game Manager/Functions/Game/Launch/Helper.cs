// Imported
using Serilog;

namespace XeniaManager
{
    public static partial class GameManager
    {
        /// <summary>
        /// Replace the current configuration file with the new one
        /// </summary>
        /// <param name="configurationFile">Location to the configuration file</param>
        /// <param name="xeniaVersion">What Xenia Version is currently selected</param>
        /// <returns>false if there are issues with changing configuration files, otherwise true</returns>
        public static bool ChangeConfigurationFile(string configurationFile, EmulatorVersion? xeniaVersion)
        {
            // Define mapping between emulator versions and their respective file paths
            Dictionary<EmulatorVersion, (string ConfigFileLocation, string DefaultConfigLocation)> emulatorPaths =
                new Dictionary<EmulatorVersion, (string ConfigFileLocation, string DefaultConfigLocation)>
                {
                    {
                        EmulatorVersion.Canary,
                        (
                            Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                                @"Emulators\Xenia Canary\xenia-canary.config.toml"),
                            Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                                @"Emulators\Xenia Canary\config\xenia-canary.config.toml")
                        )
                    },
                    {
                        EmulatorVersion.Mousehook,
                        (
                            Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                                @"Emulators\Xenia Mousehook\xenia-canary-mousehook.config.toml"),
                            Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                                @"Emulators\Xenia Mousehook\config\xenia-canary-mousehook.config.toml")
                        )
                    },
                    {
                        EmulatorVersion.Netplay,
                        (
                            Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                                @"Emulators\Xenia Netplay\xenia-canary-netplay.config.toml"),
                            Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                                @"Emulators\Xenia Netplay\config\xenia-canary-netplay.config.toml")
                        )
                    }
                };

            if (!emulatorPaths.TryGetValue(xeniaVersion.Value, out (string ConfigFileLocation, string DefaultConfigLocation) paths))
            {
                throw new ArgumentException($"Unsupported emulator version: {xeniaVersion}");
            }

            try
            {
                // Delete the original file
                if (File.Exists(paths.ConfigFileLocation))
                {
                    File.Delete(paths.ConfigFileLocation);
                }
                
                // Ensure the game configuration file exists, create if missing
                if (!File.Exists(configurationFile))
                {
                    Log.Warning(
                        $"Configuration file '{configurationFile}' is missing. Creating a new one from default.");
                    File.Copy(paths.DefaultConfigLocation, configurationFile);
                }
                
                // Copy the file
                File.Copy(configurationFile, paths.ConfigFileLocation, true);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"Error changing configuration file for {xeniaVersion}: {ex.Message}", ex);
                return false;
            }
        }
    }
}