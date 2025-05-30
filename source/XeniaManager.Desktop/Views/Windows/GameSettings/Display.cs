using System.Windows;
using Tomlyn.Model;
using XeniaManager.Core;

namespace XeniaManager.Desktop.Views.Windows;

public partial class GameSettingsEditor
{
    private void LoadDisplaySettings(TomlTable displaySection)
    {
        // fullscreen
        if (displaySection.ContainsKey("fullscreen"))
        {
            Logger.Info($"fullscreen - {(bool)displaySection["fullscreen"]}");
            BrdDisplayFullscreenSetting.Visibility = Visibility.Visible;
            ChkFullscreen.IsChecked = (bool)displaySection["fullscreen"];
        }
        else
        {
            Logger.Warning("`fullscreen` is missing from configuration file");
            BrdDisplayFullscreenSetting.Visibility = Visibility.Collapsed;
        }

        // present_letterbox
        if (displaySection.ContainsKey("present_letterbox"))
        {
            Logger.Info($"present_letterbox - {(bool)displaySection["present_letterbox"]}");
            BrdDisplayLetterboxSetting.Visibility = Visibility.Visible;
            ChkLetterbox.IsChecked = (bool)displaySection["present_letterbox"];
        }
        else
        {
            Logger.Warning("`present_letterbox` is missing from configuration file");
            BrdDisplayLetterboxSetting.Visibility = Visibility.Collapsed;
        }

        // postprocess_antialiasing
        if (displaySection.ContainsKey("postprocess_antialiasing"))
        {
            Logger.Info($"postprocess_antialiasing - {displaySection["postprocess_antialiasing"]}");
            BrdAntiAliasingSetting.Visibility = Visibility.Visible;
            CmbAntiAliasing.SelectedValue = displaySection["postprocess_antialiasing"].ToString();
        }
        else
        {
            Logger.Warning("`postprocess_antialiasing` is missing from configuration file");
            BrdAntiAliasingSetting.Visibility = Visibility.Collapsed;
        }

        // postprocess_scaling_and_sharpening
        if (displaySection.ContainsKey("postprocess_scaling_and_sharpening"))
        {
            Logger.Info($"postprocess_scaling_and_sharpening - {displaySection["postprocess_scaling_and_sharpening"]}");
            BrdScalingSharpeningSetting.Visibility = Visibility.Visible;
            CmbScalingSharpening.SelectedValue = displaySection["postprocess_scaling_and_sharpening"].ToString();
            if (CmbScalingSharpening.SelectedIndex < 0)
            {
                CmbScalingSharpening.SelectedIndex = 0;
            }
        }
        else
        {
            Logger.Warning("`postprocess_scaling_and_sharpening` is missing from configuration file");
            BrdScalingSharpeningSetting.Visibility = Visibility.Collapsed;
        }

        // postprocess_ffx_fsr_sharpness_reduction
        if (displaySection.ContainsKey("postprocess_ffx_fsr_sharpness_reduction"))
        {
            Logger.Info($"postprocess_ffx_fsr_sharpness_reduction - {displaySection["postprocess_ffx_fsr_sharpness_reduction"]}");
            BrdFsrSharpnessReductionSetting.Visibility = Visibility.Visible;
            try
            {
                SldFsrSharpnessReduction.Value = double.Parse(displaySection["postprocess_ffx_fsr_sharpness_reduction"].ToString()) * 1000;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                SldFsrSharpnessReduction.Value = 200;
            }
        }
        else
        {
            Logger.Warning("`postprocess_ffx_fsr_sharpness_reduction` is missing from configuration file");
            BrdFsrSharpnessReductionSetting.Visibility = Visibility.Collapsed;
        }

        // postprocess_ffx_cas_additional_sharpness
        if (displaySection.ContainsKey("postprocess_ffx_cas_additional_sharpness"))
        {
            Logger.Info($"postprocess_ffx_cas_additional_sharpness - {displaySection["postprocess_ffx_cas_additional_sharpness"]}");
            BrdCasAdditionalSharpnessSetting.Visibility = Visibility.Visible;
            try
            {
                SldCasAdditionalSharpness.Value = double.Parse(displaySection["postprocess_ffx_cas_additional_sharpness"].ToString()) * 1000;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                SldCasAdditionalSharpness.Value = 0;
            }
        }
        else
        {
            Logger.Warning("`postprocess_ffx_cas_additional_sharpness` is missing from configuration file");
            BrdCasAdditionalSharpnessSetting.Visibility = Visibility.Collapsed;
        }

        // postprocess_ffx_fsr_max_upsampling_passes
        if (displaySection.ContainsKey("postprocess_ffx_fsr_max_upsampling_passes"))
        {
            Logger.Info($"postprocess_ffx_fsr_max_upsampling_passes - {displaySection["postprocess_ffx_fsr_max_upsampling_passes"]}");
            BrdFsrMaxUpsamplingPassesSetting.Visibility = Visibility.Visible;
            try
            {
                SldFsrMaxUpsamplingPasses.Value = int.Parse(displaySection["postprocess_ffx_fsr_max_upsampling_passes"].ToString());
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                SldFsrMaxUpsamplingPasses.Value = 1;
            }
        }
        else
        {
            Logger.Warning("`postprocess_ffx_fsr_max_upsampling_passes` is missing from configuration file");
            BrdFsrMaxUpsamplingPassesSetting.Visibility = Visibility.Collapsed;
        }

        // postprocess_dither
        if (displaySection.ContainsKey("postprocess_dither"))
        {
            Logger.Info($"postprocess_dither - {(bool)displaySection["postprocess_dither"]}");
            BrdPostProcessDitherSetting.Visibility = Visibility.Visible;
            ChkPostProcessDither.IsChecked = (bool)displaySection["postprocess_dither"];
        }
        else
        {
            Logger.Warning("`postprocess_dither` is missing from configuration file");
            BrdPostProcessDitherSetting.Visibility = Visibility.Collapsed;
        }
    }

    private void SaveDisplaySettings(TomlTable displaySection)
    {
        // fullscreen
        if (displaySection.ContainsKey("fullscreen"))
        {
            Logger.Info($"fullscreen - {ChkFullscreen.IsChecked}");
            displaySection["fullscreen"] = ChkFullscreen.IsChecked;
        }

        // present_letterbox
        if (displaySection.ContainsKey("present_letterbox"))
        {
            Logger.Info($"present_letterbox - {ChkLetterbox.IsChecked}");
            displaySection["present_letterbox"] = ChkLetterbox.IsChecked;
        }

        // postprocess_antialiasing
        if (displaySection.ContainsKey("postprocess_antialiasing"))
        {
            Logger.Info($"postprocess_antialiasing - {CmbAntiAliasing.SelectedValue}");
            displaySection["postprocess_antialiasing"] = CmbAntiAliasing.SelectedValue;
        }

        // postprocess_scaling_and_sharpening
        if (displaySection.ContainsKey("postprocess_scaling_and_sharpening"))
        {
            Logger.Info($"postprocess_scaling_and_sharpening - {CmbScalingSharpening.SelectedValue}");
            displaySection["postprocess_scaling_and_sharpening"] = CmbScalingSharpening.SelectedValue;
        }

        // postprocess_ffx_fsr_sharpness_reduction
        if (displaySection.ContainsKey("postprocess_ffx_fsr_sharpness_reduction"))
        {
            Logger.Info($"postprocess_ffx_fsr_sharpness_reduction - {SldFsrSharpnessReduction.Value / 1000}");
            displaySection["postprocess_ffx_fsr_sharpness_reduction"] = SldFsrSharpnessReduction.Value / 1000;
        }

        // postprocess_ffx_cas_additional_sharpness
        if (displaySection.ContainsKey("postprocess_ffx_cas_additional_sharpness"))
        {
            Logger.Info($"postprocess_ffx_cas_additional_sharpness - {SldCasAdditionalSharpness.Value / 1000}");
            displaySection["postprocess_ffx_cas_additional_sharpness"] = SldCasAdditionalSharpness.Value / 1000;
        }

        // postprocess_ffx_fsr_max_upsampling_passes
        if (displaySection.ContainsKey("postprocess_ffx_fsr_max_upsampling_passes"))
        {
            Logger.Info($"postprocess_ffx_fsr_max_upsampling_passes - {SldFsrMaxUpsamplingPasses.Value}");
            displaySection["postprocess_ffx_fsr_max_upsampling_passes"] = (int)SldFsrMaxUpsamplingPasses.Value;
        }

        // postprocess_dither
        if (displaySection.ContainsKey("postprocess_dither"))
        {
            Logger.Info($"postprocess_dither - {(bool)displaySection["postprocess_dither"]}");
            displaySection["postprocess_dither"] = ChkPostProcessDither.IsChecked;
        }
    }
}