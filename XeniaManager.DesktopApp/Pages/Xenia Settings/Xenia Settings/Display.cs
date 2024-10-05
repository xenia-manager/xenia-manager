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
        /// Loads the Display Settings into the UI
        /// </summary>
        /// <param name="sectionTable">Portion of .toml file dedicated to Display Settings</param>
        private void LoadDisplaySettings(TomlTable sectionTable)
        {
            // "fullscreen" setting
            if (sectionTable.ContainsKey("fullscreen"))
            {
                Log.Information($"fullscreen - {(bool)sectionTable["fullscreen"]}");
                chkFullscreen.IsChecked = (bool)sectionTable["fullscreen"];
            }

            // "postprocess_antialiasing" setting
            if (sectionTable.ContainsKey("postprocess_antialiasing"))
            {
                Log.Information($"postprocess_antialiasing - {sectionTable["postprocess_antialiasing"] as string}");
                switch (sectionTable["postprocess_antialiasing"] as string)
                {
                    case "fxaa":
                        cmbAntiAliasing.SelectedIndex = 1;
                        break;
                    case "fxaa_extreme":
                        cmbAntiAliasing.SelectedIndex = 2;
                        break;
                    default:
                        cmbAntiAliasing.SelectedIndex = 0;
                        break;
                }
            }

            // "postprocess_scaling_and_sharpening" setting
            if (sectionTable.ContainsKey("postprocess_scaling_and_sharpening"))
            {
                Log.Information($"postprocess_scaling_and_sharpening - {sectionTable["postprocess_scaling_and_sharpening"] as string}");
                switch (sectionTable["postprocess_scaling_and_sharpening"] as string)
                {
                    case "cas":
                        cmbScalingSharpening.SelectedIndex = 1;
                        break;
                    case "fsr":
                        cmbScalingSharpening.SelectedIndex = 2;
                        break;
                    default:
                        cmbScalingSharpening.SelectedIndex = 0;
                        break;
                }
            }

            // "postprocess_dither" setting
            if (sectionTable.ContainsKey("postprocess_dither"))
            {
                Log.Information($"postprocess_dither - {(bool)sectionTable["postprocess_dither"]}");
                chkPostProcessDither.IsChecked = (bool)sectionTable["postprocess_dither"];
            }

            // "postprocess_ffx_cas_additional_sharpness" setting
            if (sectionTable.ContainsKey("postprocess_ffx_cas_additional_sharpness"))
            {
                Log.Information($"postprocess_ffx_cas_additional_sharpness - {sectionTable["postprocess_ffx_cas_additional_sharpness"].ToString()}");
                sldCASAdditionalSharpness.Value = double.Parse(sectionTable["postprocess_ffx_cas_additional_sharpness"].ToString()) * 1000;
                AutomationProperties.SetName(sldCASAdditionalSharpness, $"CAS Additional Sharpness: {Math.Round((sldCASAdditionalSharpness.Value / 1000), 3)}");
            }

            // "postprocess_ffx_fsr_max_upsampling_passes" setting
            if (sectionTable.ContainsKey("postprocess_ffx_fsr_max_upsampling_passes"))
            {
                Log.Information($"postprocess_ffx_fsr_max_upsampling_passes - {int.Parse(sectionTable["postprocess_ffx_fsr_max_upsampling_passes"].ToString())}");
                sldFSRMaxUpsamplingPasses.Value = int.Parse(sectionTable["postprocess_ffx_fsr_max_upsampling_passes"].ToString());
                AutomationProperties.SetName(sldFSRMaxUpsamplingPasses, $"FSR MaxUpsampling Passes: {sldFSRMaxUpsamplingPasses.Value}");
            }

            // "postprocess_ffx_fsr_sharpness_reduction" setting
            if (sectionTable.ContainsKey("postprocess_ffx_fsr_sharpness_reduction"))
            {
                Log.Information($"postprocess_ffx_fsr_sharpness_reduction - {double.Parse(sectionTable["postprocess_ffx_fsr_sharpness_reduction"].ToString())}");
                sldFSRSharpnessReduction.Value = double.Parse(sectionTable["postprocess_ffx_fsr_sharpness_reduction"].ToString()) * 1000;
                AutomationProperties.SetName(sldFSRSharpnessReduction, $"FSR Sharpness Reduction: {Math.Round((sldFSRSharpnessReduction.Value / 1000), 3)}");
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
                Log.Information($"fullscreen - {chkFullscreen.IsChecked}");
                sectionTable["fullscreen"] = chkFullscreen.IsChecked;
            }

            // "postprocess_antialiasing" setting
            if (sectionTable.ContainsKey("postprocess_antialiasing"))
            {
                Log.Information($"postprocess_antialiasing - {(cmbAntiAliasing.SelectedItem as ComboBoxItem).Content}");
                switch (cmbAntiAliasing.SelectedIndex)
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
                Log.Information($"postprocess_scaling_and_sharpening - {(cmbScalingSharpening.SelectedItem as ComboBoxItem).Content}");
                switch (cmbScalingSharpening.SelectedIndex)
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
                Log.Information($"postprocess_dither - {chkPostProcessDither.IsChecked}");
                sectionTable["postprocess_dither"] = chkPostProcessDither.IsChecked;
            }

            // "postprocess_ffx_cas_additional_sharpness" setting
            if (sectionTable.ContainsKey("postprocess_ffx_cas_additional_sharpness"))
            {
                Log.Information($"postprocess_ffx_cas_additional_sharpness - {(double)Math.Round(sldCASAdditionalSharpness.Value / 1000, 3)}");
                sectionTable["postprocess_ffx_cas_additional_sharpness"] = (double)Math.Round(sldCASAdditionalSharpness.Value / 1000, 3);
            }

            // "postprocess_ffx_fsr_max_upsampling_passes" setting
            if (sectionTable.ContainsKey("postprocess_ffx_fsr_max_upsampling_passes"))
            {
                Log.Information($"postprocess_ffx_fsr_max_upsampling_passes - {sldFSRMaxUpsamplingPasses.Value}");
                sectionTable["postprocess_ffx_fsr_max_upsampling_passes"] = (int)sldFSRMaxUpsamplingPasses.Value;
            }

            // "postprocess_ffx_fsr_sharpness_reduction" setting
            if (sectionTable.ContainsKey("postprocess_ffx_fsr_sharpness_reduction"))
            {
                Log.Information($"postprocess_ffx_fsr_sharpness_reduction - {(double)Math.Round(sldFSRSharpnessReduction.Value / 1000, 3)}");
                sectionTable["postprocess_ffx_fsr_sharpness_reduction"] = (double)Math.Round(sldFSRSharpnessReduction.Value / 1000, 3);
            }
        }
    }
}