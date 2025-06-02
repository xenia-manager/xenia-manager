using System.Windows;

// Imported Libraries
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

        // cl
        if (kernelSection.ContainsKey("cl"))
        {
            Logger.Info($"cl - {kernelSection["cl"]}");
            BrdCommandLineSetting.Visibility = Visibility.Visible;
            TxtCommandLine.Text = kernelSection["cl"].ToString();
        }
        else
        {
            Logger.Warning("'cl' is missing from the configuration file");
            BrdCommandLineSetting.Visibility = Visibility.Collapsed;
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

        // cl
        if (kernelSection.ContainsKey("cl"))
        {
            Logger.Info($"cl - {TxtCommandLine.Text}");
            kernelSection["cl"] = TxtCommandLine.Text;
        }
    }
}