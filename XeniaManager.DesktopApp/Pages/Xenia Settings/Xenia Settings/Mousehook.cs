﻿using System.Windows;
using System.Windows.Automation;

// Imported
using Serilog;
using Tomlyn.Model;

namespace XeniaManager.DesktopApp.Pages
{
    public partial class XeniaSettings
    {
        /// <summary>
        /// Loads the Mousehook Settings into the UI
        /// </summary>
        /// <param name="sectionTable">Portion of .toml file dedicated to Mousehook Settings</param>
        private void LoadMousehookSettings(TomlTable sectionTable)
        {
            // Showing Mousehook settings
            SpMousehookSettings.Visibility = Visibility.Visible;
            SpMousehookSettings.Tag = null;

            // "disable_autoaim" setting
            if (sectionTable.ContainsKey("disable_autoaim"))
            {
                Log.Information($"disable_autoaim - {(bool)sectionTable["disable_autoaim"]}");
                ChkDisableAutoAim.IsChecked = (bool)sectionTable["disable_autoaim"];
                
                BrdDisableAutoAimSetting.Visibility = Visibility.Visible;
                BrdDisableAutoAimSetting.Tag = null;
            }
            else
            {
                Log.Warning("`disable_autoaim` is missing from configuration file");
                BrdDisableAutoAimSetting.Visibility = Visibility.Collapsed;
                BrdDisableAutoAimSetting.Tag = "Ignore";
            }

            // "fov_sensitivity" setting
            if (sectionTable.ContainsKey("fov_sensitivity"))
            {
                Log.Information($"fov_sensitivity - {sectionTable["fov_sensitivity"]}");
                SldFovSensitivity.Value = double.Parse(sectionTable["fov_sensitivity"].ToString()) * 10;
                AutomationProperties.SetName(SldFovSensitivity, $"FOV Sensitivity: {SldFovSensitivity.Value}");
                
                BrdFovSensitivitySetting.Visibility = Visibility.Visible;
                BrdFovSensitivitySetting.Tag = null;
            }
            else
            {
                Log.Warning("`fov_sensitivity` is missing from configuration file");
                BrdFovSensitivitySetting.Visibility = Visibility.Collapsed;
                BrdFovSensitivitySetting.Tag = "Ignore";
            }

            // "ge_aim_turn_distance" setting
            if (sectionTable.ContainsKey("ge_aim_turn_distance"))
            {
                Log.Information($"ge_aim_turn_distance - {sectionTable["ge_aim_turn_distance"]}");
                SldAimTurnDistance.Value = double.Parse(sectionTable["ge_aim_turn_distance"].ToString()) * 1000;
                AutomationProperties.SetName(SldAimTurnDistance, $"Aim Turn Distance: {SldAimTurnDistance.Value}");
                
                BrdAimTurnDistanceSetting.Visibility = Visibility.Visible;
                BrdAimTurnDistanceSetting.Tag = null;
            }
            else
            {
                Log.Warning("`ge_aim_turn_distance` is missing from configuration file");
                BrdAimTurnDistanceSetting.Visibility = Visibility.Collapsed;
                BrdAimTurnDistanceSetting.Tag = "Ignore";
            }

            // "ge_debug_menu" setting
            if (sectionTable.ContainsKey("ge_debug_menu"))
            {
                Log.Information($"ge_debug_menu - {(bool)sectionTable["ge_debug_menu"]}");
                ChkGoldenEyeDebugMenu.IsChecked = (bool)sectionTable["ge_debug_menu"];
                
                BrdGoldenEyeDebugMenuSetting.Visibility = Visibility.Visible;
                BrdGoldenEyeDebugMenuSetting.Tag = null;
            }
            else
            {
                Log.Warning("`ge_debug_menu` is missing from configuration file");
                BrdGoldenEyeDebugMenuSetting.Visibility = Visibility.Collapsed;
                BrdGoldenEyeDebugMenuSetting.Tag = "Ignore";
            }

            // "ge_gun_sway" setting
            if (sectionTable.ContainsKey("ge_gun_sway"))
            {
                Log.Information($"ge_gun_sway - {(bool)sectionTable["ge_gun_sway"]}");
                ChkGunSway.IsChecked = (bool)sectionTable["ge_gun_sway"];
                
                BrdGunSwaySetting.Visibility = Visibility.Visible;
                BrdGunSwaySetting.Tag = null;
            }
            else
            {
                Log.Warning("˙ge_gun_sway` is missing from configuration file");
                BrdGunSwaySetting.Visibility = Visibility.Collapsed;
                BrdGunSwaySetting.Tag = "Ignore";
            }

            // "ge_remove_blur" setting
            if (sectionTable.ContainsKey("ge_remove_blur"))
            {
                Log.Information($"ge_remove_blur - {(bool)sectionTable["ge_remove_blur"]}");
                ChkGoldenEyeRemoveBlur.IsChecked = (bool)sectionTable["ge_remove_blur"];
                
                BrdGoldenEyeRemoveBlurSetting.Visibility = Visibility.Visible;
                BrdGoldenEyeRemoveBlurSetting.Tag = null;
            }
            else
            {
                Log.Warning("`ge_remove_blur` is missing from configuration file");
                BrdGoldenEyeRemoveBlurSetting.Visibility = Visibility.Collapsed;
                BrdGoldenEyeRemoveBlurSetting.Tag = "Ignore";
            }

            // "invert_x" setting
            if (sectionTable.ContainsKey("invert_x"))
            {
                Log.Information($"invert_x - {(bool)sectionTable["invert_x"]}");
                ChkInvertXAxis.IsChecked = (bool)sectionTable["invert_x"];
                
                BrdInvertXAxisSetting.Visibility = Visibility.Visible;
                BrdInvertXAxisSetting.Tag = null;
            }
            else
            {
                Log.Warning("`invert_x` is missing from configuration file");
                BrdInvertXAxisSetting.Visibility = Visibility.Collapsed;
                BrdInvertXAxisSetting.Tag = "Ignore";
            }

            // "invert_y" setting
            if (sectionTable.ContainsKey("invert_y"))
            {
                Log.Information($"invert_y - {(bool)sectionTable["invert_y"]}");
                ChkInvertYAxis.IsChecked = (bool)sectionTable["invert_y"];
                
                BrdInvertYAxisSetting.Visibility = Visibility.Visible;
                BrdInvertYAxisSetting.Tag = null;
            }
            else
            {
                Log.Warning("`invert_y` is missing from configuration file");
                BrdInvertYAxisSetting.Visibility = Visibility.Collapsed;
                BrdInvertYAxisSetting.Tag = "Ignore";
            }
            
            // "menu_sensitivity" setting
            if (sectionTable.ContainsKey("menu_sensitivity"))
            {
                Log.Information($"menu_sensitivity - {sectionTable["menu_sensitivity"]}");
                SldMenuSensitivity.Value = double.Parse(sectionTable["menu_sensitivity"].ToString()) * 10;
                AutomationProperties.SetName(SldMenuSensitivity,
                    $"Menu Sensitivity: {SldMenuSensitivity.Value}");
                
                BrdMenuSensitivitySetting.Visibility = Visibility.Visible;
                BrdMenuSensitivitySetting.Tag = null;
            }
            else
            {
                Log.Warning("`menu_sensitivity` is missing from configuration file");
                BrdMenuSensitivitySetting.Visibility = Visibility.Collapsed;
                BrdMenuSensitivitySetting.Tag = "Ignore";
            }

            // "rdr_snappy_wheel" setting
            if (sectionTable.ContainsKey("rdr_snappy_wheel"))
            {
                Log.Information($"rdr_snappy_wheel - {(bool)sectionTable["rdr_snappy_wheel"]}");
                ChkRdrSnappyWheel.IsChecked = (bool)sectionTable["rdr_snappy_wheel"];
                
                BrdRdrSnappyWheelSetting.Visibility = Visibility.Visible;
                BrdRdrSnappyWheelSetting.Tag = null;
            }
            else
            {
                Log.Warning("`rdr_snappy_wheel` is missing from configuration file");
                BrdRdrSnappyWheelSetting.Visibility = Visibility.Collapsed;
                BrdRdrSnappyWheelSetting.Tag = "Ignore";
            }

            // "rdr_turbo_gallop_horse" setting
            if (sectionTable.ContainsKey("rdr_turbo_gallop_horse"))
            {
                Log.Information($"rdr_turbo_gallop_horse - {(bool)sectionTable["rdr_turbo_gallop_horse"]}");
                ChkRdrTurboGallopHorse.IsChecked = (bool)sectionTable["rdr_turbo_gallop_horse"];
                
                BrdRdrTurboGallopHorseSetting.Visibility = Visibility.Visible;
                BrdRdrTurboGallopHorseSetting.Tag = null;
            }
            else
            {
                Log.Warning("`rdr_turbo_gallop_horse` is missing from configuration file");
                BrdRdrTurboGallopHorseSetting.Visibility = Visibility.Collapsed;
                BrdRdrTurboGallopHorseSetting.Tag = "Ignore";
            }

            // "sensitivity" setting
            if (sectionTable.ContainsKey("sensitivity"))
            {
                Log.Information($"sensitivity - {sectionTable["sensitivity"]}");
                SldMouseSensitivity.Value = double.Parse(sectionTable["sensitivity"].ToString()) * 10;
                AutomationProperties.SetName(SldMouseSensitivity, $"Mouse Sensitivity: {SldMouseSensitivity.Value}");
                
                BrdMouseSensitivitySetting.Visibility = Visibility.Visible;
                BrdMouseSensitivitySetting.Tag = null;
            }
            else
            {
                Log.Warning("`sensitivity` is missing from configuration file");
                BrdMouseSensitivitySetting.Visibility = Visibility.Collapsed;
                BrdMouseSensitivitySetting.Tag = "Ignore";
            }

            // "sr2_better_drive_cam" setting
            if (sectionTable.ContainsKey("sr2_better_drive_cam"))
            {
                Log.Information($"sr2_better_drive_cam - {(bool)sectionTable["sr2_better_drive_cam"]}");
                ChkSaintsRow2BetterDriveCam.IsChecked = (bool)sectionTable["sr2_better_drive_cam"];
                
                BrdSaintsRow2BetterDriveCamSetting.Visibility = Visibility.Visible;
                BrdSaintsRow2BetterDriveCamSetting.Tag = null;
            }
            else
            {
                Log.Warning("`sr2_better_drive_cam` is missing from configuration file");
                BrdSaintsRow2BetterDriveCamSetting.Visibility = Visibility.Collapsed;
                BrdSaintsRow2BetterDriveCamSetting.Tag = "Ignore";
            }

            // "sr2_better_handbrake_cam" setting
            if (sectionTable.ContainsKey("sr2_better_handbrake_cam"))
            {
                Log.Information($"sr2_better_handbrake_cam - {(bool)sectionTable["sr2_better_handbrake_cam"]}");
                ChkSaintsRow2BetterHandbrakeCam.IsChecked = (bool)sectionTable["sr2_better_handbrake_cam"];
                
                BrdSaintsRow2BetterHandbrakeCamSetting.Visibility = Visibility.Visible;
                BrdSaintsRow2BetterHandbrakeCamSetting.Tag = null;
            }
            else
            {
                Log.Warning("`sr2_better_handbrake_cam` is missing from configuration file");
                BrdSaintsRow2BetterHandbrakeCamSetting.Visibility = Visibility.Collapsed;
                BrdSaintsRow2BetterHandbrakeCamSetting.Tag = "Ignore";
            }
            
            // "sr2_hold_fine_aim" setting
            if (sectionTable.ContainsKey("sr2_hold_fine_aim"))
            {
                Log.Information($"sr2_hold_fine_aim - {(bool)sectionTable["sr2_hold_fine_aim"]}");
                ChkSaintsRow2HoldFineAim.IsChecked = (bool)sectionTable["sr2_hold_fine_aim"];
                
                BrdSaintsRow2HoldFineAimSetting.Visibility = Visibility.Visible;
                BrdSaintsRow2HoldFineAimSetting.Tag = null;
            }
            else
            {
                Log.Warning("`sr2_hold_fine_aim` is missing from configuration file");
                BrdSaintsRow2HoldFineAimSetting.Visibility = Visibility.Collapsed;
                BrdSaintsRow2HoldFineAimSetting.Tag = "Ignore";
            }
        }

        /// <summary>
        /// Saves the Mousehook Settings into the configuration file
        /// </summary>
        /// <param name="sectionTable">Portion of .toml file dedicated to Mousehook Settings</param>
        private void SaveMousehookSettings(TomlTable sectionTable)
        {
            // "disable_autoaim" setting
            if (sectionTable.ContainsKey("disable_autoaim"))
            {
                Log.Information($"disable_autoaim - {ChkDisableAutoAim.IsChecked}");
                sectionTable["disable_autoaim"] = ChkDisableAutoAim.IsChecked;
            }

            // "fov_sensitivity" setting
            if (sectionTable.ContainsKey("fov_sensitivity"))
            {
                Log.Information($"fov_sensitivity - {Math.Round(SldFovSensitivity.Value / 10, 1)}");
                sectionTable["fov_sensitivity"] = Math.Round(SldFovSensitivity.Value / 10, 1);
            }

            // "ge_aim_turn_distance" setting
            if (sectionTable.ContainsKey("ge_aim_turn_distance"))
            {
                Log.Information($"ge_aim_turn_distance - {Math.Round(SldAimTurnDistance.Value / 1000, 3)}");
                sectionTable["ge_aim_turn_distance"] = Math.Round(SldAimTurnDistance.Value / 1000, 3);
            }

            // "ge_debug_menu" setting
            if (sectionTable.ContainsKey("ge_debug_menu"))
            {
                Log.Information($"ge_debug_menu - {ChkGoldenEyeDebugMenu.IsChecked}");
                sectionTable["ge_debug_menu"] = ChkGoldenEyeDebugMenu.IsChecked;
            }

            // "ge_gun_sway" setting
            if (sectionTable.ContainsKey("ge_gun_sway"))
            {
                Log.Information($"ge_gun_sway - {ChkGunSway.IsChecked}");
                sectionTable["ge_gun_sway"] = ChkGunSway.IsChecked;
            }
            
            // "ge_remove_blur" setting
            if (sectionTable.ContainsKey("ge_remove_blur"))
            {
                Log.Information($"ge_remove_blur - {ChkGoldenEyeRemoveBlur.IsChecked}");
                sectionTable["ge_remove_blur"] = ChkGoldenEyeRemoveBlur.IsChecked;
            }

            // "invert_x" setting
            if (sectionTable.ContainsKey("invert_x"))
            {
                Log.Information($"invert_x - {ChkInvertXAxis.IsChecked}");
                sectionTable["invert_x"] = ChkInvertXAxis.IsChecked;
            }

            // "invert_y" setting
            if (sectionTable.ContainsKey("invert_y"))
            {
                Log.Information($"invert_y - {ChkInvertYAxis.IsChecked}");
                sectionTable["invert_y"] = ChkInvertYAxis.IsChecked;
            }
            
            // "menu_sensitivity" setting
            if (sectionTable.ContainsKey("menu_sensitivity"))
            {
                Log.Information($"menu_sensitivity - {Math.Round(SldMenuSensitivity.Value / 10, 1)}");
                sectionTable["menu_sensitivity"] = Math.Round(SldMenuSensitivity.Value / 10, 1);
            }

            // "rdr_snappy_wheel" setting
            if (sectionTable.ContainsKey("rdr_snappy_wheel"))
            {
                Log.Information($"rdr_snappy_wheel - {ChkRdrSnappyWheel.IsChecked}");
                sectionTable["rdr_snappy_wheel"] = ChkRdrSnappyWheel.IsChecked;
            }

            // "rdr_turbo_gallop_horse" setting
            if (sectionTable.ContainsKey("rdr_turbo_gallop_horse"))
            {
                Log.Information($"rdr_turbo_gallop_horse - {ChkRdrTurboGallopHorse.IsChecked}");
                sectionTable["rdr_turbo_gallop_horse"] = ChkRdrTurboGallopHorse.IsChecked;
            }

            // "sensitivity" setting
            if (sectionTable.ContainsKey("sensitivity"))
            {
                Log.Information($"sensitivity - {Math.Round(SldMouseSensitivity.Value / 10, 1)}");
                sectionTable["sensitivity"] = Math.Round(SldMouseSensitivity.Value / 10, 1);
            }

            // "sr2_better_drive_cam" setting
            if (sectionTable.ContainsKey("sr2_better_drive_cam"))
            {
                Log.Information($"sr2_better_drive_cam - {ChkSaintsRow2BetterDriveCam.IsChecked}");
                sectionTable["sr2_better_drive_cam"] = ChkSaintsRow2BetterDriveCam.IsChecked;
            }

            // "sr2_better_handbrake_cam" setting
            if (sectionTable.ContainsKey("sr2_better_handbrake_cam"))
            {
                Log.Information($"sr2_better_handbrake_cam - {ChkSaintsRow2BetterHandbrakeCam.IsChecked}");
                sectionTable["sr2_better_handbrake_cam"] = ChkSaintsRow2BetterHandbrakeCam.IsChecked;
            }
            
            // "sr2_hold_fine_aim" setting
            if (sectionTable.ContainsKey("sr2_hold_fine_aim"))
            {
                Log.Information($"sr2_hold_fine_aim - {ChkSaintsRow2HoldFineAim.IsChecked}");
                sectionTable["sr2_hold_fine_aim"] = ChkSaintsRow2HoldFineAim.IsChecked;
            }
        }
    }
}