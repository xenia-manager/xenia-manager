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
        /// Loads the XConfig Settings into the UI
        /// </summary>
        /// <param name="sectionTable">Portion of .toml file dedicated to XConfig Settings</param>
        private void LoadXConfigSettings(TomlTable sectionTable)
        {
            // "user_country" setting
            if (sectionTable.ContainsKey("user_country"))
            {
                Log.Information($"user_country - {int.Parse(sectionTable["user_country"].ToString())}");
                Dictionary<int, string> countryMap = countryIDMap.ToDictionary(kv => kv.Key, kv => kv.Value);
                if (countryMap.TryGetValue(int.Parse(sectionTable["user_country"].ToString()), out string country))
                {
                    foreach (string item in cmbUserCountry.Items)
                    {
                        if (item == country)
                        {
                            cmbUserCountry.SelectedItem = item;
                            break;
                        }
                    }
                }
            }

            // "user_language" setting
            if (sectionTable.ContainsKey("user_language"))
            {
                Log.Information($"user_language - {int.Parse(sectionTable["user_language"].ToString())}");
                Dictionary<int, string> numberMap = languageMap.ToDictionary(kv => kv.Value, kv => kv.Key);
                if (numberMap.TryGetValue(int.Parse(sectionTable["user_language"].ToString()), out string language))
                {
                    foreach (ComboBoxItem item in cmbUserLanguage.Items)
                    {
                        if (item.Content.ToString() == language)
                        {
                            cmbUserLanguage.SelectedItem = item;
                            break;
                        }
                    }
                }
            }
        }
    }
}