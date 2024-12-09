// Imported

using Serilog;
using Tomlyn.Model;

namespace XeniaManager.DesktopApp.Pages
{
    public partial class XeniaSettings
    {
        // Functions for loading Settings into the UI
        /// <summary>
        /// Loads the CPU Settings into the UI
        /// </summary>
        /// <param name="sectionTable">Portion of .toml file dedicated to CPU Settings</param>
        private void LoadCpuSettings(TomlTable sectionTable)
        {
            // "break_on_unimplemented_instructions" setting
            if (sectionTable.ContainsKey("break_on_unimplemented_instructions"))
            {
                Log.Information(
                    $"break_on_unimplemented_instructions - {(bool)sectionTable["break_on_unimplemented_instructions"]}");
                ChkBreakOnUnimplementedInstructions.IsChecked =
                    (bool)sectionTable["break_on_unimplemented_instructions"];
            }
        }

        /// <summary>
        /// Saves the CPU Settings into the configuration file
        /// </summary>
        /// <param name="sectionTable">Portion of .toml file dedicated to CPU Settings</param>
        private void SaveCpuSettings(TomlTable sectionTable)
        {
            // "break_on_unimplemented_instructions" setting
            if (sectionTable.ContainsKey("break_on_unimplemented_instructions"))
            {
                Log.Information(
                    $"break_on_unimplemented_instructions - {ChkBreakOnUnimplementedInstructions.IsChecked}");
                sectionTable["break_on_unimplemented_instructions"] = ChkBreakOnUnimplementedInstructions.IsChecked;
            }
        }
    }
}