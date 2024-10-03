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
        /// Loads the Display Settings into the UI
        /// </summary>
        /// <param name="sectionTable">Portion of .toml file dedicated to Display Settings</param>
        private void LoadDisplaySettings(TomlTable sectionTable)
        {
            // "fullscreen" setting
            if (sectionTable.ContainsKey("fullscreen"))
            {
                Log.Information($"fullscreen - {(bool)sectionTable["fullscreen"]}");
                chkFullscreen.IsChecked = (bool)sectionTable["fullscreen"];
            }
        }
    }
}