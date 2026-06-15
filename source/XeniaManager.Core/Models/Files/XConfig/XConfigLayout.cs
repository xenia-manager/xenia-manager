namespace XeniaManager.Core.Models.Files.XConfig;

/// <summary>
/// Byte offsets and sizes for the XConfigData binary blob (6680 bytes).
/// </summary>
public static class XConfigOffsets
{
    public const int Static = 0x0000;
    public const int Statistic = 0x010E;
    public const int Secured = 0x06E6;
    public const int User = 0x08E6;
    public const int XnetMachineAccount = 0x0AE3;
    public const int XnetParameters = 0x0CD3;
    public const int MediaCenter = 0x0CE0;
    public const int Console = 0x142C;
    public const int Dvd = 0x1570;
    public const int Iptv = 0x1808;
    public const int System = 0x1A08;
    public const int TotalSize = 0x1A18;

    public static int CategoryOffset(XConfigCategory category) => category switch
    {
        XConfigCategory.Static => Static,
        XConfigCategory.Statistic => Statistic,
        XConfigCategory.Secured => Secured,
        XConfigCategory.User => User,
        XConfigCategory.XnetMachineAccount => XnetMachineAccount,
        XConfigCategory.XnetParameters => XnetParameters,
        XConfigCategory.MediaCenter => MediaCenter,
        XConfigCategory.Console => Console,
        XConfigCategory.Dvd => Dvd,
        XConfigCategory.Iptv => Iptv,
        XConfigCategory.System => System,
        _ => 0,
    };

    public static int CategorySize(XConfigCategory category) => category switch
    {
        XConfigCategory.Static => 270,
        XConfigCategory.Statistic => 1496,
        XConfigCategory.Secured => 512,
        XConfigCategory.User => 509,
        XConfigCategory.XnetMachineAccount => 496,
        XConfigCategory.XnetParameters => 13,
        XConfigCategory.MediaCenter => 1868,
        XConfigCategory.Console => 324,
        XConfigCategory.Dvd => 664,
        XConfigCategory.Iptv => 512,
        XConfigCategory.System => 16,
        _ => 0,
    };
}

/// <summary>
/// Describes a single registered field in the XConfig blob.
/// </summary>
/// <param name="Category">The category this field belongs to.</param>
/// <param name="SettingId">The setting identifier within the category.</param>
/// <param name="Size">Size of the field in bytes.</param>
/// <param name="AbsoluteOffset">Offset from the start of the XConfigData byte array.</param>
public readonly record struct FieldDescriptor(
    XConfigCategory Category,
    ushort SettingId,
    ushort Size,
    int AbsoluteOffset
);

/// <summary>
/// Registry of all XConfig fields that can be accessed via ReadSetting/WriteSetting.
/// Matches the C++ kFields[] array from xconfig.cc.
/// </summary>
public static class XConfigFields
{
    private static readonly Dictionary<(XConfigCategory, ushort), FieldDescriptor> Fields = Build();

    public static FieldDescriptor? Find(XConfigCategory category, ushort settingId)
    {
        if (Fields.TryGetValue((category, settingId), out FieldDescriptor desc))
        {
            return desc;
        }
        return null;
    }

    private static int CatOff(XConfigCategory cat) => XConfigOffsets.CategoryOffset(cat);

    private static Dictionary<(XConfigCategory, ushort), FieldDescriptor> Build()
    {
        Dictionary<(XConfigCategory, ushort), FieldDescriptor> map = new Dictionary<(XConfigCategory, ushort), FieldDescriptor>();

        // Static (0x0)
        Add(map, XConfigCategory.Static, (ushort)XConfigStaticSetting.FirstPowerOnDate, 5, CatOff(XConfigCategory.Static) + 0x008);

        // Secured (0x2)
        Add(map, XConfigCategory.Secured, (ushort)XConfigSecuredSetting.OnlineNetworkId, 4, CatOff(XConfigCategory.Secured) + 0x08);
        Add(map, XConfigCategory.Secured, (ushort)XConfigSecuredSetting.MacAddress, 6, CatOff(XConfigCategory.Secured) + 0x20);
        Add(map, XConfigCategory.Secured, (ushort)XConfigSecuredSetting.AvRegion, 4, CatOff(XConfigCategory.Secured) + 0x28);
        Add(map, XConfigCategory.Secured, (ushort)XConfigSecuredSetting.GameRegion, 2, CatOff(XConfigCategory.Secured) + 0x2C);
        Add(map, XConfigCategory.Secured, (ushort)XConfigSecuredSetting.DvdRegion, 4, CatOff(XConfigCategory.Secured) + 0x34);
        Add(map, XConfigCategory.Secured, (ushort)XConfigSecuredSetting.ResetKey, 4, CatOff(XConfigCategory.Secured) + 0x38);
        Add(map, XConfigCategory.Secured, (ushort)XConfigSecuredSetting.SystemFlags, 4, CatOff(XConfigCategory.Secured) + 0x3C);
        Add(map, XConfigCategory.Secured, (ushort)XConfigSecuredSetting.PowerMode, 2, CatOff(XConfigCategory.Secured) + 0x40);
        Add(map, XConfigCategory.Secured, (ushort)XConfigSecuredSetting.PowerVcsControl, 2, CatOff(XConfigCategory.Secured) + 0x42);

        // User (0x3)
        Add(map, XConfigCategory.User, (ushort)XConfigUserSetting.TimeZoneBias, 4, CatOff(XConfigCategory.User) + 0x008);
        Add(map, XConfigCategory.User, (ushort)XConfigUserSetting.TimeZoneStdName, 4, CatOff(XConfigCategory.User) + 0x00C);
        Add(map, XConfigCategory.User, (ushort)XConfigUserSetting.TimeZoneDltName, 4, CatOff(XConfigCategory.User) + 0x010);
        Add(map, XConfigCategory.User, (ushort)XConfigUserSetting.TimeZoneStdDate, 4, CatOff(XConfigCategory.User) + 0x014);
        Add(map, XConfigCategory.User, (ushort)XConfigUserSetting.TimeZoneDltDate, 4, CatOff(XConfigCategory.User) + 0x018);
        Add(map, XConfigCategory.User, (ushort)XConfigUserSetting.TimeZoneStdBias, 4, CatOff(XConfigCategory.User) + 0x01C);
        Add(map, XConfigCategory.User, (ushort)XConfigUserSetting.TimeZoneDltBias, 4, CatOff(XConfigCategory.User) + 0x020);
        Add(map, XConfigCategory.User, (ushort)XConfigUserSetting.DefaultProfile, 8, CatOff(XConfigCategory.User) + 0x024);
        Add(map, XConfigCategory.User, (ushort)XConfigUserSetting.Language, 4, CatOff(XConfigCategory.User) + 0x02C);
        Add(map, XConfigCategory.User, (ushort)XConfigUserSetting.VideoFlags, 4, CatOff(XConfigCategory.User) + 0x030);
        Add(map, XConfigCategory.User, (ushort)XConfigUserSetting.AudioFlags, 4, CatOff(XConfigCategory.User) + 0x034);
        Add(map, XConfigCategory.User, (ushort)XConfigUserSetting.RetailFlags, 4, CatOff(XConfigCategory.User) + 0x038);
        Add(map, XConfigCategory.User, (ushort)XConfigUserSetting.DevkitFlags, 4, CatOff(XConfigCategory.User) + 0x03C);
        Add(map, XConfigCategory.User, (ushort)XConfigUserSetting.Country, 1, CatOff(XConfigCategory.User) + 0x040);
        Add(map, XConfigCategory.User, (ushort)XConfigUserSetting.PcFlags, 1, CatOff(XConfigCategory.User) + 0x041);
        Add(map, XConfigCategory.User, (ushort)XConfigUserSetting.SmbConfig, 256, CatOff(XConfigCategory.User) + 0x044);
        Add(map, XConfigCategory.User, (ushort)XConfigUserSetting.LivePuid, 8, CatOff(XConfigCategory.User) + 0x144);
        Add(map, XConfigCategory.User, (ushort)XConfigUserSetting.LiveCredentials, 16, CatOff(XConfigCategory.User) + 0x14C);
        Add(map, XConfigCategory.User, (ushort)XConfigUserSetting.AvCompositeScreenSz, 4, CatOff(XConfigCategory.User) + 0x15C);
        Add(map, XConfigCategory.User, (ushort)XConfigUserSetting.AvComponentScreenSz, 4, CatOff(XConfigCategory.User) + 0x160);
        Add(map, XConfigCategory.User, (ushort)XConfigUserSetting.AvVgaScreenSz, 4, CatOff(XConfigCategory.User) + 0x164);
        Add(map, XConfigCategory.User, (ushort)XConfigUserSetting.PcGame, 4, CatOff(XConfigCategory.User) + 0x168);
        Add(map, XConfigCategory.User, (ushort)XConfigUserSetting.PcPassword, 4, CatOff(XConfigCategory.User) + 0x16C);
        Add(map, XConfigCategory.User, (ushort)XConfigUserSetting.PcMovie, 4, CatOff(XConfigCategory.User) + 0x170);
        Add(map, XConfigCategory.User, (ushort)XConfigUserSetting.PcGameRating, 4, CatOff(XConfigCategory.User) + 0x174);
        Add(map, XConfigCategory.User, (ushort)XConfigUserSetting.PcMovieRating, 4, CatOff(XConfigCategory.User) + 0x178);
        Add(map, XConfigCategory.User, (ushort)XConfigUserSetting.PcHint, 1, CatOff(XConfigCategory.User) + 0x17C);
        Add(map, XConfigCategory.User, (ushort)XConfigUserSetting.PcHintAnswer, 32, CatOff(XConfigCategory.User) + 0x17D);
        Add(map, XConfigCategory.User, (ushort)XConfigUserSetting.PcOverride, 32, CatOff(XConfigCategory.User) + 0x19D);
        Add(map, XConfigCategory.User, (ushort)XConfigUserSetting.MusicPlaybackMode, 4, CatOff(XConfigCategory.User) + 0x1BD);
        Add(map, XConfigCategory.User, (ushort)XConfigUserSetting.MusicVolume, 4, CatOff(XConfigCategory.User) + 0x1C1);
        Add(map, XConfigCategory.User, (ushort)XConfigUserSetting.MusicFlags, 4, CatOff(XConfigCategory.User) + 0x1C5);
        Add(map, XConfigCategory.User, (ushort)XConfigUserSetting.ArcadeFlags, 4, CatOff(XConfigCategory.User) + 0x1C9);
        Add(map, XConfigCategory.User, (ushort)XConfigUserSetting.PcVersion, 4, CatOff(XConfigCategory.User) + 0x1CD);
        Add(map, XConfigCategory.User, (ushort)XConfigUserSetting.PcTv, 4, CatOff(XConfigCategory.User) + 0x1D1);
        Add(map, XConfigCategory.User, (ushort)XConfigUserSetting.PcTvRating, 4, CatOff(XConfigCategory.User) + 0x1D5);
        Add(map, XConfigCategory.User, (ushort)XConfigUserSetting.PcExplicitVideo, 4, CatOff(XConfigCategory.User) + 0x1D9);
        Add(map, XConfigCategory.User, (ushort)XConfigUserSetting.PcExplicitVideoRating, 4, CatOff(XConfigCategory.User) + 0x1DD);
        Add(map, XConfigCategory.User, (ushort)XConfigUserSetting.PcUnratedVideo, 4, CatOff(XConfigCategory.User) + 0x1E1);
        Add(map, XConfigCategory.User, (ushort)XConfigUserSetting.PcUnratedVideoRating, 4, CatOff(XConfigCategory.User) + 0x1E5);
        Add(map, XConfigCategory.User, (ushort)XConfigUserSetting.VideoOutputBlackLevels, 4, CatOff(XConfigCategory.User) + 0x1E9);
        Add(map, XConfigCategory.User, (ushort)XConfigUserSetting.VideoPlayerDisplayMode, 1, CatOff(XConfigCategory.User) + 0x1ED);
        Add(map, XConfigCategory.User, (ushort)XConfigUserSetting.AlternateVideoTimingId, 4, CatOff(XConfigCategory.User) + 0x1EE);
        Add(map, XConfigCategory.User, (ushort)XConfigUserSetting.VideoDriverOptions, 4, CatOff(XConfigCategory.User) + 0x1F2);
        Add(map, XConfigCategory.User, (ushort)XConfigUserSetting.MusicUiFlags, 4, CatOff(XConfigCategory.User) + 0x1F6);
        Add(map, XConfigCategory.User, (ushort)XConfigUserSetting.VideoMediaSourceType, 1, CatOff(XConfigCategory.User) + 0x1FA);
        Add(map, XConfigCategory.User, (ushort)XConfigUserSetting.MusicMediaSourceType, 1, CatOff(XConfigCategory.User) + 0x1FB);
        Add(map, XConfigCategory.User, (ushort)XConfigUserSetting.PhotoMediaSourceType, 1, CatOff(XConfigCategory.User) + 0x1FC);

        // Console (0x7)
        Add(map, XConfigCategory.Console, (ushort)XConfigConsoleSetting.ScreenSaver, 2, CatOff(XConfigCategory.Console) + 0x008);
        Add(map, XConfigCategory.Console, (ushort)XConfigConsoleSetting.AutoShutOff, 2, CatOff(XConfigCategory.Console) + 0x00A);
        Add(map, XConfigCategory.Console, (ushort)XConfigConsoleSetting.WirelessSettings, 256, CatOff(XConfigCategory.Console) + 0x00C);
        Add(map, XConfigCategory.Console, (ushort)XConfigConsoleSetting.CameraSettings, 4, CatOff(XConfigCategory.Console) + 0x10C);
        Add(map, XConfigCategory.Console, (ushort)XConfigConsoleSetting.PlayTimerData, 20, CatOff(XConfigCategory.Console) + 0x12C);
        Add(map, XConfigCategory.Console, (ushort)XConfigConsoleSetting.MediaDisableAutoLaunch, 2, CatOff(XConfigCategory.Console) + 0x140);
        Add(map, XConfigCategory.Console, (ushort)XConfigConsoleSetting.KeyboardLayout, 2, CatOff(XConfigCategory.Console) + 0x142);

        // IPTV (0x9)
        Add(map, XConfigCategory.Iptv, (ushort)XConfigIptvSetting.ServiceProviderName, 120, CatOff(XConfigCategory.Iptv) + 0x008);

        // System (0xA)
        Add(map, XConfigCategory.System, (ushort)XConfigSystemSetting.AlarmTime, 8, CatOff(XConfigCategory.System) + 0x04);
        Add(map, XConfigCategory.System, (ushort)XConfigSystemSetting.PreviousFlashVersion, 4, CatOff(XConfigCategory.System) + 0x0C);

        return map;
    }

    private static void Add(Dictionary<(XConfigCategory, ushort), FieldDescriptor> map, XConfigCategory category,
        ushort settingId, int size, int absoluteOffset)
    {
        map[(category, settingId)] = new FieldDescriptor(category, settingId, (ushort)size, absoluteOffset);
    }
}