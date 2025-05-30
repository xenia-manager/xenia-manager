using System.Windows;
using Tomlyn.Model;
using XeniaManager.Core;

namespace XeniaManager.Desktop.Views.Windows;

public partial class GameSettingsEditor
{
    private void LoadHidSettings(TomlTable hidSection)
    {
        // hid
        if (hidSection.ContainsKey("hid"))
        {
            Logger.Info($"hid - {hidSection["hid"]}");
            BrdInputSystemSetting.Visibility = Visibility.Visible;
            CmbInputSystem.SelectedValue = hidSection["hid"];
            if (CmbInputSystem.SelectedIndex < 0)
            {
                CmbInputSystem.SelectedIndex = 0;
            }
        }
        else
        {
            Logger.Warning("'hid' is missing from the configuration file");
            BrdInputSystemSetting.Visibility = Visibility.Collapsed;
        }

        // keyboard_mode
        if (hidSection.ContainsKey("keyboard_mode"))
        {
            Logger.Info($"keyboard_mode - {hidSection["keyboard_mode"]}");
            BrdKeyboardModeSetting.Visibility = Visibility.Visible;
            try
            {
                CmbKeyboardMode.SelectedIndex = int.Parse(hidSection["keyboard_mode"].ToString());
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                CmbKeyboardMode.SelectedIndex = 0;
            }
        }
        else
        {
            Logger.Warning("'keyboard_mode' is missing from the configuration file");
            BrdKeyboardModeSetting.Visibility = Visibility.Collapsed;
        }

        // keyboard_user_index
        if (hidSection.ContainsKey("keyboard_user_index"))
        {
            Logger.Info($"keyboard_user_index - {hidSection["keyboard_user_index"]}");
            BrdKeyboardUserIndexSetting.Visibility = Visibility.Visible;
            try
            {
                CmbKeyboardUserIndex.SelectedIndex = int.Parse(hidSection["keyboard_user_index"].ToString());
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                CmbKeyboardUserIndex.SelectedIndex = 0;
            }
        }
        else
        {
            Logger.Warning("'keyboard_user_index' is missing from the configuration file");
            BrdKeyboardUserIndexSetting.Visibility = Visibility.Collapsed;
        }
        
        // left_stick_deadzone_percentage
        if (hidSection.ContainsKey("left_stick_deadzone_percentage"))
        {
            Logger.Info($"left_stick_deadzone_percentage - {hidSection["left_stick_deadzone_percentage"]}");
            BrdLeftStickDeadzoneSetting.Visibility = Visibility.Visible;
            try
            {
                SldLeftStickDeadzone.Value = double.Parse(hidSection["left_stick_deadzone_percentage"].ToString()) * 10;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                SldLeftStickDeadzone.Value = 0;
            }
        }
        else
        {
            Logger.Warning("'left_stick_deadzone_percentage' is missing from the configuration file");
            BrdLeftStickDeadzoneSetting.Visibility = Visibility.Collapsed;
        }
        
        // right_stick_deadzone_percentage
        if (hidSection.ContainsKey("right_stick_deadzone_percentage"))
        {
            Logger.Info($"right_stick_deadzone_percentage - {hidSection["right_stick_deadzone_percentage"]}");
            BrdRightStickDeadzoneSetting.Visibility = Visibility.Visible;
            try
            {
                SldRightStickDeadzone.Value = double.Parse(hidSection["right_stick_deadzone_percentage"].ToString()) * 10;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                SldRightStickDeadzone.Value = 0;
            }
        }
        else
        {
            Logger.Warning("'right_stick_deadzone_percentage' is missing from the configuration file");
            BrdRightStickDeadzoneSetting.Visibility = Visibility.Collapsed;
        }
        
        // vibration
        if (hidSection.ContainsKey("vibration"))
        {
            Logger.Info($"vibration - {hidSection["vibration"]}");
            BrdControllerVibrationSetting.Visibility = Visibility.Visible;
            ChkControllerVibration.IsChecked = (bool)hidSection["vibration"];
        }
        else
        {
            Logger.Warning("'vibration' is missing from the configuration file");
            BrdControllerVibrationSetting.Visibility = Visibility.Collapsed;
        }
    }

    private void SaveHidSettings(TomlTable hidSection)
    {
        // hid
        if (hidSection.ContainsKey("hid"))
        {
            Logger.Info($"hid - {CmbInputSystem.SelectedItem}");
            hidSection["hid"] = CmbInputSystem.SelectedValue;
        }

        // keyboard_mode
        if (hidSection.ContainsKey("keyboard_mode"))
        {
            Logger.Info($"keyboard_mode - {CmbKeyboardMode.SelectedIndex}");
            hidSection["keyboard_mode"] = CmbKeyboardMode.SelectedIndex;
        }

        // keyboard_user_index
        if (hidSection.ContainsKey("keyboard_user_index"))
        {
            Logger.Info($"keyboard_user_index - {CmbKeyboardUserIndex.SelectedIndex}");
            hidSection["keyboard_user_index"] = CmbKeyboardUserIndex.SelectedIndex;
        }
        
        // left_stick_deadzone_percentage
        if (hidSection.ContainsKey("left_stick_deadzone_percentage"))
        {
            Logger.Info($"left_stick_deadzone_percentage - {SldLeftStickDeadzone.Value}");
            hidSection["left_stick_deadzone_percentage"] = SldLeftStickDeadzone.Value / 10;
        }
        
        // right_stick_deadzone_percentage
        if (hidSection.ContainsKey("right_stick_deadzone_percentage"))
        {
            Logger.Info($"right_stick_deadzone_percentage - {SldRightStickDeadzone.Value}");
            hidSection["right_stick_deadzone_percentage"] = SldRightStickDeadzone.Value / 10;
        }
        
        // vibration
        if (hidSection.ContainsKey("vibration"))
        {
            Logger.Info($"vibration - {ChkControllerVibration.IsChecked}");
            hidSection["vibration"] = ChkControllerVibration.IsChecked;
        }
    }
}