using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;

// Imported
using Newtonsoft.Json;
using NvAPIWrapper.DRS;
using NvAPIWrapper.DRS.SettingValues;
using Serilog;
using Tomlyn;
using Tomlyn.Model;
using Tomlyn.Syntax;
using Xenia_Manager.Classes;
using Xenia_Manager.Windows;

namespace Xenia_Manager.Pages
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class XeniaSettings : Page
    {
        /// <summary>
        /// Every installed game is stored in here after reading from .JSON file
        /// </summary>
        private List<InstalledGame> Games;

        /// <summary>
        /// This is instance of NVAPI class which is used to interact with NVIDIA driver settings
        /// </summary>
        private NVAPI NvidiaApi = new NVAPI();

        /// <summary>
        /// Dictionary to map each language to its number
        /// </summary>
        private Dictionary<string, int> languageMap = new Dictionary<string, int>
        {
            { "English", 1 },
            { "Japanese/日本語", 2 },
            { "Deutsche", 3 },
            { "Français", 4 },
            { "Español", 5 },
            { "Italiano", 6 },
            { "한국어", 7 },
            { "繁體中文", 8 },
            { "Português", 9 },
            { "Polski", 11 },
            { "русский", 12 },
            { "Svenska", 13 },
            { "Türk", 14 },
            { "Norsk", 15 },
            { "Nederlands", 16 },
            { "简体中文", 17 }
        };

        public XeniaSettings()
        {
            InitializeComponent();
            InitializeAsync();
        }

        /// <summary>
        /// Loads all of the installed games into "Games" List
        /// </summary>
        private async Task LoadInstalledGames()
        {
            try
            {
                ConfigurationFilesList.Items.Clear();
                if (App.appConfiguration.XeniaStable != null)
                {
                    ConfigurationFilesList.Items.Add("Xenia Stable Profile");
                }
                if (App.appConfiguration.XeniaCanary != null)
                {
                    ConfigurationFilesList.Items.Add("Xenia Canary Profile");
                }
                if (App.appConfiguration.XeniaNetplay != null)
                {
                    ConfigurationFilesList.Items.Add("Xenia Netplay Profile");
                }
                if (File.Exists(Path.Combine(App.baseDirectory, "installedGames.json")))
                {
                    Log.Information("Loading all of the games into the ComboBox");
                    string JSON = System.IO.File.ReadAllText(Path.Combine(App.baseDirectory, "installedGames.json"));
                    Games = JsonConvert.DeserializeObject<List<InstalledGame>>((JSON));

                    // Sorting the list
                    Games.Sort((Game1, Game2) => string.Compare(Game1.Title, Game2.Title, StringComparison.Ordinal));
                    foreach (InstalledGame Game in Games)
                    {
                        if (Game.ConfigFilePath != null)
                        {
                            Log.Information($"Adding {Game.Title} to the ConfigurationList ComboBox");
                            ConfigurationFilesList.Items.Add(Game.Title);
                        }
                    }
                }
                else
                {
                    Log.Information("No installed games found");
                }
                ConfigurationFilesList.SelectedIndex = 0;
                await Task.Delay(1);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// Hides all of the settings that aren't in all versions of Xenia
        /// </summary>
        private void HideNonUniversalSettings()
        {
            InternalDisplayResolutionOption.Visibility = Visibility.Collapsed;
            NetplaySettings.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Reads the .toml file of the emulator
        /// </summary>
        /// <param name="configLocation"></param>
        private async Task ReadConfigFile(string configLocation)
        {
            try
            {
                string configText = File.ReadAllText(configLocation);
                TomlTable configFile = Toml.Parse(configText).ToModel();
                foreach (var section in configFile)
                {
                    TomlTable sectionTable = section.Value as TomlTable;
                    if (sectionTable == null)
                    {
                        continue;
                    };
                    switch (section.Key)
                    {
                        case "APU":
                            Log.Information("APU settings");

                            // "apu" setting
                            Log.Information($"apu - {sectionTable["apu"].ToString()}");
                            foreach (var item in apuSelector.Items)
                            {
                                if (item is ComboBoxItem comboBoxItem && comboBoxItem.Content.ToString() == sectionTable["apu"].ToString())
                                {
                                    apuSelector.SelectedItem = comboBoxItem;
                                    continue;
                                }
                            }

                            // "apu_max_queued_frames" setting
                            if (sectionTable.ContainsKey("apu_max_queued_frames"))
                            {
                                Log.Information($"apu_max_queued_frames - {sectionTable["apu_max_queued_frames"].ToString()}");
                                apuMaxQueuedFramesTextBox.Text = sectionTable["apu_max_queued_frames"].ToString();
                                apuMaxQueuedFramesOption.Visibility = Visibility.Visible;
                            }
                            else
                            {
                                // Disable the option because it's not in the configuration file
                                apuMaxQueuedFramesOption.Visibility = Visibility.Collapsed;
                            }

                            // "mute" setting
                            Log.Information($"mute - {(bool)sectionTable["mute"]}");
                            Mute.IsChecked = (bool)sectionTable["mute"];

                            // "use_dedicated_xma_thread" setting
                            if (sectionTable.ContainsKey("use_dedicated_xma_thread"))
                            {
                                Log.Information($"use_dedicated_xma_thread - {(bool)sectionTable["use_dedicated_xma_thread"]}");
                                UseXMAThread.IsChecked = (bool)sectionTable["use_dedicated_xma_thread"];
                                DedicatedXMAThreadOption.Visibility = Visibility.Visible;
                            }
                            else
                            {
                                // Disable the option because it's not in the configuration file
                                DedicatedXMAThreadOption.Visibility = Visibility.Collapsed;
                            }

                            // "use_new_decoder" setting
                            if (sectionTable.ContainsKey("use_new_decoder"))
                            {
                                Log.Information($"use_new_decoder - {(bool)sectionTable["use_new_decoder"]}");
                                UseNewDecoder.IsChecked = (bool)sectionTable["use_new_decoder"];
                                NewAudioDecoderOption.Visibility = Visibility.Visible;
                            }
                            else
                            {
                                // Disable the option because it's not in the configuration file
                                NewAudioDecoderOption.Visibility = Visibility.Collapsed;
                            }
                            break;
                        case "CPU":
                            // "break_on_unimplemented_instructions" setting
                            Log.Information($"break_on_unimplemented_instructions - {(bool)sectionTable["break_on_unimplemented_instructions"]}");
                            BreakOnUnimplementedInstructions.IsChecked = (bool)sectionTable["break_on_unimplemented_instructions"];

                            break;
                        case "Content":
                            Log.Information("Content settings");
                            // "license_mask" setting
                            Log.Information($"license_mask - {int.Parse(sectionTable["license_mask"].ToString())}");
                            switch (int.Parse(sectionTable["license_mask"].ToString()))
                            {
                                case -1:
                                    // All Licenses
                                    licenseMaskSelector.SelectedIndex = 2;
                                    break;
                                case 0:
                                    // No License
                                    licenseMaskSelector.SelectedIndex = 0;
                                    break;
                                case 1:
                                    // First License
                                    licenseMaskSelector.SelectedIndex = 1;
                                    break;
                                default:
                                    break;
                            }
                            break;
                        case "D3D12":
                            Log.Information("Direct3D12 settings");

                            // "d3d12_allow_variable_refresh_rate_and_tearing" setting
                            Log.Information($"d3d12_allow_variable_refresh_rate_and_tearing - {(bool)sectionTable["d3d12_allow_variable_refresh_rate_and_tearing"]}");
                            D3D12VRR.IsChecked = (bool)sectionTable["d3d12_allow_variable_refresh_rate_and_tearing"];

                            // "d3d12_readback_resolve" setting
                            Log.Information($"d3d12_readback_resolve - {(bool)sectionTable["d3d12_readback_resolve"]}");
                            D3D12ReadbackResolve.IsChecked = (bool)sectionTable["d3d12_readback_resolve"];

                            // "d3d12_queue_priority" setting
                            Log.Information($"d3d12_queue_priority - {int.Parse(sectionTable["d3d12_queue_priority"].ToString())}");
                            D3D12QueuePrioritySelector.SelectedIndex = int.Parse(sectionTable["d3d12_queue_priority"].ToString());

                            break;
                        case "Display":
                            Log.Information("Display settings");

                            // "fullscreen" setting
                            Log.Information($"fullscreen - {(bool)sectionTable["fullscreen"]}");
                            Fullscreen.IsChecked = (bool)sectionTable["fullscreen"];

                            // "postprocess_antialiasing" setting
                            Log.Information($"postprocess_antialiasing - {sectionTable["postprocess_antialiasing"] as string}");
                            switch (sectionTable["postprocess_antialiasing"] as string)
                            {
                                case "fxaa":
                                    AntiAliasingSelector.SelectedIndex = 1;
                                    break;
                                case "fxaa_extreme":
                                    AntiAliasingSelector.SelectedIndex = 2;
                                    break;
                                default:
                                    AntiAliasingSelector.SelectedIndex = 0;
                                    break;
                            }

                            // "postprocess_dither" setting
                            Log.Information($"postprocess_dither - {(bool)sectionTable["postprocess_dither"]}");
                            PostProcessDither.IsChecked = (bool)sectionTable["postprocess_dither"];

                            // "postprocess_ffx_cas_additional_sharpness" setting
                            Log.Information($"postprocess_ffx_cas_additional_sharpness - {sectionTable["postprocess_ffx_cas_additional_sharpness"].ToString()}");
                            CASAdditionalSharpness.Value = double.Parse(sectionTable["postprocess_ffx_cas_additional_sharpness"].ToString()) * 1000;
                            AutomationProperties.SetName(CASAdditionalSharpness, $"CAS Additional Sharpness: {Math.Round((CASAdditionalSharpness.Value / 1000), 3)}");

                            // "postprocess_ffx_fsr_max_upsampling_passes" setting
                            Log.Information($"postprocess_ffx_fsr_max_upsampling_passes - {int.Parse(sectionTable["postprocess_ffx_fsr_max_upsampling_passes"].ToString())}");
                            FSRMaxUpsamplingPasses.Value = int.Parse(sectionTable["postprocess_ffx_fsr_max_upsampling_passes"].ToString());
                            AutomationProperties.SetName(FSRMaxUpsamplingPasses, $"FSR MaxUpsampling Passes: {FSRMaxUpsamplingPasses.Value}");

                            // "postprocess_ffx_fsr_sharpness_reduction" setting
                            Log.Information($"postprocess_ffx_fsr_sharpness_reduction - {double.Parse(sectionTable["postprocess_ffx_fsr_sharpness_reduction"].ToString())}");
                            FSRSharpnessReduction.Value = double.Parse(sectionTable["postprocess_ffx_fsr_sharpness_reduction"].ToString()) * 1000;
                            AutomationProperties.SetName(FSRSharpnessReduction, $"FSR Sharpness Reduction: {Math.Round((FSRSharpnessReduction.Value / 1000), 3)}");

                            // "postprocess_scaling_and_sharpening" setting
                            Log.Information($"postprocess_scaling_and_sharpening - {sectionTable["postprocess_scaling_and_sharpening"] as string}");
                            switch (sectionTable["postprocess_scaling_and_sharpening"] as string)
                            {
                                case "cas":
                                    ScalingAndSharpeningSelector.SelectedIndex = 1;
                                    break;
                                case "fsr":
                                    ScalingAndSharpeningSelector.SelectedIndex = 2;
                                    break;
                                default:
                                    ScalingAndSharpeningSelector.SelectedIndex = 0;
                                    break;
                            }

                            // "present_letterbox" setting
                            Log.Information($"present_letterbox - {(bool)sectionTable["present_letterbox"]}");
                            Letterbox.IsChecked = (bool)sectionTable["present_letterbox"];
                            break;
                        case "GPU":
                            Log.Information("GPU settings");

                            // "draw_resolution_scale" setting
                            Log.Information($"draw_resolution_scale_x - {sectionTable["draw_resolution_scale_x"].ToString()}");
                            Log.Information($"draw_resolution_scale_y - {sectionTable["draw_resolution_scale_y"].ToString()}");
                            if (int.Parse(sectionTable["draw_resolution_scale_x"].ToString()) == int.Parse(sectionTable["draw_resolution_scale_y"].ToString()))
                            {
                                DrawResolutionScale.Value = int.Parse(sectionTable["draw_resolution_scale_x"].ToString());
                            }

                            // "framerate_limit" setting
                            if (sectionTable.ContainsKey("framerate_limit"))
                            {
                                Log.Information($"framerate_limit - {sectionTable["framerate_limit"].ToString()}");
                                FrameRateLimit.Value = int.Parse(sectionTable["framerate_limit"].ToString());
                                AutomationProperties.SetName(FrameRateLimit, $"Xenia Framerate Limiter: {FrameRateLimit.Value} FPS");
                                XeniaFramerateLimiterOption.Visibility = Visibility.Visible;
                            }
                            else
                            {
                                // Disable the option because it's not in the configuration file
                                XeniaFramerateLimiterOption.Visibility = Visibility.Collapsed;
                            }

                            // "gamma_render_target_as_srgb" setting
                            Log.Information($"gamma_render_target_as_srgb - {sectionTable["gamma_render_target_as_srgb"]}");
                            gammaRenderTargetAsSRGB.IsChecked = (bool)sectionTable["gamma_render_target_as_srgb"];

                            // "gpu" setting
                            Log.Information($"gpu - {sectionTable["gpu"] as string}");
                            switch (sectionTable["gpu"] as string)
                            {
                                case "d3d12":
                                    gpuSelector.SelectedIndex = 1;
                                    break;
                                case "vulkan":
                                    gpuSelector.SelectedIndex = 2;
                                    break;
                                default:
                                    gpuSelector.SelectedIndex = 0;
                                    break;
                            }

                            // "vsync" setting
                            Log.Information($"vsync - {sectionTable["vsync"]}");
                            vSync.IsChecked = (bool)sectionTable["vsync"];

                            // "render_target_path_d3d12" setting
                            Log.Information($"render_target_path_d3d12 - {sectionTable["render_target_path_d3d12"] as string}");
                            switch (sectionTable["render_target_path_d3d12"] as string)
                            {
                                case "rtv":
                                    D3D12RenderTargetPathSelector.SelectedIndex = 1;
                                    break;
                                case "rov":
                                    D3D12RenderTargetPathSelector.SelectedIndex = 2;
                                    break;
                                default:
                                    D3D12RenderTargetPathSelector.SelectedIndex = 0;
                                    break;
                            }

                            // "render_target_path_vulkan" setting
                            Log.Information($"render_target_path_vulkan - {sectionTable["render_target_path_vulkan"] as string}");
                            switch (sectionTable["render_target_path_vulkan"] as string)
                            {
                                case "fbo":
                                    VulkanRenderTargetPathSelector.SelectedIndex = 1;
                                    break;
                                case "fsi":
                                    VulkanRenderTargetPathSelector.SelectedIndex = 2;
                                    break;
                                default:
                                    VulkanRenderTargetPathSelector.SelectedIndex = 0;
                                    break;
                            }

                            // "clear_memory_page_state" setting
                            if (sectionTable.ContainsKey("clear_memory_page_state"))
                            {
                                Log.Information($"clear_memory_page_state - {(bool)sectionTable["clear_memory_page_state"]}");
                                ClearGPUCache.IsChecked = (bool)sectionTable["clear_memory_page_state"];
                                ClearMemoryPageStatusOption.Visibility = Visibility.Visible;
                            }
                            else
                            {
                                // Disable the option because it's not in the configuration file
                                ClearMemoryPageStatusOption.Visibility = Visibility.Collapsed;
                            }

                            break;
                        case "General":
                            Log.Information("General settings");

                            // "allow_plugins" setting
                            if (sectionTable.ContainsKey("allow_plugins"))
                            {
                                Log.Information($"allow_plugins - {(bool)sectionTable["allow_plugins"]}");
                                AllowPlugins.IsChecked = (bool)sectionTable["allow_plugins"];
                                AllowPluginsOption.Visibility = Visibility.Visible;
                            }
                            else
                            {
                                // Disable the option because it's not in the configuration file
                                AllowPluginsOption.Visibility = Visibility.Collapsed;
                            }

                            // "apply_patches" setting
                            if (sectionTable.ContainsKey("apply_patches"))
                            {
                                Log.Information($"apply_patches - {(bool)sectionTable["apply_patches"]}");
                                ApplyPatches.IsChecked = (bool)sectionTable["apply_patches"];
                                ApplyPatchesOption.Visibility = Visibility.Visible;
                            }
                            else
                            {
                                // Disable the option because it's not in the configuration file
                                ApplyPatchesOption.Visibility = Visibility.Collapsed;
                            }

                            // "controller_hotkeys" setting
                            if (sectionTable.ContainsKey("controller_hotkeys"))
                            {
                                Log.Information($"controller_hotkeys - {(bool)sectionTable["controller_hotkeys"]}");
                                ControllerHotkeys.IsChecked = (bool)sectionTable["controller_hotkeys"];
                                ControllerHotkeysOption.Visibility = Visibility.Visible;
                            }
                            else
                            {
                                // Disable the option because it's not in the configuration file
                                ControllerHotkeysOption.Visibility = Visibility.Collapsed;
                            }

                            // "discord" setting
                            Log.Information($"discord - {(bool)sectionTable["discord"]}");
                            DiscordRPC.IsChecked = (bool)sectionTable["discord"];

                            break;
                        case "HID":
                            Log.Information("HID settings");

                            // "hid" setting
                            Log.Information($"hid - {sectionTable["hid"] as string}");
                            switch (sectionTable["hid"] as string)
                            {
                                case "sdl":
                                    InputSystemSelector.SelectedIndex = 1;
                                    break;
                                case "xinput":
                                    InputSystemSelector.SelectedIndex = 2;
                                    break;
                                case "winkey":
                                    InputSystemSelector.SelectedIndex = 3;
                                    break;
                                default:
                                    InputSystemSelector.SelectedIndex = 0;
                                    break;
                            }

                            // "left_stick_deadzone_percentage" setting
                            if (sectionTable.ContainsKey("left_stick_deadzone_percentage"))
                            {
                                Log.Information($"left_stick_deadzone_percentage - {double.Parse(sectionTable["left_stick_deadzone_percentage"].ToString())}");
                                LeftStickDeadzonePercentage.Value = Math.Round(double.Parse(sectionTable["left_stick_deadzone_percentage"].ToString()) * 10, 1);
                                AutomationProperties.SetName(LeftStickDeadzonePercentage, $"Left Stick Deadzone Percentage: {Math.Round((LeftStickDeadzonePercentage.Value / 10), 1)}");
                                LeftStickDeadzoneOption.Visibility = Visibility.Visible;
                            }
                            else
                            {
                                // Disable the option because it's not in the configuration file
                                LeftStickDeadzoneOption.Visibility = Visibility.Collapsed;
                            }

                            // "right_stick_deadzone_percentage" setting
                            if (sectionTable.ContainsKey("right_stick_deadzone_percentage"))
                            {
                                Log.Information($"right_stick_deadzone_percentage - {double.Parse(sectionTable["left_stick_deadzone_percentage"].ToString())}");
                                RightStickDeadzonePercentage.Value = Math.Round(double.Parse(sectionTable["right_stick_deadzone_percentage"].ToString()) * 10, 1);
                                AutomationProperties.SetName(RightStickDeadzonePercentage, $"Right Stick Deadzone Percentage: {Math.Round((RightStickDeadzonePercentage.Value / 10), 1)}");
                                RightStickDeadzoneOption.Visibility = Visibility.Visible;
                            }
                            else
                            {
                                // Disable the option because it's not in the configuration file
                                RightStickDeadzoneOption.Visibility = Visibility.Collapsed;
                            }

                            // "vibration" setting
                            if (sectionTable.ContainsKey("vibration"))
                            {
                                Log.Information($"vibration - {(bool)sectionTable["vibration"]}");
                                ControllerVibration.IsChecked = (bool)sectionTable["vibration"];
                                ControllerVibrationOption.Visibility = Visibility.Visible;
                            }
                            else
                            {
                                // Disable the option because it's not in the configuration file
                                ControllerVibrationOption.Visibility = Visibility.Collapsed;
                            }

                            break;
                        case "Kernel":
                            Log.Information("Kernel settings");

                            // "apply_title_update" setting
                            if (sectionTable.ContainsKey("apply_title_update"))
                            {
                                Log.Information($"apply_title_update - {(bool)sectionTable["apply_title_update"]}");
                                ApplyTitleUpdate.IsChecked = (bool)sectionTable["apply_title_update"];
                                ApplyTitleUpdateOption.Visibility = Visibility.Visible;
                            }
                            else
                            {
                                // Disable the option because it's not in the configuration file
                                ApplyTitleUpdateOption.Visibility = Visibility.Collapsed;
                            }

                            break;
                        case "Live":
                            Log.Information("Live settings");
                            NetplaySettings.Visibility = Visibility.Visible;

                            // "api_list" setting
                            if (sectionTable.ContainsKey("api_list"))
                            {
                                Log.Information($"api_list - {sectionTable["api_list"]}");
                                string[] split = sectionTable["api_list"].ToString().Split(',');
                                foreach (string apiAddress in split)
                                {
                                    if (apiAddress != "")
                                    {
                                        ApiAddress.Items.Add(apiAddress);
                                    }
                                }
                            }

                            // "api_address" setting
                            if (sectionTable.ContainsKey("api_address"))
                            {
                                Log.Information($"api_address - {sectionTable["api_address"]}");
                                // Looking for the current API Address
                                if (ApiAddress.Items.Contains(sectionTable["api_address"].ToString()))
                                {
                                    ApiAddress.SelectedItem = sectionTable["api_address"].ToString();
                                }
                                else if (ApiAddress.Items.Contains($@"{sectionTable["api_address"].ToString()}/"))
                                {
                                    ApiAddress.SelectedItem = $@"{sectionTable["api_address"].ToString()}/";
                                }
                            }

                            // "upnp" setting
                            if (sectionTable.ContainsKey("upnp"))
                            {
                                Log.Information($"upnp - {(bool)sectionTable["upnp"]}");
                                UPnP.IsChecked = (bool)sectionTable["upnp"];
                            }
                            break;
                        case "Memory":
                            // "protect_zero" setting
                            Log.Information($"protect_zero - {(bool)sectionTable["protect_zero"]}");
                            ProtectZero.IsChecked = (bool)sectionTable["protect_zero"];

                            break;
                        case "Storage":
                            Log.Information("Storage settings");

                            // "mount_cache" setting
                            Log.Information($"mount_cache - {(bool)sectionTable["mount_cache"]}");
                            MountCache.IsChecked = (bool)sectionTable["mount_cache"];

                            // "mount_scratch" setting
                            Log.Information($"mount_scratch - {(bool)sectionTable["mount_scratch"]}");
                            MountScratch.IsChecked = (bool)sectionTable["mount_scratch"];

                            break;
                        case "UI":
                            Log.Information("UI settings");

                            // "show_achievement_notification" setting
                            if (sectionTable.ContainsKey("show_achievement_notification"))
                            {
                                Log.Information($"show_achievement_notification - {(bool)sectionTable["show_achievement_notification"]}");
                                ShowAchievementNotifications.IsChecked = (bool)sectionTable["show_achievement_notification"];
                                ShowAchievementNotificationsOption.Visibility = Visibility.Visible;
                            }
                            else
                            {
                                // Disable the option because it's not in the configuration file
                                ShowAchievementNotificationsOption.Visibility = Visibility.Collapsed;
                            }

                            break;
                        case "User":
                            Log.Information("User settings");

                            NetplaySettings.Visibility = Visibility.Visible;

                            if (sectionTable.ContainsKey("user_0_name"))
                            {
                                user0GamerTagTextBox.Text = sectionTable["user_0_name"].ToString();
                            }
                            if (sectionTable.ContainsKey("user_1_name"))
                            {
                                user1GamerTagTextBox.Text = sectionTable["user_1_name"].ToString();
                            }
                            if (sectionTable.ContainsKey("user_2_name"))
                            {
                                user2GamerTagTextBox.Text = sectionTable["user_2_name"].ToString();
                            }
                            if (sectionTable.ContainsKey("user_3_name"))
                            {
                                user3GamerTagTextBox.Text = sectionTable["user_3_name"].ToString();
                            }
                            break;
                        case "Video":
                            Log.Information("Video settings");
                            // "internal_display_resolution" setting
                            if (sectionTable.ContainsKey("internal_display_resolution"))
                            {
                                Log.Information($"internal_display_resolution - {int.Parse(sectionTable["internal_display_resolution"].ToString())}");
                                InternalDisplayResolutionSelector.SelectedIndex = int.Parse(sectionTable["internal_display_resolution"].ToString());
                                InternalDisplayResolutionOption.Visibility = Visibility.Visible;
                            }
                            else
                            {
                                // Disable the option because it's not in the configuration file
                                InternalDisplayResolutionOption.Visibility = Visibility.Collapsed;
                            }

                            break;
                        case "Vulkan":
                            Log.Information("Vulkan settings");

                            // "vulkan_allow_present_mode_immediate" setting
                            Log.Information($"vulkan_allow_present_mode_immediate - {(bool)sectionTable["vulkan_allow_present_mode_immediate"]}");
                            VulkanPresentModeImmediate.IsChecked = (bool)sectionTable["vulkan_allow_present_mode_immediate"];

                            // "vulkan_allow_present_mode_mailbox" setting
                            Log.Information($"vulkan_allow_present_mode_mailbox - {(bool)sectionTable["vulkan_allow_present_mode_mailbox"]}");
                            VulkanPresentModeMailbox.IsChecked = (bool)sectionTable["vulkan_allow_present_mode_mailbox"];

                            // "vulkan_allow_present_mode_fifo_relaxed" setting
                            Log.Information($"vulkan_allow_present_mode_fifo_relaxed - {(bool)sectionTable["vulkan_allow_present_mode_fifo_relaxed"]}");
                            VulkanPresentModeFIFORelaxed.IsChecked = (bool)sectionTable["vulkan_allow_present_mode_fifo_relaxed"];

                            break;
                        case "XConfig":
                            Log.Information("XConfig settings");
                            // "user_language" setting
                            if (sectionTable.ContainsKey("user_language"))
                            {
                                Log.Information($"user_language - {int.Parse(sectionTable["user_language"].ToString())}");
                                Dictionary<int, string>  numberMap = languageMap.ToDictionary(kv => kv.Value, kv => kv.Key);
                                if (numberMap.TryGetValue(int.Parse(sectionTable["user_language"].ToString()), out string language))
                                {
                                    foreach (ComboBoxItem item in UserLanguageSelector.Items)
                                    {
                                        if (item.Content.ToString() == language)
                                        {
                                            UserLanguageSelector.SelectedItem = item;
                                            break;
                                        }
                                    }
                                }
                            }
                            break;
                        default:
                            break;
                    }
                }
                await Task.Delay(1);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// Reads NVIDIA Profile if possible
        /// </summary>
        private async Task ReadNVIDIAProfile()
        {
            try
            {
                // Initialize NvidiaAPI
                bool initialized = NvidiaApi.Initialize();

                // Check if the initialization was sucessful
                if (initialized)
                {
                    Log.Information("NVIDIA API sucessfully initialized");

                    // Grabbing the Xenia Profile
                    NvidiaApi.FindAppProfile();
                    Log.Information("Xenia profile found");

                    // Grabbing VSync setting
                    Log.Information("Grabbing VSync setting");
                    ProfileSetting vSync = NvidiaApi.GetSetting(KnownSettingId.VSyncMode);
                    if (vSync != null)
                    {
                        Log.Information($"{vSync.CurrentValue}");
                        switch (vSync.CurrentValue)
                        {
                            case (uint)138504007:
                                Log.Information("VSync - Force Off");
                                NvidiaVSyncSelector.SelectedIndex = 1;
                                break;
                            case (uint)1199655232:
                                Log.Information("VSync - Force On");
                                NvidiaVSyncSelector.SelectedIndex = 2;
                                break;
                            case (uint)411601032:
                                Log.Information("VSync - Adaptive");
                                NvidiaVSyncSelector.SelectedIndex = 3;
                                break;
                            default:
                                Log.Information("VSync - Default");
                                NvidiaVSyncSelector.SelectedIndex = 0;
                                break;
                        }
                    }
                    else
                    {
                        Log.Information("VSync - Default");
                        NvidiaVSyncSelector.SelectedIndex = 0;
                    }

                    // Grabbing Framerate Limit setting
                    Log.Information("Grabbing Framerate Limit setting");
                    ProfileSetting FramerateLimiter = NvidiaApi.GetSetting((uint)0x10835002);
                    if (FramerateLimiter != null)
                    {
                        Log.Information($"Framerate Limit - {FramerateLimiter.CurrentValue} FPS");
                        NvidiaFrameRateLimiter.Value = Convert.ToDouble(FramerateLimiter.CurrentValue);
                        if (NvidiaFrameRateLimiter.Value == 0)
                        {
                            NvidiaFrameRateLimiterValue.Text = "Off";
                        }
                    }
                    else
                    {
                        Log.Information($"Framerate Limiter - Off");
                        NvidiaFrameRateLimiter.Value = 0;
                        NvidiaFrameRateLimiterValue.Text = "Off";
                    }
                    AutomationProperties.SetName(NvidiaFrameRateLimiter, $"NVIDIA Framerate Limiter: {NvidiaFrameRateLimiter.Value} FPS");
                }
                else
                {
                    Log.Error("Failed to initialize NVIDIA API (Most likely no NVIDIA GPU)");
                    NvidiaDriverSettings.Visibility = Visibility.Collapsed;
                }
                await Task.Delay(1);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// Function that executes other functions asynchronously
        /// </summary>
        public async void InitializeAsync()
        {
            try
            {
                await Dispatcher.InvokeAsync(async () =>
                {
                    Mouse.OverrideCursor = Cursors.Wait;
                    await LoadInstalledGames(); // Loading installed games

                    // Disabling settings that aren't universal across all versions of Xenia
                    HideNonUniversalSettings();

                    // Loading first option
                    switch (ConfigurationFilesList.SelectedItem.ToString())
                    {
                        case "Xenia Stable Profile":
                            Log.Information("Loading Xenia Stable Profile");
                            await ReadConfigFile(Path.Combine(App.baseDirectory, App.appConfiguration.XeniaStable.ConfigurationFileLocation));
                            break;
                        case "Xenia Canary Profile":
                            Log.Information("Loading Xenia Canary Profile");
                            await ReadConfigFile(Path.Combine(App.baseDirectory, App.appConfiguration.XeniaCanary.ConfigurationFileLocation));
                            break;
                        case "Xenia Netplay Profile":
                            Log.Information("Loading Xenia Netplay Profile");
                            await ReadConfigFile(Path.Combine(App.baseDirectory, App.appConfiguration.XeniaNetplay.ConfigurationFileLocation));
                            break;
                        default:
                            break;
                    }
                    await ReadNVIDIAProfile();
                    GC.Collect();
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
            }
            finally
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    Mouse.OverrideCursor = null;
                });

            }
        }

        // UI Interactions
        /// <summary>
        /// When selecting different profile, reload the UI with those settings
        /// </summary>
        private async void ConfigurationFilesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (ConfigurationFilesList.SelectedItem != null)
                {
                    HideNonUniversalSettings();
                    switch (ConfigurationFilesList.SelectedItem.ToString())
                    {
                        case "Xenia Stable Profile":
                            Log.Information("Loading Xenia Stable Profile");
                            await ReadConfigFile(Path.Combine(App.baseDirectory, App.appConfiguration.XeniaStable.ConfigurationFileLocation));
                            CopyDefaultSettings.Visibility = Visibility.Collapsed;
                            break;
                        case "Xenia Canary Profile":
                            Log.Information("Loading Xenia Canary Profile");
                            await ReadConfigFile(Path.Combine(App.baseDirectory, App.appConfiguration.XeniaCanary.ConfigurationFileLocation));
                            CopyDefaultSettings.Visibility = Visibility.Collapsed;
                            break;
                        case "Xenia Netplay Profile":
                            Log.Information("Loading Xenia Netplay Profile");
                            await ReadConfigFile(Path.Combine(App.baseDirectory, App.appConfiguration.XeniaNetplay.ConfigurationFileLocation));
                            CopyDefaultSettings.Visibility = Visibility.Collapsed;
                            break;
                        default:
                            InstalledGame selectedGame = Games.First(game => game.Title == ConfigurationFilesList.SelectedItem.ToString());
                            Log.Information($"Loading configuration file of {selectedGame.Title}");
                            if (File.Exists(selectedGame.ConfigFilePath))
                            {
                                await ReadConfigFile(selectedGame.ConfigFilePath);
                            }
                            else if (File.Exists(Path.Combine(App.baseDirectory, selectedGame.ConfigFilePath)))
                            {
                                await ReadConfigFile(Path.Combine(App.baseDirectory, selectedGame.ConfigFilePath));
                            }
                            else
                            {
                                if (selectedGame.EmulatorVersion != "Custom")
                                {
                                    Log.Information("Game configuration file not found");
                                    Log.Information("Creating a new configuration file");
                                    switch (selectedGame.EmulatorVersion)
                                    {
                                        case "Stable":
                                            File.Copy(Path.Combine(App.baseDirectory, App.appConfiguration.XeniaStable.ConfigurationFileLocation), Path.Combine(App.baseDirectory, App.appConfiguration.XeniaStable.EmulatorLocation, $@"config\{selectedGame.Title}.config.toml"), true);
                                            break;
                                        case "Canary":
                                            File.Copy(Path.Combine(App.baseDirectory, App.appConfiguration.XeniaCanary.ConfigurationFileLocation), Path.Combine(App.baseDirectory, App.appConfiguration.XeniaCanary.EmulatorLocation, $@"config\{selectedGame.Title}.config.toml"), true);
                                            break;
                                        case "Netplay":
                                            File.Copy(Path.Combine(App.baseDirectory, App.appConfiguration.XeniaNetplay.ConfigurationFileLocation), Path.Combine(App.baseDirectory, App.appConfiguration.XeniaNetplay.EmulatorLocation, $@"config\{selectedGame.Title}.config.toml"), true);
                                            break;
                                        default:
                                            break;
                                    }
                                    Log.Information($"Loading new configuration file of {selectedGame.Title}");
                                    await ReadConfigFile(Path.Combine(App.baseDirectory, selectedGame.ConfigFilePath));
                                }
                            }
                            if (selectedGame.EmulatorVersion != "Custom")
                            {
                                CopyDefaultSettings.Visibility = Visibility.Visible;
                            }
                            else
                            {
                                CopyDefaultSettings.Visibility = Visibility.Collapsed;
                            }
                            break;
                    }
                    await ReadNVIDIAProfile();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
                return;
            }
        }

        /// <summary>
        /// Function that saves NVIDIA Settings into the Xenia profile
        /// </summary>
        private async Task SaveNVIDIASettings()
        {
            try
            {
                if (NvidiaDriverSettings.Visibility != Visibility.Collapsed)
                {
                    // "VSync" setting
                    switch (NvidiaVSyncSelector.SelectedIndex)
                    {
                        case 1:
                            NvidiaApi.SetSettingValue(KnownSettingId.VSyncMode, (uint)138504007);
                            break;
                        case 2:
                            NvidiaApi.SetSettingValue(KnownSettingId.VSyncMode, (uint)1199655232);
                            break;
                        case 3:
                            NvidiaApi.SetSettingValue(KnownSettingId.VSyncMode, (uint)411601032);
                            break;
                        default:
                            NvidiaApi.SetSettingValue(KnownSettingId.VSyncMode, (uint)1620202130);
                            break;
                    }

                    // FrameRate Limiter
                    Log.Information($"{(uint)NvidiaFrameRateLimiter.Value}");
                    NvidiaApi.SetSettingValue((uint)0x10835002, (uint)NvidiaFrameRateLimiter.Value);
                }
                await Task.Delay(1);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// Saves all of the changes to the existing configuration file
        /// </summary>
        /// <param name="configLocation">Location of the configuration file</param>
        private async Task SaveChanges(string configLocation)
        {
            try
            {
                string configText = File.ReadAllText(configLocation);
                TomlTable configFile = Toml.Parse(configText).ToModel();
                foreach (var section in configFile)
                {
                    TomlTable sectionTable = section.Value as TomlTable;
                    if (sectionTable == null)
                    {
                        continue;
                    };
                    switch (section.Key)
                    {
                        case "APU":
                            // "apu" setting
                            ComboBoxItem apuSelectorValue = apuSelector.Items[apuSelector.SelectedIndex] as ComboBoxItem;
                            sectionTable["apu"] = apuSelectorValue.Content;

                            // "apu_max_queued_frames" setting
                            if (sectionTable.ContainsKey("apu_max_queued_frames"))
                            {
                                try
                                {
                                    int apuint = int.Parse(apuMaxQueuedFramesTextBox.Text);
                                    if (apuint < 16)
                                    {
                                        MessageBox.Show("apu_max_queued_frames minimal value is 16");
                                        apuint = 16;
                                    }
                                    sectionTable["apu_max_queued_frames"] = apuint;
                                }
                                catch (Exception ex)
                                {
                                    Log.Error(ex.Message + "\nFull Error:\n" + ex);
                                    MessageBox.Show("Invalid input: apu_max_queued_frames must be a number.\nSetting the default value of 64.");
                                    sectionTable["apu_max_queued_frames"] = 64;
                                }
                            }

                            // "mute" setting
                            sectionTable["mute"] = Mute.IsChecked;

                            // "use_dedicated_xma_thread" setting
                            if (sectionTable.ContainsKey("use_dedicated_xma_thread"))
                            {
                                sectionTable["use_dedicated_xma_thread"] = UseXMAThread.IsChecked;
                            }

                            // "use_new_decoder" setting
                            if (sectionTable.ContainsKey("use_new_decoder"))
                            {
                                sectionTable["use_new_decoder"] = UseNewDecoder.IsChecked;
                            }
                            break;
                        case "CPU":
                            // "break_on_unimplemented_instructions" setting
                            sectionTable["break_on_unimplemented_instructions"] = BreakOnUnimplementedInstructions.IsChecked;

                            break;
                        case "Content":
                            // "license_mask" setting
                            if (sectionTable.ContainsKey("license_mask"))
                            {
                                if (licenseMaskSelector.SelectedItem is ComboBoxItem selectedItem)
                                {
                                    string selectedLanguage = selectedItem.Content.ToString();
                                    switch (selectedLanguage)
                                    {
                                        case "No Licenses":
                                            sectionTable["license_mask"] = 0;
                                            break;
                                        case "First License":
                                            sectionTable["license_mask"] = 1;
                                            break;
                                        case "All Licenses":
                                            sectionTable["license_mask"] = -1;
                                            break;
                                        default:
                                            sectionTable["license_mask"] = 0;
                                            break;
                                    }
                                }
                            }
                            break;
                        case "D3D12":
                            // "d3d12_allow_variable_refresh_rate_and_tearing" setting
                            sectionTable["d3d12_allow_variable_refresh_rate_and_tearing"] = D3D12VRR.IsChecked;

                            // "d3d12_readback_resolve" setting
                            sectionTable["d3d12_readback_resolve"] = D3D12ReadbackResolve.IsChecked;

                            // "d3d12_queue_priority" setting
                            sectionTable["d3d12_queue_priority"] = D3D12QueuePrioritySelector.SelectedIndex;

                            break;
                        case "Display":
                            // "fullscreen" setting
                            sectionTable["fullscreen"] = Fullscreen.IsChecked;

                            // "postprocess_antialiasing" setting
                            switch (AntiAliasingSelector.SelectedIndex)
                            {
                                case 1:
                                    sectionTable["postprocess_antialiasing"] = "fxaa";
                                    break;
                                case 2:
                                    sectionTable["postprocess_antialiasing"] = "fxaa_extreme";
                                    break;
                                default:
                                    sectionTable["postprocess_antialiasing"] = "";
                                    break;
                            }

                            // "postprocess_dither" setting
                            sectionTable["postprocess_dither"] = PostProcessDither.IsChecked;

                            // "postprocess_ffx_cas_additional_sharpness" setting
                            sectionTable["postprocess_ffx_cas_additional_sharpness"] = (double)Math.Round(CASAdditionalSharpness.Value / 1000, 3);

                            // "postprocess_ffx_fsr_max_upsampling_passes" setting
                            sectionTable["postprocess_ffx_fsr_max_upsampling_passes"] = (int)FSRMaxUpsamplingPasses.Value;

                            // "postprocess_ffx_fsr_sharpness_reduction" setting
                            sectionTable["postprocess_ffx_fsr_sharpness_reduction"] = (double)Math.Round(FSRSharpnessReduction.Value / 1000, 3);

                            // "postprocess_scaling_and_sharpening" setting
                            switch (ScalingAndSharpeningSelector.SelectedIndex)
                            {
                                case 1:
                                    sectionTable["postprocess_scaling_and_sharpening"] = "cas";
                                    break;
                                case 2:
                                    sectionTable["postprocess_scaling_and_sharpening"] = "fsr";
                                    break;
                                default:
                                    sectionTable["postprocess_scaling_and_sharpening"] = "";
                                    break;
                            }

                            // "present_letterbox" setting
                            sectionTable["present_letterbox"] = Letterbox.IsChecked;
                            break;
                        case "GPU":
                            // "draw_resolution_scale" setting
                            sectionTable["draw_resolution_scale_x"] = (int)DrawResolutionScale.Value;
                            sectionTable["draw_resolution_scale_y"] = (int)DrawResolutionScale.Value;

                            // "framerate_limit" setting
                            if (sectionTable.ContainsKey("framerate_limit"))
                            {
                                sectionTable["framerate_limit"] = (int)FrameRateLimit.Value;
                            }

                            // "gamma_render_target_as_srgb" setting
                            sectionTable["gamma_render_target_as_srgb"] = gammaRenderTargetAsSRGB.IsChecked;

                            // "gpu" setting
                            switch (gpuSelector.SelectedIndex)
                            {
                                case 1:
                                    sectionTable["gpu"] = "d3d12";
                                    break;
                                case 2:
                                    sectionTable["gpu"] = "vulkan";
                                    break;
                                default:
                                    sectionTable["gpu"] = "any";
                                    break;
                            }

                            // "vsync" setting
                            sectionTable["vsync"] = vSync.IsChecked;

                            // "render_target_path_d3d12" setting
                            switch (D3D12RenderTargetPathSelector.SelectedIndex)
                            {
                                case 1:
                                    sectionTable["render_target_path_d3d12"] = "rtv";
                                    break;
                                case 2:
                                    sectionTable["render_target_path_d3d12"] = "rov";
                                    break;
                                default:
                                    sectionTable["render_target_path_d3d12"] = "";
                                    break;
                            }

                            // "render_target_path_vulkan" setting
                            switch (VulkanRenderTargetPathSelector.SelectedIndex)
                            {
                                case 1:
                                    sectionTable["render_target_path_vulkan"] = "fbo";
                                    break;
                                case 2:
                                    sectionTable["render_target_path_vulkan"] = "fsi";
                                    break;
                                default:
                                    sectionTable["render_target_path_vulkan"] = "";
                                    break;
                            }

                            // "clear_memory_page_state" setting
                            if (sectionTable.ContainsKey("clear_memory_page_state"))
                            {
                                sectionTable["clear_memory_page_state"] = ClearGPUCache.IsChecked;
                            }

                            break;
                        case "General":
                            // "allow_plugins" setting
                            if (sectionTable.ContainsKey("allow_plugins"))
                            {
                                sectionTable["allow_plugins"] = AllowPlugins.IsChecked;
                            }

                            // "apply_patches" setting
                            if (sectionTable.ContainsKey("apply_patches"))
                            {
                                sectionTable["apply_patches"] = ApplyPatches.IsChecked;
                            }

                            // "controller_hotkeys" setting
                            if (sectionTable.ContainsKey("controller_hotkeys"))
                            {
                                sectionTable["controller_hotkeys"] = ControllerHotkeys.IsChecked;
                            }

                            // "discord" setting
                            sectionTable["discord"] = DiscordRPC.IsChecked;

                            break;
                        case "HID":
                            // "hid" setting
                            switch (InputSystemSelector.SelectedIndex)
                            {
                                case 1:
                                    sectionTable["hid"] = "sdl";
                                    break;
                                case 2:
                                    sectionTable["hid"] = "xinput";
                                    break;
                                case 3:
                                    sectionTable["hid"] = "winkey";
                                    break;
                                default:
                                    sectionTable["hid"] = "any";
                                    break;
                            }

                            // "left_stick_deadzone_percentage" setting
                            if (sectionTable.ContainsKey("left_stick_deadzone_percentage"))
                            {
                                if ((LeftStickDeadzonePercentage.Value / 10) == 0 || (LeftStickDeadzonePercentage.Value / 10) == 1)
                                {
                                    sectionTable["left_stick_deadzone_percentage"] = (int)(LeftStickDeadzonePercentage.Value / 10);
                                }
                                else
                                {
                                    sectionTable["left_stick_deadzone_percentage"] = Math.Round(LeftStickDeadzonePercentage.Value / 10, 1);
                                };
                            }

                            // "right_stick_deadzone_percentage" setting
                            if (sectionTable.ContainsKey("right_stick_deadzone_percentage"))
                            {
                                if ((RightStickDeadzonePercentage.Value / 10) == 0 || (RightStickDeadzonePercentage.Value / 10) == 1)
                                {
                                    sectionTable["right_stick_deadzone_percentage"] = (int)(RightStickDeadzonePercentage.Value / 10);
                                }
                                else
                                {
                                    sectionTable["right_stick_deadzone_percentage"] = Math.Round(RightStickDeadzonePercentage.Value / 10, 1);
                                };
                            }

                            // "vibration" setting
                            if (sectionTable.ContainsKey("vibration"))
                            {
                                sectionTable["vibration"] = ControllerVibration.IsChecked;
                            }

                            break;
                        case "Kernel":
                            // "apply_title_update" setting
                            if (sectionTable.ContainsKey("apply_title_update"))
                            {
                                sectionTable["apply_title_update"] = ApplyTitleUpdate.IsChecked;
                            }

                            break;
                        case "Live":
                            // "api_address" setting
                            if (sectionTable.ContainsKey("api_address"))
                            {
                                sectionTable["api_address"] = ApiAddress.SelectedItem.ToString();
                            }

                            // "upnp" setting
                            if (sectionTable.ContainsKey("upnp"))
                            {
                                sectionTable["upnp"] = UPnP.IsChecked;
                            }
                            break;
                        case "Memory":
                            // "protect_zero" setting
                            sectionTable["protect_zero"] = ProtectZero.IsChecked;

                            break;
                        case "Storage":
                            // "mount_cache" setting
                            sectionTable["mount_cache"] = MountCache.IsChecked;

                            // "mount_scratch" setting
                            sectionTable["mount_scratch"] = MountScratch.IsChecked;

                            break;
                        case "UI":
                            // "show_achievement_notification" setting
                            if (sectionTable.ContainsKey("show_achievement_notification"))
                            {
                                sectionTable["show_achievement_notification"] = ShowAchievementNotifications.IsChecked;
                            }

                            break;
                        case "User":
                            if (sectionTable.ContainsKey("user_0_name"))
                            {
                                sectionTable["user_0_name"] = user0GamerTagTextBox.Text;
                            }
                            if (sectionTable.ContainsKey("user_1_name"))
                            {
                                sectionTable["user_1_name"] = user1GamerTagTextBox.Text;
                            }
                            if (sectionTable.ContainsKey("user_2_name"))
                            {
                                sectionTable["user_2_name"] = user2GamerTagTextBox.Text;
                            }
                            if (sectionTable.ContainsKey("user_3_name"))
                            {
                                sectionTable["user_3_name"] = user3GamerTagTextBox.Text;
                            }
                            break;
                        case "Video":
                            // "internal_display_resolution" setting
                            if (sectionTable.ContainsKey("internal_display_resolution"))
                            {
                                sectionTable["internal_display_resolution"] = InternalDisplayResolutionSelector.SelectedIndex;
                            }

                            break;
                        case "Vulkan":
                            // "vulkan_allow_present_mode_immediate" setting
                            sectionTable["vulkan_allow_present_mode_immediate"] = VulkanPresentModeImmediate.IsChecked;

                            // "vulkan_allow_present_mode_mailbox" setting
                            sectionTable["vulkan_allow_present_mode_mailbox"] = VulkanPresentModeMailbox.IsChecked;

                            // "vulkan_allow_present_mode_fifo_relaxed" setting
                            sectionTable["vulkan_allow_present_mode_fifo_relaxed"] = VulkanPresentModeFIFORelaxed.IsChecked;

                            break;
                        case "XConfig":
                            // "user_language" setting
                            if (sectionTable.ContainsKey("user_language"))
                            {
                                if (UserLanguageSelector.SelectedItem is ComboBoxItem selectedItem)
                                {
                                    string selectedLanguage = selectedItem.Content.ToString();
                                    if (languageMap.TryGetValue(selectedLanguage, out int languageNumber))
                                    {
                                        sectionTable["user_language"] = languageNumber;
                                    }
                                }
                            }
                            break;
                        default:
                            break;
                    }
                }
                File.WriteAllText(configLocation, Toml.FromModel(configFile));
                if (NvidiaDriverSettings.Visibility != Visibility.Collapsed)
                {
                    await SaveNVIDIASettings();
                }
                MessageBox.Show("Settings are saved");
                await Task.Delay(1);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// When button "Save changes" is pressed, save changes to the configuration file
        /// </summary>
        private async void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Log.Information("Saving changes");
                switch (ConfigurationFilesList.SelectedItem.ToString())
                {
                    case "Xenia Stable Profile":
                        await SaveChanges(Path.Combine(App.baseDirectory, App.appConfiguration.XeniaStable.ConfigurationFileLocation));
                        break;
                    case "Xenia Canary Profile":
                        await SaveChanges(Path.Combine(App.baseDirectory, App.appConfiguration.XeniaCanary.ConfigurationFileLocation));
                        break;
                    case "Xenia Netplay Profile":
                        await SaveChanges(Path.Combine(App.baseDirectory, App.appConfiguration.XeniaNetplay.ConfigurationFileLocation));
                        break;
                    default:
                        // Game specific
                        InstalledGame selectedGame = Games.FirstOrDefault(game => game.Title == ConfigurationFilesList.SelectedItem.ToString());
                        if (selectedGame.EmulatorVersion != "Custom")
                        {
                            await SaveChanges(Path.Combine(App.baseDirectory, selectedGame.ConfigFilePath));
                        }
                        else
                        {
                            await SaveChanges(selectedGame.ConfigFilePath);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
                return;
            }
        }

        /// <summary>
        /// Opens the configuration file in an editor (Usually Notepad if no default app is found)
        /// </summary>
        private async void OpenConfigurationFile_Click(object sender, RoutedEventArgs e)
        {
            string configPath = "";
            ProcessStartInfo startInfo = new ProcessStartInfo();
            Process process;
            await Task.Delay(1);
            try
            {
                switch (ConfigurationFilesList.SelectedItem.ToString())
                {
                    case "Xenia Stable Profile":
                        configPath = Path.Combine(App.baseDirectory, App.appConfiguration.XeniaStable.ConfigurationFileLocation);
                        break;
                    case "Xenia Canary Profile":
                        configPath = Path.Combine(App.baseDirectory, App.appConfiguration.XeniaCanary.ConfigurationFileLocation);
                        break;
                    case "Xenia Netplay Profile":
                        configPath = Path.Combine(App.baseDirectory, App.appConfiguration.XeniaNetplay.ConfigurationFileLocation);
                        break;
                    default:
                        InstalledGame selectedGame = Games.First(game => game.Title == ConfigurationFilesList.SelectedItem.ToString());
                        Log.Information($"{selectedGame.Title} is selected");
                        configPath = Path.Combine(App.baseDirectory, selectedGame.ConfigFilePath);
                        break;
                }
                startInfo = new ProcessStartInfo
                {
                    FileName = configPath,
                    UseShellExecute = true
                };

                Log.Information("Loading the configuration file in the default app");
                process = Process.Start(startInfo);
                if (process == null)
                {
                    // If process is null, it means it didn't start successfully
                    throw new Exception("Process did not start successfully.");
                }
                Log.Information("Waiting for exit");
                await process.WaitForExitAsync();
                Log.Information("Reading changes made to the configuration file");
                await ReadConfigFile(configPath);

            }
            catch (System.ComponentModel.Win32Exception)
            {
                Log.Warning("Default application not found");
                Log.Information("Trying to open the file with notepad");
                startInfo.FileName = "notepad.exe";
                startInfo.Arguments = configPath;
                process = Process.Start(startInfo);
                if (process == null)
                {
                    // If process is null, it means it didn't start successfully
                    throw new Exception("Process did not start successfully.");
                }
                Log.Information("Waiting for exit");
                await process.WaitForExitAsync();
                Log.Information("Reading changes made to the configuration file");
                await ReadConfigFile(configPath);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
                return;
            }
        }

        /// <summary>
        /// Copies all of the settings from the default file to the currently selected game's configuration file
        /// </summary>
        private async void CopyDefaultSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                InstalledGame selectedGame = Games.First(game => game.Title == ConfigurationFilesList.SelectedItem.ToString());
                switch (selectedGame.EmulatorVersion)
                {
                    case "Stable":
                        await ReadConfigFile(Path.Combine(App.baseDirectory, App.appConfiguration.XeniaStable.ConfigurationFileLocation));
                        break;
                    case "Canary":
                        await ReadConfigFile(Path.Combine(App.baseDirectory, App.appConfiguration.XeniaCanary.ConfigurationFileLocation));
                        break;
                    case "Netplay":
                        await ReadConfigFile(Path.Combine(App.baseDirectory, App.appConfiguration.XeniaNetplay.ConfigurationFileLocation));
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
                return;
            }
        }

        /// <summary>
        /// This checks for input to be less than 12 characters
        /// If it is, change it back to default setting
        /// </summary>
        private void apuMaxQueuedFramesTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (apuMaxQueuedFramesTextBox.Text.Length > 12)
            {
                MessageBox.Show("You went over the allowed limit");
                apuMaxQueuedFramesTextBox.Text = "64";
            }
        }

        /// <summary>
        /// Displays the value on the textbox below this slider
        /// </summary>
        private void FrameRateLimit_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider slider = sender as Slider;
            if (slider != null)
            {
                // Text shown under the slider
                if (slider.Value == 0)
                {
                    FrameRateLimiterValue.Text = "Off";
                }
                else
                {
                    FrameRateLimiterValue.Text = $"{slider.Value} FPS";
                }
                AutomationProperties.SetName(FrameRateLimit, $"Xenia Framerate Limiter: {slider.Value} FPS");
            }
        }

        /// <summary>
        /// Checks if the value of NvidiaFrameRateLimiter slider isn't 1-19
        /// </summary>
        private void NvidiaFrameRateLimiter_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider slider = sender as Slider;
            if (slider != null)
            {
                // This makes sure the FrameRate Limiter value isn't 1 - 19
                if ((int)slider.Value > 0 && (int)slider.Value < 20)
                {
                    slider.Value = 20;
                }

                // Text shown under the slider
                if (slider.Value == 0)
                {
                    NvidiaFrameRateLimiterValue.Text = "Off";
                }
                else
                {
                    NvidiaFrameRateLimiterValue.Text = $"{slider.Value} FPS";
                }
                AutomationProperties.SetName(NvidiaFrameRateLimiter, $"NVIDIA Framerate Limiter: {slider.Value} FPS");
            }
        }

        /// <summary>
        /// Checks which option is selected and then shows specific settings for that Graphics API
        /// </summary>
        private void gpuSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (gpuSelector.SelectedIndex)
            {
                case 1:
                    Direct3DSettings.Visibility = Visibility.Visible;
                    VulkanSettings.Visibility = Visibility.Collapsed;
                    break;
                case 2:
                    Direct3DSettings.Visibility = Visibility.Collapsed;
                    VulkanSettings.Visibility = Visibility.Visible;
                    break;
                default:
                    Direct3DSettings.Visibility = Visibility.Visible;
                    VulkanSettings.Visibility = Visibility.Visible;
                    break;
            }
        }

        /// <summary>
        /// Checks for value changes on DrawResolutionScale
        /// </summary>
        private void DrawResolutionScale_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            AutomationProperties.SetName(DrawResolutionScale, $"Draw Resolution Scale: {DrawResolutionScale.Value}");
        }

        /// <summary>
        /// Checks for value changes on CASAdditionalSharpness and shows them on the textbox
        /// </summary>
        private void CASAdditionalSharpness_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            CASAdditionalSharpnessValue.Text = Math.Round((CASAdditionalSharpness.Value / 1000), 3).ToString();
            AutomationProperties.SetName(CASAdditionalSharpness, $"CAS Additional Sharpness: {Math.Round((CASAdditionalSharpness.Value / 1000), 3)}");
        }

        /// <summary>
        /// Checks for value changes on FSRSharpnessReduction slider and shows them on the textbox
        /// </summary>
        private void FSRSharpnessReduction_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            FSRSharpnessReductionValue.Text = Math.Round((FSRSharpnessReduction.Value / 1000), 3).ToString();
            AutomationProperties.SetName(FSRSharpnessReduction, $"FSR Sharpness Reduction: {Math.Round((FSRSharpnessReduction.Value / 1000), 3)}");
        }

        /// <summary>
        /// Checks for value changes on FSRMaxUpsamplingPasses slider and shows them on the textbox
        /// </summary>
        private void FSRMaxUpsamplingPasses_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            AutomationProperties.SetName(FSRMaxUpsamplingPasses, $"FSR MaxUpsampling Passes: {FSRMaxUpsamplingPasses.Value}");
        }

        /// <summary>
        /// Check to see if there are less than 15 characters in the gamertag textbox
        /// </summary>
        private void GamerTagTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox.Text.Length > 15)
            {
                int selectionStart = textBox.SelectionStart;
                textBox.Text = textBox.Text.Substring(0, 15);
                textBox.SelectionStart = selectionStart > 15 ? 15 : selectionStart;
            }
        }

        /// <summary>
        /// Checks for value changes on LeftStickDeadzonePercentage slider and shows them on the textbox
        /// </summary>
        private void LeftStickDeadzonePercentage_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            LeftStickDeadzonePercentageValue.Text = Math.Round((LeftStickDeadzonePercentage.Value / 10), 1).ToString();
            AutomationProperties.SetName(LeftStickDeadzonePercentage, $"Left Stick Deadzone Percentage: {Math.Round((LeftStickDeadzonePercentage.Value / 10), 1)}");
        }

        /// <summary>
        /// Checks for value changes on RightStickDeadzonePercentage slider and shows them on the textbox
        /// </summary>
        private void RightStickDeadzonePercentage_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            RightStickDeadzonePercentageValue.Text = Math.Round((RightStickDeadzonePercentage.Value / 10), 1).ToString();
            AutomationProperties.SetName(RightStickDeadzonePercentage, $"Right Stick Deadzone Percentage: {Math.Round((RightStickDeadzonePercentage.Value / 10), 1)}");
        }
    }
}
