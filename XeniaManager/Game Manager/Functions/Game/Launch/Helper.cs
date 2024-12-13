using System.Runtime.InteropServices;

// Imported
using Serilog;

namespace XeniaManager
{
    public static partial class GameManager
    {
        /// <summary>
        /// Declares the CreateSymbolicLink function from kernel32.dll, a Windows API for creating symbolic links
        /// </summary>
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern bool CreateSymbolicLink(string lpSymlinkFileName, string lpTargetFileName, int dwFlags);

        /// <summary>
        /// Create a Symbolic Link of the configuration file for Xenia
        /// </summary>
        /// <param name="configurationFile">Location to the configuration file</param>
        /// <param name="xeniaVersion">What Xenia Version is currently selected</param>
        public static void ChangeConfigurationFile(string configurationFile, EmulatorVersion? xeniaVersion)
        {
            // Define mapping between emulator versions and their respective file paths
            Dictionary<EmulatorVersion, (string SymbolicLink, string OriginalConfig)> emulatorPaths =
                new Dictionary<EmulatorVersion, (string SymbolicLink, string OriginalConfig)>
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

            if (!emulatorPaths.TryGetValue(xeniaVersion.Value, out (string SymbolicLink, string OriginalConfig) paths))
            {
                throw new ArgumentException($"Unsupported emulator version: {xeniaVersion}");
            }

            string symbolicLinkName = paths.SymbolicLink;
            string originalConfigurationFile = paths.OriginalConfig;

            try
            {
                // Delete existing symbolic link if it exists
                if (File.Exists(symbolicLinkName))
                {
                    File.Delete(symbolicLinkName);
                }

                // Ensure the game configuration file exists, create if missing
                if (!File.Exists(configurationFile))
                {
                    Log.Warning(
                        $"Configuration file '{configurationFile}' is missing. Creating a new one from default.");
                    File.Copy(originalConfigurationFile, configurationFile);
                }

                // Create the symbolic link
                bool result = CreateSymbolicLink(symbolicLinkName, configurationFile, 0x0);
                if (!result)
                {
                    throw new InvalidOperationException("Failed to create the symbolic link.");
                }
            }
            catch (Exception ex)
            {
                throw new IOException($"Error changing configuration file for {xeniaVersion}: {ex.Message}", ex);
            }
        }
    }
}