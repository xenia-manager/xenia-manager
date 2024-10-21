using System;

// Imported
using Serilog;
using Tomlyn.Model;
using Tomlyn;
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
        /// Function that downloads & installs the selected game patch
        /// </summary>
        public static async Task DownloadPatch(Game game, string selectedPatch)
        {
			try
			{
                // Grabbing the path to the emulator
                string emulatorLocation = game.EmulatorVersion switch
                {
                    EmulatorVersion.Canary => ConfigurationManager.AppConfig.XeniaCanary.EmulatorLocation,
                    EmulatorVersion.Mousehook => ConfigurationManager.AppConfig.XeniaMousehook.EmulatorLocation,
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

        /// <summary>
        /// Function that installs local patch file
        /// </summary>
        public static void InstallLocalPatch(Game game, string patchFileLocation)
        {
            // Checking emulator version
            string EmulatorLocation = game.EmulatorVersion switch
            {
                EmulatorVersion.Canary => ConfigurationManager.AppConfig.XeniaCanary.EmulatorLocation,
                EmulatorVersion.Mousehook => ConfigurationManager.AppConfig.XeniaMousehook.EmulatorLocation,
                EmulatorVersion.Netplay => ConfigurationManager.AppConfig.XeniaNetplay.EmulatorLocation,
                _ => throw new InvalidOperationException("Unexpected build type")
            };

            Log.Information($"Selected file: {patchFileLocation}");
            System.IO.File.Copy(patchFileLocation, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, EmulatorLocation, @$"patches\{Path.GetFileName(patchFileLocation)}"), true);
            game.FileLocations.PatchFilePath = Path.Combine(EmulatorLocation, @$"patches\{Path.GetFileName(patchFileLocation)}");
            // Save changes
            GameManager.Save();
        }

        /// <summary>
        /// Loads original and new patch file and checks if they match via hash and then adds only the new patches to the original file
        /// </summary>
        /// <param name="originalPatchFile">Path towards the original patch file</param>
        /// <param name="newPatchFile">Path towards the new patch file that we're migrating</param>
        public static void AddAdditionalPatches(string originalPatchFileLocation, string newPatchFileLocation)
        {
            try
            {
                // Reading .toml files as TomlTable
                TomlTable originalPatchFile = Toml.ToModel(File.ReadAllText(originalPatchFileLocation));
                TomlTable newPatchFile = Toml.ToModel(File.ReadAllText(newPatchFileLocation));

                // Checking if hashes match
                if (originalPatchFile["hash"].ToString() == newPatchFile["hash"].ToString())
                {
                    Log.Information("These patch files match");
                    TomlTableArray originalPatches = originalPatchFile["patch"] as TomlTableArray;
                    TomlTableArray newPatches = newPatchFile["patch"] as TomlTableArray;

                    // Checking for new patches
                    Log.Information("Looking for any new patches");
                    foreach (TomlTable patch in newPatches)
                    {
                        if (!originalPatches.Any(p => p["name"].ToString() == patch["name"].ToString()))
                        {
                            Log.Information($"{patch["name"].ToString()} is being added to the game patch file");
                            originalPatches.Add(patch);
                        }
                    }

                    Log.Information("Saving changes");
                    string updatedPatchFile = Toml.FromModel(originalPatchFile);
                    File.WriteAllText(originalPatchFileLocation, updatedPatchFile);
                    Log.Information("Additional patches have been added");
                }
                else
                {
                    Log.Error("Patches do not match");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                return;
            }
        }
    }
}
