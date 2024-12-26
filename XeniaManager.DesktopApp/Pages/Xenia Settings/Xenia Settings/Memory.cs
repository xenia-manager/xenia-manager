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
                
                BrdProtectZeroPageSetting.Visibility = Visibility.Visible;
                BrdProtectZeroPageSetting.Tag = null;
            }
            else
            {
                BrdProtectZeroPageSetting.Visibility = Visibility.Collapsed;
                BrdProtectZeroPageSetting.Tag = "Ignore";
            }
            
            // "scribble_heap" setting
            if (sectionTable.ContainsKey("scribble_heap"))
            {
                Log.Information($"scribble_heap - {(bool)sectionTable["scribble_heap"]}");
                ChkScribbleHeap.IsChecked = (bool)sectionTable["scribble_heap"];
                
                BrdScribbleHeapSetting.Visibility = Visibility.Visible;
                BrdScribbleHeapSetting.Tag = null;
            }
            else
            {
                BrdScribbleHeapSetting.Visibility = Visibility.Collapsed;
                BrdScribbleHeapSetting.Tag = "Ignore";
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
            
            // "scribble_heap" setting
            if (sectionTable.ContainsKey("scribble_heap"))
            {
                Log.Information($"scribble_heap - {ChkScribbleHeap.IsChecked}");
                sectionTable["scribble_heap"] = ChkScribbleHeap.IsChecked;
            }
        }
    }
}