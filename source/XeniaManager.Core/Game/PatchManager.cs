using System.Collections;
using System.Collections.ObjectModel;
using Octokit;
using Tomlyn;
using Tomlyn.Model;
using XeniaManager.Core.Downloader;

namespace XeniaManager.Core.Game;

public class Patch
{
    public string Name { get; set; }
    public bool IsEnabled { get; set; }
    public string? Description { get; set; }
}

public static class PatchManager
{
    private static DownloadManager _downloadManager { get; set; } = new DownloadManager();

    public static async Task DownloadPatch(Game game, RepositoryContent selectedPatch)
    {
        string emulatorLocation = game.XeniaVersion switch
        {
            XeniaVersion.Canary => Constants.Xenia.Canary.PatchFolderLocation,
            XeniaVersion.Mousehook => Constants.Xenia.Mousehook.PatchFolderLocation,
            _ => throw new NotImplementedException($"This version of Xenia isn't supported")
        };
        Logger.Debug($"Emulator location: {emulatorLocation}");
        Logger.Debug($"Patch URL: {selectedPatch.DownloadUrl}");
        await _downloadManager.DownloadFileAsync(selectedPatch.DownloadUrl, Path.Combine(Constants.DirectoryPaths.Base, emulatorLocation, selectedPatch.Name));
        game.FileLocations.Patch = Path.Combine(emulatorLocation, selectedPatch.Name);
        Logger.Info($"{game.Title} patch has been installed.");
        GameManager.SaveLibrary();
    }

    public static void InstallLocalPatch(Game game, string emulatorPatchesFolder, string patchLocation)
    {
        File.Copy(patchLocation, Path.Combine(Constants.DirectoryPaths.Base, emulatorPatchesFolder, $"{game.GameId} - {game.Title}.patch.toml"), true);
        game.FileLocations.Patch = Path.Combine(emulatorPatchesFolder, $"{game.GameId} - {game.Title}.patch.toml");
        GameManager.SaveLibrary();
    }

    public static ObservableCollection<Patch> ReadPatchFile(string patchLocation)
    {
        if (!File.Exists(patchLocation))
        {
            throw new Exception("Patch file does not exist.");
        }

        ObservableCollection<Patch> patches = new ObservableCollection<Patch>();
        try
        {
            TomlTable tomlModel = Toml.ToModel(File.ReadAllText(patchLocation));
            TomlTableArray tomlPatches = tomlModel["patch"] as TomlTableArray;
            Logger.Debug($"Found {tomlPatches.Count} patches in patch file.");
            foreach (TomlTable tomlPatch in tomlPatches)
            {
                Patch newPatch = new Patch
                {
                    Name = tomlPatch["name"].ToString(),
                    IsEnabled = bool.Parse(tomlPatch["is_enabled"].ToString()),
                    Description = tomlPatch.TryGetValue("desc", out var value) ? value.ToString() : "No Description"
                };
                Logger.Debug($"Patch Name: {newPatch.Name}, Enabled: {newPatch.IsEnabled}");
                patches.Add(newPatch);
            }
            return patches;
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
            throw new Exception("Failed to read patch file.");
        }
    }

    public static void SavePatchFile(IEnumerable patches, string patchLocation)
    {
        try
        {
            // Read the patch file and apply changes
            TomlTable model = Toml.ToModel(File.ReadAllText(patchLocation));

            TomlTableArray tomlPatches = model["patch"] as TomlTableArray;
            foreach (Patch patch in patches)
            {
                foreach (TomlTable patchTable in tomlPatches)
                {
                    if (patchTable.ContainsKey("name") && patchTable["name"].Equals(patch.Name))
                    {
                        Logger.Debug($"{patch.Name}: {patchTable["is_enabled"]} -> {patch.IsEnabled}");
                        patchTable["is_enabled"] = patch.IsEnabled;
                        break;
                    }
                }
            }
            // Write the updated TOML content back to the file
            File.WriteAllText(patchLocation, Toml.FromModel(model));
            Logger.Info("Patches saved successfully");
        }
        catch (Exception ex)
        {
            throw new Exception("Failed to save patch file.");
        }
    }

    public static string AddAdditionalPatches(string originalPatchLocation, string newPatchLocation)
    {
        TomlTable originalPatchFile = Toml.ToModel(File.ReadAllText(originalPatchLocation));
        TomlTable newPatchFile = Toml.ToModel(File.ReadAllText(newPatchLocation));

        try
        {
            if (originalPatchFile["hash"].ToString() == newPatchFile["hash"].ToString())
            {
                Logger.Info("Patch files match");
                TomlTableArray originalPatches = originalPatchFile["patch"] as TomlTableArray;
                TomlTableArray newPatches = newPatchFile["patch"] as TomlTableArray;
                string addedPatches = string.Empty;

                Logger.Info("Looking for new patches");
                foreach (TomlTable newPatch in newPatches)
                {
                    if (!originalPatches.Any(patch => patch["name"].ToString() == newPatch["name"].ToString()))
                    {
                        Logger.Info($"{newPatch["name"]} is added to the game patch file");
                        addedPatches += $"{newPatch["name"]}\n";
                        originalPatches.Add(newPatch);
                    }
                }

                Logger.Info("Saving changes");
                File.WriteAllText(originalPatchLocation, Toml.FromModel(originalPatchFile));
                Logger.Info("Additional patches have been added");
                return addedPatches;
            }
            else
            {
                Logger.Error("Patch files do not match");
                return String.Empty;
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
            return String.Empty;
        }
    }

    public static void RemoveGamePatches(Game game)
    {
        Logger.Info($"Removing patch for {game}");
        if (File.Exists(Path.Combine(Constants.DirectoryPaths.Base, game.FileLocations.Patch)))
        {
            File.Delete(Path.Combine(Constants.DirectoryPaths.Base, game.FileLocations.Patch));
        }

        game.FileLocations.Patch = null;
        Logger.Info("Patch removed");
        GameManager.SaveLibrary();
    }
}