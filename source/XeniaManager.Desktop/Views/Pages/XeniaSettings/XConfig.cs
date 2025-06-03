using System.Windows;

// Imported Libraries
using Tomlyn.Model;
using XeniaManager.Core;

namespace XeniaManager.Desktop.Views.Pages;

public partial class XeniaSettingsPage
{
    private void LoadXConfigSettings(TomlTable xConfigSection)
    {
        // user_country
        if (xConfigSection.ContainsKey("user_country"))
        {
            Logger.Info($"user_country - {xConfigSection["user_country"]}");
            BrdUserCountrySetting.Visibility = Visibility.Visible;
            try
            {
                CmbUserCountry.SelectedValue = int.Parse(xConfigSection["user_country"].ToString());
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                CmbUserCountry.SelectedValue = 103;
            }
        }
        else
        {
            Logger.Warning("'user_country' is missing from the configuration file");
            BrdUserCountrySetting.Visibility = Visibility.Collapsed;
        }
        
        // user_language
        if (xConfigSection.ContainsKey("user_language"))
        {
            Logger.Info($"user_language - {xConfigSection["user_language"]}");
            BrdUserLanguageSetting.Visibility = Visibility.Visible;
            try
            {
                CmbUserLanguage.SelectedValue = int.Parse(xConfigSection["user_language"].ToString());
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                CmbUserLanguage.SelectedValue = 1;
            }
        }
        else
        {
            Logger.Warning("'user_language' is missing from the configuration file");
            BrdUserLanguageSetting.Visibility = Visibility.Collapsed;
        }
    }

    private void SaveXConfigSettings(TomlTable xConfigSection)
    {
        // user_country
        if (xConfigSection.ContainsKey("user_country"))
        {
            Logger.Info($"user_country - {CmbUserCountry.SelectedItem}");
            xConfigSection["user_country"] = CmbUserCountry.SelectedValue;
        }
        
        // user_language
        if (xConfigSection.ContainsKey("user_language"))
        {
            Logger.Info($"user_language - {CmbUserLanguage.SelectedItem}");
            xConfigSection["user_language"] = CmbUserLanguage.SelectedValue;
        }
    }
}