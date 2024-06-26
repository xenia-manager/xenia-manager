﻿using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

// Imported
using Serilog;
using Xenia_Manager.Windows;
using Xenia_Manager.Classes;
using Newtonsoft.Json;
using Tomlyn;
using Tomlyn.Model;
using Tomlyn.Syntax;


namespace Xenia_Manager.Pages
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : Page
    {
        /// <summary>
        /// Every installed game is stored in here after reading from .JSON file
        /// </summary>
        private List<InstalledGame> Games;

        /// <summary>
        /// This is just to skip the initial SelectionChange when adding items via XAML
        /// </summary>
        private bool test = false;

        public Settings()
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
                if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + @"installedGames.json"))
                {
                    Log.Information("Loading all of the games into the ComboBox");
                    string JSON = System.IO.File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + @"installedGames.json");
                    Games = JsonConvert.DeserializeObject<List<InstalledGame>>((JSON));

                    // Sorting the list
                    Games.Sort((Game1, Game2) => string.Compare(Game1.Title, Game2.Title, StringComparison.Ordinal));
                    foreach (InstalledGame Game in Games)
                    {
                        Log.Information($"Adding {Game.Title} to the ConfigurationList ComboBox");
                        ConfigurationFilesList.Items.Add(Game.Title);
                    }
                }
                else
                {
                    Log.Information("No installed games found");
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
                            Log.Information($"apu_max_queued_frames - {sectionTable["apu_max_queued_frames"].ToString()}");
                            apuMaxQueuedFramesTextBox.Text = sectionTable["apu_max_queued_frames"].ToString();

                            // "mute" setting
                            Log.Information($"mute - {(bool)sectionTable["mute"]}");
                            Mute.IsChecked = (bool)sectionTable["mute"];

                            // "use_dedicated_xma_thread" setting
                            Log.Information($"use_dedicated_xma_thread - {(bool)sectionTable["use_dedicated_xma_thread"]}");
                            UseXMAThread.IsChecked = (bool)sectionTable["use_dedicated_xma_thread"];

                            // "use_new_decoder" setting
                            Log.Information($"use_new_decoder - {(bool)sectionTable["use_new_decoder"]}");
                            UseNewDecoder.IsChecked = (bool)sectionTable["use_new_decoder"];
                            break;
                        case "Content":
                            Log.Information("Content settings");
                            // "license_mask" setting
                            Log.Information($"license_mask - {int.Parse(sectionTable["license_mask"].ToString())}");
                            if (int.Parse(sectionTable["license_mask"].ToString()) == -1)
                            {
                                licenseMaskSelector.SelectedIndex = 0;
                            }
                            else
                            {
                                licenseMaskSelector.SelectedIndex = int.Parse(sectionTable["license_mask"].ToString());
                            }
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

                            // "postprocess_ffx_fsr_max_upsampling_passes" setting
                            Log.Information($"postprocess_ffx_fsr_max_upsampling_passes - {int.Parse(sectionTable["postprocess_ffx_fsr_max_upsampling_passes"].ToString())}");
                            FSRMaxUpsamplingPasses.Value = int.Parse(sectionTable["postprocess_ffx_fsr_max_upsampling_passes"].ToString());

                            // "postprocess_ffx_fsr_sharpness_reduction" setting
                            Log.Information($"postprocess_ffx_fsr_sharpness_reduction - {double.Parse(sectionTable["postprocess_ffx_fsr_sharpness_reduction"].ToString())}");
                            FSRSharpnessReduction.Value = double.Parse(sectionTable["postprocess_ffx_fsr_sharpness_reduction"].ToString()) * 1000;

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
                            Log.Information($"framerate_limit - {sectionTable["framerate_limit"].ToString()}");
                            FrameRateLimit.Value = int.Parse(sectionTable["framerate_limit"].ToString());

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
                            break;
                        case "General":
                            Log.Information("General settings");

                            // "allow_plugins" setting
                            Log.Information($"allow_plugins - {(bool)sectionTable["allow_plugins"]}");
                            AllowPlugins.IsChecked = (bool)sectionTable["allow_plugins"];

                            // "apply_patches" setting
                            Log.Information($"apply_patches - {(bool)sectionTable["apply_patches"]}");
                            ApplyPatches.IsChecked = (bool)sectionTable["apply_patches"];

                            // "controller_hotkeys" setting
                            Log.Information($"controller_hotkeys - {(bool)sectionTable["controller_hotkeys"]}");
                            ControllerHotkeys.IsChecked = (bool)sectionTable["controller_hotkeys"];

                            // "discord" setting
                            Log.Information($"discord - {(bool)sectionTable["discord"]}");
                            DiscordRPC.IsChecked = (bool)sectionTable["discord"];

                            break;
                        case "HID":
                            Log.Information("HID settings");

                            // "left_stick_deadzone_percentage" setting
                            Log.Information($"left_stick_deadzone_percentage - {double.Parse(sectionTable["left_stick_deadzone_percentage"].ToString())}");
                            LeftStickDeadzonePercentage.Value = Math.Round(double.Parse(sectionTable["left_stick_deadzone_percentage"].ToString()) * 10, 1);

                            // "right_stick_deadzone_percentage" setting
                            Log.Information($"right_stick_deadzone_percentage - {double.Parse(sectionTable["left_stick_deadzone_percentage"].ToString())}");
                            RightStickDeadzonePercentage.Value = Math.Round(double.Parse(sectionTable["right_stick_deadzone_percentage"].ToString()) * 10, 1);

                            // "vibration" setting
                            Log.Information($"vibration - {(bool)sectionTable["vibration"]}");
                            ControllerVibration.IsChecked = (bool)sectionTable["vibration"];

                            break;
                        case "Kernel":
                            Log.Information("Kernel settings");

                            // "apply_title_update" setting
                            Log.Information($"apply_title_update - {(bool)sectionTable["apply_title_update"]}");
                            ApplyTitleUpdate.IsChecked = (bool)sectionTable["apply_title_update"];

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
                            Log.Information($"show_achievement_notification - {(bool)sectionTable["show_achievement_notification"]}");
                            ShowAchievementNotifications.IsChecked = (bool)sectionTable["show_achievement_notification"];

                            break;
                        case "Video":
                            Log.Information("Video settings");

                            // "internal_display_resolution" setting
                            Log.Information($"internal_display_resolution - {int.Parse(sectionTable["internal_display_resolution"].ToString())}");
                            InternalDisplayResolutionSelector.SelectedIndex = int.Parse(sectionTable["internal_display_resolution"].ToString());

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
        /// Function that executes other functions asynchronously
        /// </summary>
        private async void InitializeAsync()
        {
            try
            {
                await Dispatcher.InvokeAsync(async () =>
                {
                    Mouse.OverrideCursor = Cursors.Wait;
                    await LoadInstalledGames();
                    Log.Information("Loading default configuration file");
                    await ReadConfigFile(App.appConfiguration.EmulatorLocation + "xenia-canary.config.toml");
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

                            // "mute" setting
                            sectionTable["mute"] = Mute.IsChecked;

                            // "use_dedicated_xma_thread" setting
                            sectionTable["use_dedicated_xma_thread"] = UseXMAThread.IsChecked;

                            // "use_new_decoder" setting
                            sectionTable["use_new_decoder"] = UseNewDecoder.IsChecked;
                            break;
                        case "Content":
                            // "license_mask" setting
                            sectionTable["license_mask"] = licenseMaskSelector.SelectedIndex;
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
                            break;
                        case "GPU":
                            // "draw_resolution_scale" setting
                            sectionTable["draw_resolution_scale_x"] = (int)DrawResolutionScale.Value;
                            sectionTable["draw_resolution_scale_y"] = (int)DrawResolutionScale.Value;

                            // "framerate_limit" setting
                            sectionTable["framerate_limit"] = (int)FrameRateLimit.Value;

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

                            break;
                        case "General":
                            // "allow_plugins" setting
                            sectionTable["allow_plugins"] = AllowPlugins.IsChecked;

                            // "apply_patches" setting
                            sectionTable["apply_patches"] = ApplyPatches.IsChecked;

                            // "controller_hotkeys" setting
                            sectionTable["controller_hotkeys"] = ControllerHotkeys.IsChecked;

                            // "discord" setting
                            sectionTable["discord"] = DiscordRPC.IsChecked;

                            break;
                        case "HID":
                            // "left_stick_deadzone_percentage" setting
                            if ((LeftStickDeadzonePercentage.Value / 10) == 0 || (LeftStickDeadzonePercentage.Value / 10) == 1)
                            {
                                sectionTable["left_stick_deadzone_percentage"] = (int)(LeftStickDeadzonePercentage.Value / 10);
                            }
                            else
                            {
                                sectionTable["left_stick_deadzone_percentage"] = Math.Round(LeftStickDeadzonePercentage.Value / 10, 1);
                            };

                            // "right_stick_deadzone_percentage" setting
                            if ((RightStickDeadzonePercentage.Value / 10) == 0 || (RightStickDeadzonePercentage.Value / 10) == 1)
                            {
                                sectionTable["right_stick_deadzone_percentage"] = (int)(RightStickDeadzonePercentage.Value / 10);
                            }
                            else
                            {
                                sectionTable["right_stick_deadzone_percentage"] = Math.Round(RightStickDeadzonePercentage.Value / 10, 1);
                            };

                            // "vibration" setting
                            sectionTable["vibration"] = ControllerVibration.IsChecked;

                            break;
                        case "Kernel":
                            // "apply_title_update" setting
                            sectionTable["apply_title_update"] = ApplyTitleUpdate.IsChecked;

                            break;
                        case "Storage":
                            // "mount_cache" setting
                            sectionTable["mount_cache"] = MountCache.IsChecked;

                            // "mount_scratch" setting
                            sectionTable["mount_scratch"] = MountScratch.IsChecked;

                            break;
                        case "UI":
                            // "show_achievement_notification" setting
                            sectionTable["show_achievement_notification"] = ShowAchievementNotifications.IsChecked;

                            break;
                        case "Video":
                            // "internal_display_resolution" setting
                            sectionTable["internal_display_resolution"] = InternalDisplayResolutionSelector.SelectedIndex;

                            break;
                        default:
                            break;
                    }
                }
                File.WriteAllText(configLocation, Toml.FromModel(configFile));
                await Task.Delay(1);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message + "\nFull Error:\n" + ex);
                MessageBox.Show(ex.Message);
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
                if (test)
                {
                    if (ConfigurationFilesList.SelectedIndex > 0)
                    {
                        InstalledGame selectedGame = Games.First(game => game.Title == ConfigurationFilesList.SelectedItem.ToString());
                        Log.Information($"Loading configuration file of {selectedGame.Title}");
                        await ReadConfigFile(selectedGame.ConfigFilePath);
                    }
                    else
                    {
                        Log.Information("Loading default configuration file");
                        await ReadConfigFile(App.appConfiguration.EmulatorLocation + "xenia-canary.config.toml");
                    }
                }
                else
                {
                    test = true;
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
        /// When button "Save changes" is pressed, save changes to the configuration file
        /// </summary>
        private async void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Log.Information("Saving changes");
                if (ConfigurationFilesList.SelectedIndex == 0)
                {
                    await SaveChanges(App.appConfiguration.EmulatorLocation + "xenia-canary.config.toml");
                }
                else
                {
                    InstalledGame selectedGame = Games.FirstOrDefault(game => game.Title == ConfigurationFilesList.SelectedItem.ToString());
                    await SaveChanges(selectedGame.ConfigFilePath);
                };
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
        /// Checks for value changes on CASAdditionalSharpness and shows them on the textbox
        /// </summary>
        private void CASAdditionalSharpness_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            CASAdditionalSharpnessValue.Text = Math.Round((CASAdditionalSharpness.Value / 1000), 3).ToString();
        }

        /// <summary>
        /// Checks for value changes on FSRSharpnessReduction slider and shows them on the textbox
        /// </summary>
        private void FSRSharpnessReduction_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            FSRSharpnessReductionValue.Text = Math.Round((FSRSharpnessReduction.Value / 1000), 3).ToString();
        }

        /// <summary>
        /// Checks for value changes on LeftStickDeadzonePercentage slider and shows them on the textbox
        /// </summary>
        private void LeftStickDeadzonePercentage_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            LeftStickDeadzonePercentageValue.Text = Math.Round((LeftStickDeadzonePercentage.Value / 10), 1).ToString();
        }

        /// <summary>
        /// Checks for value changes on RightStickDeadzonePercentage slider and shows them on the textbox
        /// </summary>
        private void RightStickDeadzonePercentage_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            RightStickDeadzonePercentageValue.Text = Math.Round((RightStickDeadzonePercentage.Value / 10), 1).ToString();
        }
    }
}
