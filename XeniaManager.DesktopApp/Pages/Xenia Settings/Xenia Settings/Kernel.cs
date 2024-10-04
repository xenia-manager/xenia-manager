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

            // "max_signed_profiles" setting
            if (sectionTable.ContainsKey("max_signed_profiles"))
            {
                Log.Information($"max_signed_profiles - {int.Parse(sectionTable["max_signed_profiles"].ToString())}");
                cmbMaxSignedProfiles.SelectedIndex = int.Parse(sectionTable["max_signed_profiles"].ToString()) - 1;
            }
        }
    }
}