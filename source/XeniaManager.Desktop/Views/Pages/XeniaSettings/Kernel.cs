using System.Windows;
using Tomlyn.Model;
using XeniaManager.Core;

namespace XeniaManager.Desktop.Views.Pages;

public partial class XeniaSettingsPage
{
    private void LoadKernelSettings(TomlTable kernelSection)
    {
        // apply_title_update
        if (kernelSection.ContainsKey("apply_title_update"))
        {
            Logger.Info($"apply_title_update - {kernelSection["apply_title_update"]}");
            BrdTitleUpdatesSetting.Visibility = Visibility.Visible;
            ChkTitleUpdates.IsChecked = (bool)kernelSection["apply_title_update"];
        }
        else
        {
            Logger.Warning("'apply_title_update' is missing from the configuration file");
            BrdTitleUpdatesSetting.Visibility = Visibility.Collapsed;
        }
    }

    private void SaveKernelSettings(TomlTable kernelSection)
    {
        // apply_title_update
        if (kernelSection.ContainsKey("apply_title_update"))
        {
            Logger.Info($"apply_title_update - {ChkTitleUpdates.IsChecked}");
            kernelSection["apply_title_update"] = ChkTitleUpdates.IsChecked;
        }
    }
}