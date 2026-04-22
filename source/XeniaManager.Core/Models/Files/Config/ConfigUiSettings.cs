using XeniaManager.Core.Utilities;

namespace XeniaManager.Core.Models.Files.Config;

/// <summary>
/// Provides predefined UI definitions for common Xenia configuration sections.
/// Use these definitions to customize how config options are displayed in the editor.
/// </summary>
public static class ConfigUiSettings
{
    /// <summary>
    /// Gets a UI definition for APU (Audio) settings.
    /// Settings from the [APU] section.
    /// </summary>
    private static ConfigUiDefinition ApuSettings => new ConfigUiDefinition(
        new ConfigSectionDefinition("APU")
            {
                DisplayName = LocalizationHelper.GetText("ConfigUiSettings.Section.APU")
            }
            .AddComboBox("apu", new Dictionary<object, string>
            {
                { "any", LocalizationHelper.GetText("ConfigUiSettings.APU.apu.option.any") },
                { "nop", LocalizationHelper.GetText("ConfigUiSettings.APU.apu.option.nop") },
                { "sdl", LocalizationHelper.GetText("ConfigUiSettings.APU.apu.option.sdl") },
                { "xaudio2", LocalizationHelper.GetText("ConfigUiSettings.APU.apu.option.xaudio2") }
            }, LocalizationHelper.GetText("ConfigUiSettings.APU.apu.Title"), LocalizationHelper.GetText("ConfigUiSettings.APU.apu.Comment"))
            .AddSlider("apu_max_queued_frames", 4, 64, 1, LocalizationHelper.GetText("ConfigUiSettings.APU.apu_max_queued_frames.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.APU.apu_max_queued_frames.Comment"))
            .AddToggle("mute", LocalizationHelper.GetText("ConfigUiSettings.APU.mute.Title"), LocalizationHelper.GetText("ConfigUiSettings.APU.mute.Comment"))
            .AddToggle("enable_xmp", LocalizationHelper.GetText("ConfigUiSettings.APU.enable_xmp.Title"), LocalizationHelper.GetText("ConfigUiSettings.APU.enable_xmp.Comment"))
            .AddSlider("xmp_default_volume", 0, 100, 1, LocalizationHelper.GetText("ConfigUiSettings.APU.xmp_default_volume.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.APU.xmp_default_volume.Comment"))
            .AddToggle("use_dedicated_xma_thread", LocalizationHelper.GetText("ConfigUiSettings.APU.use_dedicated_xma_thread.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.APU.use_dedicated_xma_thread.Comment"))
            .AddToggle("use_new_decoder", LocalizationHelper.GetText("ConfigUiSettings.APU.use_new_decoder.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.APU.use_new_decoder.Comment"))
            .AddComboBox("xma_decoder", new Dictionary<object, string>
                {
                    { "fake", LocalizationHelper.GetText("ConfigUiSettings.APU.xma_decoder.option.fake") },
                    { "master", LocalizationHelper.GetText("ConfigUiSettings.APU.xma_decoder.option.master") },
                    { "old", LocalizationHelper.GetText("ConfigUiSettings.APU.xma_decoder.option.old") },
                    { "new", LocalizationHelper.GetText("ConfigUiSettings.APU.xma_decoder.option.new") }
                }, LocalizationHelper.GetText("ConfigUiSettings.APU.xma_decoder.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.APU.xma_decoder.Comment")))
    {
        Title = LocalizationHelper.GetText("ConfigUiSettings.Section.APU")
    };

    /// <summary>
    /// Gets a UI definition for Content settings.
    /// Settings from the [Content] section.
    /// </summary>
    private static ConfigUiDefinition ContentSettings => new ConfigUiDefinition(
        new ConfigSectionDefinition("Content")
            {
                DisplayName = LocalizationHelper.GetText("ConfigUiSettings.Section.Content")
            }
            .AddComboBox("license_mask", new Dictionary<object, string>
                {
                    { 0, LocalizationHelper.GetText("ConfigUiSettings.Content.license_mask.option.0") },
                    { 1, LocalizationHelper.GetText("ConfigUiSettings.Content.license_mask.option.1") },
                    { -1, LocalizationHelper.GetText("ConfigUiSettings.Content.license_mask.option.-1") }
                }, LocalizationHelper.GetText("ConfigUiSettings.Content.license_mask.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.Content.license_mask.Comment"))
    )
    {
        Title = LocalizationHelper.GetText("ConfigUiSettings.Section.Content")
    };

    /// <summary>
    /// Gets a UI definition for CPU settings.
    /// Settings from the [CPU] section.
    /// </summary>
    private static ConfigUiDefinition CpuSettings => new ConfigUiDefinition(
        new ConfigSectionDefinition("CPU")
            {
                DisplayName = LocalizationHelper.GetText("ConfigUiSettings.Section.CPU")
            }
            .AddToggle("break_on_unimplemented_instructions", LocalizationHelper.GetText("ConfigUiSettings.CPU.break_on_unimplemented_instructions.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.CPU.break_on_unimplemented_instructions.Comment"))
            .AddToggle("disable_context_promotion", LocalizationHelper.GetText("ConfigUiSettings.CPU.disable_context_promotion.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.CPU.disable_context_promotion.Comment"))
            .AddToggle("disassemble_functions", LocalizationHelper.GetText("ConfigUiSettings.CPU.disassemble_functions.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.CPU.disassemble_functions.Comment"))
    )
    {
        Title = LocalizationHelper.GetText("ConfigUiSettings.Section.CPU")
    };

    /// <summary>
    /// Gets a UI definition for D3D12 settings.
    /// Settings from the [D3D12] section.
    /// </summary>
    private static ConfigUiDefinition D3D12Settings => new ConfigUiDefinition(
        new ConfigSectionDefinition("D3D12")
            {
                DisplayName = LocalizationHelper.GetText("ConfigUiSettings.Section.D3D12")
            }
            .AddToggle("d3d12_allow_variable_refresh_rate_and_tearing", LocalizationHelper.GetText("ConfigUiSettings.D3D12.d3d12_allow_variable_refresh_rate_and_tearing.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.D3D12.d3d12_allow_variable_refresh_rate_and_tearing.Comment"))
            .AddComboBox("d3d12_queue_priority", new Dictionary<object, string>
                {
                    { 0, LocalizationHelper.GetText("ConfigUiSettings.D3D12.d3d12_queue_priority.option.0") },
                    { 1, LocalizationHelper.GetText("ConfigUiSettings.D3D12.d3d12_queue_priority.option.1") },
                    { 2, LocalizationHelper.GetText("ConfigUiSettings.D3D12.d3d12_queue_priority.option.2") }
                }, LocalizationHelper.GetText("ConfigUiSettings.D3D12.d3d12_queue_priority.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.D3D12.d3d12_queue_priority.Comment"))
    )
    {
        Title = LocalizationHelper.GetText("ConfigUiSettings.Section.D3D12")
    };

    /// <summary>
    /// Gets a UI definition for Display settings.
    /// Settings from the [Display] section.
    /// </summary>
    private static ConfigUiDefinition DisplaySettings => new ConfigUiDefinition(
        new ConfigSectionDefinition("Display")
            {
                DisplayName = LocalizationHelper.GetText("ConfigUiSettings.Section.Display")
            }
            .AddToggle("fullscreen", LocalizationHelper.GetText("ConfigUiSettings.Display.fullscreen.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.Display.fullscreen.Comment"))
            .AddToggle("present_letterbox", LocalizationHelper.GetText("ConfigUiSettings.Display.present_letterbox.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.Display.present_letterbox.Comment"))
            .AddComboBox("postprocess_antialiasing", new Dictionary<object, string>
                {
                    { "none", LocalizationHelper.GetText("ConfigUiSettings.Display.postprocess_antialiasing.option.none") },
                    { "fxaa", LocalizationHelper.GetText("ConfigUiSettings.Display.postprocess_antialiasing.option.fxaa") },
                    { "fxaa_extreme", LocalizationHelper.GetText("ConfigUiSettings.Display.postprocess_antialiasing.option.fxaa_extreme") }
                }, LocalizationHelper.GetText("ConfigUiSettings.Display.postprocess_antialiasing.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.Display.postprocess_antialiasing.Comment"))
            .AddComboBox("postprocess_scaling_and_sharpening", new Dictionary<object, string>
                {
                    { "bilinear", LocalizationHelper.GetText("ConfigUiSettings.Display.postprocess_scaling_and_sharpening.option.bilinear") },
                    { "cas", LocalizationHelper.GetText("ConfigUiSettings.Display.postprocess_scaling_and_sharpening.option.cas") },
                    { "fsr", LocalizationHelper.GetText("ConfigUiSettings.Display.postprocess_scaling_and_sharpening.option.fsr") }
                }, LocalizationHelper.GetText("ConfigUiSettings.Display.postprocess_scaling_and_sharpening.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.Display.postprocess_scaling_and_sharpening.Comment"))
            .AddSlider("postprocess_ffx_fsr_sharpness_reduction", 0, 1, 0.01, LocalizationHelper.GetText("ConfigUiSettings.Display.postprocess_ffx_fsr_sharpness_reduction.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.Display.postprocess_ffx_fsr_sharpness_reduction.Comment"), "F2")
            .AddSlider("postprocess_ffx_cas_additional_sharpness", 0, 1, 0.01, LocalizationHelper.GetText("ConfigUiSettings.Display.postprocess_ffx_cas_additional_sharpness.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.Display.postprocess_ffx_cas_additional_sharpness.Comment"), "F2")
            .AddSlider("postprocess_ffx_fsr_max_upsampling_passes", 1, 4, 1, LocalizationHelper.GetText("ConfigUiSettings.Display.postprocess_ffx_fsr_max_upsampling_passes.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.Display.postprocess_ffx_fsr_max_upsampling_passes.Comment"))
            .AddToggle("postprocess_dither", LocalizationHelper.GetText("ConfigUiSettings.Display.postprocess_dither.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.Display.postprocess_dither.Comment"))
    )
    {
        Title = LocalizationHelper.GetText("ConfigUiSettings.Section.Display")
    };

    /// <summary>
    /// Gets a UI definition for General settings.
    /// Settings from the [General] section.
    /// </summary>
    private static ConfigUiDefinition GeneralSettings => new ConfigUiDefinition(
        new ConfigSectionDefinition("General")
            {
                DisplayName = LocalizationHelper.GetText("ConfigUiSettings.Section.General")
            }
            .AddToggle("allow_plugins", LocalizationHelper.GetText("ConfigUiSettings.General.allow_plugins.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.General.allow_plugins.Comment"))
            .AddToggle("apply_patches", LocalizationHelper.GetText("ConfigUiSettings.General.apply_patches.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.General.apply_patches.Comment"))
            .AddToggle("controller_hotkeys", LocalizationHelper.GetText("ConfigUiSettings.General.controller_hotkeys.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.General.controller_hotkeys.Comment"))
            .AddToggle("discord", LocalizationHelper.GetText("ConfigUiSettings.General.discord.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.General.discord.Comment"))
            .AddTextBox("notification_sound_path", LocalizationHelper.GetText("ConfigUiSettings.General.notification_sound_path.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.General.notification_sound_path.Comment"))
            .AddTextBox("launch_module", LocalizationHelper.GetText("ConfigUiSettings.General.launch_module.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.General.launch_module.Comment"))
    )
    {
        Title = LocalizationHelper.GetText("ConfigUiSettings.Section.General")
    };

    /// <summary>
    /// Gets a UI definition for GPU settings.
    /// Settings from the [GPU] section.
    /// </summary>
    private static ConfigUiDefinition GpuSettings => new ConfigUiDefinition(
        new ConfigSectionDefinition("GPU")
            {
                DisplayName = LocalizationHelper.GetText("ConfigUiSettings.Section.GPU")
            }
            .AddComboBox("gpu", new Dictionary<object, string>
                {
                    { "any", LocalizationHelper.GetText("ConfigUiSettings.GPU.gpu.option.any") },
                    { "d3d12", LocalizationHelper.GetText("ConfigUiSettings.GPU.gpu.option.d3d12") },
                    { "vulkan", LocalizationHelper.GetText("ConfigUiSettings.GPU.gpu.option.vulkan") },
                    { "null", LocalizationHelper.GetText("ConfigUiSettings.GPU.gpu.option.null") }
                }, LocalizationHelper.GetText("ConfigUiSettings.GPU.gpu.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.GPU.gpu.Comment"))
            .AddComboBox("render_target_path_d3d12", new Dictionary<object, string>
                {
                    { "any", LocalizationHelper.GetText("ConfigUiSettings.GPU.render_target_path_d3d12.option.any") },
                    { "rtv", LocalizationHelper.GetText("ConfigUiSettings.GPU.render_target_path_d3d12.option.rtv") },
                    { "rov", LocalizationHelper.GetText("ConfigUiSettings.GPU.render_target_path_d3d12.option.rov") }
                }, LocalizationHelper.GetText("ConfigUiSettings.GPU.render_target_path_d3d12.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.GPU.render_target_path_d3d12.Comment"))
            .AddComboBox("render_target_path_vulkan", new Dictionary<object, string>
                {
                    { "any", LocalizationHelper.GetText("ConfigUiSettings.GPU.render_target_path_vulkan.option.any") },
                    { "fbo", LocalizationHelper.GetText("ConfigUiSettings.GPU.render_target_path_vulkan.option.fbo") },
                    { "fsi", LocalizationHelper.GetText("ConfigUiSettings.GPU.render_target_path_vulkan.option.fsi") }
                }, LocalizationHelper.GetText("ConfigUiSettings.GPU.render_target_path_vulkan.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.GPU.render_target_path_vulkan.Comment"))
            .AddComboBox("readback_resolve", new Dictionary<object, string>
                {
                    { "none", LocalizationHelper.GetText("ConfigUiSettings.GPU.readback_resolve.option.none") },
                    { "fast", LocalizationHelper.GetText("ConfigUiSettings.GPU.readback_resolve.option.fast") },
                    { "full", LocalizationHelper.GetText("ConfigUiSettings.GPU.readback_resolve.option.full") }
                }, LocalizationHelper.GetText("ConfigUiSettings.GPU.readback_resolve.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.GPU.readback_resolve.Comment"))
            .AddToggle("async_shader_compilation", LocalizationHelper.GetText("ConfigUiSettings.GPU.async_shader_compilation.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.GPU.async_shader_compilation.Comment"))
            .AddSlider("draw_resolution_scale_x", 1, 7, 1, LocalizationHelper.GetText("ConfigUiSettings.GPU.draw_resolution_scale_x.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.GPU.draw_resolution_scale_x.Comment"))
            .AddSlider("draw_resolution_scale_y", 1, 7, 1, LocalizationHelper.GetText("ConfigUiSettings.GPU.draw_resolution_scale_y.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.GPU.draw_resolution_scale_y.Comment"))
            .AddComboBox("anisotropic_override", new Dictionary<object, string>
                {
                    { -1, LocalizationHelper.GetText("ConfigUiSettings.GPU.anisotropic_override.option.-1") },
                    { 0, LocalizationHelper.GetText("ConfigUiSettings.GPU.anisotropic_override.option.0") },
                    { 1, LocalizationHelper.GetText("ConfigUiSettings.GPU.anisotropic_override.option.1") },
                    { 2, LocalizationHelper.GetText("ConfigUiSettings.GPU.anisotropic_override.option.2") },
                    { 3, LocalizationHelper.GetText("ConfigUiSettings.GPU.anisotropic_override.option.3") },
                    { 4, LocalizationHelper.GetText("ConfigUiSettings.GPU.anisotropic_override.option.4") },
                    { 5, LocalizationHelper.GetText("ConfigUiSettings.GPU.anisotropic_override.option.5") }
                }, LocalizationHelper.GetText("ConfigUiSettings.GPU.anisotropic_override.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.GPU.anisotropic_override.Comment"))
            .AddNumberBox("framerate_limit", 0, 1000, LocalizationHelper.GetText("ConfigUiSettings.GPU.framerate_limit.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.GPU.framerate_limit.Comment"))
            .AddToggle("vsync", LocalizationHelper.GetText("ConfigUiSettings.GPU.vsync.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.GPU.vsync.Comment"))
            .AddToggle("gpu_allow_invalid_fetch_constants", LocalizationHelper.GetText("ConfigUiSettings.GPU.gpu_allow_invalid_fetch_constants.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.GPU.gpu_allow_invalid_fetch_constants.Comment"))
            .AddToggle("gamma_render_target_as_srgb", LocalizationHelper.GetText("ConfigUiSettings.GPU.gamma_render_target_as_srgb.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.GPU.gamma_render_target_as_srgb.Comment"))
            .AddNumberBox("query_occlusion_sample_lower_threshold", -1, int.MaxValue, LocalizationHelper.GetText("ConfigUiSettings.GPU.query_occlusion_sample_lower_threshold.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.GPU.query_occlusion_sample_lower_threshold.Comment"))
            .AddNumberBox("query_occlusion_sample_upper_threshold", -1, int.MaxValue, LocalizationHelper.GetText("ConfigUiSettings.GPU.query_occlusion_sample_upper_threshold.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.GPU.query_occlusion_sample_upper_threshold.Comment"))
            .AddToggle("use_fuzzy_alpha_epsilon", LocalizationHelper.GetText("ConfigUiSettings.GPU.use_fuzzy_alpha_epsilon.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.GPU.use_fuzzy_alpha_epsilon.Comment"))
            .AddToggle("clear_memory_page_state", LocalizationHelper.GetText("ConfigUiSettings.GPU.clear_memory_page_state.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.GPU.clear_memory_page_state.Comment"))
    )
    {
        Title = LocalizationHelper.GetText("ConfigUiSettings.Section.GPU")
    };

    /// <summary>
    /// Gets a UI definition for HID.WinKey settings.
    /// Settings from the [HID.WinKey] section.
    /// </summary>
    private static ConfigUiDefinition HidWinKeySettings => new ConfigUiDefinition(
        new ConfigSectionDefinition("HID.WinKey")
            {
                DisplayName = LocalizationHelper.GetText("ConfigUiSettings.Section.HID.WinKey")
            }
            .AddTextBox("keybind_a", LocalizationHelper.GetText("ConfigUiSettings.HID.WinKey.keybind_a.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.HID.WinKey.keybind_a.Comment"))
            .AddTextBox("keybind_b", LocalizationHelper.GetText("ConfigUiSettings.HID.WinKey.keybind_b.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.HID.WinKey.keybind_b.Comment"))
            .AddTextBox("keybind_back", LocalizationHelper.GetText("ConfigUiSettings.HID.WinKey.keybind_back.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.HID.WinKey.keybind_back.Comment"))
            .AddTextBox("keybind_dpad_down", LocalizationHelper.GetText("ConfigUiSettings.HID.WinKey.keybind_dpad_down.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.HID.WinKey.keybind_dpad_down.Comment"))
            .AddTextBox("keybind_dpad_left", LocalizationHelper.GetText("ConfigUiSettings.HID.WinKey.keybind_dpad_left.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.HID.WinKey.keybind_dpad_left.Comment"))
            .AddTextBox("keybind_dpad_right", LocalizationHelper.GetText("ConfigUiSettings.HID.WinKey.keybind_dpad_right.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.HID.WinKey.keybind_dpad_right.Comment"))
            .AddTextBox("keybind_dpad_up", LocalizationHelper.GetText("ConfigUiSettings.HID.WinKey.keybind_dpad_up.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.HID.WinKey.keybind_dpad_up.Comment"))
            .AddTextBox("keybind_guide", LocalizationHelper.GetText("ConfigUiSettings.HID.WinKey.keybind_guide.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.HID.WinKey.keybind_guide.Comment"))
            .AddTextBox("keybind_left_shoulder", LocalizationHelper.GetText("ConfigUiSettings.HID.WinKey.keybind_left_shoulder.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.HID.WinKey.keybind_left_shoulder.Comment"))
            .AddTextBox("keybind_left_thumb", LocalizationHelper.GetText("ConfigUiSettings.HID.WinKey.keybind_left_thumb.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.HID.WinKey.keybind_left_thumb.Comment"))
            .AddTextBox("keybind_left_thumb_down", LocalizationHelper.GetText("ConfigUiSettings.HID.WinKey.keybind_left_thumb_down.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.HID.WinKey.keybind_left_thumb_down.Comment"))
            .AddTextBox("keybind_left_thumb_left", LocalizationHelper.GetText("ConfigUiSettings.HID.WinKey.keybind_left_thumb_left.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.HID.WinKey.keybind_left_thumb_left.Comment"))
            .AddTextBox("keybind_left_thumb_right", LocalizationHelper.GetText("ConfigUiSettings.HID.WinKey.keybind_left_thumb_right.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.HID.WinKey.keybind_left_thumb_right.Comment"))
            .AddTextBox("keybind_left_thumb_up", LocalizationHelper.GetText("ConfigUiSettings.HID.WinKey.keybind_left_thumb_up.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.HID.WinKey.keybind_left_thumb_up.Comment"))
            .AddTextBox("keybind_left_trigger", LocalizationHelper.GetText("ConfigUiSettings.HID.WinKey.keybind_left_trigger.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.HID.WinKey.keybind_left_trigger.Comment"))
            .AddTextBox("keybind_right_shoulder", LocalizationHelper.GetText("ConfigUiSettings.HID.WinKey.keybind_right_shoulder.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.HID.WinKey.keybind_right_shoulder.Comment"))
            .AddTextBox("keybind_right_thumb", LocalizationHelper.GetText("ConfigUiSettings.HID.WinKey.keybind_right_thumb.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.HID.WinKey.keybind_right_thumb.Comment"))
            .AddTextBox("keybind_right_thumb_down", LocalizationHelper.GetText("ConfigUiSettings.HID.WinKey.keybind_right_thumb_down.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.HID.WinKey.keybind_right_thumb_down.Comment"))
            .AddTextBox("keybind_right_thumb_left", LocalizationHelper.GetText("ConfigUiSettings.HID.WinKey.keybind_right_thumb_left.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.HID.WinKey.keybind_right_thumb_left.Comment"))
            .AddTextBox("keybind_right_thumb_right", LocalizationHelper.GetText("ConfigUiSettings.HID.WinKey.keybind_right_thumb_right.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.HID.WinKey.keybind_right_thumb_right.Comment"))
            .AddTextBox("keybind_right_thumb_up", LocalizationHelper.GetText("ConfigUiSettings.HID.WinKey.keybind_right_thumb_up.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.HID.WinKey.keybind_right_thumb_up.Comment"))
            .AddTextBox("keybind_right_trigger", LocalizationHelper.GetText("ConfigUiSettings.HID.WinKey.keybind_right_trigger.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.HID.WinKey.keybind_right_trigger.Comment"))
            .AddTextBox("keybind_start", LocalizationHelper.GetText("ConfigUiSettings.HID.WinKey.keybind_start.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.HID.WinKey.keybind_start.Comment"))
            .AddTextBox("keybind_x", LocalizationHelper.GetText("ConfigUiSettings.HID.WinKey.keybind_x.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.HID.WinKey.keybind_x.Comment"))
            .AddTextBox("keybind_y", LocalizationHelper.GetText("ConfigUiSettings.HID.WinKey.keybind_y.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.HID.WinKey.keybind_y.Comment"))
    )
    {
        Title = LocalizationHelper.GetText("ConfigUiSettings.Section.HID.WinKey")
    };

    /// <summary>
    /// Gets a UI definition for HID settings.
    /// Settings from the [HID] section.
    /// </summary>
    private static ConfigUiDefinition HidSettings => new ConfigUiDefinition(
        new ConfigSectionDefinition("HID")
            {
                DisplayName = LocalizationHelper.GetText("ConfigUiSettings.Section.HID")
            }
            .AddComboBox("hid", new Dictionary<object, string>
            {
                { "any", LocalizationHelper.GetText("ConfigUiSettings.HID.hid.option.any") },
                { "nop", LocalizationHelper.GetText("ConfigUiSettings.HID.hid.option.nop") },
                { "sdl", LocalizationHelper.GetText("ConfigUiSettings.HID.hid.option.sdl") },
                { "winkey", LocalizationHelper.GetText("ConfigUiSettings.HID.hid.option.winkey") },
                { "xinput", LocalizationHelper.GetText("ConfigUiSettings.HID.hid.option.xinput") }
            }, LocalizationHelper.GetText("ConfigUiSettings.HID.hid.Title"), LocalizationHelper.GetText("ConfigUiSettings.HID.hid.Comment"))
            .AddComboBox("keyboard_mode", new Dictionary<object, string>
            {
                { 0, LocalizationHelper.GetText("ConfigUiSettings.HID.keyboard_mode.option.0") },
                { 1, LocalizationHelper.GetText("ConfigUiSettings.HID.keyboard_mode.option.1") },
                { 2, LocalizationHelper.GetText("ConfigUiSettings.HID.keyboard_mode.option.2") }
            }, LocalizationHelper.GetText("ConfigUiSettings.HID.keyboard_mode.Title"), LocalizationHelper.GetText("ConfigUiSettings.HID.keyboard_mode.Comment"))
            .AddSlider("keyboard_user_index", 0, 3, 1, LocalizationHelper.GetText("ConfigUiSettings.HID.keyboard_user_index.Title"), LocalizationHelper.GetText("ConfigUiSettings.HID.keyboard_user_index.Comment"))
            .AddSlider("left_stick_deadzone_percentage", 0, 1, 0.1, LocalizationHelper.GetText("ConfigUiSettings.HID.left_stick_deadzone_percentage.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.HID.left_stick_deadzone_percentage.Comment"), "F1")
            .AddSlider("right_stick_deadzone_percentage", 0, 1, 0.1, LocalizationHelper.GetText("ConfigUiSettings.HID.right_stick_deadzone_percentage.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.HID.right_stick_deadzone_percentage.Comment"), "F1")
            .AddToggle("vibration", LocalizationHelper.GetText("ConfigUiSettings.HID.vibration.Title"), LocalizationHelper.GetText("ConfigUiSettings.HID.vibration.Comment"))
    )
    {
        Title = LocalizationHelper.GetText("ConfigUiSettings.Section.HID")
    };

    /// <summary>
    /// Gets a UI definition for Kernel settings.
    /// Settings from the [Kernel] section.
    /// </summary>
    private static ConfigUiDefinition KernelSettings => new ConfigUiDefinition(
        new ConfigSectionDefinition("Kernel")
            {
                DisplayName = LocalizationHelper.GetText("ConfigUiSettings.Section.Kernel")
            }
            .AddToggle("apply_title_update", LocalizationHelper.GetText("ConfigUiSettings.Kernel.apply_title_update.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.Kernel.apply_title_update.Comment"))
            .AddTextBox("cl", LocalizationHelper.GetText("ConfigUiSettings.Kernel.cl.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.Kernel.cl.Comment"))
    )
    {
        Title = LocalizationHelper.GetText("ConfigUiSettings.Section.Kernel")
    };

    /// <summary>
    /// Gets a UI definition for Logging settings.
    /// Settings from the [Logging] section.
    /// </summary>
    private static ConfigUiDefinition LoggingSettings => new ConfigUiDefinition(
        new ConfigSectionDefinition("Logging")
            {
                DisplayName = LocalizationHelper.GetText("ConfigUiSettings.Section.Logging")
            }
            .AddToggle("enable_console", LocalizationHelper.GetText("ConfigUiSettings.Logging.enable_console.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.Logging.enable_console.Comment"))
            .AddComboBox("log_level", new Dictionary<object, string>
                {
                    { 0, LocalizationHelper.GetText("ConfigUiSettings.Logging.log_level.option.0") },
                    { 1, LocalizationHelper.GetText("ConfigUiSettings.Logging.log_level.option.1") },
                    { 2, LocalizationHelper.GetText("ConfigUiSettings.Logging.log_level.option.2") },
                    { 3, LocalizationHelper.GetText("ConfigUiSettings.Logging.log_level.option.3") }
                }, LocalizationHelper.GetText("ConfigUiSettings.Logging.log_level.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.Logging.log_level.Comment"))
    )
    {
        Title = LocalizationHelper.GetText("ConfigUiSettings.Section.Logging")
    };

    /// <summary>
    /// Gets a UI definition for Memory settings.
    /// Settings from the [Memory] section.
    /// </summary>
    private static ConfigUiDefinition MemorySettings => new ConfigUiDefinition(
        new ConfigSectionDefinition("Memory")
            {
                DisplayName = LocalizationHelper.GetText("ConfigUiSettings.Section.Memory")
            }
            .AddToggle("protect_zero", LocalizationHelper.GetText("ConfigUiSettings.Memory.protect_zero.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.Memory.protect_zero.Comment"))
            .AddToggle("scribble_heap", LocalizationHelper.GetText("ConfigUiSettings.Memory.scribble_heap.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.Memory.scribble_heap.Comment"))
    )
    {
        Title = LocalizationHelper.GetText("ConfigUiSettings.Section.Memory")
    };

    /// <summary>
    /// Gets a UI definition for Storage settings.
    /// Settings from the [Storage] sections.
    /// </summary>
    private static ConfigUiDefinition StorageSettings => new ConfigUiDefinition(
        new ConfigSectionDefinition("Storage")
            {
                DisplayName = LocalizationHelper.GetText("ConfigUiSettings.Section.Storage")
            }
            .AddToggle("mount_cache", LocalizationHelper.GetText("ConfigUiSettings.Storage.mount_cache.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.Storage.mount_cache.Comment"))
            .AddToggle("mount_scratch", LocalizationHelper.GetText("ConfigUiSettings.Storage.mount_scratch.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.Storage.mount_scratch.Comment"))
    )
    {
        Title = LocalizationHelper.GetText("ConfigUiSettings.Section.Storage")
    };

    /// <summary>
    /// Gets a UI definition for UI settings.
    /// Settings from the [UI] sections.
    /// </summary>
    private static ConfigUiDefinition UiSettings => new ConfigUiDefinition(
        new ConfigSectionDefinition("UI")
            {
                DisplayName = LocalizationHelper.GetText("ConfigUiSettings.Section.UI")
            }
            .AddToggle("show_achievement_notification", LocalizationHelper.GetText("ConfigUiSettings.UI.show_achievement_notification.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.UI.show_achievement_notification.Comment"))
    )
    {
        Title = LocalizationHelper.GetText("ConfigUiSettings.Section.UI")
    };

    /// <summary>
    /// Gets a UI definition for Video settings.
    /// Settings from the [Video] section.
    /// </summary>
    private static ConfigUiDefinition VideoSettings => new ConfigUiDefinition(
        new ConfigSectionDefinition("Video")
            {
                DisplayName = LocalizationHelper.GetText("ConfigUiSettings.Section.Video")
            }
            .AddComboBox("internal_display_resolution", new Dictionary<object, string>
                {
                    { 0, LocalizationHelper.GetText("ConfigUiSettings.Video.internal_display_resolution.option.0") },
                    { 1, LocalizationHelper.GetText("ConfigUiSettings.Video.internal_display_resolution.option.1") },
                    { 2, LocalizationHelper.GetText("ConfigUiSettings.Video.internal_display_resolution.option.2") },
                    { 3, LocalizationHelper.GetText("ConfigUiSettings.Video.internal_display_resolution.option.3") },
                    { 4, LocalizationHelper.GetText("ConfigUiSettings.Video.internal_display_resolution.option.4") },
                    { 5, LocalizationHelper.GetText("ConfigUiSettings.Video.internal_display_resolution.option.5") },
                    { 6, LocalizationHelper.GetText("ConfigUiSettings.Video.internal_display_resolution.option.6") },
                    { 7, LocalizationHelper.GetText("ConfigUiSettings.Video.internal_display_resolution.option.7") },
                    { 8, LocalizationHelper.GetText("ConfigUiSettings.Video.internal_display_resolution.option.8") },
                    { 9, LocalizationHelper.GetText("ConfigUiSettings.Video.internal_display_resolution.option.9") },
                    { 10, LocalizationHelper.GetText("ConfigUiSettings.Video.internal_display_resolution.option.10") },
                    { 11, LocalizationHelper.GetText("ConfigUiSettings.Video.internal_display_resolution.option.11") },
                    { 12, LocalizationHelper.GetText("ConfigUiSettings.Video.internal_display_resolution.option.12") },
                    { 13, LocalizationHelper.GetText("ConfigUiSettings.Video.internal_display_resolution.option.13") },
                    { 14, LocalizationHelper.GetText("ConfigUiSettings.Video.internal_display_resolution.option.14") },
                    { 15, LocalizationHelper.GetText("ConfigUiSettings.Video.internal_display_resolution.option.15") },
                    { 16, LocalizationHelper.GetText("ConfigUiSettings.Video.internal_display_resolution.option.16") },
                    { 17, LocalizationHelper.GetText("ConfigUiSettings.Video.internal_display_resolution.option.17") }
                }, LocalizationHelper.GetText("ConfigUiSettings.Video.internal_display_resolution.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.Video.internal_display_resolution.Comment"))
            .AddNumberBox("internal_display_resolution_x", 1, 1920, LocalizationHelper.GetText("ConfigUiSettings.Video.internal_display_resolution_x.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.Video.internal_display_resolution_x.Comment"))
            .AddNumberBox("internal_display_resolution_y", 1, 1080, LocalizationHelper.GetText("ConfigUiSettings.Video.internal_display_resolution_y.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.Video.internal_display_resolution_y.Comment"))
            .AddToggle("widescreen", LocalizationHelper.GetText("ConfigUiSettings.Video.widescreen.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.Video.widescreen.Comment"))
    )
    {
        Title = LocalizationHelper.GetText("ConfigUiSettings.Section.Video")
    };

    /// <summary>
    /// Gets a UI definition for Vulkan settings.
    /// Settings from the [Vulkan] section.
    /// </summary>
    private static ConfigUiDefinition VulkanSettings => new ConfigUiDefinition(
        new ConfigSectionDefinition("Vulkan")
            {
                DisplayName = LocalizationHelper.GetText("ConfigUiSettings.Section.Vulkan")
            }
            .AddToggle("vulkan_allow_present_mode_immediate", LocalizationHelper.GetText("ConfigUiSettings.Vulkan.vulkan_allow_present_mode_immediate.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.Vulkan.vulkan_allow_present_mode_immediate.Comment"))
            .AddToggle("vulkan_allow_present_mode_mailbox", LocalizationHelper.GetText("ConfigUiSettings.Vulkan.vulkan_allow_present_mode_mailbox.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.Vulkan.vulkan_allow_present_mode_mailbox.Comment"))
            .AddToggle("vulkan_allow_present_mode_fifo_relaxed", LocalizationHelper.GetText("ConfigUiSettings.Vulkan.vulkan_allow_present_mode_fifo_relaxed.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.Vulkan.vulkan_allow_present_mode_fifo_relaxed.Comment"))
    )
    {
        Title = LocalizationHelper.GetText("ConfigUiSettings.Section.Vulkan")
    };

    /// <summary>
    /// Gets a UI definition for Live (Xbox Live) settings.
    /// Settings from the [Live] section.
    /// </summary>
    private static ConfigUiDefinition LiveSettings => new ConfigUiDefinition(
        new ConfigSectionDefinition("Live")
            {
                DisplayName = LocalizationHelper.GetText("ConfigUiSettings.Section.Live")
            }
            .AddTextBox("api_address", LocalizationHelper.GetText("ConfigUiSettings.Live.api_address.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.Live.api_address.Comment"))
            .AddSlider("discord_presence_user_index", 0, 3, LocalizationHelper.GetText("ConfigUiSettings.Live.discord_presence_user_index.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.Live.discord_presence_user_index.Comment"))
            .AddComboBox("network_mode", new Dictionary<object, string>
                {
                    { 0, LocalizationHelper.GetText("ConfigUiSettings.Live.network_mode.option.0") },
                    { 1, LocalizationHelper.GetText("ConfigUiSettings.Live.network_mode.option.1") },
                    { 2, LocalizationHelper.GetText("ConfigUiSettings.Live.network_mode.option.2") }
                }, LocalizationHelper.GetText("ConfigUiSettings.Live.network_mode.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.Live.network_mode.Comment"))
            .AddToggle("upnp", LocalizationHelper.GetText("ConfigUiSettings.Live.upnp.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.Live.upnp.Comment"))
            .AddToggle("xhttp", LocalizationHelper.GetText("ConfigUiSettings.Live.xhttp.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.Live.xhttp.Comment"))
            .AddToggle("xlink_kai_systemlink_hack", LocalizationHelper.GetText("ConfigUiSettings.Live.xlink_kai_systemlink_hack.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.Live.xlink_kai_systemlink_hack.Comment"))
    )
    {
        Title = LocalizationHelper.GetText("ConfigUiSettings.Section.Live")
    };

    /// <summary>
    /// Gets a UI definition for MouseHook settings.
    /// Settings from the [MouseHook] section.
    /// </summary>
    private static ConfigUiDefinition MouseHookSettings => new ConfigUiDefinition(
        new ConfigSectionDefinition("MouseHook")
            {
                DisplayName = LocalizationHelper.GetText("ConfigUiSettings.Section.MouseHook")
            }
            .AddToggle("disable_autoaim", LocalizationHelper.GetText("ConfigUiSettings.MouseHook.disable_autoaim.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.MouseHook.disable_autoaim.Comment"))
            .AddToggle("disable_fullscreen_esc_bind", LocalizationHelper.GetText("ConfigUiSettings.MouseHook.disable_fullscreen_esc_bind.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.MouseHook.disable_fullscreen_esc_bind.Comment"))
            .AddSlider("fov_sensitivity", 0.0, 2.0, 0.01, LocalizationHelper.GetText("ConfigUiSettings.MouseHook.fov_sensitivity.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.MouseHook.fov_sensitivity.Comment"), "F2")
            .AddSlider("ge_aim_turn_distance", 0.0, 1.0, 0.01, LocalizationHelper.GetText("ConfigUiSettings.MouseHook.ge_aim_turn_distance.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.MouseHook.ge_aim_turn_distance.Comment"), "F2")
            .AddToggle("ge_debug_menu", LocalizationHelper.GetText("ConfigUiSettings.MouseHook.ge_debug_menu.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.MouseHook.ge_debug_menu.Comment"))
            .AddToggle("ge_gun_sway", LocalizationHelper.GetText("ConfigUiSettings.MouseHook.ge_gun_sway.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.MouseHook.ge_gun_sway.Comment"))
            .AddToggle("ge_remove_blur", LocalizationHelper.GetText("ConfigUiSettings.MouseHook.ge_remove_blur.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.MouseHook.ge_remove_blur.Comment"))
            .AddToggle("internal_hook", LocalizationHelper.GetText("ConfigUiSettings.MouseHook.internal_hook.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.MouseHook.internal_hook.Comment"))
            .AddToggle("invert_x", LocalizationHelper.GetText("ConfigUiSettings.MouseHook.invert_x.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.MouseHook.invert_x.Comment"))
            .AddToggle("invert_y", LocalizationHelper.GetText("ConfigUiSettings.MouseHook.invert_y.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.MouseHook.invert_y.Comment"))
            .AddSlider("menu_sensitivity", 0.0, 2.0, 0.01, LocalizationHelper.GetText("ConfigUiSettings.MouseHook.menu_sensitivity.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.MouseHook.menu_sensitivity.Comment"), "F2")
            .AddToggle("rdr_snappy_wheel", LocalizationHelper.GetText("ConfigUiSettings.MouseHook.rdr_snappy_wheel.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.MouseHook.rdr_snappy_wheel.Comment"))
            .AddToggle("rdr_turbo_gallop_horse", LocalizationHelper.GetText("ConfigUiSettings.MouseHook.rdr_turbo_gallop_horse.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.MouseHook.rdr_turbo_gallop_horse.Comment"))
            .AddSlider("sensitivity", 0.0, 5.0, 0.01, LocalizationHelper.GetText("ConfigUiSettings.MouseHook.sensitivity.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.MouseHook.sensitivity.Comment"), "F2")
            .AddSlider("source_sniper_sensitivity", 0, 10, LocalizationHelper.GetText("ConfigUiSettings.MouseHook.source_sniper_sensitivity.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.MouseHook.source_sniper_sensitivity.Comment"))
            .AddToggle("sr1_increase_vehicle_rotation_limit", LocalizationHelper.GetText("ConfigUiSettings.MouseHook.sr1_increase_vehicle_rotation_limit.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.MouseHook.sr1_increase_vehicle_rotation_limit.Comment"))
            .AddToggle("sr2_better_handbrake_cam", LocalizationHelper.GetText("ConfigUiSettings.MouseHook.sr2_better_handbrake_cam.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.MouseHook.sr2_better_handbrake_cam.Comment"))
            .AddToggle("sr2_hold_fine_aim", LocalizationHelper.GetText("ConfigUiSettings.MouseHook.sr2_hold_fine_aim.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.MouseHook.sr2_hold_fine_aim.Comment"))
            .AddToggle("sr_better_drive_cam", LocalizationHelper.GetText("ConfigUiSettings.MouseHook.sr_better_drive_cam.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.MouseHook.sr_better_drive_cam.Comment"))
            .AddToggle("sr_disable_shared_reload", LocalizationHelper.GetText("ConfigUiSettings.MouseHook.sr_disable_shared_reload.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.MouseHook.sr_disable_shared_reload.Comment"))
            .AddToggle("swap_wheel", LocalizationHelper.GetText("ConfigUiSettings.MouseHook.swap_wheel.Title"),
                LocalizationHelper.GetText("ConfigUiSettings.MouseHook.swap_wheel.Comment"))
    )
    {
        Title = LocalizationHelper.GetText("ConfigUiSettings.Section.MouseHook")
    };

    /// <summary>
    /// Gets a combined UI definition for all common settings.
    /// </summary>
    public static ConfigUiDefinition AllSettings
    {
        get
        {
            ConfigUiDefinition definition = new ConfigUiDefinition();

            definition.AddSection(ApuSettings.Sections[0]);
            definition.AddSection(CpuSettings.Sections[0]);
            definition.AddSection(DisplaySettings.Sections[0]);
            definition.AddSection(GpuSettings.Sections[0]);
            definition.AddSection(VideoSettings.Sections[0]);
            definition.AddSection(D3D12Settings.Sections[0]);
            definition.AddSection(VulkanSettings.Sections[0]);
            definition.AddSection(ContentSettings.Sections[0]);
            definition.AddSection(GeneralSettings.Sections[0]);
            definition.AddSection(LiveSettings.Sections[0]);
            definition.AddSection(HidSettings.Sections[0]);
            definition.AddSection(HidWinKeySettings.Sections[0]);
            definition.AddSection(MouseHookSettings.Sections[0]);
            definition.AddSection(KernelSettings.Sections[0]);
            definition.AddSection(MemorySettings.Sections[0]);
            definition.AddSection(StorageSettings.Sections[0]);
            definition.AddSection(UiSettings.Sections[0]);
            definition.AddSection(LoggingSettings.Sections[0]);

            return definition;
        }
    }
}