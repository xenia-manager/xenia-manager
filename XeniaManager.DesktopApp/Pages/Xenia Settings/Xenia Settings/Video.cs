using System.Windows.Controls;

// Imported
using Serilog;
using Tomlyn.Model;

namespace XeniaManager.DesktopApp.Pages
{
    public partial class XeniaSettings
    {
        // Functions for loading Settings into the UI
        /// <summary>
        /// Loads the Video Settings into the UI
        /// </summary>
        /// <param name="sectionTable">Portion of .toml file dedicated to Video Settings</param>
        private void LoadVideoSettings(TomlTable sectionTable)
        {
            // "internal_display_resolution" setting
            if (sectionTable.ContainsKey("internal_display_resolution"))
            {
                Log.Information(
                    $"internal_display_resolution - {int.Parse(sectionTable["internal_display_resolution"].ToString())}");
                CmbInternalDisplayResolution.SelectedIndex =
                    int.Parse(sectionTable["internal_display_resolution"].ToString());
            }

            // "widescreen" setting
            if (sectionTable.ContainsKey("widescreen"))
            {
                Log.Information($"widescreen - {(bool)sectionTable["widescreen"]}");
                ChkWidescreen.IsChecked = (bool)sectionTable["widescreen"];
            }
        }

        /// <summary>
        /// Saves the Video Settings into the configuration file
        /// </summary>
        /// <param name="sectionTable">Portion of .toml file dedicated to Video Settings</param>
        private void SaveVideoSettings(TomlTable sectionTable)
        {
            // "internal_display_resolution" setting
            if (sectionTable.ContainsKey("internal_display_resolution"))
            {
                Log.Information(
                    $"internal_display_resolution - {(CmbInternalDisplayResolution.SelectedItem as ComboBoxItem).Content}");
                sectionTable["internal_display_resolution"] = CmbInternalDisplayResolution.SelectedIndex;
            }

            // "widescreen" setting
            if (sectionTable.ContainsKey("widescreen"))
            {
                Log.Information($"widescreen - {ChkWidescreen.IsChecked}");
                sectionTable["widescreen"] = ChkWidescreen.IsChecked;
            }
        }
    }
}