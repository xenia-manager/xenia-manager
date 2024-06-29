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
            ConfigurationFilesList.Items.Add("Default Profile");
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
                            config.APU = new Apu
                            {
                                apu = sectionTable["apu"] as string,
                                apu_max_queued_frames = int.Parse(sectionTable["apu_max_queued_frames"].ToString()),
                                mute = (bool)sectionTable["mute"],
                                use_dedicated_xma_thread = (bool)sectionTable["use_dedicated_xma_thread"],
                                use_new_decoder = (bool)sectionTable["use_new_decoder"]
                            };
                            break;
                        case "Content":
                            config.Content = new Content
                            { 
                                license_mask = int.Parse(sectionTable["license_mask"].ToString())
                            };
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
    }
}
