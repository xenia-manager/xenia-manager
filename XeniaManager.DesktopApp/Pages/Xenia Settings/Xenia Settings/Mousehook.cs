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
        /// <summary>
        /// Loads the Mousehook Settings into the UI
        /// </summary>
        /// <param name="sectionTable">Portion of .toml file dedicated to Mousehook Settings</param>
        private void LoadMousehookSettings(TomlTable sectionTable)
        {
            // "disable_autoaim" setting
            if (sectionTable.ContainsKey("disable_autoaim"))
            {
                Log.Information($"disable_autoaim - {(bool)sectionTable["disable_autoaim"]}");
                chkDisableAutoAim.IsChecked = (bool)sectionTable["disable_autoaim"];
            }

            // "fov_sensitivity" setting
            if (sectionTable.ContainsKey("fov_sensitivity"))
            {
                Log.Information($"fov_sensitivity - {sectionTable["fov_sensitivity"].ToString()}");
                sldFOVSensitivity.Value = double.Parse(sectionTable["fov_sensitivity"].ToString()) * 10;
                AutomationProperties.SetName(sldFOVSensitivity, $"FOV Sensitivity: {sldFOVSensitivity.Value}");
            }

            // "ge_aim_turn_distance" setting
            if (sectionTable.ContainsKey("ge_aim_turn_distance"))
            {
                Log.Information($"ge_aim_turn_distance - {sectionTable["ge_aim_turn_distance"].ToString()}");
                sldAimTurnDistance.Value = double.Parse(sectionTable["ge_aim_turn_distance"].ToString()) * 1000;
                AutomationProperties.SetName(sldAimTurnDistance, $"Aim Turn Distance: {sldAimTurnDistance.Value}");
            }

            // "ge_debug_menu" setting
            if (sectionTable.ContainsKey("ge_debug_menu"))
            {
                Log.Information($"ge_debug_menu - {(bool)sectionTable["ge_debug_menu"]}");
                chkGoldenEyeDebugMenu.IsChecked = (bool)sectionTable["ge_debug_menu"];
            }

            // "ge_gun_sway" setting
            if (sectionTable.ContainsKey("ge_gun_sway"))
            {
                Log.Information($"ge_gun_sway - {(bool)sectionTable["ge_gun_sway"]}");
                chkGunSway.IsChecked = (bool)sectionTable["ge_gun_sway"];
            }

            // "ge_menu_sensitivity" setting
            if (sectionTable.ContainsKey("ge_menu_sensitivity"))
            {
                Log.Information($"ge_menu_sensitivity - {sectionTable["ge_menu_sensitivity"].ToString()}");
                sldGoldenEyeMenuSensitivity.Value = double.Parse(sectionTable["ge_menu_sensitivity"].ToString()) * 10;
                AutomationProperties.SetName(sldGoldenEyeMenuSensitivity, $"GoldenEye Menu Sensitivity: {sldGoldenEyeMenuSensitivity.Value}");
            }

            // "ge_remove_blur" setting
            if (sectionTable.ContainsKey("ge_remove_blur"))
            {
                Log.Information($"ge_remove_blur - {(bool)sectionTable["ge_remove_blur"]}");
                chkGoldenEyeRemoveBlur.IsChecked = (bool)sectionTable["ge_remove_blur"];
            }

            // "invert_x" setting
            if (sectionTable.ContainsKey("invert_x"))
            {
                Log.Information($"invert_x - {(bool)sectionTable["invert_x"]}");
                chkInvertXAxis.IsChecked = (bool)sectionTable["invert_x"];
            }

            // "invert_y" setting
            if (sectionTable.ContainsKey("invert_y"))
            {
                Log.Information($"invert_y - {(bool)sectionTable["invert_y"]}");
                chkInvertYAxis.IsChecked = (bool)sectionTable["invert_y"];
            }

            // "rdr_snappy_wheel" setting
            if (sectionTable.ContainsKey("rdr_snappy_wheel"))
            {
                Log.Information($"rdr_snappy_wheel - {(bool)sectionTable["rdr_snappy_wheel"]}");
                chkRDRSnappyWheel.IsChecked = (bool)sectionTable["rdr_snappy_wheel"];
            }

            // "rdr_turbo_gallop_horse" setting
            if (sectionTable.ContainsKey("rdr_turbo_gallop_horse"))
            {
                Log.Information($"rdr_turbo_gallop_horse - {(bool)sectionTable["rdr_turbo_gallop_horse"]}");
                chkRDRTurboGallopHorse.IsChecked = (bool)sectionTable["rdr_turbo_gallop_horse"];
            }

            // "sensitivity" setting
            if (sectionTable.ContainsKey("sensitivity"))
            {
                Log.Information($"sensitivity - {sectionTable["sensitivity"].ToString()}");
                sldMouseSensitivity.Value = double.Parse(sectionTable["sensitivity"].ToString()) * 10;
                AutomationProperties.SetName(sldMouseSensitivity, $"Mouse Sensitivity: {sldMouseSensitivity.Value}");
            }

            // "sr2_better_drive_cam" setting
            if (sectionTable.ContainsKey("sr2_better_drive_cam"))
            {
                Log.Information($"sr2_better_drive_cam - {(bool)sectionTable["sr2_better_drive_cam"]}");
                chkSaintsRow2BetterDriveCam.IsChecked = (bool)sectionTable["sr2_better_drive_cam"];
            }

            // "sr2_better_handbrake_cam" setting
            if (sectionTable.ContainsKey("sr2_better_handbrake_cam"))
            {
                Log.Information($"sr2_better_handbrake_cam - {(bool)sectionTable["sr2_better_handbrake_cam"]}");
                chkSaintsRow2BetterHandbrakeCam.IsChecked = (bool)sectionTable["sr2_better_handbrake_cam"];
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
                Log.Information($"disable_autoaim - {(bool)sectionTable["disable_autoaim"]}");
                sectionTable["disable_autoaim"] = chkDisableAutoAim.IsChecked;
            }

            // "fov_sensitivity" setting
            if (sectionTable.ContainsKey("fov_sensitivity"))
            {
                Log.Information($"fov_sensitivity - {sectionTable["fov_sensitivity"].ToString()}");
                sectionTable["fov_sensitivity"] = Math.Round(sldFOVSensitivity.Value / 10, 1);
            }

            // "ge_aim_turn_distance" setting
            if (sectionTable.ContainsKey("ge_aim_turn_distance"))
            {
                Log.Information($"ge_aim_turn_distance - {sectionTable["ge_aim_turn_distance"].ToString()}");
                sectionTable["ge_aim_turn_distance"] = Math.Round(sldAimTurnDistance.Value / 1000, 3);
            }

            // "ge_debug_menu" setting
            if (sectionTable.ContainsKey("ge_debug_menu"))
            {
                Log.Information($"ge_debug_menu - {(bool)sectionTable["ge_debug_menu"]}");
                sectionTable["ge_debug_menu"] = chkGoldenEyeDebugMenu.IsChecked;
            }

            // "ge_gun_sway" setting
            if (sectionTable.ContainsKey("ge_gun_sway"))
            {
                Log.Information($"ge_gun_sway - {(bool)sectionTable["ge_gun_sway"]}");
                sectionTable["ge_gun_sway"] = chkGunSway.IsChecked;
            }

            // "ge_menu_sensitivity" setting
            if (sectionTable.ContainsKey("ge_menu_sensitivity"))
            {
                Log.Information($"ge_menu_sensitivity - {sectionTable["ge_menu_sensitivity"].ToString()}");
                sectionTable["ge_menu_sensitivity"] = Math.Round(sldGoldenEyeMenuSensitivity.Value / 10, 1);
            }

            // "ge_remove_blur" setting
            if (sectionTable.ContainsKey("ge_remove_blur"))
            {
                Log.Information($"ge_remove_blur - {(bool)sectionTable["ge_remove_blur"]}");
                sectionTable["ge_remove_blur"] = chkGoldenEyeRemoveBlur.IsChecked;
            }

            // "invert_x" setting
            if (sectionTable.ContainsKey("invert_x"))
            {
                Log.Information($"invert_x - {(bool)sectionTable["invert_x"]}");
                sectionTable["invert_x"] = chkInvertXAxis.IsChecked;
            }

            // "invert_y" setting
            if (sectionTable.ContainsKey("invert_y"))
            {
                Log.Information($"invert_y - {(bool)sectionTable["invert_y"]}");
                sectionTable["invert_y"] = chkInvertYAxis.IsChecked;
            }

            // "rdr_snappy_wheel" setting
            if (sectionTable.ContainsKey("rdr_snappy_wheel"))
            {
                Log.Information($"rdr_snappy_wheel - {(bool)sectionTable["rdr_snappy_wheel"]}");
                sectionTable["rdr_snappy_wheel"] = chkRDRSnappyWheel.IsChecked;
            }

            // "rdr_turbo_gallop_horse" setting
            if (sectionTable.ContainsKey("rdr_turbo_gallop_horse"))
            {
                Log.Information($"rdr_turbo_gallop_horse - {(bool)sectionTable["rdr_turbo_gallop_horse"]}");
                sectionTable["rdr_turbo_gallop_horse"] = chkRDRTurboGallopHorse.IsChecked;
            }

            // "sensitivity" setting
            if (sectionTable.ContainsKey("sensitivity"))
            {
                Log.Information($"sensitivity - {sectionTable["sensitivity"].ToString()}");
                sectionTable["sensitivity"] = Math.Round(sldMouseSensitivity.Value / 10, 1);
            }

            // "sr2_better_drive_cam" setting
            if (sectionTable.ContainsKey("sr2_better_drive_cam"))
            {
                Log.Information($"sr2_better_drive_cam - {(bool)sectionTable["sr2_better_drive_cam"]}");
                sectionTable["sr2_better_drive_cam"] = chkSaintsRow2BetterDriveCam.IsChecked;
            }

            // "sr2_better_handbrake_cam" setting
            if (sectionTable.ContainsKey("sr2_better_handbrake_cam"))
            {
                Log.Information($"sr2_better_handbrake_cam - {(bool)sectionTable["sr2_better_handbrake_cam"]}");
                sectionTable["sr2_better_handbrake_cam"] = chkSaintsRow2BetterHandbrakeCam.IsChecked;
            }
        }
    }
}