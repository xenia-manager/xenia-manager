using System.Windows;
using Tomlyn.Model;
using XeniaManager.Core;

namespace XeniaManager.Desktop.Views.Pages;

public partial class XeniaSettingsPage
{
    private void LoadGpuSettings(TomlTable gpuSection)
    {
        // vsync
        if (gpuSection.ContainsKey("vsync"))
        {
            Logger.Info($"vsync - {gpuSection["vsync"]}");
            BrdXeniaVerticalSyncSetting.Visibility = Visibility.Visible;
            ChkXeniaVSync.IsChecked = (bool)gpuSection["vsync"];
        }
        else
        {
            Logger.Warning("`vsync` is missing from configuration file");
            BrdXeniaVerticalSyncSetting.Visibility = Visibility.Collapsed;
        }

        // framerate_limit
        if (gpuSection.ContainsKey("framerate_limit"))
        {
            Logger.Info($"framerate_limit - {gpuSection["framerate_limit"]}");
            BrdXeniaFramerateLimitSetting.Visibility = Visibility.Visible;
            try
            {
                SldXeniaFramerate.Value = int.Parse(gpuSection["framerate_limit"].ToString());
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                SldXeniaFramerate.Value = 60;
            }
        }
        else
        {
            Logger.Warning("`framerate_limit` is missing from configuration file");
            BrdXeniaFramerateLimitSetting.Visibility = Visibility.Collapsed;
        }
    }

    private void SaveGpuSettings(TomlTable gpuSection)
    {
        // vsync
        if (gpuSection.ContainsKey("vsync"))
        {
            Logger.Info($"vsync - {ChkXeniaVSync.IsChecked}");
            gpuSection["vsync"] = ChkXeniaVSync.IsChecked;
        }

        // framerate_limit
        if (gpuSection.ContainsKey("framerate_limit"))
        {
            Logger.Info($"framerate_limit - {gpuSection["framerate_limit"]}");
            gpuSection["framerate_limit"] = (int)SldXeniaFramerate.Value;
        }
    }
}