using System.Windows;
using Tomlyn.Model;
using XeniaManager.Core;

namespace XeniaManager.Desktop.Views.Pages;

public partial class XeniaSettingsPage
{
    private void LoadDisplaySettings(TomlTable displaySection)
    {
        // fullscreen
        if (displaySection.ContainsKey("fullscreen"))
        {
            Logger.Info($"fullscreen - {(bool)displaySection["fullscreen"]}");
            BrdDisplayFullscreenSetting.Visibility = Visibility.Visible;
            ChkFullscreen.IsChecked = (bool)displaySection["fullscreen"];
        }
        else
        {
            Logger.Warning("`fullscreen` is missing from configuration file");
            BrdDisplayFullscreenSetting.Visibility = Visibility.Collapsed;
        }
        
        // present_letterbox
        if (displaySection.ContainsKey("present_letterbox"))
        {
            Logger.Info($"present_letterbox - {(bool)displaySection["present_letterbox"]}");
            BrdDisplayLetterboxSetting.Visibility = Visibility.Visible;
            ChkLetterbox.IsChecked = (bool)displaySection["present_letterbox"];
        }
        else
        {
            Logger.Warning("`present_letterbox` is missing from configuration file");
            BrdDisplayLetterboxSetting.Visibility = Visibility.Collapsed;
        }
    }

    private void SaveDisplaySettings(TomlTable displaySection)
    {
        // fullscreen
        if (displaySection.ContainsKey("fullscreen"))
        {
            Logger.Info($"fullscreen - {ChkFullscreen.IsChecked}");
            displaySection["fullscreen"] = ChkFullscreen.IsChecked;
        }
        
        // present_letterbox
        if (displaySection.ContainsKey("present_letterbox"))
        {
            Logger.Info($"present_letterbox - {ChkLetterbox.IsChecked}");
            displaySection["present_letterbox"] = ChkLetterbox.IsChecked;
        }
    }
}