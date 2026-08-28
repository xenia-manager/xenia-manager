namespace XeniaManager.Core.Models.Files.XConfig;

/// <summary>
/// Category identifiers for Xbox 360 XConfig settings storage.
/// Each category is a fixed-layout sub-struct within the 6680-byte XConfigData blob.
/// </summary>
public enum XConfigCategory : ushort
{
    Static = 0x00,
    Statistic = 0x01,
    Secured = 0x02,
    User = 0x03,
    XnetMachineAccount = 0x04,
    XnetParameters = 0x05,
    MediaCenter = 0x06,
    Console = 0x07,
    Dvd = 0x08,
    Iptv = 0x09,
    System = 0x0A,
    Devkit = 0x0B,
}

/// <summary>
/// Setting IDs for the Static category (0x0).
/// </summary>
public enum XConfigStaticSetting : byte
{
    FirstPowerOnDate = 0x01
}

/// <summary>
/// Setting IDs for the Secured category (0x2).
/// </summary>
public enum XConfigSecuredSetting : byte
{
    OnlineNetworkId = 0x08,
    MacAddress = 0x01,
    AvRegion = 0x02,
    GameRegion = 0x03,
    DvdRegion = 0x04,
    ResetKey = 0x05,
    SystemFlags = 0x06,
    PowerMode = 0x07,
    PowerVcsControl = 0x09
}

/// <summary>
/// Setting IDs for the User category (0x3).
/// </summary>
public enum XConfigUserSetting : byte
{
    TimeZoneBias = 0x01,
    TimeZoneStdName = 0x02,
    TimeZoneDltName = 0x03,
    TimeZoneStdDate = 0x04,
    TimeZoneDltDate = 0x05,
    TimeZoneStdBias = 0x06,
    TimeZoneDltBias = 0x07,
    DefaultProfile = 0x08,
    Language = 0x09,
    VideoFlags = 0x0A,
    AudioFlags = 0x0B,
    RetailFlags = 0x0C,
    DevkitFlags = 0x0D,
    Country = 0x0E,
    PcFlags = 0x0F,
    SmbConfig = 0x10,
    LivePuid = 0x11,
    LiveCredentials = 0x12,
    AvCompositeScreenSz = 0x13,
    AvComponentScreenSz = 0x14,
    AvVgaScreenSz = 0x15,
    PcGame = 0x16,
    PcPassword = 0x17,
    PcMovie = 0x18,
    PcGameRating = 0x19,
    PcMovieRating = 0x1A,
    PcHint = 0x1B,
    PcHintAnswer = 0x1C,
    PcOverride = 0x1D,
    MusicPlaybackMode = 0x1E,
    MusicVolume = 0x1F,
    MusicFlags = 0x20,
    ArcadeFlags = 0x21,
    PcVersion = 0x22,
    PcTv = 0x23,
    PcTvRating = 0x24,
    PcExplicitVideo = 0x25,
    PcExplicitVideoRating = 0x26,
    PcUnratedVideo = 0x27,
    PcUnratedVideoRating = 0x28,
    VideoOutputBlackLevels = 0x29,
    VideoPlayerDisplayMode = 0x2A,
    AlternateVideoTimingId = 0x2B,
    VideoDriverOptions = 0x2C,
    MusicUiFlags = 0x2D,
    VideoMediaSourceType = 0x2E,
    MusicMediaSourceType = 0x2F,
    PhotoMediaSourceType = 0x30
}

/// <summary>
/// Setting IDs for the Console category (0x7).
/// </summary>
public enum XConfigConsoleSetting : byte
{
    ScreenSaver = 0x01,
    AutoShutOff = 0x02,
    WirelessSettings = 0x03,
    CameraSettings = 0x04,
    PlayTimerData = 0x05,
    MediaDisableAutoLaunch = 0x06,
    KeyboardLayout = 0x07
}

/// <summary>
/// Setting IDs for the IPTV category (0x9).
/// </summary>
public enum XConfigIptvSetting : byte
{
    ServiceProviderName = 0x01
}

/// <summary>
/// Setting IDs for the System category (0xA).
/// </summary>
public enum XConfigSystemSetting : byte
{
    AlarmTime = 0x01,
    PreviousFlashVersion = 0x02
}