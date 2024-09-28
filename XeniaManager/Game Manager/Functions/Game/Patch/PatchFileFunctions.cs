using ImageMagick.Drawing;
using Serilog;
using System;
using System.Collections.ObjectModel;
using Tomlyn.Model;
using Tomlyn;

namespace XeniaManager
{
    public static partial class GameManager
    {
        /// <summary>
        /// Reads the .toml file
        /// </summary>
        public static ObservableCollection<Patch> ReadPatchFile(string patchLocation)
        {
            // Checks if the file exists
            if (!File.Exists(patchLocation))
            {
                return null;
            }

            ObservableCollection<Patch> Patches = new ObservableCollection<Patch>();
            string content = File.ReadAllText(patchLocation);
            try
            {
                TomlTable model = Toml.ToModel(content);
                TomlTableArray patches = model["patch"] as TomlTableArray;
                foreach (var patch in patches)
                {
                    Patch newPatch = new Patch
                    {
                        Name = patch["name"].ToString(),
                        IsEnabled = bool.Parse(patch["is_enabled"].ToString())
                    };
                    if (patch.ContainsKey("desc"))
                    {
                        newPatch.Description = patch["desc"].ToString();
                    }
                    else
                    {
                        newPatch.Description = "No description";
                    }
                    Patches.Add(newPatch);
                }
                return Patches;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                return null;
            }
        }

        /// <summary>
        /// Saves the game patches into the .toml file
        /// </summary>
        public static void SavePatchFile(ObservableCollection<Patch> Patches, string patchLocation)
        {
            try
            {
                // Read the patch file and apply changes
                string content = File.ReadAllText(patchLocation);
                TomlTable model = Toml.ToModel(content);

                TomlTableArray patches = model["patch"] as TomlTableArray;
                foreach (var patch in Patches)
                {
                    foreach (TomlTable patchTable in patches)
                    {
                        if (patchTable.ContainsKey("name") && patchTable["name"].Equals(patch.Name))
                        {
                            patchTable["is_enabled"] = patch.IsEnabled;
                            break;
                        }
                    }
                }

                // Serialize the TOML model back to a string
                string updatedContent = Toml.FromModel(model);

                // Write the updated TOML content back to the file
                File.WriteAllText(patchLocation, updatedContent);
                Log.Information("Patches saved successfully");
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
            }
        }
    }
}
