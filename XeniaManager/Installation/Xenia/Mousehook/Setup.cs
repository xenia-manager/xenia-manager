// Imported
using Serilog;

namespace XeniaManager.Installation
{
    public partial class Xenia
    {
        /// <summary>
        /// Function that sets up Xenia Mousehook
        /// </summary>
        public void MousehookSetup()
        {
            RegistrySetup(); // Setup Registry to remove the popup on the first launch
            Log.Information("Creating a configuration file for usage of Xenia Canary");
            ConfigurationManager.AppConfig.XeniaMousehook = new EmulatorInfo
            {
                EmulatorLocation = @"Emulators\Xenia Mousehook\",
                ExecutableLocation = @"Emulators\Xenia Mousehook\xenia_canary_mousehook.exe",
                ConfigurationFileLocation = @"Emulators\Xenia Mousehook\xenia-canary-mousehook.config.toml",
                Version = InstallationManager.TagName,
                ReleaseDate = InstallationManager.ReleaseDate,
                LastUpdateCheckDate = DateTime.Now
            };
            Log.Information("Saving changes to the configuration file");
            ConfigurationManager.SaveConfigurationFile();

            // Add portable.txt so the Xenia Emulator is in portable mode
            if (!File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                    @"Emulators\Xenia Mousehook\portable.txt")))
            {
                File.Create(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                    @"Emulators\Xenia Mousehook\portable.txt"));
            }

            // Add "config" directory for storing game specific configuration files
            Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                @"Emulators\Xenia Mousehook\config"));

            // Add "patches" directory for storing game specific patch files
            Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                @"Emulators\Xenia Mousehook\patches"));

            // Generate Xenia Mousehook Configuration file
            InstallationManager.GenerateConfigFileAndProfile(
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                    ConfigurationManager.AppConfig.XeniaMousehook.ExecutableLocation),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                    ConfigurationManager.AppConfig.XeniaMousehook.ConfigurationFileLocation));

            // Move the configuration file to a new location
            if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                    ConfigurationManager.AppConfig.XeniaMousehook.ConfigurationFileLocation)))
            {
                Log.Information("Moving the configuration file so we can create a Symbolic Link to it");
                File.Move(
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                        ConfigurationManager.AppConfig.XeniaMousehook.ConfigurationFileLocation),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                        ConfigurationManager.AppConfig.XeniaMousehook.EmulatorLocation,
                        @"config\xenia-canary-mousehook.config.toml"));
                ConfigurationManager.AppConfig.XeniaMousehook.ConfigurationFileLocation =
                    @"Emulators\Xenia Mousehook\config\xenia-canary-mousehook.config.toml";
                Log.Information("Creating Symbolic Link for the Xenia Mousehook");
                GameManager.ChangeConfigurationFile(
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                        ConfigurationManager.AppConfig.XeniaMousehook.ConfigurationFileLocation),
                    EmulatorVersion.Mousehook);
            }

            ConfigurationManager.SaveConfigurationFile(); // Save changes
        }
    }
}