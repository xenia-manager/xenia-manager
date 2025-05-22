using System.Collections.ObjectModel;
using XeniaManager.Core.Game;

namespace XeniaManager.Desktop.ViewModel.Pages;

public class XeniaSettingsViewModel
{
    public Dictionary<string, string> AudioSystems { get; } = new Dictionary<string, string>
    {
        { "Any", "any" },
        { "Nop", "nop"},
        { "SDL", "sdl"},
        { "XAudio2", "xaudio2"}
    };

    public ObservableCollection<string> InternalDisplayResolutions { get; } =
    [
        "640x480", "640x576", "720x480", "720x576",
        "800x600", "848x480", "1024x768", "1152x864",
        "1280x720", "1280x768", "1280x960", "1280x1024",
        "1360x768", "1440x900", "1680x1050",
        "1920x540", "1920x1080", "Custom"
    ];

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

    public ObservableCollection<string> LicenseMasks { get; } =
    [
        "No Licenses", "First License", "All Licenses"
    ];

    public Dictionary<int, string> Countries { get; } = new Dictionary<int, string>
    {
        { 1, "United Arab Emirates" }, { 2, "Albania" }, { 3, "Armenia" }, { 4, "Argentina" }, { 5, "Austria" },
        { 6, "Australia" }, { 7, "Azerbaijan" }, { 8, "Belgium" }, { 9, "Bulgaria" },
        { 10, "Bahrain" }, { 11, "Brunei" }, { 12, "Bolivia" }, { 13, "Brazil" }, { 14, "Belarus" },
        { 15, "Belize" }, { 16, "Canada" }, { 18, "Switzerland" }, { 19, "Chile" },
        { 20, "China" }, { 21, "Colombia" }, { 22, "Costa Rica" }, { 23, "Czech Republic" }, { 24, "Germany" },
        { 25, "Denmark" }, { 26, "Dominican Republic" }, { 27, "Algeria" }, { 28, "Ecuador" },
        { 29, "Estonia" }, { 30, "Egypt" }, { 31, "Spain" }, { 32, "Finland" }, { 33, "Faroe Islands" },
        { 34, "France" }, { 35, "United Kingdom" }, { 36, "Georgia" }, { 37, "Greece" },
        { 38, "Guatemala" }, { 39, "Hong Kong" }, { 40, "Honduras" }, { 41, "Croatia" }, { 42, "Hungary" },
        { 43, "Indonesia" }, { 44, "Ireland" }, { 45, "Israel" }, { 46, "India" },
        { 47, "Iraq" }, { 48, "Iran" }, { 49, "Iceland" }, { 50, "Italy" }, { 51, "Jamaica" },
        { 52, "Jordan" }, { 53, "Japan" }, { 54, "Kenya" }, { 55, "Kyrgyzstan" },
        { 56, "South Korea" }, { 57, "Kuwait" }, { 58, "Kazakhstan" }, { 59, "Lebanon" }, { 60, "Liechtenstein" },
        { 61, "Lithuania" }, { 62, "Luxembourg" }, { 63, "Latvia" }, { 64, "Libya" },
        { 65, "Morocco" }, { 66, "Monaco" }, { 67, "North Macedonia" }, { 68, "Mongolia" }, { 69, "Macau" },
        { 70, "Maldives" }, { 71, "Mexico" }, { 72, "Malaysia" }, { 73, "Nicaragua" },
        { 74, "Netherlands" }, { 75, "Norway" }, { 76, "New Zealand" }, { 77, "Oman" }, { 78, "Panama" },
        { 79, "Peru" }, { 80, "Philippines" }, { 81, "Pakistan" }, { 82, "Poland" },
        { 83, "Puerto Rico" }, { 84, "Portugal" }, { 85, "Paraguay" }, { 86, "Qatar" }, { 87, "Romania" },
        { 88, "Russia" }, { 89, "Saudi Arabia" }, { 90, "Sweden" }, { 91, "Singapore" },
        { 92, "Slovenia" }, { 93, "Slovakia" }, { 95, "El Salvador" }, { 96, "Syria" }, { 97, "Thailand" },
        { 98, "Tunisia" }, { 99, "Turkey" }, { 100, "Trinidad and Tobago" }, { 101, "Taiwan" },
        { 102, "Ukraine" }, { 103, "United States" }, { 104, "Uruguay" }, { 105, "Uzbekistan" },
        { 106, "Venezuela" }, { 107, "Vietnam" }, { 108, "Yemen" }, { 109, "South Africa" }
    };

    public Dictionary<int, string> Languages { get; } = new Dictionary<int, string>
    {
        { 1, "English" },
        { 2, "Japanese/日本語" },
        { 3, "Deutsche" },
        { 4, "Français" },
        { 5, "Español" },
        { 6, "Italiano" },
        { 7, "한국어" },
        { 8, "繁體中文" },
        { 9, "Português" },
        { 11, "Polski" },
        { 12, "русский" },
        { 13, "Svenska" },
        { 14, "Türk" },
        { 15, "Norsk" },
        { 16, "Nederlands" },
        { 17, "简体中文" }
    };

    public ObservableCollection<string> InputSystems { get; } =
    [
        "Any", "SDL2", "XInput", "Keyboard"
    ];

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