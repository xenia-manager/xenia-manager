using System.Windows;

// Imported Libraries
using Tomlyn.Model;
using XeniaManager.Core;

namespace XeniaManager.Desktop.Views.Pages;

public partial class XeniaSettingsPage
{
    private void LoadUiSettings(TomlTable uiSection)
    {
        // show_achievement_notification
        if (uiSection.ContainsKey("show_achievement_notification"))
        {
            Logger.Info($"show_achievement_notification - {uiSection["show_achievement_notification"]}");
            BrdShowAchievementNotificationsSetting.Visibility = Visibility.Visible;
            ChkShowAchievementNotifications.IsChecked = (bool)uiSection["show_achievement_notification"];
        }
        else
        {
            Logger.Warning("'show_achievement_notification' is missing from the configuration file");
            BrdShowAchievementNotificationsSetting.Visibility = Visibility.Collapsed;
        }
    }

    private void SaveUiSettings(TomlTable uiSection)
    {
        // show_achievement_notification
        if (uiSection.ContainsKey("show_achievement_notification"))
        {
            Logger.Info($"show_achievement_notification - {ChkShowAchievementNotifications.IsChecked}");
            uiSection["show_achievement_notification"] = ChkShowAchievementNotifications.IsChecked;
        }
    }
}