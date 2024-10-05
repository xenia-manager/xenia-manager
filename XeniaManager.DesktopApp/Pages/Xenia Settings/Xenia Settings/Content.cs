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
        /// <summary>
        /// Loads the Content Settings into the UI
        /// </summary>
        /// <param name="sectionTable">Portion of .toml file dedicated to Content Settings</param>
        private void LoadContentSettings(TomlTable sectionTable)
        {
            // "license_mask" setting
            if (sectionTable.ContainsKey("license_mask"))
            {
                Log.Information($"license_mask - {int.Parse(sectionTable["license_mask"].ToString())}");
                switch (int.Parse(sectionTable["license_mask"].ToString()))
                {
                    case -1:
                        // All Licenses
                        cmbLicenseMask.SelectedIndex = 2;
                        break;
                    case 0:
                        // No License
                        cmbLicenseMask.SelectedIndex = 0;
                        break;
                    case 1:
                        // First License
                        cmbLicenseMask.SelectedIndex = 1;
                        break;
                    default:
                        break;
                }
            }
        }

        /// <summary>
        /// Saves the Content Settings into the configuration file
        /// </summary>
        /// <param name="sectionTable">Portion of .toml file dedicated to Content Settings</param>
        private void SaveContentSettings(TomlTable sectionTable)
        {
            // "license_mask" setting
            if (sectionTable.ContainsKey("license_mask"))
            {
                if (cmbLicenseMask.SelectedItem is ComboBoxItem selectedItem)
                {
                    Log.Information($"license_mask - {selectedItem.Content}");
                    switch (selectedItem.Content)
                    {
                        case "No Licenses":
                            sectionTable["license_mask"] = 0;
                            break;
                        case "First License":
                            sectionTable["license_mask"] = 1;
                            break;
                        case "All Licenses":
                            sectionTable["license_mask"] = -1;
                            break;
                        default:
                            sectionTable["license_mask"] = 0;
                            break;
                    }
                }
            }
        }
    }
}