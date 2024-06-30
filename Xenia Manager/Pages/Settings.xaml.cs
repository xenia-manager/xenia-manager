using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using Tomlyn;
using Tomlyn.Model;
using Tomlyn.Syntax;

using Serilog;
using Xenia_Manager.Windows;
using Xenia_Manager.Classes;
using System.Reflection;
namespace Xenia_Manager.Pages
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : Page
    {
        /// <summary>
        /// Xenia configuration file
        /// </summary>
        private EmulatorConfiguration config;

        public Settings()
        {
            InitializeComponent();
            InitializeAsync();
        }

        /// <summary>
        /// Reads the .toml file of the emulator
        /// </summary>
        /// <param name="configLocation"></param>
        private async Task ReadConfigFile(string configLocation)
        {
            try
            {
                config = new EmulatorConfiguration();
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
                            config.Display = new Display
                            {
                                fullscreen = (bool)sectionTable["fullscreen"],
                                postprocess_antialiasing = sectionTable["postprocess_antialiasing"] as string,
                                postprocess_dither = (bool)sectionTable["postprocess_dither"],
                                postprocess_ffx_cas_additional_sharpness = double.Parse(sectionTable["postprocess_ffx_cas_additional_sharpness"].ToString()),
                                postprocess_ffx_fsr_max_upsampling_passes = int.Parse(sectionTable["postprocess_ffx_fsr_max_upsampling_passes"].ToString()),
                                postprocess_ffx_fsr_sharpness_reduction = double.Parse(sectionTable["postprocess_ffx_fsr_sharpness_reduction"].ToString()),
                                postprocess_scaling_and_sharpening = sectionTable["postprocess_scaling_and_sharpening"] as string
                            };
                            break;
                        case "GPU":
                            config.GPU = new Gpu
                            {
                                draw_resolution_scale_x = int.Parse(sectionTable["draw_resolution_scale_x"].ToString()),
                                draw_resolution_scale_y = int.Parse(sectionTable["draw_resolution_scale_y"].ToString()),
                                framerate_limit = int.Parse(sectionTable["framerate_limit"].ToString()),
                                gamma_render_target_as_srgb = (bool)sectionTable["gamma_render_target_as_srgb"],
                                gpu = sectionTable["gpu"] as string,
                                vsync = (bool)sectionTable["vsync"]
                            };
                            break;
                        case "General":
                            config.General = new General
                            {
                                allow_plugins = (bool)sectionTable["allow_plugins"],
                                apply_patches = (bool)sectionTable["apply_patches"],
                                controller_hotkeys = (bool)sectionTable["controller_hotkeys"],
                                discord = (bool)sectionTable["discord"],
                                time_scalar = int.Parse(sectionTable["time_scalar"].ToString())
                            };
                            break;
                        case "HID":
                            config.HID = new Hid
                            {
                                left_stick_deadzone_percentage = double.Parse(sectionTable["left_stick_deadzone_percentage"].ToString()),
                                right_stick_deadzone_percentage = double.Parse(sectionTable["right_stick_deadzone_percentage"].ToString()),
                                vibration = (bool)sectionTable["vibration"]
                            };
                            break;
                        case "Kernel":
                            config.Kernel = new Kernel
                            {
                                apply_title_update = (bool)sectionTable["apply_title_update"]
                            };
                            break;
                        case "Storage":
                            config.Storage = new Storage
                            {
                                mount_cache = (bool)sectionTable["mount_cache"],
                                mount_scratch = (bool)sectionTable["mount_scratch"]
                            };
                            break;
                        case "UI":
                            config.UI = new Ui
                            {
                                show_achievement_notification = (bool)sectionTable["show_achievement_notification"]
                            };
                            break;
                        case "Video":
                            config.Video = new Video
                            {
                                avpack = int.Parse(sectionTable["avpack"].ToString()),
                                internal_display_resolution = int.Parse(sectionTable["internal_display_resolution"].ToString()),
                                use_50Hz_mode = (bool)sectionTable["use_50Hz_mode"],
                                video_standard = int.Parse(sectionTable["video_standard"].ToString())
                            };
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
                    Log.Information("Reading default configuration file");
                    await ReadConfigFile(App.appConfiguration.EmulatorLocation + "xenia-canary.config - Copy.toml");
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
                                MessageBox.Show("Invalid input\napu_max_queued_frames must be a number");
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
                            break;
                        case "GPU":
                            break;
                        case "General":
                            break;
                        case "HID":
                            break;
                        case "Kernel":
                            break;
                        case "Storage":
                            break;
                        case "UI":
                            break;
                        case "Video":
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

        /// <summary>
        /// When button "Save changes" is pressed, save changes to the configuration file
        /// </summary>
        private async void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ConfigurationFilesList.SelectedIndex == 0)
                {
                    await SaveChanges(App.appConfiguration.EmulatorLocation + "xenia-canary.config - Copy.toml");
                }
                await Task.Delay(1);
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
    }
}
