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
    }
}