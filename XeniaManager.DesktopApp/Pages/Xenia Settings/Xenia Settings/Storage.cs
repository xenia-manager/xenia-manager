// Imported
using Serilog;
using Tomlyn.Model;

namespace XeniaManager.DesktopApp.Pages
{
    public partial class XeniaSettings
    {
        // Functions for loading Settings into the UI
        /// <summary>
        /// Loads the Storage Settings into the UI
        /// </summary>
        /// <param name="sectionTable">Portion of .toml file dedicated to Storage Settings</param>
        private void LoadStorageSettings(TomlTable sectionTable)
        {
            // "mount_cache" setting
            if (sectionTable.ContainsKey("mount_cache"))
            {
                Log.Information($"mount_cache - {(bool)sectionTable["mount_cache"]}");
                ChkMountCache.IsChecked = (bool)sectionTable["mount_cache"];
            }

            // "mount_scratch" setting
            if (sectionTable.ContainsKey("mount_scratch"))
            {
                Log.Information($"mount_scratch - {(bool)sectionTable["mount_scratch"]}");
                ChkMountScratch.IsChecked = (bool)sectionTable["mount_scratch"];
            }
        }

        /// <summary>
        /// Saves the Storage Settings into the configuration file
        /// </summary>
        /// <param name="sectionTable">Portion of .toml file dedicated to Storage Settings</param>
        private void SaveStorageSettings(TomlTable sectionTable)
        {
            // "mount_cache" setting
            if (sectionTable.ContainsKey("mount_cache"))
            {
                Log.Information($"mount_cache - {ChkMountCache.IsChecked}");
                sectionTable["mount_cache"] = ChkMountCache.IsChecked;
            }

            // "mount_scratch" setting
            if (sectionTable.ContainsKey("mount_scratch"))
            {
                Log.Information($"mount_scratch - {ChkMountScratch.IsChecked}");
                sectionTable["mount_scratch"] = ChkMountScratch.IsChecked;
            }
        }
    }
}