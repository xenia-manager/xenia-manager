namespace XeniaManager.Core.Models.Files.XConfig;

/// <summary>
/// Xbox 360 dashboard language IDs (XLanguage).
/// </summary>
public enum XLanguage : uint
{
    Invalid = 0,
    English = 1,
    Japanese = 2,
    German = 3,
    French = 4,
    Spanish = 5,
    Italian = 6,
    Korean = 7,
    TChinese = 8,
    Portuguese = 9,
    Polish = 11,
    Russian = 12,
    Swedish = 13,
    Turkish = 14,
    Norwegian = 15,
    Dutch = 16,
    SChinese = 17
}

/// <summary>
/// Xbox 360 online country codes (XOnlineCountry).
/// </summary>
public enum XOnlineCountry : byte
{
    UnitedArabEmirates = 1,
    Albania = 2,
    Armenia = 3,
    Argentina = 4,
    Austria = 5,
    Australia = 6,
    Azerbaijan = 7,
    Belgium = 8,
    Bulgaria = 9,
    Bahrain = 10,
    BruneiDarussalam = 11,
    Bolivia = 12,
    Brazil = 13,
    Belarus = 14,
    Belize = 15,
    Canada = 16,
    Switzerland = 18,
    Chile = 19,
    China = 20,
    Colombia = 21,
    CostaRica = 22,
    CzechRepublic = 23,
    Germany = 24,
    Denmark = 25,
    DominicanRepublic = 26,
    Algeria = 27,
    Ecuador = 28,
    Estonia = 29,
    Egypt = 30,
    Spain = 31,
    Finland = 32,
    FaroeIslands = 33,
    France = 34,
    GreatBritain = 35,
    Georgia = 36,
    Greece = 37,
    Guatemala = 38,
    HongKong = 39,
    Honduras = 40,
    Croatia = 41,
    Hungary = 42,
    Indonesia = 43,
    Ireland = 44,
    Israel = 45,
    India = 46,
    Iraq = 47,
    Iran = 48,
    Iceland = 49,
    Italy = 50,
    Jamaica = 51,
    Jordan = 52,
    Japan = 53,
    Kenya = 54,
    Kyrgyzstan = 55,
    Korea = 56,
    Kuwait = 57,
    Kazakhstan = 58,
    Lebanon = 59,
    Liechtenstein = 60,
    Lithuania = 61,
    Luxembourg = 62,
    Latvia = 63,
    Libya = 64,
    Morocco = 65,
    Monaco = 66,
    Macedonia = 67,
    Mongolia = 68,
    Macau = 69,
    Maldives = 70,
    Mexico = 71,
    Malaysia = 72,
    Nicaragua = 73,
    Netherlands = 74,
    Norway = 75,
    NewZealand = 76,
    Oman = 77,
    Panama = 78,
    Peru = 79,
    Philippines = 80,
    Pakistan = 81,
    Poland = 82,
    PuertoRico = 83,
    Portugal = 84,
    Paraguay = 85,
    Qatar = 86,
    Romania = 87,
    RussianFederation = 88,
    SaudiArabia = 89,
    Sweden = 90,
    Singapore = 91,
    Slovenia = 92,
    SlovakRepublic = 93,
    ElSalvador = 95,
    Syria = 96,
    Thailand = 97,
    Tunisia = 98,
    Turkey = 99,
    TrinidadAndTobago = 100,
    Taiwan = 101,
    Ukraine = 102,
    UnitedStates = 103,
    Uruguay = 104,
    Uzbekistan = 105,
    Venezuela = 106,
    VietNam = 107,
    Yemen = 108,
    SouthAfrica = 109,
    Zimbabwe = 110
}

/// <summary>
/// XConfig AV region bitmask (X_AV_REGION).
/// </summary>
public enum XAvRegion : uint
{
    NtscM = 0x00400100,
    NtscJ = 0x00400200,
    Pal = 0x00400400,
    Pal50 = 0x00800300
}

/// <summary>
/// XConfig video flags (X_VIDEO_FLAGS).
/// </summary>
[Flags]
public enum XVideoFlags : uint
{
    RatioNormal = 0x00000000,
    Widescreen = 0x00010000
}

/// <summary>
/// XConfig audio flags (X_AUDIO_FLAGS).
/// </summary>
[Flags]
public enum XAudioFlags : uint
{
    DigitalStereo = 0x00000000,
    DolbyProLogic = 0x00000001,
    AnalogMono = 0x00000002,
    StereoBypass = 0x00000003,
    DolbyDigital = 0x00010000,
    DolbyDigitalWithWmaPro = 0x00030000,
    LowLatency = 0x80000000
}

/// <summary>
/// XConfig retail flags (X_RETAIL_FLAGS).
/// </summary>
[Flags]
public enum XRetailFlags : uint
{
    DstOff = 0x00000002,
    TwentyFourHourClock = 0x00000008,
    DashboardInitialized = 0x00000040,
    DashboardStartup = 0x00000080,
    IptvStartup = 0x00000800,
    IptvEnabled = 0x00001000,
    DiscStartup = 0x00002000,
    BackgroundDownloadOn = 0x00010000,
    McxDownloaderStartup = 0x00020000,
    IptvDvrEnabled = 0x00080000,
    IptvDisabled = 0x02000000,
    KinectInitialized = 0x20000000,
    KinectDisabled = 0x80000000
}

/// <summary>
/// XConfig video output black level (X_BLACK_LEVEL).
/// </summary>
public enum XBlackLevel : uint
{
    High = 0x00000100,
    Intermediate = 0x00000200,
    Normal = 0x00000300
}

/// <summary>
/// XConfig screen saver setting (X_SCREENSAVER).
/// </summary>
public enum XScreenSaver : short
{
    On = 0x000A,
    Off = 0x1000
}

/// <summary>
/// XConfig auto shut-off setting (X_AUTO_SHUTDOWN).
/// </summary>
public enum XAutoShutOff : short
{
    Off = 0x0000,
    OneHour = 0x003C,
    SixHours = 0x0168
}

/// <summary>
/// XConfig keyboard layout (X_KEYBOARD_LAYOUT).
/// </summary>
public enum XKeyboardLayout : short
{
    Default = 0x0000,
    EnglishQwerty = 0x0001
}

/// <summary>
/// XConfig parental control flags (X_PC_FLAGS).
/// </summary>
[Flags]
public enum XPcFlags : byte
{
    XblAllowed = 0x01,
    XblMembershipCreationAllowed = 0x02,
    XboxOneGameAllowed = 0x04,
    PcEnabled = 0x80
}

/// <summary>
/// XConfig resolution packed as (width &lt;&lt; 16) | height, matching Xenia's Resolution struct.
/// </summary>
public enum XConfigResolution : int
{
    R640x480 = 0x028001E0,
    R640x576 = 0x02800240,
    R720x480 = 0x02D001E0,
    R720x576 = 0x02D00240,
    R800x600 = 0x03200258,
    R848x480 = 0x035001E0,
    R1024x768 = 0x04000300,
    R1152x864 = 0x04800360,
    R1280x720 = 0x050002D0,
    R1280x768 = 0x05000300,
    R1280x960 = 0x050003C0,
    R1280x1024 = 0x05000400,
    R1360x768 = 0x05500300,
    R1440x768 = 0x05A00300,
    R1440x900 = 0x05A00384,
    R1600x768 = 0x06400300,
    R1680x720 = 0x069002D0,
    R1680x1050 = 0x0690041A,
    R1920x540 = 0x0780021C,
    R1920x1080 = 0x07800438
}