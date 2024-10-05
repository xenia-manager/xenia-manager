using System;
using System.Net;
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
        /// Loads the Live Settings into the UI
        /// </summary>
        /// <param name="sectionTable">Portion of .toml file dedicated to Live Settings</param>
        private void LoadLiveSettings(TomlTable sectionTable)
        {
            // Showing Netplay settings
            NetplaySettings.Visibility = Visibility.Visible;
            NetplaySettings.Tag = null;

            // "api_list" setting
            if (sectionTable.ContainsKey("api_list"))
            {
                Log.Information($"api_list - {sectionTable["api_list"]}");
                cmbApiAddress.Items.Clear();
                string[] split = sectionTable["api_list"].ToString().Split(',');
                foreach (string apiAddress in split)
                {
                    if (apiAddress != "")
                    {
                        cmbApiAddress.Items.Add(apiAddress);
                    }
                }
            }

            // "api_address" setting
            if (sectionTable.ContainsKey("api_address"))
            {
                Log.Information($"api_address - {sectionTable["api_address"]}");
                // Looking for the current API Address
                if (cmbApiAddress.Items.Contains(sectionTable["api_address"].ToString()))
                {
                    cmbApiAddress.SelectedItem = sectionTable["api_address"].ToString();
                }
                else
                {
                    cmbApiAddress.Items.Add(sectionTable["api_address"].ToString());
                    cmbApiAddress.SelectedItem = sectionTable["api_address"].ToString();
                }
            }

            // "upnp" setting
            if (sectionTable.ContainsKey("upnp"))
            {
                Log.Information($"upnp - {(bool)sectionTable["upnp"]}");
                chkUPnP.IsChecked = (bool)sectionTable["upnp"];
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
                string selectedItem = cmbApiAddress.Items.Cast<string>().FirstOrDefault(item => item == cmbApiAddress.Text);
                Log.Information($"api_address - {selectedItem}");
                if (selectedItem != null)
                {
                    // Text is one of the items in the ItemsSource
                    sectionTable["api_address"] = selectedItem;
                }
                else
                {
                    // Text is not in the ItemsSource
                    sectionTable["api_address"] = cmbApiAddress.Text;
                }
            }

            // "upnp" setting
            if (sectionTable.ContainsKey("upnp"))
            {
                Log.Information($"upnp - {chkUPnP.IsChecked}");
                sectionTable["upnp"] = chkUPnP.IsChecked;
            }
        }
    }
}