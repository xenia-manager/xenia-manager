﻿using System.Windows;

// Imported
using Serilog;
using Tomlyn.Model;

namespace XeniaManager.DesktopApp.Pages
{
    public partial class XeniaSettings
    {
        // Functions for loading Settings into the UI
        /// <summary>
        /// Loads the Live Settings into the UI
        /// </summary>
        /// <param name="sectionTable">Portion of .toml file dedicated to Live Settings</param>
        private void LoadLiveSettings(TomlTable sectionTable)
        {
            // Showing Netplay settings
            SpNetplaySettings.Visibility = Visibility.Visible;
            SpNetplaySettings.Tag = null;

            // "api_list" setting
            if (sectionTable.ContainsKey("api_list"))
            {
                Log.Information($"api_list - {sectionTable["api_list"]}");
                CmbApiAddress.Items.Clear();
                string[] split = sectionTable["api_list"].ToString().Split(',');
                foreach (string apiAddress in split)
                {
                    if (apiAddress != "")
                    {
                        CmbApiAddress.Items.Add(apiAddress);
                    }
                }
                
                BrdNetplayApiAddressSetting.Visibility = Visibility.Visible;
                BrdNetplayApiAddressSetting.Tag = null;
            }
            else
            {
                Log.Warning("`api_list` is missing from configuration file");
                BrdNetplayApiAddressSetting.Visibility = Visibility.Collapsed;
                BrdNetplayApiAddressSetting.Tag = "Ignore";
            }

            // "api_address" setting
            if (sectionTable.ContainsKey("api_address"))
            {
                Log.Information($"api_address - {sectionTable["api_address"]}");
                // Looking for the current API Address
                if (CmbApiAddress.Items.Contains(sectionTable["api_address"].ToString()))
                {
                    CmbApiAddress.SelectedItem = sectionTable["api_address"].ToString();
                }
                else
                {
                    CmbApiAddress.Items.Add(sectionTable["api_address"].ToString());
                    CmbApiAddress.SelectedItem = sectionTable["api_address"].ToString();
                }
            }
            else
            {
                Log.Warning("`api_address` is missing from configuration file");
            }

            // "upnp" setting
            if (sectionTable.ContainsKey("upnp"))
            {
                Log.Information($"upnp - {(bool)sectionTable["upnp"]}");
                ChkUPnP.IsChecked = (bool)sectionTable["upnp"];
                
                BrdNetplayUPnPSetting.Visibility = Visibility.Visible;
                BrdNetplayUPnPSetting.Tag = null;
            }
            else
            {
                Log.Warning("`upnp` is missing from configuration file");
                BrdNetplayUPnPSetting.Visibility = Visibility.Collapsed;
                BrdNetplayUPnPSetting.Tag = "Ignore";
            }
        }

        /// <summary>
        /// Saves the Live Settings into the configuration file
        /// </summary>
        /// <param name="sectionTable">Portion of .toml file dedicated to Live Settings</param>
        private void SaveLiveSettings(TomlTable sectionTable)
        {
            // "api_address" setting
            if (sectionTable.ContainsKey("api_address"))
            {
                string selectedItem = CmbApiAddress.Items.Cast<string>()
                    .FirstOrDefault(item => item == CmbApiAddress.Text);
                Log.Information($"api_address - {selectedItem}");
                if (selectedItem != null)
                {
                    // Text is one of the items in the ItemsSource
                    sectionTable["api_address"] = selectedItem;
                }
                else
                {
                    // Text is not in the ItemsSource
                    sectionTable["api_address"] = CmbApiAddress.Text;
                }
            }

            // "upnp" setting
            if (sectionTable.ContainsKey("upnp"))
            {
                Log.Information($"upnp - {ChkUPnP.IsChecked}");
                sectionTable["upnp"] = ChkUPnP.IsChecked;
            }
        }
    }
}