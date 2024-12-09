// Imported
using Serilog;
using Tomlyn.Model;

namespace XeniaManager.DesktopApp.Pages
{
    public partial class XeniaSettings
    {
        // Functions for loading Settings into the UI
        /// <summary>
        /// Loads the Memory Settings into the UI
        /// </summary>
        /// <param name="sectionTable">Portion of .toml file dedicated to Memory Settings</param>
        private void LoadMemorySettings(TomlTable sectionTable)
        {
            // "protect_zero" setting
            if (sectionTable.ContainsKey("protect_zero"))
            {
                Log.Information($"protect_zero - {(bool)sectionTable["protect_zero"]}");
                ChkProtectZero.IsChecked = (bool)sectionTable["protect_zero"];
            }
        }

        /// <summary>
        /// Saves the Memory Settings into the configuration file
        /// </summary>
        /// <param name="sectionTable">Portion of .toml file dedicated to Memory Settings</param>
        private void SaveMemorySettings(TomlTable sectionTable)
        {
            // "protect_zero" setting
            if (sectionTable.ContainsKey("protect_zero"))
            {
                Log.Information($"protect_zero - {ChkProtectZero.IsChecked}");
                sectionTable["protect_zero"] = ChkProtectZero.IsChecked;
            }
        }
    }
}