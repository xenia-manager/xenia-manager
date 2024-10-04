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
        /// Loads the CPU Settings into the UI
        /// </summary>
        /// <param name="sectionTable">Portion of .toml file dedicated to CPU Settings</param>
        private void LoadCPUSettings(TomlTable sectionTable)
        {
            // "break_on_unimplemented_instructions" setting
            if (sectionTable.ContainsKey("break_on_unimplemented_instructions"))
            {
                Log.Information($"break_on_unimplemented_instructions - {(bool)sectionTable["break_on_unimplemented_instructions"]}");
                chkBreakOnUnimplementedInstructions.IsChecked = (bool)sectionTable["break_on_unimplemented_instructions"];
            }
        }

        /// <summary>
        /// Saves the CPU Settings into the configuration file
        /// </summary>
        /// <param name="sectionTable">Portion of .toml file dedicated to CPU Settings</param>
        private void SaveCPUSettings(TomlTable sectionTable)
        {

        }
    }
}