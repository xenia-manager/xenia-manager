using System.Windows;

// Imported Libraries
using Tomlyn.Model;
using XeniaManager.Core;

namespace XeniaManager.Desktop.Views.Windows;

public partial class GameSettingsEditor
{
    private void LoadMousehookSettings(TomlTable hidSection)
    {
        _viewModel.MousehookSettingsVisibility = Visibility.Visible;
        // disable_autoaim
        if (hidSection.ContainsKey("disable_autoaim"))
        {
            Logger.Info($"disable_autoaim - {hidSection["disable_autoaim"]}");
            BrdMousehookAutoaimSetting.Visibility = Visibility.Visible;
            ChkMousehookAutoaim.IsChecked = (bool)hidSection["disable_autoaim"];
        }
        else
        {
            Logger.Warning("'disable_autoaim' is missing from the configuration file");
            BrdMousehookAutoaimSetting.Visibility = Visibility.Collapsed;
        }

        // invert_x
        if (hidSection.ContainsKey("invert_x"))
        {
            Logger.Info($"invert_x - {hidSection["invert_x"]}");
            BrdMousehookInvertXSetting.Visibility = Visibility.Visible;
            ChkMousehookInvertX.IsChecked = (bool)hidSection["invert_x"];
        }
        else
        {
            Logger.Warning("'invert_x' is missing from the configuration file");
            BrdMousehookInvertXSetting.Visibility = Visibility.Collapsed;
        }

        // invert_x
        if (hidSection.ContainsKey("invert_y"))
        {
            Logger.Info($"invert_y - {hidSection["invert_y"]}");
            BrdMousehookInvertYSetting.Visibility = Visibility.Visible;
            ChkMousehookInvertY.IsChecked = (bool)hidSection["invert_y"];
        }
        else
        {
            Logger.Warning("'invert_x' is missing from the configuration file");
            BrdMousehookInvertYSetting.Visibility = Visibility.Collapsed;
        }

        // ge_aim_turn_distance
        if (hidSection.ContainsKey("ge_aim_turn_distance"))
        {
            Logger.Info($"ge_aim_turn_distance - {hidSection["ge_aim_turn_distance"]}");
            BrdMousehookAimTurnDistanceSetting.Visibility = Visibility.Visible;
            try
            {
                SldMousehookAimTurnDistance.Value = double.Parse(hidSection["ge_aim_turn_distance"].ToString()) * 1000;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                SldMousehookAimTurnDistance.Value = 0;
            }
        }
        else
        {
            Logger.Warning("'ge_aim_turn_distance' is missing from the configuration file");
            BrdMousehookAimTurnDistanceSetting.Visibility = Visibility.Collapsed;
        }

        // fov_sensitivity
        if (hidSection.ContainsKey("fov_sensitivity"))
        {
            Logger.Info($"fov_sensitivity - {hidSection["fov_sensitivity"]}");
            BrdMousehookFovSensitivitySetting.Visibility = Visibility.Visible;
            try
            {
                SldMousehookFovSensitivity.Value = double.Parse(hidSection["fov_sensitivity"].ToString()) * 100;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                SldMousehookFovSensitivity.Value = 0;
            }
        }
        else
        {
            Logger.Warning("'fov_sensitivity' is missing from the configuration file");
            BrdMousehookFovSensitivitySetting.Visibility = Visibility.Collapsed;
        }

        // sensitivity
        if (hidSection.ContainsKey("sensitivity"))
        {
            Logger.Info($"sensitivity - {hidSection["sensitivity"]}");
            BrdMousehookSensitivitySetting.Visibility = Visibility.Visible;
            try
            {
                SldMousehookSensitivity.Value = double.Parse(hidSection["sensitivity"].ToString()) * 100;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                SldMousehookSensitivity.Value = 0;
            }
        }
        else
        {
            Logger.Warning("'sensitivity' is missing from the configuration file");
            BrdMousehookSensitivitySetting.Visibility = Visibility.Collapsed;
        }

        // menu_sensitivity
        if (hidSection.ContainsKey("menu_sensitivity"))
        {
            Logger.Info($"menu_sensitivity - {hidSection["menu_sensitivity"]}");
            BrdMousehookMenuSensitivitySetting.Visibility = Visibility.Visible;
            try
            {
                SldMousehookMenuSensitivity.Value = double.Parse(hidSection["menu_sensitivity"].ToString()) * 100;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                SldMousehookMenuSensitivity.Value = 0;
            }
        }
        else
        {
            Logger.Warning("'menu_sensitivity' is missing from the configuration file");
            BrdMousehookMenuSensitivitySetting.Visibility = Visibility.Collapsed;
        }

        // ge_debug_menu
        if (hidSection.ContainsKey("ge_debug_menu"))
        {
            Logger.Info($"ge_debug_menu - {hidSection["ge_debug_menu"]}");
            BrdMousehookGoldenEyeDebugMenuSetting.Visibility = Visibility.Visible;
            ChkMousehookGoldenEyeDebugMenu.IsChecked = (bool)hidSection["ge_debug_menu"];
        }
        else
        {
            Logger.Warning("'ge_debug_menu' is missing from the configuration file");
            BrdMousehookGoldenEyeDebugMenuSetting.Visibility = Visibility.Collapsed;
        }

        // ge_gun_sway
        if (hidSection.ContainsKey("ge_gun_sway"))
        {
            Logger.Info($"ge_gun_sway - {hidSection["ge_gun_sway"]}");
            BrdMousehookGunSwaySetting.Visibility = Visibility.Visible;
            ChkMousehookGunSway.IsChecked = (bool)hidSection["ge_gun_sway"];
        }
        else
        {
            Logger.Warning("'ge_gun_sway' is missing from the configuration file");
            BrdMousehookGunSwaySetting.Visibility = Visibility.Collapsed;
        }

        // ge_remove_blur
        if (hidSection.ContainsKey("ge_remove_blur"))
        {
            Logger.Info($"ge_remove_blur - {hidSection["ge_remove_blur"]}");
            BrdMousehookGoldenEyeRemoveBlurSetting.Visibility = Visibility.Visible;
            ChkMousehookGoldenEyeRemoveBlur.IsChecked = (bool)hidSection["ge_remove_blur"];
        }
        else
        {
            Logger.Warning("'ge_remove_blur' is missing from the configuration file");
            BrdMousehookGoldenEyeRemoveBlurSetting.Visibility = Visibility.Collapsed;
        }

        // rdr_snappy_wheel
        if (hidSection.ContainsKey("rdr_snappy_wheel"))
        {
            Logger.Info($"rdr_snappy_wheel - {hidSection["rdr_snappy_wheel"]}");
            BrdMousehookRdrSnappyWheelSetting.Visibility = Visibility.Visible;
            ChkMousehookRdrSnappyWheel.IsChecked = (bool)hidSection["rdr_snappy_wheel"];
        }
        else
        {
            Logger.Warning("'rdr_snappy_wheel' is missing from the configuration file");
            BrdMousehookRdrSnappyWheelSetting.Visibility = Visibility.Collapsed;
        }

        // rdr_turbo_gallop_horse
        if (hidSection.ContainsKey("rdr_turbo_gallop_horse"))
        {
            Logger.Info($"rdr_turbo_gallop_horse - {hidSection["rdr_turbo_gallop_horse"]}");
            BrdMousehookRdrTurboGallopHorseSetting.Visibility = Visibility.Visible;
            ChkMousehookRdrTurboGallopHorse.IsChecked = (bool)hidSection["rdr_turbo_gallop_horse"];
        }
        else
        {
            Logger.Warning("'rdr_turbo_gallop_horse' is missing from the configuration file");
            BrdMousehookRdrTurboGallopHorseSetting.Visibility = Visibility.Collapsed;
        }

        // sr2_better_drive_cam
        if (hidSection.ContainsKey("sr2_better_drive_cam"))
        {
            Logger.Info($"sr2_better_drive_cam - {hidSection["sr2_better_drive_cam"]}");
            BrdMousehookSr2BetterDriveCamSetting.Visibility = Visibility.Visible;
            ChkMousehookSr2BetterDriveCam.IsChecked = (bool)hidSection["sr2_better_drive_cam"];
        }
        else
        {
            Logger.Warning("'sr2_better_drive_cam' is missing from the configuration file");
            BrdMousehookSr2BetterDriveCamSetting.Visibility = Visibility.Collapsed;
        }

        // sr2_better_handbrake_cam
        if (hidSection.ContainsKey("sr2_better_handbrake_cam"))
        {
            Logger.Info($"sr2_better_handbrake_cam - {hidSection["sr2_better_handbrake_cam"]}");
            BrdMousehookSr2BetterHandbrakeCamSetting.Visibility = Visibility.Visible;
            ChkMousehookSr2BetterHandbrakeCam.IsChecked = (bool)hidSection["sr2_better_handbrake_cam"];
        }
        else
        {
            Logger.Warning("'sr2_better_handbrake_cam' is missing from the configuration file");
            BrdMousehookSr2BetterHandbrakeCamSetting.Visibility = Visibility.Collapsed;
        }

        // sr2_hold_fine_aim
        if (hidSection.ContainsKey("sr2_hold_fine_aim"))
        {
            Logger.Info($"sr2_hold_fine_aim - {hidSection["sr2_hold_fine_aim"]}");
            BrdMousehookSr2HoldFineAimSetting.Visibility = Visibility.Visible;
            ChkMousehookSr2HoldFineAim.IsChecked = (bool)hidSection["sr2_hold_fine_aim"];
        }
        else
        {
            Logger.Warning("'sr2_hold_fine_aim' is missing from the configuration file");
            BrdMousehookSr2HoldFineAimSetting.Visibility = Visibility.Collapsed;
        }
    }

    private void SaveMousehookSettings(TomlTable hidSection)
    {
        // disable_autoaim
        if (hidSection.ContainsKey("disable_autoaim"))
        {
            Logger.Info($"disable_autoaim - {ChkMousehookAutoaim.IsChecked}");
            hidSection["disable_autoaim"] = ChkMousehookAutoaim.IsChecked;
        }

        // invert_x
        if (hidSection.ContainsKey("invert_x"))
        {
            Logger.Info($"invert_x - {ChkMousehookInvertX.IsChecked}");
            hidSection["invert_x"] = ChkMousehookInvertX.IsChecked;
        }

        // invert_y
        if (hidSection.ContainsKey("invert_y"))
        {
            Logger.Info($"invert_y - {ChkMousehookInvertY.IsChecked}");
            hidSection["invert_y"] = ChkMousehookInvertY.IsChecked;
        }

        // ge_aim_turn_distance
        if (hidSection.ContainsKey("ge_aim_turn_distance"))
        {
            Logger.Info($"ge_aim_turn_distance - {SldMousehookAimTurnDistance.Value}");
            hidSection["ge_aim_turn_distance"] = SldMousehookAimTurnDistance.Value / 1000;
        }

        // fov_sensitivity
        if (hidSection.ContainsKey("fov_sensitivity"))
        {
            Logger.Info($"fov_sensitivity - {SldMousehookFovSensitivity.Value}");
            hidSection["fov_sensitivity"] = SldMousehookFovSensitivity.Value / 100;
        }

        // sensitivity
        if (hidSection.ContainsKey("sensitivity"))
        {
            Logger.Info($"sensitivity - {SldMousehookSensitivity.Value}");
            hidSection["sensitivity"] = SldMousehookSensitivity.Value / 100;
        }

        // menu_sensitivity
        if (hidSection.ContainsKey("menu_sensitivity"))
        {
            Logger.Info($"menu_sensitivity - {SldMousehookMenuSensitivity.Value}");
            hidSection["menu_sensitivity"] = SldMousehookMenuSensitivity.Value / 100;
        }

        // ge_debug_menu
        if (hidSection.ContainsKey("ge_debug_menu"))
        {
            Logger.Info($"ge_debug_menu - {ChkMousehookGoldenEyeDebugMenu.IsChecked}");
            hidSection["ge_debug_menu"] = ChkMousehookGoldenEyeDebugMenu.IsChecked;
        }

        // ge_gun_sway
        if (hidSection.ContainsKey("ge_gun_sway"))
        {
            Logger.Info($"ge_gun_sway - {ChkMousehookGunSway.IsChecked}");
            hidSection["ge_gun_sway"] = ChkMousehookGunSway.IsChecked;
        }

        // ge_remove_blur
        if (hidSection.ContainsKey("ge_remove_blur"))
        {
            Logger.Info($"ge_remove_blur - {ChkMousehookGoldenEyeRemoveBlur.IsChecked}");
            hidSection["ge_remove_blur"] = ChkMousehookGoldenEyeRemoveBlur.IsChecked;
        }

        // rdr_snappy_wheel
        if (hidSection.ContainsKey("rdr_snappy_wheel"))
        {
            Logger.Info($"rdr_snappy_wheel - {ChkMousehookRdrSnappyWheel.IsChecked}");
            hidSection["rdr_snappy_wheel"] = ChkMousehookRdrSnappyWheel.IsChecked;
        }

        // rdr_turbo_gallop_horse
        if (hidSection.ContainsKey("rdr_turbo_gallop_horse"))
        {
            Logger.Info($"rdr_turbo_gallop_horse - {ChkMousehookRdrTurboGallopHorse.IsChecked}");
            hidSection["rdr_turbo_gallop_horse"] = ChkMousehookRdrTurboGallopHorse.IsChecked;
        }

        // sr2_better_drive_cam
        if (hidSection.ContainsKey("sr2_better_drive_cam"))
        {
            Logger.Info($"sr2_better_drive_cam - {ChkMousehookSr2BetterDriveCam.IsChecked}");
            hidSection["sr2_better_drive_cam"] = ChkMousehookSr2BetterDriveCam.IsChecked;
        }

        // sr2_better_handbrake_cam
        if (hidSection.ContainsKey("sr2_better_handbrake_cam"))
        {
            Logger.Info($"sr2_better_handbrake_cam - {ChkMousehookSr2BetterHandbrakeCam.IsChecked}");
            hidSection["sr2_better_handbrake_cam"] = ChkMousehookSr2BetterHandbrakeCam.IsChecked;
        }

        // sr2_hold_fine_aim
        if (hidSection.ContainsKey("sr2_hold_fine_aim"))
        {
            Logger.Info($"sr2_hold_fine_aim - {ChkMousehookSr2HoldFineAim.IsChecked}");
            hidSection["sr2_hold_fine_aim"] = ChkMousehookSr2HoldFineAim.IsChecked;
        }
    }
}