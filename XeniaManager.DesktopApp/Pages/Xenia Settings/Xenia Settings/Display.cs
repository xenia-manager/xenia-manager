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
        /// Loads the Display Settings into the UI
        /// </summary>
        /// <param name="sectionTable">Portion of .toml file dedicated to Display Settings</param>
        private void LoadDisplaySettings(TomlTable sectionTable)
        {
            // "fullscreen" setting
            if (sectionTable.ContainsKey("fullscreen"))
            {
                Log.Information($"fullscreen - {(bool)sectionTable["fullscreen"]}");
                ChkFullscreen.IsChecked = (bool)sectionTable["fullscreen"];

                BrdFullscreenSetting.Visibility = Visibility.Visible;
                BrdFullscreenSetting.Tag = null;
            }
            else
            {
                Log.Warning("`fullscreen` is missing from configuration file");
                BrdFullscreenSetting.Visibility = Visibility.Collapsed;
                BrdFullscreenSetting.Tag = "Ignore";
            }

            // "postprocess_antialiasing" setting
            if (sectionTable.ContainsKey("postprocess_antialiasing"))
            {
                Log.Information($"postprocess_antialiasing - {sectionTable["postprocess_antialiasing"] as string}");
                switch (sectionTable["postprocess_antialiasing"] as string)
                {
                    case "fxaa":
                        CmbAntiAliasing.SelectedIndex = 1;
                        break;
                    case "fxaa_extreme":
                        CmbAntiAliasing.SelectedIndex = 2;
                        break;
                    default:
                        CmbAntiAliasing.SelectedIndex = 0;
                        break;
                }
                
                BrdAntiAliasingSetting.Visibility = Visibility.Visible;
                BrdAntiAliasingSetting.Tag = null;
            }
            else
            {
                Log.Warning("`postprocess_antialiasing` is missing from configuration file");
                BrdAntiAliasingSetting.Visibility = Visibility.Collapsed;
                BrdAntiAliasingSetting.Tag = "Ignore";
            }

            // "postprocess_scaling_and_sharpening" setting
            if (sectionTable.ContainsKey("postprocess_scaling_and_sharpening"))
            {
                Log.Information(
                    $"postprocess_scaling_and_sharpening - {sectionTable["postprocess_scaling_and_sharpening"] as string}");
                switch (sectionTable["postprocess_scaling_and_sharpening"] as string)
                {
                    case "cas":
                        CmbScalingSharpening.SelectedIndex = 1;
                        break;
                    case "fsr":
                        CmbScalingSharpening.SelectedIndex = 2;
                        break;
                    default:
                        CmbScalingSharpening.SelectedIndex = 0;
                        break;
                }
                
                BrdScalingSharpeningSetting.Visibility = Visibility.Visible;
                BrdScalingSharpeningSetting.Tag = null;
            }
            else
            {
                Log.Warning("`postprocess_scaling_and_sharpening` is missing from configuration file");
                BrdScalingSharpeningSetting.Visibility = Visibility.Collapsed;
                BrdScalingSharpeningSetting.Tag = "Ignore";
            }

            // "postprocess_dither" setting
            if (sectionTable.ContainsKey("postprocess_dither"))
            {
                Log.Information($"postprocess_dither - {(bool)sectionTable["postprocess_dither"]}");
                ChkPostProcessDither.IsChecked = (bool)sectionTable["postprocess_dither"];
                
                BrdPostProcessDitherSetting.Visibility = Visibility.Visible;
                BrdPostProcessDitherSetting.Tag = null;
            }
            else
            {
                Log.Warning("`postprocess_dither` is missing from configuration file");
                BrdPostProcessDitherSetting.Visibility = Visibility.Collapsed;
                BrdPostProcessDitherSetting.Tag = "Ignore";
            }

            // "postprocess_ffx_cas_additional_sharpness" setting
            if (sectionTable.ContainsKey("postprocess_ffx_cas_additional_sharpness"))
            {
                Log.Information(
                    $"postprocess_ffx_cas_additional_sharpness - {sectionTable["postprocess_ffx_cas_additional_sharpness"]}");
                SldCasAdditionalSharpness.Value =
                    double.Parse(sectionTable["postprocess_ffx_cas_additional_sharpness"].ToString()) * 1000;
                AutomationProperties.SetName(SldCasAdditionalSharpness,
                    $"CAS Additional Sharpness: {Math.Round((SldCasAdditionalSharpness.Value / 1000), 3)}");
                
                BrdCasAdditionalSharpnessSetting.Visibility = Visibility.Visible;
                BrdCasAdditionalSharpnessSetting.Tag = null;
            }
            else
            {
                Log.Warning("`postprocess_ffx_cas_additional_sharpness` is missing from configuration file");
                BrdCasAdditionalSharpnessSetting.Visibility = Visibility.Collapsed;
                BrdCasAdditionalSharpnessSetting.Tag = "Ignore";
            }

            // "postprocess_ffx_fsr_max_upsampling_passes" setting
            if (sectionTable.ContainsKey("postprocess_ffx_fsr_max_upsampling_passes"))
            {
                Log.Information(
                    $"postprocess_ffx_fsr_max_upsampling_passes - {int.Parse(sectionTable["postprocess_ffx_fsr_max_upsampling_passes"].ToString())}");
                SldFsrMaxUpsamplingPasses.Value =
                    int.Parse(sectionTable["postprocess_ffx_fsr_max_upsampling_passes"].ToString());
                AutomationProperties.SetName(SldFsrMaxUpsamplingPasses,
                    $"FSR MaxUpsampling Passes: {SldFsrMaxUpsamplingPasses.Value}");
                
                BrdFsrMaxUpsamplingPassesSetting.Visibility = Visibility.Visible;
                BrdFsrMaxUpsamplingPassesSetting.Tag = null;
            }
            else
            {
                Log.Warning("`postprocess_ffx_fsr_max_upsampling_passes` is missing from configuration file");
                BrdFsrMaxUpsamplingPassesSetting.Visibility = Visibility.Collapsed;
                BrdFsrMaxUpsamplingPassesSetting.Tag = "Ignore";
            }

            // "postprocess_ffx_fsr_sharpness_reduction" setting
            if (sectionTable.ContainsKey("postprocess_ffx_fsr_sharpness_reduction"))
            {
                Log.Information(
                    $"postprocess_ffx_fsr_sharpness_reduction - {double.Parse(sectionTable["postprocess_ffx_fsr_sharpness_reduction"].ToString())}");
                SldFsrSharpnessReduction.Value =
                    double.Parse(sectionTable["postprocess_ffx_fsr_sharpness_reduction"].ToString()) * 1000;
                AutomationProperties.SetName(SldFsrSharpnessReduction,
                    $"FSR Sharpness Reduction: {Math.Round((SldFsrSharpnessReduction.Value / 1000), 3)}");
                
                BrdFsrSharpnessReductionSetting.Visibility = Visibility.Visible;
                BrdFsrSharpnessReductionSetting.Tag = null;
            }
            else
            {
                Log.Warning("`postprocess_ffx_fsr_sharpness_reduction` is missing from configuration file");
                BrdFsrSharpnessReductionSetting.Visibility = Visibility.Collapsed;
                BrdFsrSharpnessReductionSetting.Tag = "Ignore";
            }

            // "present_letterbox" setting
            if (sectionTable.ContainsKey("present_letterbox"))
            {
                Log.Information($"present_letterbox - {(bool)sectionTable["present_letterbox"]}");
                ChkLetterbox.IsChecked = (bool)sectionTable["present_letterbox"];
                
                BrdLetterboxSetting.Visibility = Visibility.Visible;
                BrdLetterboxSetting.Tag = null;
            }
            else
            {
                Log.Warning("`present_letterbox` is missing from configuration file");
                BrdLetterboxSetting.Visibility = Visibility.Collapsed;
                BrdLetterboxSetting.Tag = "Ignore";
            }
        }

        /// <summary>
        /// Saves the Display Settings into the configuration file
        /// </summary>
        /// <param name="sectionTable">Portion of .toml file dedicated to Display Settings</param>
        private void SaveDisplaySettings(TomlTable sectionTable)
        {
            // "fullscreen" setting
            if (sectionTable.ContainsKey("fullscreen"))
            {
                Log.Information($"fullscreen - {ChkFullscreen.IsChecked}");
                sectionTable["fullscreen"] = ChkFullscreen.IsChecked;
            }

            // "postprocess_antialiasing" setting
            if (sectionTable.ContainsKey("postprocess_antialiasing"))
            {
                Log.Information($"postprocess_antialiasing - {(CmbAntiAliasing.SelectedItem as ComboBoxItem).Content}");
                switch (CmbAntiAliasing.SelectedIndex)
                {
                    case 1:
                        // "fxaa_extreme"
                        sectionTable["postprocess_antialiasing"] = "fxaa";
                        break;
                    case 2:
                        // "fxaa_extreme"
                        sectionTable["postprocess_antialiasing"] = "fxaa_extreme";
                        break;
                    default:
                        // "none"
                        sectionTable["postprocess_antialiasing"] = "";
                        break;
                }
            }

            // "postprocess_scaling_and_sharpening" setting
            if (sectionTable.ContainsKey("postprocess_scaling_and_sharpening"))
            {
                Log.Information(
                    $"postprocess_scaling_and_sharpening - {(CmbScalingSharpening.SelectedItem as ComboBoxItem).Content}");
                switch (CmbScalingSharpening.SelectedIndex)
                {
                    case 1:
                        // "cas"
                        sectionTable["postprocess_scaling_and_sharpening"] = "cas";
                        break;
                    case 2:
                        // "fsr"
                        sectionTable["postprocess_scaling_and_sharpening"] = "fsr";
                        break;
                    default:
                        // "bilinear"
                        sectionTable["postprocess_scaling_and_sharpening"] = "";
                        break;
                }
            }

            // "postprocess_dither" setting
            if (sectionTable.ContainsKey("postprocess_dither"))
            {
                Log.Information($"postprocess_dither - {ChkPostProcessDither.IsChecked}");
                sectionTable["postprocess_dither"] = ChkPostProcessDither.IsChecked;
            }

            // "postprocess_ffx_cas_additional_sharpness" setting
            if (sectionTable.ContainsKey("postprocess_ffx_cas_additional_sharpness"))
            {
                Log.Information(
                    $"postprocess_ffx_cas_additional_sharpness - {Math.Round(SldCasAdditionalSharpness.Value / 1000, 3)}");
                sectionTable["postprocess_ffx_cas_additional_sharpness"] =
                    Math.Round(SldCasAdditionalSharpness.Value / 1000, 3);
            }

            // "postprocess_ffx_fsr_max_upsampling_passes" setting
            if (sectionTable.ContainsKey("postprocess_ffx_fsr_max_upsampling_passes"))
            {
                Log.Information($"postprocess_ffx_fsr_max_upsampling_passes - {SldFsrMaxUpsamplingPasses.Value}");
                sectionTable["postprocess_ffx_fsr_max_upsampling_passes"] = (int)SldFsrMaxUpsamplingPasses.Value;
            }

            // "postprocess_ffx_fsr_sharpness_reduction" setting
            if (sectionTable.ContainsKey("postprocess_ffx_fsr_sharpness_reduction"))
            {
                Log.Information(
                    $"postprocess_ffx_fsr_sharpness_reduction - {Math.Round(SldFsrSharpnessReduction.Value / 1000, 3)}");
                sectionTable["postprocess_ffx_fsr_sharpness_reduction"] =
                    Math.Round(SldFsrSharpnessReduction.Value / 1000, 3);
            }

            // "present_letterbox" setting
            if (sectionTable.ContainsKey("present_letterbox"))
            {
                Log.Information($"present_letterbox - {ChkLetterbox.IsChecked}");
                sectionTable["present_letterbox"] = ChkLetterbox.IsChecked;
            }
        }
    }
}