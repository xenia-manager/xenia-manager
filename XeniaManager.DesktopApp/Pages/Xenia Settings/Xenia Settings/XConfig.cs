using System.Windows;
using System.Windows.Controls;

// Imported
using Serilog;
using Tomlyn.Model;

namespace XeniaManager.DesktopApp.Pages
{
    public partial class XeniaSettings
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
                Dictionary<int, string> countryMap = countryIdMap.ToDictionary(kv => kv.Key, kv => kv.Value);
                if (countryMap.TryGetValue(int.Parse(sectionTable["user_country"].ToString()), out string country))
                {
                    foreach (string item in CmbUserCountry.Items)
                    {
                        if (item == country)
                        {
                            CmbUserCountry.SelectedItem = item;
                            break;
                        }
                    }
                }
                
                BrdUserCountrySetting.Visibility = Visibility.Visible;
                BrdUserCountrySetting.Tag = null;
            }
            else
            {
                Log.Warning("`user_country` is missing from configuration file");
                BrdUserCountrySetting.Visibility = Visibility.Collapsed;
                BrdUserCountrySetting.Tag = "Ignore";
            }

            // "user_language" setting
            if (sectionTable.ContainsKey("user_language"))
            {
                Log.Information($"user_language - {int.Parse(sectionTable["user_language"].ToString())}");
                Dictionary<int, string> numberMap = languageMap.ToDictionary(kv => kv.Value, kv => kv.Key);
                if (numberMap.TryGetValue(int.Parse(sectionTable["user_language"].ToString()), out string language))
                {
                    foreach (ComboBoxItem item in CmbUserLanguage.Items)
                    {
                        if (item.Content.ToString() == language)
                        {
                            CmbUserLanguage.SelectedItem = item;
                            break;
                        }
                    }
                }
                
                BrdUserLanguageSetting.Visibility = Visibility.Visible;
                BrdUserLanguageSetting.Tag = null;
            }
            else
            {
                Log.Warning("`user_language` is missing from configuration file");
                BrdUserLanguageSetting.Visibility = Visibility.Collapsed;
                BrdUserLanguageSetting.Tag = "Ignore";
            }
        }

        /// <summary>
        /// Saves the XConfig Settings into the configuration file
        /// </summary>
        /// <param name="sectionTable">Portion of .toml file dedicated to XConfig Settings</param>
        private void SaveXConfigSettings(TomlTable sectionTable)
        {
            // "user_country" setting
            if (sectionTable.ContainsKey("user_country"))
            {
                Log.Information($"user_country - {CmbUserCountry.SelectedItem}");
                if (countryIdMap.ContainsValue(CmbUserCountry.SelectedItem.ToString()))
                {
                    int? key = countryIdMap.FirstOrDefault(x => x.Value == CmbUserCountry.SelectedItem.ToString()).Key;

                    if (key.HasValue)
                    {
                        sectionTable["user_country"] = key;
                    }
                    else
                    {
                        Log.Error("There was an error while parsing the key from `user_country` setting");
                        Log.Error("Setting the country to US");
                        sectionTable["user_country"] = 103;
                    }
                }
            }

            // "user_language" setting
            if (sectionTable.ContainsKey("user_language"))
            {
                Log.Information($"user_language - {(CmbUserLanguage.SelectedItem as ComboBoxItem).Content}");
                if (CmbUserLanguage.SelectedItem is ComboBoxItem selectedItem)
                {
                    if (languageMap.TryGetValue(selectedItem.Content.ToString(), out int languageNumber))
                    {
                        sectionTable["user_language"] = languageNumber;
                    }
                }
            }
        }
    }
}