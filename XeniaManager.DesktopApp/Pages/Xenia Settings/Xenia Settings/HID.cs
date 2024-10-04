using System;
using System.Windows;
using System.Windows.Automation;
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
        /// Loads the HID Settings into the UI
        /// </summary>
        /// <param name="sectionTable">Portion of .toml file dedicated to HID Settings</param>
        private void LoadHIDSettings(TomlTable sectionTable)
        {
            // "hid" setting
            Log.Information($"hid - {sectionTable["hid"] as string}");
            switch (sectionTable["hid"] as string)
            {
                case "sdl":
                    cmbInputSystem.SelectedIndex = 1;
                    break;
                case "xinput":
                    cmbInputSystem.SelectedIndex = 2;
                    break;
                case "winkey":
                    cmbInputSystem.SelectedIndex = 3;
                    break;
                default:
                    cmbInputSystem.SelectedIndex = 0;
                    break;
            }

            // "left_stick_deadzone_percentage" setting
            if (sectionTable.ContainsKey("left_stick_deadzone_percentage"))
            {
                Log.Information($"left_stick_deadzone_percentage - {double.Parse(sectionTable["left_stick_deadzone_percentage"].ToString())}");
                sldLeftStickDeadzone.Value = Math.Round(double.Parse(sectionTable["left_stick_deadzone_percentage"].ToString()) * 10, 1);
                AutomationProperties.SetName(sldLeftStickDeadzone, $"Left Stick Deadzone Percentage: {Math.Round((sldLeftStickDeadzone.Value / 10), 1)}");
            }

            // "right_stick_deadzone_percentage" setting
            if (sectionTable.ContainsKey("right_stick_deadzone_percentage"))
            {
                Log.Information($"right_stick_deadzone_percentage - {double.Parse(sectionTable["right_stick_deadzone_percentage"].ToString())}");
                sldRightStickDeadzone.Value = Math.Round(double.Parse(sectionTable["right_stick_deadzone_percentage"].ToString()) * 10, 1);
                AutomationProperties.SetName(sldRightStickDeadzone, $"Right Stick Deadzone Percentage: {Math.Round((sldRightStickDeadzone.Value / 10), 1)}");
            }

            // "vibration" setting
            if (sectionTable.ContainsKey("vibration"))
            {
                Log.Information($"vibration - {(bool)sectionTable["vibration"]}");
                chkControllerVibration.IsChecked = (bool)sectionTable["vibration"];
            }
        }
    }
}