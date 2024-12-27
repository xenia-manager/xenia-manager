using System.Windows;

// Imported
using Serilog;
using Tomlyn.Model;

namespace XeniaManager.DesktopApp.Pages
{
    public partial class XeniaSettings
    {
        // Functions for loading Settings into the UI
        /// <summary>
        /// Loads the UI Settings into the UI
        /// </summary>
        /// <param name="sectionTable">Portion of .toml file dedicated to UI Settings</param>
        private void LoadUiSettings(TomlTable sectionTable)
        {
            // "show_achievement_notification" setting
            if (sectionTable.ContainsKey("show_achievement_notification"))
            {
                Log.Information(
                    $"show_achievement_notification - {(bool)sectionTable["show_achievement_notification"]}");
                ChkShowAchievementNotifications.IsChecked = (bool)sectionTable["show_achievement_notification"];
                
                BrdShowAchievementNotificationsSetting.Visibility = Visibility.Visible;
                BrdShowAchievementNotificationsSetting.Tag = null;
            }
            else
            {
                Log.Warning("`show_achievement_notification` is missing from configuration file");
                BrdShowAchievementNotificationsSetting.Visibility = Visibility.Collapsed;
                BrdShowAchievementNotificationsSetting.Tag = "Ignore";
            }
        }

        /// <summary>
        /// Saves the UI Settings into the configuration file
        /// </summary>
        /// <param name="sectionTable">Portion of .toml file dedicated to UI Settings</param>
        private void SaveUiSettings(TomlTable sectionTable)
        {
            // "show_achievement_notification" setting
            if (sectionTable.ContainsKey("show_achievement_notification"))
            {
                Log.Information($"show_achievement_notification - {ChkShowAchievementNotifications.IsChecked}");
                sectionTable["show_achievement_notification"] = ChkShowAchievementNotifications.IsChecked;
            }
        }
    }
}