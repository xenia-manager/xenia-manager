using System.Diagnostics;
using System.Text.RegularExpressions;

// Imported
using Serilog;

namespace XeniaManager
{
    public static partial class GameManager
    {
        /// <summary>
        /// Extracts content using Xenia VFS Dump tool into their respective directories
        /// </summary>
        /// <param name="game">Selected game</param>
        /// <param name="content">Selected content</param>
        /// <returns></returns>
        public static void InstallContent(Game game, GameContent content)
        {
            try
            {
                Log.Information($"Installing {content.DisplayName}");
                Process dumpTool = new Process();
                dumpTool.StartInfo = new ProcessStartInfo
                {
                    FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                        ConfigurationManager.AppConfig.VfsDumpToolLocation),
                    WorkingDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                        Path.GetDirectoryName(ConfigurationManager.AppConfig.VfsDumpToolLocation)),
                    CreateNoWindow = true,
                    UseShellExecute = false,
                };
                switch (game.EmulatorVersion)
                {
                    case EmulatorVersion.Canary:
                        dumpTool.StartInfo.Arguments =
                            $@"""{content.Location}"" ""{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigurationManager.AppConfig.XeniaCanary.EmulatorLocation)}content\0000000000000000\{content.GameId}\{content.ContentTypeValue}\{Regex.Replace(content.DisplayName, @"[\\/:*?""<>|]", " -")}""";
                        Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                            ConfigurationManager.AppConfig.XeniaCanary.EmulatorLocation, "content", "0000000000000000",
                            content.GameId, content.ContentTypeValue)
                        );
                        break;
                    case EmulatorVersion.Mousehook:
                        dumpTool.StartInfo.Arguments =
                            $@"""{content.Location}"" ""{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigurationManager.AppConfig.XeniaMousehook.EmulatorLocation)}content\0000000000000000\{content.GameId}\{content.ContentTypeValue}\{Regex.Replace(content.DisplayName, @"[\\/:*?""<>|]", " -")}""";
                        Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                            ConfigurationManager.AppConfig.XeniaMousehook.EmulatorLocation, "content", "0000000000000000",
                            content.GameId, content.ContentTypeValue)
                        );
                        break;
                    case EmulatorVersion.Netplay:
                        dumpTool.StartInfo.Arguments =
                            $@"""{content.Location}"" ""{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigurationManager.AppConfig.XeniaNetplay.EmulatorLocation)}content\0000000000000000\{content.GameId}\{content.ContentTypeValue}\{Regex.Replace(content.DisplayName, @"[\\/:*?""<>|]", " -")}""";
                        Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                            ConfigurationManager.AppConfig.XeniaNetplay.EmulatorLocation, "content", "0000000000000000",
                            content.GameId, content.ContentTypeValue)
                        );
                        break;
                    default:
                        break;
                }

                dumpTool.Start();
                dumpTool.WaitForExit();
                Log.Information("Installation completed");
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
            }
        }
    }
}