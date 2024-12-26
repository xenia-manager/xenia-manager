// Imported

using System.Windows;
using Serilog;
using Tomlyn.Model;

namespace XeniaManager.DesktopApp.Pages
{
    public partial class XeniaSettings
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
                ChkTitleUpdates.IsChecked = (bool)sectionTable["apply_title_update"];
                
                BrdTitleUpdatesSetting.Visibility = Visibility.Visible;
                BrdTitleUpdatesSetting.Tag = null;
            }
            else
            {
                BrdTitleUpdatesSetting.Visibility = Visibility.Collapsed;
                BrdTitleUpdatesSetting.Tag = "Ignore";
            }
            
            // "cl" setting
            if (sectionTable.ContainsKey("cl"))
            {
                Log.Information($"cl - {sectionTable["cl"]}");
                TxtCl.Text = (string)sectionTable["cl"];
                
                BrdCommandLineSetting.Visibility = Visibility.Visible;
                BrdCommandLineSetting.Tag = null;
            }
            else
            {
                BrdCommandLineSetting.Visibility = Visibility.Collapsed;
                BrdCommandLineSetting.Tag = "Ignore";
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
                Log.Information($"apply_title_update - {ChkTitleUpdates.IsChecked}");
                sectionTable["apply_title_update"] = ChkTitleUpdates.IsChecked;
            }
            
            // "cl" setting
            if (sectionTable.ContainsKey("cl"))
            {
                Log.Information($"cl - {TxtCl.Text}");
                sectionTable["cl"] = TxtCl.Text;
            }
        }
    }
}