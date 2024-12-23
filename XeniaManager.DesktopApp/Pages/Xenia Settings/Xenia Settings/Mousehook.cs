using System.Windows;
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
            }

            // "fov_sensitivity" setting
            if (sectionTable.ContainsKey("fov_sensitivity"))
            {
                Log.Information($"fov_sensitivity - {sectionTable["fov_sensitivity"]}");
                SldFovSensitivity.Value = double.Parse(sectionTable["fov_sensitivity"].ToString()) * 10;
                AutomationProperties.SetName(SldFovSensitivity, $"FOV Sensitivity: {SldFovSensitivity.Value}");
            }

            // "ge_aim_turn_distance" setting
            if (sectionTable.ContainsKey("ge_aim_turn_distance"))
            {
                Log.Information($"ge_aim_turn_distance - {sectionTable["ge_aim_turn_distance"]}");
                SldAimTurnDistance.Value = double.Parse(sectionTable["ge_aim_turn_distance"].ToString()) * 1000;
                AutomationProperties.SetName(SldAimTurnDistance, $"Aim Turn Distance: {SldAimTurnDistance.Value}");
            }

            // "ge_debug_menu" setting
            if (sectionTable.ContainsKey("ge_debug_menu"))
            {
                Log.Information($"ge_debug_menu - {(bool)sectionTable["ge_debug_menu"]}");
                ChkGoldenEyeDebugMenu.IsChecked = (bool)sectionTable["ge_debug_menu"];
            }

            // "ge_gun_sway" setting
            if (sectionTable.ContainsKey("ge_gun_sway"))
            {
                Log.Information($"ge_gun_sway - {(bool)sectionTable["ge_gun_sway"]}");
                ChkGunSway.IsChecked = (bool)sectionTable["ge_gun_sway"];
            }

            // "ge_menu_sensitivity" setting
            if (sectionTable.ContainsKey("ge_menu_sensitivity"))
            {
                Log.Information($"ge_menu_sensitivity - {sectionTable["ge_menu_sensitivity"]}");
                SldGoldenEyeMenuSensitivity.Value = double.Parse(sectionTable["ge_menu_sensitivity"].ToString()) * 10;
                AutomationProperties.SetName(SldGoldenEyeMenuSensitivity,
                    $"GoldenEye Menu Sensitivity: {SldGoldenEyeMenuSensitivity.Value}");
            }

            // "ge_remove_blur" setting
            if (sectionTable.ContainsKey("ge_remove_blur"))
            {
                Log.Information($"ge_remove_blur - {(bool)sectionTable["ge_remove_blur"]}");
                ChkGoldenEyeRemoveBlur.IsChecked = (bool)sectionTable["ge_remove_blur"];
            }

            // "invert_x" setting
            if (sectionTable.ContainsKey("invert_x"))
            {
                Log.Information($"invert_x - {(bool)sectionTable["invert_x"]}");
                ChkInvertXAxis.IsChecked = (bool)sectionTable["invert_x"];
            }

            // "invert_y" setting
            if (sectionTable.ContainsKey("invert_y"))
            {
                Log.Information($"invert_y - {(bool)sectionTable["invert_y"]}");
                ChkInvertYAxis.IsChecked = (bool)sectionTable["invert_y"];
            }

            // "rdr_snappy_wheel" setting
            if (sectionTable.ContainsKey("rdr_snappy_wheel"))
            {
                Log.Information($"rdr_snappy_wheel - {(bool)sectionTable["rdr_snappy_wheel"]}");
                ChkRdrSnappyWheel.IsChecked = (bool)sectionTable["rdr_snappy_wheel"];
            }

            // "rdr_turbo_gallop_horse" setting
            if (sectionTable.ContainsKey("rdr_turbo_gallop_horse"))
            {
                Log.Information($"rdr_turbo_gallop_horse - {(bool)sectionTable["rdr_turbo_gallop_horse"]}");
                ChkRdrTurboGallopHorse.IsChecked = (bool)sectionTable["rdr_turbo_gallop_horse"];
            }

            // "sensitivity" setting
            if (sectionTable.ContainsKey("sensitivity"))
            {
                Log.Information($"sensitivity - {sectionTable["sensitivity"]}");
                SldMouseSensitivity.Value = double.Parse(sectionTable["sensitivity"].ToString()) * 10;
                AutomationProperties.SetName(SldMouseSensitivity, $"Mouse Sensitivity: {SldMouseSensitivity.Value}");
            }

            // "sr2_better_drive_cam" setting
            if (sectionTable.ContainsKey("sr2_better_drive_cam"))
            {
                Log.Information($"sr2_better_drive_cam - {(bool)sectionTable["sr2_better_drive_cam"]}");
                ChkSaintsRow2BetterDriveCam.IsChecked = (bool)sectionTable["sr2_better_drive_cam"];
            }

            // "sr2_better_handbrake_cam" setting
            if (sectionTable.ContainsKey("sr2_better_handbrake_cam"))
            {
                Log.Information($"sr2_better_handbrake_cam - {(bool)sectionTable["sr2_better_handbrake_cam"]}");
                ChkSaintsRow2BetterHandbrakeCam.IsChecked = (bool)sectionTable["sr2_better_handbrake_cam"];
            }
            
            // "sr2_hold_fine_aim" setting
            if (sectionTable.ContainsKey("sr2_hold_fine_aim"))
            {
                Log.Information($"sr2_hold_fine_aim - {(bool)sectionTable["sr2_hold_fine_aim"]}");
                ChkSaintsRow2HoldFineAim.IsChecked = (bool)sectionTable["sr2_hold_fine_aim"];
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
                sectionTable["disable_autoaim"] = ChkDisableAutoAim.IsChecked;
            }

            // "fov_sensitivity" setting
            if (sectionTable.ContainsKey("fov_sensitivity"))
            {
                Log.Information($"fov_sensitivity - {sectionTable["fov_sensitivity"]}");
                sectionTable["fov_sensitivity"] = Math.Round(SldFovSensitivity.Value / 10, 1);
            }

            // "ge_aim_turn_distance" setting
            if (sectionTable.ContainsKey("ge_aim_turn_distance"))
            {
                Log.Information($"ge_aim_turn_distance - {sectionTable["ge_aim_turn_distance"]}");
                sectionTable["ge_aim_turn_distance"] = Math.Round(SldAimTurnDistance.Value / 1000, 3);
            }

            // "ge_debug_menu" setting
            if (sectionTable.ContainsKey("ge_debug_menu"))
            {
                Log.Information($"ge_debug_menu - {(bool)sectionTable["ge_debug_menu"]}");
                sectionTable["ge_debug_menu"] = ChkGoldenEyeDebugMenu.IsChecked;
            }

            // "ge_gun_sway" setting
            if (sectionTable.ContainsKey("ge_gun_sway"))
            {
                Log.Information($"ge_gun_sway - {(bool)sectionTable["ge_gun_sway"]}");
                sectionTable["ge_gun_sway"] = ChkGunSway.IsChecked;
            }

            // "ge_menu_sensitivity" setting
            if (sectionTable.ContainsKey("ge_menu_sensitivity"))
            {
                Log.Information($"ge_menu_sensitivity - {sectionTable["ge_menu_sensitivity"]}");
                sectionTable["ge_menu_sensitivity"] = Math.Round(SldGoldenEyeMenuSensitivity.Value / 10, 1);
            }

            // "ge_remove_blur" setting
            if (sectionTable.ContainsKey("ge_remove_blur"))
            {
                Log.Information($"ge_remove_blur - {(bool)sectionTable["ge_remove_blur"]}");
                sectionTable["ge_remove_blur"] = ChkGoldenEyeRemoveBlur.IsChecked;
            }

            // "invert_x" setting
            if (sectionTable.ContainsKey("invert_x"))
            {
                Log.Information($"invert_x - {(bool)sectionTable["invert_x"]}");
                sectionTable["invert_x"] = ChkInvertXAxis.IsChecked;
            }

            // "invert_y" setting
            if (sectionTable.ContainsKey("invert_y"))
            {
                Log.Information($"invert_y - {(bool)sectionTable["invert_y"]}");
                sectionTable["invert_y"] = ChkInvertYAxis.IsChecked;
            }

            // "rdr_snappy_wheel" setting
            if (sectionTable.ContainsKey("rdr_snappy_wheel"))
            {
                Log.Information($"rdr_snappy_wheel - {(bool)sectionTable["rdr_snappy_wheel"]}");
                sectionTable["rdr_snappy_wheel"] = ChkRdrSnappyWheel.IsChecked;
            }

            // "rdr_turbo_gallop_horse" setting
            if (sectionTable.ContainsKey("rdr_turbo_gallop_horse"))
            {
                Log.Information($"rdr_turbo_gallop_horse - {(bool)sectionTable["rdr_turbo_gallop_horse"]}");
                sectionTable["rdr_turbo_gallop_horse"] = ChkRdrTurboGallopHorse.IsChecked;
            }

            // "sensitivity" setting
            if (sectionTable.ContainsKey("sensitivity"))
            {
                Log.Information($"sensitivity - {sectionTable["sensitivity"]}");
                sectionTable["sensitivity"] = Math.Round(SldMouseSensitivity.Value / 10, 1);
            }

            // "sr2_better_drive_cam" setting
            if (sectionTable.ContainsKey("sr2_better_drive_cam"))
            {
                Log.Information($"sr2_better_drive_cam - {(bool)sectionTable["sr2_better_drive_cam"]}");
                sectionTable["sr2_better_drive_cam"] = ChkSaintsRow2BetterDriveCam.IsChecked;
            }

            // "sr2_better_handbrake_cam" setting
            if (sectionTable.ContainsKey("sr2_better_handbrake_cam"))
            {
                Log.Information($"sr2_better_handbrake_cam - {(bool)sectionTable["sr2_better_handbrake_cam"]}");
                sectionTable["sr2_better_handbrake_cam"] = ChkSaintsRow2BetterHandbrakeCam.IsChecked;
            }
            
            // "sr2_hold_fine_aim" setting
            if (sectionTable.ContainsKey("sr2_hold_fine_aim"))
            {
                Log.Information($"sr2_hold_fine_aim - {(bool)sectionTable["sr2_hold_fine_aim"]}");
                sectionTable["sr2_hold_fine_aim"] = ChkSaintsRow2HoldFineAim.IsChecked;
            }
        }
    }
}