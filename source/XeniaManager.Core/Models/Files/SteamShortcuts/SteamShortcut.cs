using System.Text;

namespace XeniaManager.Core.Models.Files.SteamShortcuts;

/// <summary>
/// Represents a Steam non-Steam game shortcut.
/// Shortcuts are stored in the shortcuts.vdf file in binary VDF format.
/// </summary>
public class SteamShortcut
{
    /// <summary>
    /// Gets or sets the AppId as a byte array.
    /// For non-Steam games, this is typically a CRC32 hash with the high bit set (0x80000000).
    /// </summary>
    public byte[]? AppId { get; set; }

    /// <summary>
    /// Gets or sets the application name displayed in Steam Library.
    /// </summary>
    public string? AppName { get; set; }

    /// <summary>
    /// Gets or sets the path to the executable.
    /// </summary>
    public string? Exe { get; set; }

    /// <summary>
    /// Gets or sets the starting directory for the application.
    /// </summary>
    public string? StartDir { get; set; }

    /// <summary>
    /// Gets or sets the path to the icon file.
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Gets or sets the shortcut path.
    /// </summary>
    public string? ShortcutPath { get; set; }

    /// <summary>
    /// Gets or sets the launch options (command line arguments).
    /// </summary>
    public string? LaunchOptions { get; set; }

    /// <summary>
    /// Gets or sets whether the shortcut is hidden in the library.
    /// </summary>
    public bool IsHidden { get; set; }

    /// <summary>
    /// Gets or sets whether desktop configuration is allowed.
    /// </summary>
    public bool AllowDesktopConfig { get; set; }

    /// <summary>
    /// Gets or sets whether the Steam Overlay is enabled.
    /// </summary>
    public bool AllowOverlay { get; set; }

    /// <summary>
    /// Gets or sets whether the application is included in the VR library.
    /// </summary>
    public bool OpenVR { get; set; }

    /// <summary>
    /// Gets or sets whether this is a devkit application.
    /// </summary>
    public bool Devkit { get; set; }

    /// <summary>
    /// Gets or sets the devkit game ID.
    /// </summary>
    public string? DevkitGameID { get; set; }

    /// <summary>
    /// Gets or sets the devkit override AppId as a byte array.
    /// </summary>
    public byte[]? DevkitOverrideAppID { get; set; }

    /// <summary>
    /// Gets or sets the last playtime as a byte array (Unix timestamp).
    /// </summary>
    public byte[]? LastPlayTime { get; set; }

    /// <summary>
    /// Gets or sets the Flatpak application ID for Linux systems.
    /// </summary>
    public string? FlatpakAppID { get; set; }

    /// <summary>
    /// Gets or sets the sort-as string for library sorting.
    /// </summary>
    public string? SortAs { get; set; }

    /// <summary>
    /// Gets or sets the list of tags/collections for this shortcut.
    /// </summary>
    public List<string> Tags { get; set; } = [];

    /// <summary>
    /// Creates a new Steam shortcut.
    /// </summary>
    public SteamShortcut()
    {
    }

    /// <summary>
    /// Gets the AppId as an uint value.
    /// </summary>
    /// <returns>The AppId as uint, or 0 if not set.</returns>
    public uint GetAppIdAsUint()
    {
        if (AppId == null || AppId.Length < 4)
        {
            return 0;
        }
        return BitConverter.ToUInt32(AppId, 0);
    }

    /// <summary>
    /// Sets the AppId from an uint value.
    /// </summary>
    /// <param name="appId">The AppId value.</param>
    public void SetAppIdFromUint(uint appId)
    {
        AppId = BitConverter.GetBytes(appId);
    }

    /// <summary>
    /// Gets the DevkitOverrideAppID as an uint value.
    /// </summary>
    /// <returns>The DevkitOverrideAppID as uint, or 0 if not set.</returns>
    public uint GetDevkitOverrideAppIdAsUint()
    {
        if (DevkitOverrideAppID == null || DevkitOverrideAppID.Length < 4)
        {
            return 0;
        }
        return BitConverter.ToUInt32(DevkitOverrideAppID, 0);
    }

    /// <summary>
    /// Sets the DevkitOverrideAppID from a uint value.
    /// </summary>
    /// <param name="appId">The AppId value.</param>
    public void SetDevkitOverrideAppIdFromUint(uint appId)
    {
        DevkitOverrideAppID = BitConverter.GetBytes(appId);
    }

    /// <summary>
    /// Gets the LastPlayTime as a Unix timestamp.
    /// </summary>
    /// <returns>The last playtime as int (Unix timestamp), or 0 if not set.</returns>
    public int GetLastPlayTimeAsInt()
    {
        if (LastPlayTime == null || LastPlayTime.Length < 4)
        {
            return 0;
        }
        return BitConverter.ToInt32(LastPlayTime, 0);
    }

    /// <summary>
    /// Sets the LastPlayTime from a Unix timestamp.
    /// </summary>
    /// <param name="timestamp">The Unix timestamp.</param>
    public void SetLastPlayTimeFromInt(int timestamp)
    {
        LastPlayTime = BitConverter.GetBytes(timestamp);
    }

    /// <summary>
    /// Computes the AppId for this shortcut based on AppName and Exe.
    /// Uses CRC32 algorithm with the high bit set for non-Steam games.
    /// </summary>
    /// <returns>The computed AppId.</returns>
    public uint ComputeAppId()
    {
        string combined = (AppName ?? "") + (Exe ?? "");
        byte[] data = Encoding.UTF8.GetBytes(combined);
        uint crc = ComputeCRC32(data);
        return crc | 0x80000000;
    }

    /// <summary>
    /// Computes CRC32 hash for the given data.
    /// </summary>
    /// <param name="bytes">The data to compute CRC32 for.</param>
    /// <returns>The CRC32 hash.</returns>
    private static uint ComputeCRC32(byte[] bytes)
    {
        const uint Polynomial = 0xEDB88320;
        uint[] table = new uint[256];

        for (uint i = 0; i < 256; i++)
        {
            uint temp = i;
            for (int j = 0; j < 8; j++)
            {
                temp = (temp & 1) == 1 ? (Polynomial ^ (temp >> 1)) : (temp >> 1);
            }
            table[i] = temp;
        }

        uint crc = 0xFFFFFFFF;
        foreach (byte b in bytes)
        {
            byte index = (byte)((crc & 0xFF) ^ b);
            crc = (crc >> 8) ^ table[index];
        }
        return ~crc;
    }

    /// <summary>
    /// Returns a string representation of this shortcut.
    /// </summary>
    /// <returns>The shortcut name and AppId.</returns>
    public override string ToString()
    {
        string name = AppName ?? "Unknown";
        return $"\"{name}\" (AppId: {GetAppIdAsUint()})";
    }
}