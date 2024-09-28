using Serilog;
using System;
using XeniaManager.Downloader;

namespace XeniaManager
{
    public static partial class GameManager
    {
        /// <summary>
        /// Grabs the
        /// </summary>
        /// <param name="selectedPatch"></param>
        /// <returns>URL for the selectedPatch, otherwise empty string</returns>
        private static string PatchDownloadURLGrabber(string selectedPatch)
        {
            return gamePatchesList.FirstOrDefault(patch => patch.Title == selectedPatch).Url;
        }

        /// <summary>
        /// Function that downloads selected game patch
        /// </summary>
        /// <returns></returns>
        public static async Task PatchDownloader(Game game, string selectedPatch)
        {
			try
			{
                // Grabbing the path to the emulator
                string emulatorLocation = game.EmulatorVersion switch
                {
                    EmulatorVersion.Canary => ConfigurationManager.AppConfig.XeniaCanary.EmulatorLocation,
                    EmulatorVersion.Netplay => ConfigurationManager.AppConfig.XeniaNetplay.EmulatorLocation,
                    _ => ""
                };

                // Downloading the patch file
                string patchUrl = PatchDownloadURLGrabber(selectedPatch);
                Log.Information($"Patch URL: {patchUrl}");
                await DownloadManager.DownloadFileAsync(patchUrl, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, emulatorLocation, $@"Patches\{selectedPatch}"));

                // Adding it to game patch file
                game.FileLocations.PatchFilePath = Path.Combine(emulatorLocation, $@"Patches\{selectedPatch}");
                Log.Information($"{game.Title} patch has been installed");
                GameManager.Save();
            }
			catch (Exception ex)
			{
                Log.Error($"An error occurred: {ex.Message}");
            }
        }
    }
}
