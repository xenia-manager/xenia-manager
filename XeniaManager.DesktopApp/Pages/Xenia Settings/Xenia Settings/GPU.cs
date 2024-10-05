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
        /// Loads the GPU Settings into the UI
        /// </summary>
        /// <param name="sectionTable">Portion of .toml file dedicated to GPU Settings</param>
        private void LoadGPUSettings(TomlTable sectionTable)
        {
            // "clear_memory_page_state" setting
            if (sectionTable.ContainsKey("clear_memory_page_state"))
            {
                Log.Information($"clear_memory_page_state - {(bool)sectionTable["clear_memory_page_state"]}");
                chkClearGPUCache.IsChecked = (bool)sectionTable["clear_memory_page_state"];
            }

            // "draw_resolution_scale" setting
            if (sectionTable.ContainsKey("draw_resolution_scale_x") && sectionTable.ContainsKey("draw_resolution_scale_y"))
            {
                Log.Information($"draw_resolution_scale_x - {sectionTable["draw_resolution_scale_x"].ToString()}");
                sldDrawResolutionScale.Value = int.Parse(sectionTable["draw_resolution_scale_x"].ToString());
                Log.Information($"draw_resolution_scale_y - {sectionTable["draw_resolution_scale_y"].ToString()}");
                sldDrawResolutionScale.Value = int.Parse(sectionTable["draw_resolution_scale_y"].ToString());
            }

            // "framerate_limit" setting
            if (sectionTable.ContainsKey("framerate_limit"))
            {
                Log.Information($"framerate_limit - {sectionTable["framerate_limit"].ToString()}");
                sldXeniaFramerate.Value = int.Parse(sectionTable["framerate_limit"].ToString());
                AutomationProperties.SetName(sldXeniaFramerate, $"Xenia Framerate Limiter: {sldXeniaFramerate.Value} FPS");
            }

            // "gamma_render_target_as_srgb" setting
            if (sectionTable.ContainsKey("gamma_render_target_as_srgb"))
            {
                Log.Information($"gamma_render_target_as_srgb - {sectionTable["gamma_render_target_as_srgb"]}");
                chkGammaRenderTargetAsSRGB.IsChecked = (bool)sectionTable["gamma_render_target_as_srgb"];
            }

            // "gpu" setting
            if (sectionTable.ContainsKey("gpu"))
            {
                Log.Information($"gpu - {sectionTable["gpu"] as string}");
                switch (sectionTable["gpu"] as string)
                {
                    case "d3d12":
                        cmbGPUApi.SelectedIndex = 1;
                        break;
                    case "vulkan":
                        cmbGPUApi.SelectedIndex = 2;
                        break;
                    default:
                        cmbGPUApi.SelectedIndex = 0;
                        break;
                }
            }

            // "gpu_allow_invalid_fetch_constants" setting
            if (sectionTable.ContainsKey("gpu_allow_invalid_fetch_constants"))
            {
                Log.Information($"gpu_allow_invalid_fetch_constants - {sectionTable["gpu_allow_invalid_fetch_constants"]}");
                chkAllowInvalidFetchConstants.IsChecked = (bool)sectionTable["gpu_allow_invalid_fetch_constants"];
            }

            // "render_target_path_d3d12" setting
            if (sectionTable.ContainsKey("render_target_path_d3d12"))
            {
                Log.Information($"render_target_path_d3d12 - {sectionTable["render_target_path_d3d12"] as string}");
                switch (sectionTable["render_target_path_d3d12"] as string)
                {
                    case "rtv":
                        cmbD3D12RenderTargetPath.SelectedIndex = 1;
                        break;
                    case "rov":
                        cmbD3D12RenderTargetPath.SelectedIndex = 2;
                        break;
                    default:
                        cmbD3D12RenderTargetPath.SelectedIndex = 0;
                        break;
                }
            }

            // "render_target_path_vulkan" setting
            if (sectionTable.ContainsKey("render_target_path_vulkan"))
            {
                Log.Information($"render_target_path_vulkan - {sectionTable["render_target_path_vulkan"] as string}");
                switch (sectionTable["render_target_path_vulkan"] as string)
                {
                    case "fbo":
                        cmbVulkanRenderTargetPath.SelectedIndex = 1;
                        break;
                    case "fsi":
                        cmbVulkanRenderTargetPath.SelectedIndex = 2;
                        break;
                    default:
                        cmbVulkanRenderTargetPath.SelectedIndex = 0;
                        break;
                }
            }

            // "use_fuzzy_alpha_epsilon" setting
            if (sectionTable.ContainsKey("use_fuzzy_alpha_epsilon"))
            {
                Log.Information($"use_fuzzy_alpha_epsilon - {sectionTable["use_fuzzy_alpha_epsilon"]}");
                chkFuzzyAlphaEpsilon.IsChecked = (bool)sectionTable["use_fuzzy_alpha_epsilon"];
            }

            // "vsync" setting
            if (sectionTable.ContainsKey("vsync"))
            {
                Log.Information($"vsync - {sectionTable["vsync"]}");
                chkVSync.IsChecked = (bool)sectionTable["vsync"];
            }

            // "query_occlusion_fake_sample_count" setting
            if (sectionTable.ContainsKey("query_occlusion_fake_sample_count"))
            {
                Log.Information($"query_occlusion_fake_sample_count - {sectionTable["query_occlusion_fake_sample_count"].ToString()}");
                txtQueryOcclusionFakeSampleCount.Text = sectionTable["query_occlusion_fake_sample_count"].ToString();
            }
        }

        /// <summary>
        /// Saves the GPU Settings into the configuration file
        /// </summary>
        /// <param name="sectionTable">Portion of .toml file dedicated to GPU Settings</param>
        private void SaveGPUSettings(TomlTable sectionTable)
        {
            // "clear_memory_page_state" setting
            if (sectionTable.ContainsKey("clear_memory_page_state"))
            {
                Log.Information($"clear_memory_page_state - {chkClearGPUCache.IsChecked}");
                sectionTable["clear_memory_page_state"] = chkClearGPUCache.IsChecked;
            }

            // "draw_resolution_scale" setting
            if (sectionTable.ContainsKey("draw_resolution_scale_x") && sectionTable.ContainsKey("draw_resolution_scale_y"))
            {
                Log.Information($"draw_resolution_scale_x - {(int)sldDrawResolutionScale.Value}");
                sectionTable["draw_resolution_scale_x"] = (int)sldDrawResolutionScale.Value;
                Log.Information($"draw_resolution_scale_y - {(int)sldDrawResolutionScale.Value}");
                sectionTable["draw_resolution_scale_y"] = (int)sldDrawResolutionScale.Value;
            }

            // "framerate_limit" setting
            if (sectionTable.ContainsKey("framerate_limit"))
            {
                Log.Information($"framerate_limit - {sldXeniaFramerate.Value}");
                sectionTable["framerate_limit"] = (int)sldXeniaFramerate.Value;
            }

            // "gamma_render_target_as_srgb" setting
            if (sectionTable.ContainsKey("gamma_render_target_as_srgb"))
            {
                Log.Information($"gamma_render_target_as_srgb - {chkGammaRenderTargetAsSRGB.IsChecked}");
                sectionTable["gamma_render_target_as_srgb"] = chkGammaRenderTargetAsSRGB.IsChecked;
            }

            // "gpu" setting
            if (sectionTable.ContainsKey("gpu"))
            {
                Log.Information($"gpu - {(cmbGPUApi.SelectedItem as ComboBoxItem).Content}");
                switch (cmbGPUApi.SelectedIndex)
                {
                    case 1:
                        // "d3d12"
                        sectionTable["gpu"] = "d3d12";
                        break;
                    case 2:
                        // "vulkan"
                        sectionTable["gpu"] = "vulkan";
                        break;
                    default:
                        // "any"
                        sectionTable["gpu"] = "any";
                        break;
                }
            }

            // "gpu_allow_invalid_fetch_constants" setting
            if (sectionTable.ContainsKey("gpu_allow_invalid_fetch_constants"))
            {
                Log.Information($"gpu_allow_invalid_fetch_constants - {chkAllowInvalidFetchConstants.IsChecked}");
                sectionTable["gpu_allow_invalid_fetch_constants"] = chkAllowInvalidFetchConstants.IsChecked;
            }

            // "render_target_path_d3d12" setting
            if (sectionTable.ContainsKey("render_target_path_d3d12"))
            {
                Log.Information($"render_target_path_d3d12 - {(cmbD3D12RenderTargetPath.SelectedItem as ComboBoxItem).Content}");
                switch (cmbD3D12RenderTargetPath.SelectedIndex)
                {
                    case 1:
                        // "rtv"
                        sectionTable["render_target_path_d3d12"] = "rtv";
                        break;
                    case 2:
                        // "rov"
                        sectionTable["render_target_path_d3d12"] = "rov";
                        break;
                    default:
                        // "any"
                        sectionTable["render_target_path_d3d12"] = "";
                        break;
                }
            }

            // "render_target_path_vulkan" setting
            if (sectionTable.ContainsKey("render_target_path_vulkan"))
            {
                Log.Information($"render_target_path_vulkan - {(cmbVulkanRenderTargetPath.SelectedItem as ComboBoxItem).Content}");
                switch (cmbVulkanRenderTargetPath.SelectedIndex)
                {
                    case 1:
                        // "fbo"
                        sectionTable["render_target_path_vulkan"] = "fbo";
                        break;
                    case 2:
                        // "fsi"
                        sectionTable["render_target_path_vulkan"] = "fsi";
                        break;
                    default:
                        // "any"
                        sectionTable["render_target_path_vulkan"] = "";
                        break;
                }
            }

            // "use_fuzzy_alpha_epsilon" setting
            if (sectionTable.ContainsKey("use_fuzzy_alpha_epsilon"))
            {
                Log.Information($"use_fuzzy_alpha_epsilon - {chkFuzzyAlphaEpsilon.IsChecked}");
                sectionTable["use_fuzzy_alpha_epsilon"] = chkFuzzyAlphaEpsilon.IsChecked;
            }

            // "vsync" setting
            if (sectionTable.ContainsKey("vsync"))
            {
                Log.Information($"vsync - {chkVSync.IsChecked}");
                sectionTable["vsync"] = chkVSync.IsChecked;
            }

            // "query_occlusion_fake_sample_count" setting
            if (sectionTable.ContainsKey("query_occlusion_fake_sample_count"))
            {
                try
                {
                    int QueryOcclusionFakeSampleCountInt = int.Parse(txtQueryOcclusionFakeSampleCount.Text);
                    Log.Information($"query_occlusion_fake_sample_count - {QueryOcclusionFakeSampleCountInt.ToString()}");
                    sectionTable["query_occlusion_fake_sample_count"] = QueryOcclusionFakeSampleCountInt;
                }
                catch (Exception ex)
                {
                    // If the input is incorrect, do the default
                    Log.Error(ex.Message + "\nFull Error:\n" + ex);
                    sectionTable["query_occlusion_fake_sample_count"] = 100;
                    txtQueryOcclusionFakeSampleCount.Text = "100";
                    MessageBox.Show("Invalid input: query_occlusion_fake_sample_count must be a number.\nSetting the default value of 100.");
                }
            }
        }
    }
}