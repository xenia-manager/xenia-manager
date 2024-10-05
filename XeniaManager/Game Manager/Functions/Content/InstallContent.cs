using Serilog;
using System;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace XeniaManager
{
    public static partial class GameManager
    {
        /// <summary>
        /// Extracts content using Xenia VFS Dump tool into their respective directories
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public static void InstallContent(Game game, GameContent content)
        {
            try
            {
                Log.Information($"Installing {content.DisplayName}");
                Process DumpTool = new Process();
                DumpTool.StartInfo.FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigurationManager.AppConfig.VFSDumpToolLocation);
                DumpTool.StartInfo.WorkingDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Path.GetDirectoryName(ConfigurationManager.AppConfig.VFSDumpToolLocation));
                DumpTool.StartInfo.CreateNoWindow = true;
                DumpTool.StartInfo.UseShellExecute = false;
                switch (game.EmulatorVersion)
                {
                    /*
                    case EmulatorVersion.Stable:
                        XeniaVFSDumpTool.StartInfo.Arguments = $@"""{content.Location}"" ""{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigurationManager.AppConfig.XeniaStable.EmulatorLocation)}content\{content.GameId}\{content.ContentTypeValue}\{Regex.Replace(content.DisplayName, @"[\\/:*?""<>|]", " -")}""";
                        break;*/
                    case EmulatorVersion.Canary:
                        DumpTool.StartInfo.Arguments = $@"""{content.Location}"" ""{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigurationManager.AppConfig.XeniaCanary.EmulatorLocation)}content\0000000000000000\{content.GameId}\{content.ContentTypeValue}\{Regex.Replace(content.DisplayName, @"[\\/:*?""<>|]", " -")}""";
                        break;
                    case EmulatorVersion.Netplay:
                        DumpTool.StartInfo.Arguments = $@"""{content.Location}"" ""{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigurationManager.AppConfig.XeniaNetplay.EmulatorLocation)}content\{content.GameId}\{content.ContentTypeValue}\{Regex.Replace(content.DisplayName, @"[\\/:*?""<>|]", " -")}""";
                        break;
                    default:
                        break;
                }
                DumpTool.Start();
                DumpTool.WaitForExit();
                Log.Information("Installation completed");
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
            }
        }
    }
}
