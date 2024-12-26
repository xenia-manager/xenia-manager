using System.Windows;
using System.Windows.Automation;
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
        /// Loads the HID Settings into the UI
        /// </summary>
        /// <param name="sectionTable">Portion of .toml file dedicated to HID Settings</param>
        private void LoadHidSettings(TomlTable sectionTable)
        {
            // "hid" setting
            if (sectionTable.ContainsKey("hid"))
            {
                Log.Information($"hid - {sectionTable["hid"] as string}");
                switch (sectionTable["hid"] as string)
                {
                    case "sdl":
                        CmbInputSystem.SelectedIndex = 1;
                        break;
                    case "xinput":
                        CmbInputSystem.SelectedIndex = 2;
                        break;
                    case "winkey":
                        CmbInputSystem.SelectedIndex = 3;
                        break;
                    default:
                        CmbInputSystem.SelectedIndex = 0;
                        break;
                }
                
                BrdInputSystemSetting.Visibility = Visibility.Visible;
                BrdInputSystemSetting.Tag = null;
            }
            else
            {
                Log.Warning("`hid` is missing from configuration file");
                BrdInputSystemSetting.Visibility = Visibility.Collapsed;
                BrdInputSystemSetting.Tag = "Ignore";
            }

            // "left_stick_deadzone_percentage" setting
            if (sectionTable.ContainsKey("left_stick_deadzone_percentage"))
            {
                Log.Information($"left_stick_deadzone_percentage - {double.Parse(sectionTable["left_stick_deadzone_percentage"].ToString())}");
                SldLeftStickDeadzone.Value = Math.Round(double.Parse(sectionTable["left_stick_deadzone_percentage"].ToString()) * 10, 1);
                AutomationProperties.SetName(SldLeftStickDeadzone, $"Left Stick Deadzone Percentage: {Math.Round((SldLeftStickDeadzone.Value / 10), 1)}");
                
                BrdLeftStickDeadzoneSetting.Visibility = Visibility.Visible;
                BrdLeftStickDeadzoneSetting.Tag = null;
            }
            else
            {
                Log.Warning("`left_stick_deadzone_percentage is missing from configuration file");
                BrdLeftStickDeadzoneSetting.Visibility = Visibility.Collapsed;
                BrdLeftStickDeadzoneSetting.Tag = "Ignore";
            }

            // "right_stick_deadzone_percentage" setting
            if (sectionTable.ContainsKey("right_stick_deadzone_percentage"))
            {
                Log.Information($"right_stick_deadzone_percentage - {double.Parse(sectionTable["right_stick_deadzone_percentage"].ToString())}");
                SldRightStickDeadzone.Value = Math.Round(double.Parse(sectionTable["right_stick_deadzone_percentage"].ToString()) * 10, 1);
                AutomationProperties.SetName(SldRightStickDeadzone, $"Right Stick Deadzone Percentage: {Math.Round((SldRightStickDeadzone.Value / 10), 1)}");
                
                BrdRightStickDeadzoneSetting.Visibility = Visibility.Visible;
                BrdRightStickDeadzoneSetting.Tag = null;
            }
            else
            {
                Log.Warning("`right_stick_deadzone_percentage is missing from configuration file");
                BrdRightStickDeadzoneSetting.Visibility = Visibility.Collapsed;
                BrdRightStickDeadzoneSetting.Tag = "Ignore";
            }

            // "vibration" setting
            if (sectionTable.ContainsKey("vibration"))
            {
                Log.Information($"vibration - {(bool)sectionTable["vibration"]}");
                ChkControllerVibration.IsChecked = (bool)sectionTable["vibration"];
                
                BrdControllerVibrationSetting.Visibility = Visibility.Visible;
                BrdControllerHotkeysSetting.Tag = null;
            }
            else
            {
                Log.Warning("`vibration` is missing from configuration file");
                BrdControllerVibrationSetting.Visibility = Visibility.Collapsed;
                BrdControllerHotkeysSetting.Tag = "Ignore";
            }
        }

        /// <summary>
        /// Saves the HID Settings into the configuration file
        /// </summary>
        /// <param name="sectionTable">Portion of .toml file dedicated to HID Settings</param>
        private void SaveHidSettings(TomlTable sectionTable)
        {
            // "hid" setting
            if (sectionTable.ContainsKey("hid"))
            {
                Log.Information($"hid - {(CmbInputSystem.SelectedItem as ComboBoxItem).Content}");
                switch (CmbInputSystem.SelectedIndex)
                {
                    case 1:
                        // "sdl"
                        sectionTable["hid"] = "sdl";
                        break;
                    case 2:
                        // "xinput"
                        sectionTable["hid"] = "xinput";
                        break;
                    case 3:
                        // "winkey"
                        sectionTable["hid"] = "winkey";
                        break;
                    default:
                        // "any"
                        sectionTable["hid"] = "any";
                        break;
                }
            }

            // "left_stick_deadzone_percentage" setting
            if (sectionTable.ContainsKey("left_stick_deadzone_percentage"))
            {
                Log.Information($"left_stick_deadzone_percentage - {Math.Round(SldLeftStickDeadzone.Value / 10, 1)}");
                if ((SldLeftStickDeadzone.Value / 10) == 0 || (SldLeftStickDeadzone.Value / 10) == 1)
                {
                    sectionTable["left_stick_deadzone_percentage"] = (int)(SldLeftStickDeadzone.Value / 10);
                }
                else
                {
                    sectionTable["left_stick_deadzone_percentage"] = Math.Round(SldLeftStickDeadzone.Value / 10, 1);
                }
            }

            // "right_stick_deadzone_percentage" setting
            if (sectionTable.ContainsKey("right_stick_deadzone_percentage"))
            {
                Log.Information($"right_stick_deadzone_percentage - {Math.Round(SldRightStickDeadzone.Value, 1)}");
                if ((SldRightStickDeadzone.Value / 10) == 0 || (SldRightStickDeadzone.Value / 10) == 1)
                {
                    sectionTable["right_stick_deadzone_percentage"] = (int)(SldRightStickDeadzone.Value / 10);
                }
                else
                {
                    sectionTable["right_stick_deadzone_percentage"] = Math.Round(SldRightStickDeadzone.Value / 10, 1);
                }
            }

            // "vibration" setting
            if (sectionTable.ContainsKey("vibration"))
            {
                Log.Information($"vibration - {ChkControllerVibration.IsChecked}");
                sectionTable["vibration"] = ChkControllerVibration.IsChecked;
            }
        }
    }
}