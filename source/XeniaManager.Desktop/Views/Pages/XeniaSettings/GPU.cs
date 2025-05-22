using System.Windows;
using System.Windows.Controls;
using Tomlyn.Model;
using XeniaManager.Core;
using XeniaManager.Desktop.Components;

namespace XeniaManager.Desktop.Views.Pages;

public partial class XeniaSettingsPage
{
    private void LoadGpuSettings(TomlTable gpuSection)
    {
        // vsync
        if (gpuSection.ContainsKey("vsync"))
        {
            Logger.Info($"vsync - {gpuSection["vsync"]}");
            BrdXeniaVerticalSyncSetting.Visibility = Visibility.Visible;
            ChkXeniaVSync.IsChecked = (bool)gpuSection["vsync"];
        }
        else
        {
            Logger.Warning("`vsync` is missing from configuration file");
            BrdXeniaVerticalSyncSetting.Visibility = Visibility.Collapsed;
        }

        // framerate_limit
        if (gpuSection.ContainsKey("framerate_limit"))
        {
            Logger.Info($"framerate_limit - {gpuSection["framerate_limit"]}");
            BrdXeniaFramerateLimitSetting.Visibility = Visibility.Visible;
            try
            {
                SldXeniaFramerate.Value = int.Parse(gpuSection["framerate_limit"].ToString());
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                SldXeniaFramerate.Value = 60;
            }
        }
        else
        {
            Logger.Warning("`framerate_limit` is missing from configuration file");
            BrdXeniaFramerateLimitSetting.Visibility = Visibility.Collapsed;
        }

        // gpu
        if (gpuSection.ContainsKey("gpu"))
        {
            Logger.Info($"gpu - {gpuSection["gpu"]}");
            BrdGraphicsApiSetting.Visibility = Visibility.Visible;
            CmbGpuApi.SelectedValue = gpuSection["gpu"].ToString();
            if (CmbGpuApi.SelectedIndex < 0)
            {
                CmbGpuApi.SelectedIndex = 0;
            }
        }
        else
        {
            Logger.Warning("`gpu` is missing from configuration file");
            BrdGraphicsApiSetting.Visibility = Visibility.Collapsed;
        }

        // render_target_path_d3d12
        if (gpuSection.ContainsKey("render_target_path_d3d12"))
        {
            Logger.Info($"render_target_path_d3d12 - {gpuSection["render_target_path_d3d12"]}");
            BrdD3D12RenderTargetPathSetting.Visibility = Visibility.Visible;
            CmbD3D12RenderTargetPath.SelectedValue = gpuSection["render_target_path_d3d12"].ToString();
            if (CmbD3D12RenderTargetPath.SelectedIndex < 0)
            {
                CmbD3D12RenderTargetPath.SelectedIndex = 0;
            }
        }
        else
        {
            Logger.Warning("`render_target_path_d3d12` is missing from configuration file");
            BrdD3D12RenderTargetPathSetting.Visibility = Visibility.Collapsed;
        }

        // render_target_path_vulkan
        if (gpuSection.ContainsKey("render_target_path_vulkan"))
        {
            Logger.Info($"render_target_path_vulkan - {gpuSection["render_target_path_vulkan"]}");
            BrdVulkanRenderTargetPathSetting.Visibility = Visibility.Visible;
            CmbVulkanRenderTargetPath.SelectedValue = gpuSection["render_target_path_vulkan"].ToString();
            if (CmbVulkanRenderTargetPath.SelectedIndex < 0)
            {
                CmbVulkanRenderTargetPath.SelectedIndex = 0;
            }
        }
        else
        {
            Logger.Warning("`render_target_path_vulkan` is missing from configuration file");
            BrdVulkanRenderTargetPathSetting.Visibility = Visibility.Collapsed;
        }

        // readback_resolve
        if (gpuSection.ContainsKey("readback_resolve"))
        {
            Logger.Info($"readback_resolve - {gpuSection["readback_resolve"]}");
            BrdReadbackResolveSetting.Visibility = Visibility.Visible;
            ChkReadbackResolve.IsChecked = (bool)gpuSection["readback_resolve"];
        }
        else
        {
            Logger.Warning("`readback_resolve` is missing from configuration file");
            BrdReadbackResolveSetting.Visibility = Visibility.Collapsed;
        }

        // draw_resolution_scale
        if (gpuSection.ContainsKey("draw_resolution_scale_x") && gpuSection.ContainsKey("draw_resolution_scale_y"))
        {
            Logger.Info($"draw_resolution_scale_x - {gpuSection["draw_resolution_scale_x"]}");
            Logger.Info($"draw_resolution_scale_y - {gpuSection["draw_resolution_scale_y"]}");
            BrdDrawResolutionScaleSetting.Visibility = Visibility.Visible;
            try
            {
                SldDrawResolutionScale.Value = int.Parse(gpuSection["draw_resolution_scale_x"].ToString());
                SldDrawResolutionScale.Value = int.Parse(gpuSection["draw_resolution_scale_y"].ToString());
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                SldDrawResolutionScale.Value = 1;
            }
        }
        else
        {
            Logger.Warning("`readback_resolve` is missing from configuration file");
            BrdDrawResolutionScaleSetting.Visibility = Visibility.Collapsed;
        }

        // gpu_allow_invalid_fetch_constants
        if (gpuSection.ContainsKey("gpu_allow_invalid_fetch_constants"))
        {
            Logger.Info($"gpu_allow_invalid_fetch_constants - {gpuSection["gpu_allow_invalid_fetch_constants"]}");
            BrdAllowInvalidFetchConstantsSetting.Visibility = Visibility.Visible;
            ChkAllowInvalidFetchConstants.IsChecked = (bool)gpuSection["gpu_allow_invalid_fetch_constants"];
        }
        else
        {
            Logger.Warning("`gpu_allow_invalid_fetch_constants` is missing from configuration file");
            BrdAllowInvalidFetchConstantsSetting.Visibility = Visibility.Collapsed;
        }

        // gamma_render_target_as_srgb
        if (gpuSection.ContainsKey("gamma_render_target_as_srgb"))
        {
            Logger.Info($"gamma_render_target_as_srgb - {gpuSection["gamma_render_target_as_srgb"]}");
            BrdGammaRenderTargetAsSrgbSetting.Visibility = Visibility.Visible;
            ChkGammaRenderTargetAsSrgb.IsChecked = (bool)gpuSection["gamma_render_target_as_srgb"];
        }
        else
        {
            Logger.Warning("`gamma_render_target_as_srgb` is missing from configuration file");
            BrdGammaRenderTargetAsSrgbSetting.Visibility = Visibility.Collapsed;
        }

        // query_occlusion_sample_lower_threshold
        if (gpuSection.ContainsKey("query_occlusion_sample_lower_threshold"))
        {
            BrdQueryOcclusionFakeSampleCountLowerSetting.Visibility = Visibility.Visible;
            try
            {
                Logger.Info($"query_occlusion_sample_lower_threshold - {gpuSection["query_occlusion_sample_lower_threshold"]}");
                TxtQueryOcclusionFakeSampleCountLower.Text = int.Parse(gpuSection["query_occlusion_sample_lower_threshold"].ToString()).ToString();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                TxtQueryOcclusionFakeSampleCountLower.Text = "80";
            }
        }
        else
        {
            Logger.Warning("`query_occlusion_sample_lower_threshold` is missing from configuration file");
            BrdQueryOcclusionFakeSampleCountLowerSetting.Visibility = Visibility.Collapsed;
        }

        // query_occlusion_sample_upper_threshold
        if (gpuSection.ContainsKey("query_occlusion_sample_upper_threshold"))
        {
            BrdQueryOcclusionFakeSampleCountUpperSetting.Visibility = Visibility.Visible;
            try
            {
                Logger.Info($"query_occlusion_sample_upper_threshold - {gpuSection["query_occlusion_sample_upper_threshold"]}");
                TxtQueryOcclusionFakeSampleCountUpper.Text = int.Parse(gpuSection["query_occlusion_sample_upper_threshold"].ToString()).ToString();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                TxtQueryOcclusionFakeSampleCountUpper.Text = "100";
            }
        }
        else
        {
            Logger.Warning("`query_occlusion_sample_upper_threshold` is missing from configuration file");
            BrdQueryOcclusionFakeSampleCountUpperSetting.Visibility = Visibility.Collapsed;
        }

        // use_fuzzy_alpha_epsilon
        if (gpuSection.ContainsKey("use_fuzzy_alpha_epsilon"))
        {
            Logger.Info($"use_fuzzy_alpha_epsilon - {gpuSection["use_fuzzy_alpha_epsilon"]}");
            BrdFuzzyAlphaEpsilonSetting.Visibility = Visibility.Visible;
            ChkFuzzyAlphaEpsilon.IsChecked = (bool)gpuSection["use_fuzzy_alpha_epsilon"];
        }
        else
        {
            Logger.Warning("`use_fuzzy_alpha_epsilon` is missing from configuration file");
            BrdFuzzyAlphaEpsilonSetting.Visibility = Visibility.Collapsed;
        }

        // clear_memory_page_state
        if (gpuSection.ContainsKey("clear_memory_page_state"))
        {
            Logger.Info($"clear_memory_page_state - {gpuSection["clear_memory_page_state"]}");
            BrdClearGpuCacheSetting.Visibility = Visibility.Visible;
            ChkClearGpuCache.IsChecked = (bool)gpuSection["clear_memory_page_state"];
        }
        else
        {
            Logger.Warning("`clear_memory_page_state` is missing from configuration file");
            BrdClearGpuCacheSetting.Visibility = Visibility.Collapsed;
        }
    }

    private void SaveGpuSettings(TomlTable gpuSection)
    {
        // vsync
        if (gpuSection.ContainsKey("vsync"))
        {
            Logger.Info($"vsync - {ChkXeniaVSync.IsChecked}");
            gpuSection["vsync"] = ChkXeniaVSync.IsChecked;
        }

        // framerate_limit
        if (gpuSection.ContainsKey("framerate_limit"))
        {
            Logger.Info($"framerate_limit - {gpuSection["framerate_limit"]}");
            gpuSection["framerate_limit"] = (int)SldXeniaFramerate.Value;
        }

        // gpu
        if (gpuSection.ContainsKey("gpu"))
        {
            Logger.Info($"gpu - {CmbGpuApi.SelectedItem}");
            gpuSection["gpu"] = CmbGpuApi.SelectedValue;
        }

        // render_target_path_d3d12
        if (gpuSection.ContainsKey("render_target_path_d3d12"))
        {
            Logger.Info($"render_target_path_d3d12 - {CmbD3D12RenderTargetPath.SelectedItem}");
            gpuSection["render_target_path_d3d12"] = CmbD3D12RenderTargetPath.SelectedValue;
        }

        // render_target_path_vulkan
        if (gpuSection.ContainsKey("render_target_path_vulkan"))
        {
            Logger.Info($"render_target_path_vulkan - {CmbVulkanRenderTargetPath.SelectedItem}");
            gpuSection["render_target_path_vulkan"] = CmbVulkanRenderTargetPath.SelectedValue;
        }

        // readback_resolve
        if (gpuSection.ContainsKey("readback_resolve"))
        {
            Logger.Info($"readback_resolve - {ChkReadbackResolve.IsChecked}");
            gpuSection["readback_resolve"] = ChkReadbackResolve.IsChecked;
        }

        // draw_resolution_scale
        if (gpuSection.ContainsKey("draw_resolution_scale_x") && gpuSection.ContainsKey("draw_resolution_scale_y"))
        {
            Logger.Info($"draw_resolution_scale_x - {SldDrawResolutionScale.Value}");
            gpuSection["draw_resolution_scale_x"] = (int)SldDrawResolutionScale.Value;
            Logger.Info($"draw_resolution_scale_y - {SldDrawResolutionScale.Value}");
            gpuSection["draw_resolution_scale_y"] = (int)SldDrawResolutionScale.Value;
        }

        // gpu_allow_invalid_fetch_constants
        if (gpuSection.ContainsKey("gpu_allow_invalid_fetch_constants"))
        {
            Logger.Info($"gpu_allow_invalid_fetch_constants - {ChkAllowInvalidFetchConstants.IsChecked}");
            gpuSection["gpu_allow_invalid_fetch_constants"] = ChkAllowInvalidFetchConstants.IsChecked;
        }

        // gamma_render_target_as_srgb
        if (gpuSection.ContainsKey("gamma_render_target_as_srgb"))
        {
            Logger.Info($"gamma_render_target_as_srgb - {ChkGammaRenderTargetAsSrgb.IsChecked}");
            gpuSection["gamma_render_target_as_srgb"] = ChkGammaRenderTargetAsSrgb.IsChecked;
        }

        // query_occlusion_sample_lower_threshold
        if (gpuSection.ContainsKey("query_occlusion_sample_lower_threshold"))
        {
            int queryOcclusionSampleLowerThreshold = 0;
            try
            {
                queryOcclusionSampleLowerThreshold = int.Parse(TxtQueryOcclusionFakeSampleCountLower.Text);
                int queryOcclusionSampleUpperThreshold;
                try
                {
                    queryOcclusionSampleUpperThreshold = int.Parse(TxtQueryOcclusionFakeSampleCountUpper.Text);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                    queryOcclusionSampleUpperThreshold = int.MaxValue;
                }
                if (queryOcclusionSampleLowerThreshold < -1)
                {
                    queryOcclusionSampleLowerThreshold = -1;
                }
                else if (queryOcclusionSampleLowerThreshold > Int32.MaxValue)
                {
                    queryOcclusionSampleLowerThreshold = Int32.MaxValue;
                }
                else if (queryOcclusionSampleLowerThreshold > queryOcclusionSampleUpperThreshold)
                {
                    queryOcclusionSampleLowerThreshold = queryOcclusionSampleUpperThreshold;
                }
                TxtQueryOcclusionFakeSampleCountLower.Text = queryOcclusionSampleLowerThreshold.ToString();
                gpuSection["query_occlusion_sample_lower_threshold"] = queryOcclusionSampleLowerThreshold;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                queryOcclusionSampleLowerThreshold = 80;
                TxtQueryOcclusionFakeSampleCountLower.Text = queryOcclusionSampleLowerThreshold.ToString();
                gpuSection["query_occlusion_sample_lower_threshold"] = queryOcclusionSampleLowerThreshold;
                CustomMessageBox.Show("Invalid Input (Query Occlusion Sample Lower Threshold)", "Query Occlusion Sample Lower Threshold must be a number.\nSetting the default value of 80.");
            }
        }

        // query_occlusion_sample_upper_threshold
        if (gpuSection.ContainsKey("query_occlusion_sample_upper_threshold"))
        {
            int queryOcclusionSampleUpperThreshold = 0;
            try
            {
                queryOcclusionSampleUpperThreshold = int.Parse(TxtQueryOcclusionFakeSampleCountUpper.Text);
                int queryOcclusionSampleLowerThreshold;
                try
                {
                    queryOcclusionSampleLowerThreshold = int.Parse(TxtQueryOcclusionFakeSampleCountLower.Text);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                    queryOcclusionSampleLowerThreshold = -1;
                }
                if (queryOcclusionSampleUpperThreshold < -1)
                {
                    queryOcclusionSampleUpperThreshold = -1;
                }
                else if (queryOcclusionSampleUpperThreshold > Int32.MaxValue)
                {
                    queryOcclusionSampleUpperThreshold = Int32.MaxValue;
                }
                else if (queryOcclusionSampleUpperThreshold < queryOcclusionSampleLowerThreshold)
                {
                    queryOcclusionSampleUpperThreshold = queryOcclusionSampleLowerThreshold + 1;
                }

                TxtQueryOcclusionFakeSampleCountUpper.Text = queryOcclusionSampleUpperThreshold.ToString();
                gpuSection["query_occlusion_sample_upper_threshold"] = queryOcclusionSampleUpperThreshold;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                queryOcclusionSampleUpperThreshold = 100;
                TxtQueryOcclusionFakeSampleCountUpper.Text = queryOcclusionSampleUpperThreshold.ToString();
                gpuSection["query_occlusion_sample_lower_threshold"] = queryOcclusionSampleUpperThreshold;
                CustomMessageBox.Show("Invalid Input (Query Occlusion Sample Upper Threshold)", "Query Occlusion Sample Upper Threshold must be a number.\nSetting the default value of 100.");
            }
        }

        // use_fuzzy_alpha_epsilon
        if (gpuSection.ContainsKey("use_fuzzy_alpha_epsilon"))
        {
            Logger.Info($"use_fuzzy_alpha_epsilon - {ChkFuzzyAlphaEpsilon.IsChecked}");
            gpuSection["use_fuzzy_alpha_epsilon"] = ChkFuzzyAlphaEpsilon.IsChecked;
        }

        // clear_memory_page_state
        if (gpuSection.ContainsKey("clear_memory_page_state"))
        {
            Logger.Info($"clear_memory_page_state - {ChkClearGpuCache.IsChecked}");
            gpuSection["clear_memory_page_state"] = ChkClearGpuCache.IsChecked;
        }
    }
}