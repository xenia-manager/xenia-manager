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
        /// Loads the Kernel Settings into the UI
        /// </summary>
        /// <param name="sectionTable">Portion of .toml file dedicated to Kernel Settings</param>
        private void LoadKernelSettings(TomlTable sectionTable)
        {
            // "apply_title_update" setting
            if (sectionTable.ContainsKey("apply_title_update"))
            {
                Log.Information($"apply_title_update - {(bool)sectionTable["apply_title_update"]}");
                chkTitleUpdates.IsChecked = (bool)sectionTable["apply_title_update"];
            }
        }

        /// <summary>
        /// Saves the Kernel Settings into the configuration file
        /// </summary>
        /// <param name="sectionTable">Portion of .toml file dedicated to Kernel Settings</param>
        private void SaveKernelSettings(TomlTable sectionTable)
        {
            // "apply_title_update" setting
            if (sectionTable.ContainsKey("apply_title_update"))
            {
                Log.Information($"apply_title_update - {chkTitleUpdates.IsChecked}");
                sectionTable["apply_title_update"] = chkTitleUpdates.IsChecked;
            }
        }
    }
}