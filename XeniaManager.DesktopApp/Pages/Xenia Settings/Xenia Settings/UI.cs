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
        /// Loads the UI Settings into the UI
        /// </summary>
        /// <param name="sectionTable">Portion of .toml file dedicated to UI Settings</param>
        private void LoadUISettings(TomlTable sectionTable)
        {
            // "show_achievement_notification" setting
            if (sectionTable.ContainsKey("show_achievement_notification"))
            {
                Log.Information($"show_achievement_notification - {(bool)sectionTable["show_achievement_notification"]}");
                chkShowAchievementNotifications.IsChecked = (bool)sectionTable["show_achievement_notification"];
            }
        }
    }
}