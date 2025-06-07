using System.Collections.ObjectModel;
using XeniaManager.Core.Game;
using XeniaManager.Core.GPU.NVIDIA;

namespace XeniaManager.Desktop.ViewModel.Pages;

public class XeniaSettingsViewModel
{
    public Dictionary<string, string> AudioSystems { get; } = new Dictionary<string, string>
    {
        { "Any", "any" },
        { "Nop", "nop" },
        { "SDL", "sdl" },
        { "XAudio2", "xaudio2" }
    };

    public ObservableCollection<string> InternalDisplayResolutions { get; } =
    [
        "640x480", "640x576", "720x480", "720x576",
        "800x600", "848x480", "1024x768", "1152x864",
        "1280x720", "1280x768", "1280x960", "1280x1024",
        "1360x768", "1440x900", "1680x1050",
        "1920x540", "1920x1080", "Custom"
    ];

    public Dictionary<string, NVAPI_VSYNC_MODE> NvidiaVerticalSync { get; } = new Dictionary<string, NVAPI_VSYNC_MODE>
    {
        { "Default", NVAPI_VSYNC_MODE.DEFAULT },
        { "Force Off", NVAPI_VSYNC_MODE.FORCE_OFF },
        { "Force On", NVAPI_VSYNC_MODE.FORCE_ON },
        { "1/2 Refresh Rate", NVAPI_VSYNC_MODE.HALF_REFRESH_RATE },
        { "1/3 Refresh Rate", NVAPI_VSYNC_MODE.THIRD_REFRESH_RATE },
        { "1/4 Refresh Rate", NVAPI_VSYNC_MODE.QUARTER_REFRESH_RATE },
        { "Adaptive", NVAPI_VSYNC_MODE.ADAPTIVE },
    };

    public Dictionary<string, string> GraphicsApis { get; } = new Dictionary<string, string>
    {
        { "Any", "any" },
        { "D3D12", "d3d12" },
        { "Vulkan", "vulkan" }
    };

    public Dictionary<string, string> D3D12RenderTargetPaths { get; } = new Dictionary<string, string>
    {
        { "Auto", "" },
        { "RTV", "rtv" },
        { "ROV", "rov" }
    };

    public ObservableCollection<string> D3D12QueuePriorities { get; } =
    [
        "Normal", "High", "Realtime"
    ];

    public Dictionary<string, string> VulkanRenderTargetPaths { get; } = new Dictionary<string, string>
    {
        { "Auto", "" },
        { "FBO", "fbo" },
        { "FSI", "fsi" }
    };

    public Dictionary<string, string> PostprocessAntialiasing { get; } = new Dictionary<string, string>
    {
        { "None", "" },
        { "FXAA", "fxaa" },
        { "FXAA Extreme", "fxaa_extreme" }
    };

    public Dictionary<string, string> ScalingSharpening { get; } = new Dictionary<string, string>
    {
        { "Bilinear", "bilinear" },
        { "FidelityFX CAS", "cas" },
        { "AMD FSR", "fsr" }
    };

    public Dictionary<string, int> LicenseMasks { get; } = new Dictionary<string, int>
    {
        { "No Licenses", 0 },
        { "First License", 1 },
        { "All Licenses", -1 }
    };

    public Dictionary<string, int> Countries { get; } = new Dictionary<string, int>
    {
        { "United Arab Emirates", 1 }, { "Albania", 2 }, { "Armenia", 3 }, { "Argentina", 4 }, { "Austria", 5 },
        { "Australia", 6 }, { "Azerbaijan", 7 }, { "Belgium", 8 }, { "Bulgaria", 9 },
        { "Bahrain", 10 }, { "Brunei", 11 }, { "Bolivia", 12 }, { "Brazil", 13 }, { "Belarus", 14 },
        { "Belize", 15 }, { "Canada", 16 }, { "Switzerland", 18 }, { "Chile", 19 },
        { "China", 20 }, { "Colombia", 21 }, { "Costa Rica", 22 }, { "Czech Republic", 23 }, { "Germany", 24 },
        { "Denmark", 25 }, { "Dominican Republic", 26 }, { "Algeria", 27 }, { "Ecuador", 28 },
        { "Estonia", 29 }, { "Egypt", 30 }, { "Spain", 31 }, { "Finland", 32 }, { "Faroe Islands", 33 },
        { "France", 34 }, { "United Kingdom", 35 }, { "Georgia", 36 }, { "Greece", 37 },
        { "Guatemala", 38 }, { "Hong Kong", 39 }, { "Honduras", 40 }, { "Croatia", 41 }, { "Hungary", 42 },
        { "Indonesia", 43 }, { "Ireland", 44 }, { "Israel", 45 }, { "India", 46 },
        { "Iraq", 47 }, { "Iran", 48 }, { "Iceland", 49 }, { "Italy", 50 }, { "Jamaica", 51 },
        { "Jordan", 52 }, { "Japan", 53 }, { "Kenya", 54 }, { "Kyrgyzstan", 55 },
        { "South Korea", 56 }, { "Kuwait", 57 }, { "Kazakhstan", 58 }, { "Lebanon", 59 }, { "Liechtenstein", 60 },
        { "Lithuania", 61 }, { "Luxembourg", 62 }, { "Latvia", 63 }, { "Libya", 64 },
        { "Morocco", 65 }, { "Monaco", 66 }, { "North Macedonia", 67 }, { "Mongolia", 68 }, { "Macau", 69 },
        { "Maldives", 70 }, { "Mexico", 71 }, { "Malaysia", 72 }, { "Nicaragua", 73 },
        { "Netherlands", 74 }, { "Norway", 75 }, { "New Zealand", 76 }, { "Oman", 77 }, { "Panama", 78 },
        { "Peru", 79 }, { "Philippines", 80 }, { "Pakistan", 81 }, { "Poland", 82 },
        { "Puerto Rico", 83 }, { "Portugal", 84 }, { "Paraguay", 85 }, { "Qatar", 86 }, { "Romania", 87 },
        { "Russia", 88 }, { "Saudi Arabia", 89 }, { "Sweden", 90 }, { "Singapore", 91 },
        { "Slovenia", 92 }, { "Slovakia", 93 }, { "El Salvador", 95 }, { "Syria", 96 }, { "Thailand", 97 },
        { "Tunisia", 98 }, { "Turkey", 99 }, { "Trinidad and Tobago", 100 }, { "Taiwan", 101 },
        { "Ukraine", 102 }, { "United States", 103 }, { "Uruguay", 104 }, { "Uzbekistan", 105 },
        { "Venezuela", 106 }, { "Vietnam", 107 }, { "Yemen", 108 }, { "South Africa", 109 }
    };

    public Dictionary<string, int> Languages { get; } = new Dictionary<string, int>
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

    public Dictionary<string, string> InputSystems { get; } = new Dictionary<string, string>
    {
        { "Any", "any" },
        { "SDL2", "sdl" },
        { "Winkey", "winkey" },
        { "XInput", "xinput" }
    };

    public ObservableCollection<string> KeyboardModes { get; } =
    [
        "Disabled", "Enabled", "Passthrough"
    ];

    public ObservableCollection<int> KeyboardUserIndexes { get; } =
    [
        0, 1, 2, 3
    ];

    public ObservableCollection<string> ConfigurationFiles { get; } = new ObservableCollection<string>();

    public XeniaSettingsViewModel()
    {
        LoadConfigurationFiles();
    }

    private void LoadConfigurationFiles()
    {
        ConfigurationFiles.Clear();
        foreach (Game game in GameManager.Games)
        {
            ConfigurationFiles.Add(game.Title);
        }
        if (App.Settings.Emulator.Canary != null)
        {
            ConfigurationFiles.Add("Default Xenia Canary");
        }
        if (App.Settings.Emulator.Netplay != null)
        {
            ConfigurationFiles.Add("Default Xenia Netplay");
        }
        if (App.Settings.Emulator.Mousehook != null)
        {
            ConfigurationFiles.Add("Default Xenia Mousehook");
        }
    }
}