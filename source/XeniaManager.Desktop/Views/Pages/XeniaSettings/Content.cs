using System.Windows;
using Tomlyn.Model;
using XeniaManager.Core;

namespace XeniaManager.Desktop.Views.Pages;

public partial class XeniaSettingsPage
{
    private void LoadContentSettings(TomlTable contentSection)
    {
        // license_mask
        if (contentSection.ContainsKey("license_mask"))
        {
            Logger.Info($"license_mask - {contentSection["license_mask"]}");
            BrdLicenseMaskSetting.Visibility = Visibility.Visible;
            CmbLicenseMask.SelectedValue = contentSection["license_mask"];
            if (CmbLicenseMask.SelectedIndex < 0)
            {
                CmbLicenseMask.SelectedIndex = 0;
            }
        }
        else
        {
            Logger.Warning("'license_mask' is missing from the configuration file");
            BrdLicenseMaskSetting.Visibility = Visibility.Collapsed;
        }
    }
    
    private void SaveContentSettings(TomlTable contentSection)
    {
        // license_mask
        if (contentSection.ContainsKey("license_mask"))
        {
            Logger.Info($"license_mask - {CmbLicenseMask.SelectedItem}");
            contentSection["license_mask"] = CmbLicenseMask.SelectedValue;
        }
    }
}