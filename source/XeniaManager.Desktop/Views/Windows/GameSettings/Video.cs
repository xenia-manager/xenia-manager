using System.Windows;

// Imported Libraries
using Tomlyn.Model;
using XeniaManager.Core;
using XeniaManager.Desktop.Components;

namespace XeniaManager.Desktop.Views.Windows;

public partial class GameSettingsEditor
{
    private void LoadVideoSettings(TomlTable videoSection)
    {
        // internal_display_resolution
        if (videoSection.ContainsKey("internal_display_resolution"))
        {
            Logger.Info($"internal_display_resolution - {videoSection["internal_display_resolution"]}");
            BrdInternalDisplayResolutionSetting.Visibility = Visibility.Visible;
            try
            {
                CmbInternalDisplayResolution.SelectedIndex = int.Parse(videoSection["internal_display_resolution"].ToString());
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                CmbInternalDisplayResolution.SelectedIndex = 16;
            }
            if (CmbInternalDisplayResolution.SelectedItem == "Custom")
            {
                BrdCustomInternalDisplayResolutionWidthSetting.Visibility = Visibility.Visible;
                BrdCustomInternalDisplayResolutionHeightSetting.Visibility = Visibility.Visible;
            }
            else
            {
                BrdCustomInternalDisplayResolutionWidthSetting.Visibility = Visibility.Collapsed;
                BrdCustomInternalDisplayResolutionHeightSetting.Visibility = Visibility.Collapsed;
            }
        }
        else
        {
            Logger.Warning("`internal_display_resolution` is missing from configuration file");
            BrdInternalDisplayResolutionSetting.Visibility = Visibility.Collapsed;
        }

        // internal_display_resolution_x
        if (videoSection.ContainsKey("internal_display_resolution_x"))
        {
            Logger.Info($"internal_display_resolution_x - {videoSection["internal_display_resolution_x"]}");
            if (CmbInternalDisplayResolution.SelectedItem == "Custom")
            {
                BrdCustomInternalDisplayResolutionWidthSetting.Visibility = Visibility.Visible;
            }
            TxtCustomInternalDisplayResolutionWidth.Text = videoSection["internal_display_resolution_x"].ToString();
        }
        else
        {
            Logger.Warning("`internal_display_resolution_x` is missing from configuration file");
            BrdCustomInternalDisplayResolutionWidthSetting.Visibility = Visibility.Collapsed;
        }

        // internal_display_resolution_y
        if (videoSection.ContainsKey("internal_display_resolution_y"))
        {
            Logger.Info($"internal_display_resolution_y - {videoSection["internal_display_resolution_y"]}");
            if (CmbInternalDisplayResolution.SelectedItem == "Custom")
            {
                BrdCustomInternalDisplayResolutionHeightSetting.Visibility = Visibility.Visible;
            }
            TxtCustomInternalDisplayResolutionHeight.Text = videoSection["internal_display_resolution_y"].ToString();
        }
        else
        {
            Logger.Warning("`internal_display_resolution_y` is missing from configuration file");
            BrdCustomInternalDisplayResolutionHeightSetting.Visibility = Visibility.Collapsed;
        }
        
        // widescreen
        if (videoSection.ContainsKey("widescreen"))
        {
            Logger.Info($"widescreen - {videoSection["widescreen"]}");
            BrdDisplayWidescreenSetting.Visibility = Visibility.Visible;
            ChkWidescreen.IsChecked = (bool)videoSection["widescreen"];
        }
        else
        {
            Logger.Warning("`widescreen` is missing from configuration file");
            BrdDisplayWidescreenSetting.Visibility = Visibility.Collapsed;
        }
    }

    private void SaveVideoSettings(TomlTable videoSection)
    {
        // internal_display_resolution
        if (videoSection.ContainsKey("internal_display_resolution"))
        {
            Logger.Info($"internal_display_resolution - {CmbInternalDisplayResolution.SelectedItem}");
            videoSection["internal_display_resolution"] = CmbInternalDisplayResolution.SelectedIndex;
        }

        // internal_display_resolution_x
        if (videoSection.ContainsKey("internal_display_resolution_x"))
        {
            int resolutionWidth = 0;
            try
            {
                resolutionWidth = int.Parse(TxtCustomInternalDisplayResolutionWidth.Text);
            }
            catch (Exception ex)
            {
                Logger.Error("Invalid input for custom internal display resolution width (Setting it to default value of 1280)");
                CustomMessageBox.ShowAsync("Invalid input", "Invalid input for custom internal display resolution width.\nSetting it to default value (1280)");
                resolutionWidth = 1280;
            }

            if (resolutionWidth > 1920)
            {
                Logger.Warning("Custom internal display resolution width is too big (Setting it to maximum allowed of 1920)");
                resolutionWidth = 1920;
            }
            else if (resolutionWidth < 1)
            {
                Logger.Warning("Custom internal display resolution width is too small (Setting it to minimum allowed of 1)");
                resolutionWidth = 1;
            }

            Logger.Info($"internal_display_resolution_x - {resolutionWidth}");
            videoSection["internal_display_resolution_x"] = resolutionWidth;
            TxtCustomInternalDisplayResolutionWidth.Text = resolutionWidth.ToString();
        }

        // internal_display_resolution_y
        if (videoSection.ContainsKey("internal_display_resolution_y"))
        {
            int resolutionHeight = 0;
            try
            {
                resolutionHeight = int.Parse(TxtCustomInternalDisplayResolutionHeight.Text);
            }
            catch (Exception ex)
            {
                Logger.Error("Invalid input for custom internal display resolution height (Setting it to default value of 720)");
                CustomMessageBox.ShowAsync("Invalid input", "Invalid input for custom internal display resolution height.\nSetting it to default value (720)");
                resolutionHeight = 720;
            }

            if (resolutionHeight > 1080)
            {
                Logger.Warning("Custom internal display resolution height is too big (Setting it to maximum allowed of 1080)");
                resolutionHeight = 1080;
            }
            else if (resolutionHeight < 1)
            {
                Logger.Warning("Custom internal display resolution height is too small (Setting it to minimum allowed of 1)");
                resolutionHeight = 1;
            }

            Logger.Info($"internal_display_resolution_y - {resolutionHeight}");
            videoSection["internal_display_resolution_y"] = resolutionHeight;
            TxtCustomInternalDisplayResolutionHeight.Text = resolutionHeight.ToString();
        }
        
        // widescreen
        if (videoSection.ContainsKey("widescreen"))
        {
            Logger.Info($"widescreen - {videoSection["widescreen"]}");
            videoSection["widescreen"] = ChkWidescreen.IsChecked;
        }
    }
}