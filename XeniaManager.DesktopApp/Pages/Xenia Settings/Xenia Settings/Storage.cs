using System;
using System.Windows;
using System.Windows.Controls;

// Imported
using Serilog;
using Tomlyn.Model;

namespace XeniaManager.DesktopApp.Pages
{
    public partial class XeniaSettings : Page
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
                chkMountCache.IsChecked = (bool)sectionTable["mount_cache"];
            }

            // "mount_scratch" setting
            if (sectionTable.ContainsKey("mount_scratch"))
            {
                Log.Information($"mount_scratch - {(bool)sectionTable["mount_scratch"]}");
                chkMountScratch.IsChecked = (bool)sectionTable["mount_scratch"];
            }
        }
    }
}