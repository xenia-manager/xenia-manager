using System.Runtime.InteropServices;

namespace XeniaManager
{
    public static partial class GameManager
    {
        /// <summary>
        /// Declares the CreateSymbolicLink function from kernel32.dll, a Windows API for creating symbolic links
        /// </summary>
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern bool CreateSymbolicLink(string lpSymlinkFileName, string lpTargetFileName, int dwFlags);

        private const int SYMBOLIC_LINK_FLAG_FILE = 0x0;
        private const int SYMBOLIC_LINK_FLAG_DIRECTORY = 0x1;

        /// <summary>
        /// Create a Symbolic Link of the configuration file for Xenia
        /// </summary>
        /// <param name="configurationFile">Location to the configuration file</param>
        /// <param name="xeniaVersion">What Xenia Version is currently selected</param>
        public static void ChangeConfigurationFile(string configurationFile, EmulatorVersion? xeniaVersion)
        {
            // Grabbing the directory to the Symbolic Link
            string symbolicLinkName = "";
            switch (xeniaVersion)
            {
                case EmulatorVersion.Canary:
                    symbolicLinkName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                        @"Emulators\Xenia Canary\xenia-canary.config.toml");
                    break;
                case EmulatorVersion.Netplay:
                    symbolicLinkName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                        @"Emulators\Xenia Netplay\xenia-canary-netplay.config.toml");
                    break;
                case EmulatorVersion.Mousehook:
                    symbolicLinkName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                        @"Emulators\Xenia Mousehook\xenia-canary-mousehook.config.toml");
                    break;
                default:
                    break;
            }

            // Checking if Symbolic Link already exists and deletes it
            if (File.Exists(symbolicLinkName))
            {
                File.Delete(symbolicLinkName);
            }

            // Create a new Symbolic Link
            bool result = CreateSymbolicLink(symbolicLinkName, configurationFile, SYMBOLIC_LINK_FLAG_FILE);
            if (!result)
            {
                throw new Exception("Couldn't create Symbolic Link");
            }
        }
    }
}