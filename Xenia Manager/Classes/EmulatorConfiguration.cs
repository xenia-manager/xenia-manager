using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tomlyn;

namespace Xenia_Manager.Classes
{
    public class EmulatorConfiguration
    {
        public Apu APU { get; set; }
        public Content Content { get; set; }
        public Display Display { get; set; }
        public Gpu GPU { get; set; }
        public General General { get; set; }
        public Hid HID { get; set; }
        public Kernel Kernel { get; set; }
        public Storage Storage { get; set; }
        public Ui UI { get; set; }
        public Video Video { get; set; }
    }

    public class Apu
    {
        /// <summary>
        /// Audio system
        /// any, nop, sdl, xaudio2
        /// </summary>
        public string apu { get; set; }

        /// <summary>
        /// Allows changing max buffered audio frames to reduce audio delay. Minimum is 16.
        /// </summary>
        public int apu_max_queued_frames { get; set; }

        /// <summary>
        /// Mutes all audio output
        /// </summary>
        public bool mute { get; set; }

        /// <summary>
        /// <para>Enables XMA decoding on separate thread</para>
        /// Disabled should produce better results, but decrease performance a bit
        /// </summary>
        public bool use_dedicated_xma_thread { get; set; }

        /// <summary>
        /// Enables usage of new experimental XMA audio decoder
        /// </summary>
        public bool use_new_decoder { get; set; }
    }

    public class Content
    {
        /// <summary>
        /// Set license mask for activated content. 
        /// <para>0 = No licenses enabled.</para>
        /// 1 = First license enabled (Generally the full version license in Xbox Live Arcade titles)
        /// </summary>
        public int license_mask { get; set; }
    }

    public class Display
    {
        /// <summary>
        /// Whether to launch the emulator in fullscreen
        /// </summary>
        public bool fullscreen { get; set; }

        /// <summary>
        /// Post-processing anti-aliasing effect to apply to the image output of the game
        /// <para>Heavily recommended when AMD FidelityFX Contrast Adaptive Sharpening or Super Resolution 1.0 is active</para>
        /// none/empty string
        /// <para>fxaa - NVIDIA Fast Approximate Anti-Aliasing 3.11, normal quality preset</para>
        /// fxaa_extreme - NVIDIA Fast Approximate Anti-Aliasing 3.11, extreme quality preset 
        /// </summary>
        public string postprocess_antialiasing { get; set; }

        /// <summary>
        /// Dither the final image output from the internal precision to 8 bits per channel so gradients are smoother
        /// <para>On a 10bpc display, the lower 2 bits will still be kept, but noise will be added to them - disabling may be recommended for 10bpc, but it depends on the 10bpc displaying capabilities of the actual display used</para>
        /// </summary>
        public bool postprocess_dither { get; set; }

        /// <summary>
        /// Additional sharpness for AMD FidelityFX Contrast Adaptive Sharpening (CAS)
        /// <para>0.000 - 1.000, higher is sharper</para>
        /// </summary>
        public double postprocess_ffx_cas_additional_sharpness { get; set; }

        /// <summary>
        /// Maximum number of upsampling passes performed in AMD FidelityFX Super Resolution 1.0 (FSR) before falling back to bilinear stretching after the final pass
        /// <para>Each pass upscales only to up to 2x2 the previous size. If the game outputs a 1280x720 image, 1 pass will upscale it to up to 2560x1440 (below 4K), after 2 passes it will be upscaled to a maximum of 5120x2880 (including 3840x2160 for 4K)...</para>
        /// This variable has no effect if the display resolution isn't very high, but may be reduced on resolutions like 4K or 8K in case the performance impact of multiple FSR upsampling passes is too high, or if softer edges are desired
        /// <para>1 - 4, 4 is the maximum internally supported by Xenia</para>
        /// </summary>
        public int postprocess_ffx_fsr_max_upsampling_passes { get; set; }

        /// <summary>
        /// Sharpness reduction for AMD FidelityFX Super Resolution 1.0 (FSR), in stops
        /// 0.000 - 1.000, lower is sharper
        /// </summary>
        public double postprocess_ffx_fsr_sharpness_reduction { get; set; }

        /// <summary>
        /// Post-processing effect to use for resampling and/or sharpening of the final display output
        /// <para>bilinear - Original image at 1:1, simple bilinear stretching for resampling.</para>
        /// cas - Use AMD FidelityFX Contrast Adaptive Sharpening (CAS) for sharpening at scaling factors of up to 2x2, with additional bilinear stretching for larger factors
        /// <para>fsr - Use AMD FidelityFX Super Resolution 1.0 (FSR) for highest-quality upscaling, or AMD FidelityFX Contrast Adaptive Sharpening for sharpening while not scaling or downsampling</para>
        /// For scaling by factors of more than 2x2, multiple FSR passes are done
        /// </summary>
        public string postprocess_scaling_and_sharpening { get; set; }
    }

    public class Gpu
    {
        /// <summary>
        /// Integer pixel width scale used for scaling the rendering resolution opaquely to the game
        /// <para>1, 2 and 3 may be supported, but support of anything above 1 depends on the device properties, such as whether it supports sparse binding / tiled resources, the number of virtual address bits per resource, and other factors</para>
        /// Various effects and parts of game rendering pipelines may work incorrectly as pixels become ambiguous from the game's perspective and because half-pixel offset (which normally doesn't affect coverage when MSAA isn't used) becomes full-pixel
        /// </summary>
        public int draw_resolution_scale_x { get; set; }

        /// <summary>
        /// Integer pixel height scale used for scaling the rendering resolution opaquely to the game
        /// <para>1, 2 and 3 may be supported, but support of anything above 1 depends on the device properties, such as whether it supports sparse binding / tiled resources, the number of virtual address bits per resource, and other factors</para>
        /// Various effects and parts of game rendering pipelines may work incorrectly as pixels become ambiguous from the game's perspective and because half-pixel offset (which normally doesn't affect coverage when MSAA isn't used) becomes full-pixel
        /// </summary>
        public int draw_resolution_scale_y { get; set; }

        /// <summary>
        /// Maximum frames per second
        /// <para>Defaults to 60, when set to 0, and VSYNC is enabled</para>
        /// 0 = Unlimited Frames
        /// </summary>
        public int framerate_limit { get; set; }

        /// <summary>
        /// When the host can't write piecewise linear gamma directly with correct blending, use sRGB output on the host for conceptually correct blending in linear color space while having slightly different precision distribution in the render target and severely incorrect values if the game accesses the resulting colors directly as raw data
        /// </summary>
        public bool gamma_render_target_as_srgb { get; set; }

        /// <summary>
        /// Graphics system
        /// <para>any, d3d12, vulkan, null</para>
        /// </summary>
        public string gpu { get; set; }

        /// <summary>
        /// Enable VSYNC
        /// </summary>
        public bool vsync { get; set; }
    }

    public class General
    {
        /// <summary>
        /// Allows loading of plugins/trainers from plugins\title_id\plugin.xex
        /// <para>Plugin are homebrew xex modules which can be used for making mods</para> 
        /// This feature is experimental
        /// </summary>
        public bool allow_plugins { get; set; }

        /// <summary>
        /// Enables custom patching functionality
        /// </summary>
        public bool apply_patches { get; set; }

        /// <summary>
        /// Hotkeys for Xbox and PS controllers
        /// </summary>
        public bool controller_hotkeys { get; set; }

        /// <summary>
        /// Enable Discord rich presence
        /// </summary>
        public bool discord { get; set; }

        /// <summary>
        /// Scalar used to speed or slow time
        /// <para>1x, 2x, 1/2x, etc</para>
        /// </summary>
        public int time_scalar { get; set; }
    }

    public class Hid
    {
        /// <summary>
        /// Defines deadzone level for left stick
        /// <para>0.0 - 1.0</para>
        /// </summary>
        public double left_stick_deadzone_percentage { get; set; }

        /// <summary>
        /// Defines deadzone level for right stick
        /// <para>0.0 - 1.0</para>
        /// </summary>
        public double right_stick_deadzone_percentage { get; set; }

        /// <summary>
        /// Toggle controller vibration
        /// </summary>
        public bool vibration { get; set; }
    }

    public class Kernel
    {
        /// <summary>
        /// Apply title updates
        /// </summary>
        public bool apply_title_update { get; set; }
    }

    public class Storage
    {
        /// <summary>
        /// Enable cache mount
        /// </summary>
        public bool mount_cache { get; set; }

        /// <summary>
        /// Enable scratch mount
        /// </summary>
        public bool mount_scratch { get; set; }
    }

    public class Ui
    {
        /// <summary>
        /// Show achievement notification on screen
        /// </summary>
        public bool show_achievement_notification { get; set; }
    }

    public class Video
    {
        /// <summary>
        /// Video modes
        /// <para>0 = PAL-60 Component (SD)</para>
        /// 1 = Unused
        /// <para>2 = PAL-60 SCART</para>
        /// 3 = 480p Component (HD)
        /// <para>4 = HDMI+A</para>
        /// 5 = PAL-60 Composite/S-Video
        /// <para>6 = VGA</para>
        /// 7 = TV PAL-60
        /// <para>8 = HDMI (default)</para>
        /// </summary>
        public int avpack { get; set; }

        /// <summary>
        /// Allow game that support different resolutions to be rendered in specific resolution
        /// <para>0=640x480</para>
        /// 1=640x576
        /// <para>2=720x480</para>
        /// 3=720x576
        /// <para>4=800x600</para>
        /// 5=848x480
        /// <para>6=1024x768</para>
        /// 7=1152x864
        /// <para>8=1280x720</para>
        /// 9=1280x768
        /// <para>10=1280x960</para>
        /// 11=1280x1024
        /// <para>12=1360x768</para>
        /// 13=1440x900
        /// <para>14=1680x1050</para>
        /// 15=1920x540
        /// <para>16=1920x1080</para>
        /// </summary>
        public int internal_display_resolution { get; set; }

        /// <summary>
        /// Enables usage of PAL-50 mode
        /// </summary>
        public bool use_50Hz_mode { get; set; }

        /// <summary>
        /// Enables switching between different video signals
        /// <para>1=NTSC</para>
        /// 2=NTSC-
        /// <para>3=PAL</para>
        /// </summary>
        public int video_standard { get; set; }
    }
}
