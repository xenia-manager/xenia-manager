using System.Buffers.Binary;
using System.Runtime.InteropServices;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Models.Files.XConfig;

namespace XeniaManager.Core.Files;

/// <summary>
/// Handles reading, writing, and serialization of Xbox 360 XConfig settings.
/// XConfig is a 6680-byte binary blob containing dashboard/user/console settings,
/// stored persistently on the console's hard drive or flash memory.
/// </summary>
public class XConfigFile
{
    private readonly Lock _lock = new Lock();
    private byte[] _data;
    private string? _filePath;

    private static readonly int UserBase = XConfigOffsets.CategoryOffset(XConfigCategory.User);
    private static readonly int SecuredBase = XConfigOffsets.CategoryOffset(XConfigCategory.Secured);
    private static readonly int ConsoleBase = XConfigOffsets.CategoryOffset(XConfigCategory.Console);
    private static readonly int SystemBase = XConfigOffsets.CategoryOffset(XConfigCategory.System);

    private XConfigFile()
    {
        _data = new byte[XConfigOffsets.TotalSize];
    }

    private XConfigFile(byte[] data)
    {
        _data = data;
    }

    /// <summary>
    /// Gets the file path this XConfig was loaded from, or null if created in memory.
    /// </summary>
    public string? FilePath => _filePath;

    // User category properties
    /// <summary>
    /// Dashboard language setting (XLanguage). Offset: User + 0x02C (4 bytes).
    /// </summary>
    public XLanguage Language
    {
        get => (XLanguage)ReadUInt32(UserBase + 0x02C);
        set => WriteUInt32(UserBase + 0x02C, (uint)value);
    }

    /// <summary>
    /// Audio flags (XAudioFlags).
    /// <para>
    /// Offset: User + 0x034 (4 bytes).
    /// </para>
    /// </summary>
    public XAudioFlags AudioFlags
    {
        get => (XAudioFlags)ReadUInt32(UserBase + 0x034);
        set => WriteUInt32(UserBase + 0x034, (uint)value);
    }

    /// <summary>
    /// Video flags (XVideoFlags).
    /// <para>
    /// Offset: User + 0x030 (4 bytes).
    /// </para>
    /// </summary>
    public XVideoFlags VideoFlags
    {
        get => (XVideoFlags)ReadUInt32(UserBase + 0x030);
        set => WriteUInt32(UserBase + 0x030, (uint)value);
    }

    /// <summary>
    /// Online country code (XOnlineCountry).
    /// <para>
    /// Offset: User + 0x040 (1 byte).
    /// </para>
    /// </summary>
    public XOnlineCountry Country
    {
        get => (XOnlineCountry)ReadByte(UserBase + 0x040);
        set => WriteByte(UserBase + 0x040, (byte)value);
    }

    /// <summary>
    /// Music volume as a float ratio.
    /// <para>
    /// Offset: User + 0x1C1 (4 bytes).
    /// </para>
    /// </summary>
    public float MusicVolume
    {
        get => ReadSingle(UserBase + 0x1C1);
        set => WriteSingle(UserBase + 0x1C1, value);
    }

    /// <summary>
    /// Time zone bias in minutes from UTC.
    /// <para>
    /// Offset: User + 0x008 (4 bytes).
    /// </para>
    /// </summary>
    public int TimeZoneBias
    {
        get => ReadInt32(UserBase + 0x008);
        set => WriteInt32(UserBase + 0x008, value);
    }

    /// <summary>
    /// Daylight saving time bias in minutes.
    /// <para>
    /// Offset: User + 0x020 (4 bytes).
    /// </para>
    /// </summary>
    public int TimeZoneDltBias
    {
        get => ReadInt32(UserBase + 0x020);
        set => WriteInt32(UserBase + 0x020, value);
    }

    /// <summary>
    /// Retail flags bitmask (XRetailFlags).
    /// <para>
    /// Offset: User + 0x038 (4 bytes).
    /// </para>
    /// </summary>
    public XRetailFlags RetailFlags
    {
        get => (XRetailFlags)ReadUInt32(UserBase + 0x038);
        set => WriteUInt32(UserBase + 0x038, (uint)value);
    }

    /// <summary>
    /// Default profile XUID.
    /// <para>
    /// Offset: User + 0x024 (8 bytes).
    /// </para>
    /// </summary>
    public ulong DefaultProfile
    {
        get => ReadUInt64(UserBase + 0x024);
        set => WriteUInt64(UserBase + 0x024, value);
    }

    /// <summary>
    /// Video output black level (XBlackLevel).
    /// <para>
    /// Offset: User + 0x1E9 (4 bytes).
    /// </para>
    /// </summary>
    public XBlackLevel VideoOutputBlackLevels
    {
        get => (XBlackLevel)ReadUInt32(UserBase + 0x1E9);
        set => WriteUInt32(UserBase + 0x1E9, (uint)value);
    }

    // Resolution (packed (width << 16) | height)
    /// <summary>
    /// HDMI screen resolution as packed (width &lt;&lt; 16) | height.
    /// <para>
    /// Offset: User + 0x15C (4 bytes).
    /// </para>
    /// </summary>
    public XConfigResolution AvHdmiScreenSize
    {
        get => (XConfigResolution)ReadInt32(UserBase + 0x15C);
        set => WriteInt32(UserBase + 0x15C, (int)value);
    }

    /// <summary>
    /// Component screen resolution as packed (width &lt;&lt; 16) | height.
    /// <para>
    /// Offset: User + 0x160 (4 bytes).
    /// </para>
    /// </summary>
    public XConfigResolution AvComponentScreenSize
    {
        get => (XConfigResolution)ReadInt32(UserBase + 0x160);
        set => WriteInt32(UserBase + 0x160, (int)value);
    }

    /// <summary>
    /// VGA screen resolution as packed (width &lt;&lt; 16) | height.
    /// <para>
    /// Offset: User + 0x164 (4 bytes).
    /// </para>
    /// </summary>
    public XConfigResolution AvVgaScreenSize
    {
        get => (XConfigResolution)ReadInt32(UserBase + 0x164);
        set => WriteInt32(UserBase + 0x164, (int)value);
    }

    /// <summary>
    /// Packs a width and height into a single XConfigResolution value.
    /// </summary>
    public static XConfigResolution PackResolution(ushort width, ushort height) => (XConfigResolution)((width << 16) | height);

    /// <summary>
    /// Extracts the width component from a packed resolution value.
    /// </summary>
    public static ushort UnpackWidth(XConfigResolution packed) => (ushort)((int)packed >> 16);

    /// <summary>
    /// Extracts the height component from a packed resolution value.
    /// </summary>
    public static ushort UnpackHeight(XConfigResolution packed) => (ushort)(int)packed;

    // Parental controls
    /// <summary>
    /// Parental control flags bitmask (XPcFlags).
    /// <para>
    /// Offset: User + 0x041 (1 byte).
    /// </para>
    /// </summary>
    public XPcFlags PcFlags
    {
        get => (XPcFlags)ReadByte(UserBase + 0x041);
        set => WriteByte(UserBase + 0x041, (byte)value);
    }

    /// <summary>
    /// Whether XBL online access is allowed for this profile.
    /// </summary>
    public bool IsXblAllowed
    {
        get => (PcFlags & XPcFlags.XblAllowed) != 0;
        set => PcFlags = value ? PcFlags | XPcFlags.XblAllowed : PcFlags & ~XPcFlags.XblAllowed;
    }

    /// <summary>
    /// Whether XBL membership creation is allowed for this profile.
    /// </summary>
    public bool IsXblMembershipCreationAllowed
    {
        get => (PcFlags & XPcFlags.XblMembershipCreationAllowed) != 0;
        set => PcFlags = value ? PcFlags | XPcFlags.XblMembershipCreationAllowed : PcFlags & ~XPcFlags.XblMembershipCreationAllowed;
    }

    /// <summary>
    /// Whether Xbox One game play is allowed for this profile.
    /// </summary>
    public bool IsXboxOneGameAllowed
    {
        get => (PcFlags & XPcFlags.XboxOneGameAllowed) != 0;
        set => PcFlags = value ? PcFlags | XPcFlags.XboxOneGameAllowed : PcFlags & ~XPcFlags.XboxOneGameAllowed;
    }

    /// <summary>
    /// Whether parental controls are enabled for this profile.
    /// </summary>
    public bool IsPcEnabled
    {
        get => (PcFlags & XPcFlags.PcEnabled) != 0;
        set => PcFlags = value ? PcFlags | XPcFlags.PcEnabled : PcFlags & ~XPcFlags.PcEnabled;
    }

    // Secured category properties
    /// <summary>
    /// Online device network ID.
    /// <para>
    /// Offset: Secured + 0x08 (4 bytes).
    /// </para>
    /// </summary>
    public uint OnlineNetworkId
    {
        get => ReadUInt32(SecuredBase + 0x08);
        set => WriteUInt32(SecuredBase + 0x08, value);
    }

    /// <summary>
    /// MAC address (6 bytes).
    /// <para>
    /// Offset: Secured + 0x20.
    /// </para>
    /// </summary>
    public byte[] MacAddress
    {
        get => ReadBytes(SecuredBase + 0x20, 6);
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (value.Length != 6)
            {
                throw new ArgumentException("MAC address must be exactly 6 bytes", nameof(value));
            }
            WriteBytes(SecuredBase + 0x20, value);
        }
    }

    /// <summary>
    /// AV region (XAvRegion).
    /// <para>
    /// Offset: Secured + 0x28 (4 bytes).
    /// </para>
    /// </summary>
    public XAvRegion AvRegion
    {
        get => (XAvRegion)ReadUInt32(SecuredBase + 0x28);
        set => WriteUInt32(SecuredBase + 0x28, (uint)value);
    }

    /// <summary>
    /// Game region code.
    /// <para>
    /// Offset: Secured + 0x2C (2 bytes).
    /// </para>
    /// </summary>
    public ushort GameRegion
    {
        get => ReadUInt16(SecuredBase + 0x2C);
        set => WriteUInt16(SecuredBase + 0x2C, value);
    }

    /// <summary>
    /// DVD region code.
    /// <para>
    /// Offset: Secured + 0x34 (4 bytes).
    /// </para>
    /// </summary>
    public uint DvdRegion
    {
        get => ReadUInt32(SecuredBase + 0x34);
        set => WriteUInt32(SecuredBase + 0x34, value);
    }

    /// <summary>
    /// Console reset key.
    /// <para>
    /// Offset: Secured + 0x38 (4 bytes).
    /// </para>
    /// </summary>
    public uint ResetKey
    {
        get => ReadUInt32(SecuredBase + 0x38);
        set => WriteUInt32(SecuredBase + 0x38, value);
    }

    /// <summary>
    /// System flags bitmask.
    /// <para>
    /// Offset: Secured + 0x3C (4 bytes).
    /// </para>
    /// </summary>
    public uint SystemFlags
    {
        get => ReadUInt32(SecuredBase + 0x3C);
        set => WriteUInt32(SecuredBase + 0x3C, value);
    }

    // Console category properties
    /// <summary>
    /// Screen saver timeout setting (XScreenSaver).
    /// <para>
    /// Offset: Console + 0x008 (2 bytes).
    /// </para>
    /// </summary>
    public XScreenSaver ScreenSaver
    {
        get => (XScreenSaver)ReadInt16(ConsoleBase + 0x008);
        set => WriteInt16(ConsoleBase + 0x008, (short)value);
    }

    /// <summary>
    /// Auto shut-off timer setting (XAutoShutOff).
    /// <para>
    /// Offset: Console + 0x00A (2 bytes).
    /// </para>
    /// </summary>
    public XAutoShutOff AutoShutOff
    {
        get => (XAutoShutOff)ReadInt16(ConsoleBase + 0x00A);
        set => WriteInt16(ConsoleBase + 0x00A, (short)value);
    }

    /// <summary>
    /// Keyboard layout setting (XKeyboardLayout).
    /// <para>
    /// Offset: Console + 0x142 (2 bytes).
    /// </para>
    /// </summary>
    public XKeyboardLayout KeyboardLayout
    {
        get => (XKeyboardLayout)ReadInt16(ConsoleBase + 0x142);
        set => WriteInt16(ConsoleBase + 0x142, (short)value);
    }

    // System category properties
    /// <summary>
    /// Alarm time for scheduled wake.
    /// <para>
    /// Offset: System + 0x04 (8 bytes).
    /// </para>
    /// </summary>
    public ulong AlarmTime
    {
        get => ReadUInt64(SystemBase + 0x04);
        set => WriteUInt64(SystemBase + 0x04, value);
    }

    /// <summary>
    /// Previous flash version.
    /// <para>
    /// Offset: System + 0x0C (4 bytes).
    /// </para>
    /// </summary>
    public uint PreviousFlashVersion
    {
        get => ReadUInt32(SystemBase + 0x0C);
        set => WriteUInt32(SystemBase + 0x0C, value);
    }

    /// <summary>
    /// Creates a new XConfigFile with default settings.
    /// </summary>
    public static XConfigFile Create()
    {
        Logger.Trace<XConfigFile>("Creating new XConfig settings with defaults");
        var xconfig = new XConfigFile();
        xconfig.SetDefaults();
        Logger.Info<XConfigFile>("Created new XConfig file with defaults");
        return xconfig;
    }

    /// <summary>
    /// Loads an XConfig file from disk, or creates one with defaults if it does not exist.
    /// </summary>
    /// <param name="filePath">Path to the XConfig settings file.</param>
    /// <returns>A populated XConfigFile instance.</returns>
    public static XConfigFile Load(string filePath)
    {
        Logger.Trace<XConfigFile>($"Load started for {filePath}");

        XConfigFile xconfig;
        if (!File.Exists(filePath))
        {
            Logger.Info<XConfigFile>($"XConfig file not found at {filePath}, creating with defaults");
            xconfig = Create();
            xconfig.FlushToFile(filePath);
        }
        else
        {
            byte[] fileBytes = File.ReadAllBytes(filePath);
            Logger.Trace<XConfigFile>($"Read {fileBytes.Length} bytes from {filePath}");

            if (fileBytes.Length == XConfigOffsets.TotalSize)
            {
                xconfig = new XConfigFile(fileBytes);
                Logger.Info<XConfigFile>($"Loaded XConfig file: {filePath} ({fileBytes.Length} bytes)");
            }
            else
            {
                Logger.Warning<XConfigFile>($"XConfig file has unexpected size {fileBytes.Length} (expected {XConfigOffsets.TotalSize}), resetting to defaults");
                xconfig = Create();
                xconfig.FlushToFile(filePath);
            }
        }

        xconfig._filePath = filePath;
        Logger.Debug<XConfigFile>($"XConfig file path set to {filePath}");
        return xconfig;
    }

    /// <summary>
    /// Creates an XConfigFile from an existing byte array.
    /// </summary>
    /// <param name="data">Byte array of exactly <see cref="XConfigOffsets.TotalSize"/> bytes.</param>
    /// <returns>A new XConfigFile instance.</returns>
    public static XConfigFile FromBytes(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (data.Length != XConfigOffsets.TotalSize)
        {
            throw new ArgumentException($"Data must be exactly {XConfigOffsets.TotalSize} bytes (got {data.Length})", nameof(data));
        }

        Logger.Debug<XConfigFile>($"Creating XConfig from byte array ({data.Length} bytes)");
        Logger.Trace<XConfigFile>($"First 32 bytes: {BitConverter.ToString(data, 0, 32)}");
        return new XConfigFile((byte[])data.Clone());
    }

    /// <summary>
    /// Saves the current XConfig settings to disk.
    /// </summary>
    /// <param name="filePath">Optional path to save to. Uses the original load path if omitted.</param>
    public void Save(string? filePath = null)
    {
        string savePath = filePath ?? _filePath!;

        if (string.IsNullOrEmpty(savePath))
        {
            Logger.Error<XConfigFile>("No save path specified and the file was not loaded from a path");
            throw new InvalidOperationException("No save path specified and the file was not loaded from a path");
        }

        Logger.Debug<XConfigFile>($"Saving XConfig settings to {savePath}");

        try
        {
            string? directory = Path.GetDirectoryName(savePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Logger.Info<XConfigFile>($"Creating directory: {directory}");
                Directory.CreateDirectory(directory);
            }

            lock (_lock)
            {
                FlushToFile(savePath);
            }

            _filePath = savePath;
            Logger.Info<XConfigFile>($"Successfully saved XConfig settings to {savePath} ({_data.Length} bytes)");
        }
        catch (Exception ex)
        {
            Logger.Error<XConfigFile>($"Failed to save XConfig settings to {savePath}: {ex.Message}");
            Logger.LogExceptionDetails<XConfigFile>(ex);
            throw;
        }
    }

    /// <summary>
    /// Gets the size of a registered setting field.
    /// </summary>
    /// <param name="category">The setting category.</param>
    /// <param name="settingId">The setting identifier within the category.</param>
    /// <returns>The field size in bytes, or 0 if the setting is not registered.</returns>
    public ushort GetSettingSize(XConfigCategory category, ushort settingId)
    {
        FieldDescriptor? field = XConfigFields.Find(category, settingId);
        if (field is null)
        {
            Logger.Debug<XConfigFile>($"Setting not found: category={category}, settingId={settingId}");
            return 0;
        }
        Logger.Trace<XConfigFile>($"Setting size for category={category}, settingId={settingId}: {field.Value.Size} bytes");
        return field.Value.Size;
    }

    /// <summary>
    /// Reads a raw setting field into the provided buffer.
    /// </summary>
    /// <param name="category">The setting category.</param>
    /// <param name="settingId">The setting identifier within the category.</param>
    /// <param name="buffer">Buffer to receive the raw bytes.</param>
    public void ReadSetting(XConfigCategory category, ushort settingId, byte[] buffer)
    {
        FieldDescriptor? field = XConfigFields.Find(category, settingId);
        if (field is null)
        {
            Logger.Warning<XConfigFile>($"Unknown setting on read: category={category}, settingId={settingId}");
            return;
        }

        Logger.Trace<XConfigFile>($"Reading setting: category={category}, settingId={settingId}, offset=0x{field.Value.AbsoluteOffset:X4}, size={field.Value.Size}");

        lock (_lock)
        {
            int bytesToCopy = Math.Min(field.Value.Size, buffer.Length);
            Buffer.BlockCopy(_data, field.Value.AbsoluteOffset, buffer, 0, bytesToCopy);
        }
    }

    /// <summary>
    /// Writes raw bytes to a setting field, flushing to disk.
    /// </summary>
    /// <param name="category">The setting category.</param>
    /// <param name="settingId">The setting identifier within the category.</param>
    /// <param name="buffer">Source buffer with the raw bytes to write.</param>
    public void WriteSetting(XConfigCategory category, ushort settingId, byte[] buffer)
    {
        FieldDescriptor? field = XConfigFields.Find(category, settingId);
        if (field is null)
        {
            Logger.Warning<XConfigFile>($"Unknown setting on write: category={category}, settingId={settingId}");
            return;
        }

        Logger.Trace<XConfigFile>($"Writing setting: category={category}, settingId={settingId}, offset=0x{field.Value.AbsoluteOffset:X4}, size={field.Value.Size}");

        lock (_lock)
        {
            int bytesToCopy = Math.Min(field.Value.Size, buffer.Length);
            Buffer.BlockCopy(buffer, 0, _data, field.Value.AbsoluteOffset, bytesToCopy);
            FlushToFile();
        }
    }

    /// <summary>
    /// Reads a typed setting value from the XConfig blob.
    /// </summary>
    /// <typeparam name="T">An unmanaged type matching the field's size.</typeparam>
    /// <param name="category">The setting category.</param>
    /// <param name="settingId">The setting identifier within the category.</param>
    /// <returns>The deserialized value, or default if the setting is unrecognized.</returns>
    public T ReadSetting<T>(XConfigCategory category, ushort settingId) where T : unmanaged
    {
        FieldDescriptor? field = XConfigFields.Find(category, settingId);
        if (field is null)
        {
            Logger.Warning<XConfigFile>($"Unknown setting on typed read: category={category}, settingId={settingId}");
            return default;
        }

        int tSize = Marshal.SizeOf<T>();
        if (tSize != field.Value.Size)
        {
            Logger.Warning<XConfigFile>($"Type size mismatch for category={category}, settingId={settingId}: expected {field.Value.Size} bytes, got {tSize}");
            return default;
        }

        Logger.Trace<XConfigFile>($"Reading typed setting: category={category}, settingId={settingId}, offset=0x{field.Value.AbsoluteOffset:X4}, type={typeof(T).Name}");

        lock (_lock)
        {
            int size = field.Value.Size;
            byte[] buffer = new byte[size];
            Buffer.BlockCopy(_data, field.Value.AbsoluteOffset, buffer, 0, size);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(buffer);
            }
            return MemoryMarshal.Read<T>(buffer.AsSpan());
        }
    }

    /// <summary>
    /// Writes a typed setting value to the XConfig blob, flushing to disk.
    /// </summary>
    /// <typeparam name="T">An unmanaged type matching the field's size.</typeparam>
    /// <param name="category">The setting category.</param>
    /// <param name="settingId">The setting identifier within the category.</param>
    /// <param name="value">The value to write.</param>
    public void WriteSetting<T>(XConfigCategory category, ushort settingId, T value) where T : unmanaged
    {
        FieldDescriptor? field = XConfigFields.Find(category, settingId);
        if (field is null)
        {
            Logger.Warning<XConfigFile>($"Unknown setting on typed write: category={category}, settingId={settingId}");
            return;
        }

        int tSize = Marshal.SizeOf<T>();
        if (tSize != field.Value.Size)
        {
            Logger.Warning<XConfigFile>($"Type size mismatch for category={category}, settingId={settingId}: expected {field.Value.Size} bytes, got {tSize}");
            return;
        }

        Logger.Trace<XConfigFile>($"Writing typed setting: category={category}, settingId={settingId}, offset=0x{field.Value.AbsoluteOffset:X4}, type={typeof(T).Name}");

        lock (_lock)
        {
            int size = field.Value.Size;
            byte[] buffer = new byte[size];
            MemoryMarshal.Write(buffer.AsSpan(), in value);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(buffer);
            }
            Buffer.BlockCopy(buffer, 0, _data, field.Value.AbsoluteOffset, size);
            FlushToFile();
        }
    }

    /// <summary>
    /// Returns a clone of the internal XConfig data buffer.
    /// </summary>
    public byte[] GetRawData()
    {
        Logger.Trace<XConfigFile>("Getting raw data buffer");
        lock (_lock)
        {
            byte[] clone = (byte[])_data.Clone();
            Logger.Trace<XConfigFile>($"Returning raw data buffer ({clone.Length} bytes)");
            return clone;
        }
    }

    /// <summary>
    /// Resets all settings to their default values.
    /// </summary>
    public void SetDefaults()
    {
        Logger.Trace<XConfigFile>("Setting XConfig defaults");

        lock (_lock)
        {
            Array.Clear(_data, 0, _data.Length);

            int securedBase = XConfigOffsets.CategoryOffset(XConfigCategory.Secured);
            int userBase = XConfigOffsets.CategoryOffset(XConfigCategory.User);
            int iptvBase = XConfigOffsets.CategoryOffset(XConfigCategory.Iptv);

            // Secured: AV region = NTSCM (0x00400100)
            BinaryPrimitives.WriteUInt32BigEndian(_data.AsSpan(securedBase + 0x28), 0x00400100u);
            Logger.Trace<XConfigFile>("  Default: Secured.AvRegion = NTSCM");

            // User: VideoFlags = RatioNormal (0)
            BinaryPrimitives.WriteUInt32BigEndian(_data.AsSpan(userBase + 0x030), 0u);
            Logger.Trace<XConfigFile>("  Default: User.VideoFlags = RatioNormal");

            // User: Language = kEnglish (1)
            BinaryPrimitives.WriteUInt32BigEndian(_data.AsSpan(userBase + 0x02C), 1u);
            Logger.Trace<XConfigFile>("  Default: User.Language = English");

            // User: Country = UnitedStates (103)
            _data[userBase + 0x040] = 103;
            Logger.Trace<XConfigFile>("  Default: User.Country = UnitedStates");

            // User: AudioFlags = DolbyDigital | DolbyProLogic (0x00010001)
            BinaryPrimitives.WriteUInt32BigEndian(_data.AsSpan(userBase + 0x034), 0x00010001u);
            Logger.Trace<XConfigFile>("  Default: User.AudioFlags = DolbyDigital | DolbyProLogic");

            // User: AvCompositeScreenSz = 1280x720 packed as (1280 << 16) | 720 = 0x050002D0
            BinaryPrimitives.WriteUInt32BigEndian(_data.AsSpan(userBase + 0x15C), 0x050002D0u);
            Logger.Trace<XConfigFile>("  Default: User.AvCompositeScreenSz = 1280x720");

            // User: AvComponentScreenSz = 1280x720
            BinaryPrimitives.WriteUInt32BigEndian(_data.AsSpan(userBase + 0x160), 0x050002D0u);
            Logger.Trace<XConfigFile>("  Default: User.AvComponentScreenSz = 1280x720");

            // User: AvVgaScreenSz = 720x576 packed as (720 << 16) | 576 = 0x02D00240
            BinaryPrimitives.WriteUInt32BigEndian(_data.AsSpan(userBase + 0x164), 0x02D00240u);
            Logger.Trace<XConfigFile>("  Default: User.AvVgaScreenSz = 720x576");

            // User: RetailFlags = DashboardInitialized (0x00000040)
            BinaryPrimitives.WriteUInt32BigEndian(_data.AsSpan(userBase + 0x038), 0x00000040u);
            Logger.Trace<XConfigFile>("  Default: User.RetailFlags = DashboardInitialized");

            // User: PcFlags (parental control) = XBLAllowed | XBLMembershipCreationAllowed (0x03)
            _data[userBase + 0x041] = 0x03;
            Logger.Trace<XConfigFile>("  Default: User.PcFlags = XBLAllowed | XBLMembershipCreationAllowed");

            // User: PcGame = NoGameRestrictions (0x000000FF)
            BinaryPrimitives.WriteUInt32BigEndian(_data.AsSpan(userBase + 0x168), 0x000000FFu);
            Logger.Trace<XConfigFile>("  Default: User.PcGame = NoGameRestrictions");

            // User: MusicVolume = 0.7f
            BinaryPrimitives.WriteUInt32BigEndian(_data.AsSpan(userBase + 0x1C1), 0x3F333333u);
            Logger.Trace<XConfigFile>("  Default: User.MusicVolume = 0.7");

            // User: TimeZone defaults (London GMT/BST)
            BinaryPrimitives.WriteUInt32BigEndian(_data.AsSpan(userBase + 0x008), 0u);
            _data[userBase + 0x00C] = (byte)'G';
            _data[userBase + 0x00D] = (byte)'M';
            _data[userBase + 0x00E] = (byte)'T';
            _data[userBase + 0x00F] = 0;
            _data[userBase + 0x010] = (byte)'B';
            _data[userBase + 0x011] = (byte)'S';
            _data[userBase + 0x012] = (byte)'T';
            _data[userBase + 0x013] = 0;
            _data[userBase + 0x014] = 0x0A;
            _data[userBase + 0x015] = 0x05;
            _data[userBase + 0x016] = 0x00;
            _data[userBase + 0x017] = 0x02;
            _data[userBase + 0x018] = 0x03;
            _data[userBase + 0x019] = 0x05;
            _data[userBase + 0x01A] = 0x00;
            _data[userBase + 0x01B] = 0x01;
            BinaryPrimitives.WriteInt32BigEndian(_data.AsSpan(userBase + 0x01C), 0);
            BinaryPrimitives.WriteInt32BigEndian(_data.AsSpan(userBase + 0x020), -60);
            Logger.Trace<XConfigFile>("  Default: User.TimeZone = London GMT/BST");

            // IPTV: ServiceProviderName = "Xenia TV" as UTF-16BE
            byte[] providerName = System.Text.Encoding.BigEndianUnicode.GetBytes("Xenia TV\0");
            int copyLen = Math.Min(providerName.Length, 120);
            Buffer.BlockCopy(providerName, 0, _data, iptvBase + 0x008, copyLen);
            Logger.Trace<XConfigFile>("  Default: IPTV.ServiceProviderName = Xenia TV");

            Logger.Debug<XConfigFile>("Set XConfig defaults successfully");
        }
    }

    // Low-level byte buffer access (no auto-flush)
    private byte ReadByte(int offset)
    {
        lock (_lock)
        {
            return _data[offset];
        }
    }

    private void WriteByte(int offset, byte value)
    {
        lock (_lock)
        {
            _data[offset] = value;
        }
    }

    private short ReadInt16(int offset)
    {
        lock (_lock)
        {
            return BinaryPrimitives.ReadInt16BigEndian(_data.AsSpan(offset));
        }
    }

    private void WriteInt16(int offset, short value)
    {
        lock (_lock)
        {
            BinaryPrimitives.WriteInt16BigEndian(_data.AsSpan(offset), value);
        }
    }

    private ushort ReadUInt16(int offset)
    {
        lock (_lock)
        {
            return BinaryPrimitives.ReadUInt16BigEndian(_data.AsSpan(offset));
        }
    }

    private void WriteUInt16(int offset, ushort value)
    {
        lock (_lock)
        {
            BinaryPrimitives.WriteUInt16BigEndian(_data.AsSpan(offset), value);
        }
    }

    private int ReadInt32(int offset)
    {
        lock (_lock)
        {
            return BinaryPrimitives.ReadInt32BigEndian(_data.AsSpan(offset));
        }
    }

    private void WriteInt32(int offset, int value)
    {
        lock (_lock)
        {
            BinaryPrimitives.WriteInt32BigEndian(_data.AsSpan(offset), value);
        }
    }

    private uint ReadUInt32(int offset)
    {
        lock (_lock)
        {
            return BinaryPrimitives.ReadUInt32BigEndian(_data.AsSpan(offset));
        }
    }

    private void WriteUInt32(int offset, uint value)
    {
        lock (_lock)
        {
            BinaryPrimitives.WriteUInt32BigEndian(_data.AsSpan(offset), value);
        }
    }

    private ulong ReadUInt64(int offset)
    {
        lock (_lock)
        {
            return BinaryPrimitives.ReadUInt64BigEndian(_data.AsSpan(offset));
        }
    }

    private void WriteUInt64(int offset, ulong value)
    {
        lock (_lock)
        {
            BinaryPrimitives.WriteUInt64BigEndian(_data.AsSpan(offset), value);
        }
    }

    private float ReadSingle(int offset)
    {
        lock (_lock)
        {
            return BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32BigEndian(_data.AsSpan(offset)));
        }
    }

    private void WriteSingle(int offset, float value)
    {
        lock (_lock)
        {
            BinaryPrimitives.WriteInt32BigEndian(_data.AsSpan(offset), BitConverter.SingleToInt32Bits(value));
        }
    }

    private byte[] ReadBytes(int offset, int count)
    {
        lock (_lock)
        {
            byte[] result = new byte[count];
            Buffer.BlockCopy(_data, offset, result, 0, count);
            return result;
        }
    }

    private void WriteBytes(int offset, byte[] value)
    {
        lock (_lock)
        {
            Buffer.BlockCopy(value, 0, _data, offset, value.Length);
        }
    }

    private void FlushToFile()
    {
        if (string.IsNullOrEmpty(_filePath))
        {
            return;
        }

        FlushToFile(_filePath);
    }

    private void FlushToFile(string path)
    {
        Logger.Trace<XConfigFile>($"Flushing {_data.Length} bytes to {path}");
        File.WriteAllBytes(path, _data);
        Logger.Trace<XConfigFile>($"Flushed to {path} successfully");
    }
}