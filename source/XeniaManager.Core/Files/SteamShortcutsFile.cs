using System.Text;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Models.Files.SteamShortcuts;

namespace XeniaManager.Core.Files;

/// <summary>
/// Handles loading, saving, and manipulation of Steam shortcuts.vdf files.
/// The shortcuts.vdf file is a binary VDF format used by Steam to store non-Steam game shortcuts.
/// File location: steam_install_dir\userdata\user_id\config\shortcuts.vdf
/// </summary>
public class SteamShortcutsFile
{
    /// <summary>
    /// Gets the list of shortcuts.
    /// </summary>
    public List<SteamShortcut> Shortcuts { get; private set; } = [];

    /// <summary>
    /// Gets a read-only list of shortcuts.
    /// </summary>
    public IReadOnlyList<SteamShortcut> ShortcutsReadOnly => Shortcuts.AsReadOnly();

    /// <summary>
    /// The file path of this shortcuts file.
    /// </summary>
    public string? FilePath { get; private set; }

    /// <summary>
    /// Private constructor to enforce factory methods.
    /// </summary>
    private SteamShortcutsFile()
    {
    }

    /// <summary>
    /// Creates a new empty Steam shortcuts file.
    /// </summary>
    /// <returns>A new SteamShortcutsFile instance.</returns>
    public static SteamShortcutsFile Create()
    {
        Logger.Info<SteamShortcutsFile>("Creating new Steam shortcuts file");
        return new SteamShortcutsFile();
    }

    /// <summary>
    /// Loads a Steam shortcuts file from the specified path.
    /// </summary>
    /// <param name="filePath">The path to the shortcuts.vdf file to load.</param>
    /// <returns>A new SteamShortcutsFile instance.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
    /// <exception cref="FormatException">Thrown when the file has an invalid binary VDF format.</exception>
    public static SteamShortcutsFile Load(string filePath)
    {
        Logger.Debug<SteamShortcutsFile>($"Loading Steam shortcuts file from {filePath}");

        if (!File.Exists(filePath))
        {
            Logger.Error<SteamShortcutsFile>($"Steam shortcuts file does not exist: {filePath}");
            throw new FileNotFoundException($"Steam shortcuts file does not exist at {filePath}", filePath);
        }

        FileInfo fileInfo = new FileInfo(filePath);
        if (fileInfo.Length == 0)
        {
            Logger.Info<SteamShortcutsFile>($"Steam shortcuts file is empty: {filePath}");
            return Create();
        }

        byte[] content = File.ReadAllBytes(filePath);
        Logger.Info<SteamShortcutsFile>($"Loaded Steam shortcuts file: {filePath} ({content.Length} bytes)");

        SteamShortcutsFile shortcutsFile = FromBytes(content);
        shortcutsFile.FilePath = filePath;

        return shortcutsFile;
    }

    /// <summary>
    /// Parses a Steam shortcuts file from a byte array.
    /// </summary>
    /// <param name="bytes">The binary VDF content to parse.</param>
    /// <returns>A new SteamShortcutsFile instance.</returns>
    /// <exception cref="FormatException">Thrown when the binary VDF content is invalid.</exception>
    public static SteamShortcutsFile FromBytes(byte[] bytes)
    {
        Logger.Trace<SteamShortcutsFile>($"Parsing Steam shortcuts file from bytes ({bytes.Length} bytes)");

        try
        {
            SteamShortcutsFile shortcutsFile = new SteamShortcutsFile();
            ParseBinaryVdf(bytes, shortcutsFile);

            Logger.Info<SteamShortcutsFile>($"Successfully parsed Steam shortcuts file with {shortcutsFile.Shortcuts.Count} shortcuts");
            return shortcutsFile;
        }
        catch (Exception ex)
        {
            Logger.Error<SteamShortcutsFile>($"Failed to parse Steam shortcuts file");
            Logger.LogExceptionDetails<SteamShortcutsFile>(ex);
            throw new FormatException($"Invalid binary VDF content.", ex);
        }
    }

    /// <summary>
    /// Parses the binary VDF content.
    /// </summary>
    private static void ParseBinaryVdf(byte[] bytes, SteamShortcutsFile shortcutsFile)
    {
        using MemoryStream ms = new MemoryStream(bytes);
        using BinaryReader reader = new BinaryReader(ms, Encoding.UTF8);

        // Read root type and key
        VdfType rootType = ReadVdfType(reader);
        string rootKey = ReadNullTerminatedString(reader);

        if (rootType != VdfType.Dictionary)
        {
            throw new FormatException($"Expected root dictionary (0x00), got 0x{(byte)rootType:X2}");
        }

        if (rootKey != "shortcuts")
        {
            throw new FormatException($"Expected root key 'shortcuts', got '{rootKey}'");
        }

        // Read each shortcut
        while (true)
        {
            VdfType type = ReadVdfType(reader);
            if (type == VdfType.End)
            {
                break; // End-of-shortcuts dictionary
            }

            if (type != VdfType.Dictionary)
            {
                throw new FormatException($"Expected dictionary key (0x00), got 0x{(byte)type:X2}");
            }

            // Read and discard the index key (e.g., "0", "1", etc.)
            ReadNullTerminatedString(reader);

            SteamShortcut shortcut = ParseShortcut(reader);
            shortcutsFile.Shortcuts.Add(shortcut);
        }
    }

    /// <summary>
    /// Parses a single shortcut from the binary reader.
    /// </summary>
    private static SteamShortcut ParseShortcut(BinaryReader reader)
    {
        SteamShortcut shortcut = new SteamShortcut();

        while (true)
        {
            VdfType type = ReadVdfType(reader);
            if (type == VdfType.End)
            {
                break; // End of this shortcut dictionary
            }

            string key = ReadNullTerminatedString(reader);

            switch (type)
            {
                case VdfType.Dictionary:
                    // Nested dictionary, e.g., tags
                    if (key == "tags")
                    {
                        shortcut.Tags = ParseTags(reader);
                    }
                    else
                    {
                        SkipDictionary(reader);
                    }
                    break;

                case VdfType.String:
                    string val = ReadNullTerminatedString(reader);
                    AssignString(shortcut, key, val);
                    break;

                case VdfType.Int32:
                    int intval = reader.ReadInt32();
                    AssignInt(shortcut, key, intval);
                    break;

                default:
                    Logger.Warning<SteamShortcutsFile>($"Unknown type 0x{(byte)type:X2} for key '{key}', skipping");
                    break;
            }
        }

        return shortcut;
    }

    /// <summary>
    /// Parses the tags dictionary.
    /// </summary>
    private static List<string> ParseTags(BinaryReader reader)
    {
        List<string> tags = new List<string>();

        while (true)
        {
            VdfType type = ReadVdfType(reader);
            if (type == VdfType.End)
            {
                break; // End-of-tags dictionary
            }

            if (type != VdfType.String)
            {
                Logger.Warning<SteamShortcutsFile>($"Unexpected type in tags dict: 0x{(byte)type:X2}, skipping");
                continue;
            }

            // Read and discard the index key
            ReadNullTerminatedString(reader);
            // Read the tag value
            string tag = ReadNullTerminatedString(reader);
            tags.Add(tag);
        }

        return tags;
    }

    /// <summary>
    /// Skips a dictionary and all its contents.
    /// </summary>
    private static void SkipDictionary(BinaryReader reader)
    {
        while (true)
        {
            VdfType type = ReadVdfType(reader);
            if (type == VdfType.End)
            {
                break;
            }

            // Read and discard the key
            ReadNullTerminatedString(reader);

            switch (type)
            {
                case VdfType.Dictionary:
                    SkipDictionary(reader);
                    break;
                case VdfType.String:
                    ReadNullTerminatedString(reader);
                    break;
                case VdfType.Int32:
                    reader.ReadInt32();
                    break;
                default:
                    Logger.Warning<SteamShortcutsFile>($"Unknown type 0x{(byte)type:X2} while skipping dictionary");
                    break;
            }
        }
    }

    /// <summary>
    /// Assigns a string value to the appropriate shortcut property.
    /// </summary>
    private static void AssignString(SteamShortcut shortcut, string key, string val)
    {
        switch (key)
        {
            case "AppName":
                shortcut.AppName = val;
                break;
            case "Exe":
                shortcut.Exe = val;
                break;
            case "StartDir":
                shortcut.StartDir = val;
                break;
            case "icon":
                shortcut.Icon = val;
                break;
            case "ShortcutPath":
                shortcut.ShortcutPath = val;
                break;
            case "LaunchOptions":
                shortcut.LaunchOptions = val;
                break;
            case "FlatpakAppID":
                shortcut.FlatpakAppID = val;
                break;
            case "sortas":
                shortcut.SortAs = val;
                break;
            case "DevkitGameID":
                shortcut.DevkitGameID = val;
                break;
            default:
                Logger.Trace<SteamShortcutsFile>($"Unknown string key '{key}', ignoring");
                break;
        }
    }

    /// <summary>
    /// Assigns an int value to the appropriate shortcut property.
    /// </summary>
    private static void AssignInt(SteamShortcut shortcut, string key, int val)
    {
        switch (key)
        {
            case "appid":
                shortcut.AppId = BitConverter.GetBytes(val);
                break;
            case "IsHidden":
                shortcut.IsHidden = val != 0;
                break;
            case "AllowDesktopConfig":
                shortcut.AllowDesktopConfig = val != 0;
                break;
            case "AllowOverlay":
                shortcut.AllowOverlay = val != 0;
                break;
            case "OpenVR":
                shortcut.OpenVR = val != 0;
                break;
            case "Devkit":
                shortcut.Devkit = val != 0;
                break;
            case "DevkitOverrideAppID":
                shortcut.DevkitOverrideAppID = BitConverter.GetBytes(val);
                break;
            case "LastPlayTime":
                shortcut.LastPlayTime = BitConverter.GetBytes(val);
                break;
            default:
                Logger.Trace<SteamShortcutsFile>($"Unknown int key '{key}', ignoring");
                break;
        }
    }

    /// <summary>
    /// Reads a null-terminated string from the binary reader.
    /// </summary>
    private static string ReadNullTerminatedString(BinaryReader reader)
    {
        List<byte> bytes = [];
        while (true)
        {
            byte b = reader.ReadByte();
            if (b == '\0')
            {
                break;
            }
            bytes.Add(b);
        }
        return Encoding.UTF8.GetString(bytes.ToArray());
    }

    /// <summary>
    /// Reads a VDF type byte from the binary reader.
    /// </summary>
    private static VdfType ReadVdfType(BinaryReader reader)
    {
        return (VdfType)reader.ReadByte();
    }

    /// <summary>
    /// Saves the Steam shortcuts file to the specified path.
    /// </summary>
    /// <param name="filePath">Optional path to save to. If null, uses the original path.</param>
    /// <exception cref="InvalidOperationException">Thrown when no path is specified and the file was not loaded from a path.</exception>
    public void Save(string? filePath = null)
    {
        string savePath = filePath ?? FilePath!;

        if (string.IsNullOrEmpty(savePath))
        {
            Logger.Error<SteamShortcutsFile>("No save path specified and the file was not loaded from a path");
            throw new InvalidOperationException("No save path specified and the file was not loaded from a path");
        }

        Logger.Debug<SteamShortcutsFile>($"Saving Steam shortcuts file to {savePath}");

        try
        {
            byte[] content = ToBytes();

            // Ensure directory exists
            string? directory = Path.GetDirectoryName(savePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Logger.Info<SteamShortcutsFile>($"Creating directory: {directory}");
                Directory.CreateDirectory(directory);
            }

            File.WriteAllBytes(savePath, content);
            FilePath = savePath;
            Logger.Info<SteamShortcutsFile>($"Successfully saved Steam shortcuts file to {savePath} ({content.Length} bytes)");
        }
        catch (Exception ex)
        {
            Logger.Error<SteamShortcutsFile>($"Failed to save Steam shortcuts file to {savePath}: {ex.Message}");
            Logger.LogExceptionDetails<SteamShortcutsFile>(ex);
            throw;
        }
    }

    /// <summary>
    /// Converts the shortcuts to a binary VDF byte array.
    /// </summary>
    /// <returns>The binary VDF data.</returns>
    public byte[] ToBytes()
    {
        Logger.Trace<SteamShortcutsFile>("Converting Steam shortcuts to binary VDF");

        // Check for duplicate AppIds
        HashSet<string> seenAppIds = [];
        foreach (SteamShortcut shortcut in Shortcuts)
        {
            if (shortcut.AppId == null)
            {
                continue;
            }

            string appIdString = BitConverter.ToString(shortcut.AppId);
            if (!seenAppIds.Add(appIdString))
            {
                Logger.Error<SteamShortcutsFile>("Duplicate AppId detected, cannot save");
                throw new InvalidOperationException("Duplicate AppId detected. Each shortcut must have a unique AppId.");
            }
        }

        using MemoryStream ms = new MemoryStream();
        using BinaryWriter writer = new BinaryWriter(ms, Encoding.UTF8);

        // Write root: TYPE_NONE (0x00), "shortcuts", separator
        WriteVdfType(writer, VdfType.Dictionary);
        writer.Write(Encoding.UTF8.GetBytes("shortcuts"));
        WriteVdfType(writer, VdfType.Separator);

        // Write each shortcut
        for (int i = 0; i < Shortcuts.Count; i++)
        {
            WriteVdfType(writer, VdfType.Separator);
            writer.Write(Encoding.UTF8.GetBytes(i.ToString()));
            WriteVdfType(writer, VdfType.Separator);

            WriteShortcut(writer, Shortcuts[i]);
        }

        // Write end markers
        WriteVdfType(writer, VdfType.End); // End-of-shortcuts dictionary
        WriteVdfType(writer, VdfType.End); // End of root dictionary
        WriteVdfType(writer, VdfType.End); // Extra terminator per VDF spec

        byte[] result = ms.ToArray();
        Logger.Debug<SteamShortcutsFile>($"Generated binary VDF: {result.Length} bytes");
        return result;
    }

    /// <summary>
    /// Writes a single shortcut to the binary writer.
    /// </summary>
    private static void WriteShortcut(BinaryWriter writer, SteamShortcut shortcut)
    {
        // Write AppID: type (0x02), key, separator, then 4 raw bytes
        WriteVdfType(writer, VdfType.Int32);
        writer.Write(Encoding.ASCII.GetBytes("appid"));
        WriteVdfType(writer, VdfType.Separator);
        if (shortcut.AppId is { Length: 4 })
        {
            writer.Write(shortcut.AppId);
        }
        else
        {
            writer.Write([0, 0, 0, 0]);
        }

        WriteStringField(writer, "AppName", shortcut.AppName ?? "");
        WriteStringField(writer, "Exe", FixFormatting(shortcut.Exe, true));
        WriteStringField(writer, "StartDir", FixFormatting(shortcut.StartDir));
        WriteStringField(writer, "icon", FixFormatting(shortcut.Icon));
        WriteStringField(writer, "ShortcutPath", shortcut.ShortcutPath ?? "");
        WriteStringField(writer, "LaunchOptions", shortcut.LaunchOptions ?? "");
        WriteBoolField(writer, "IsHidden", shortcut.IsHidden);
        WriteBoolField(writer, "AllowDesktopConfig", shortcut.AllowDesktopConfig);
        WriteBoolField(writer, "AllowOverlay", shortcut.AllowOverlay);
        WriteBoolField(writer, "OpenVR", shortcut.OpenVR);
        WriteBoolField(writer, "Devkit", shortcut.Devkit);
        WriteStringField(writer, "DevkitGameID", shortcut.DevkitGameID ?? "");
        WriteIntField(writer, "DevkitOverrideAppID", GetIntFromBytes(shortcut.DevkitOverrideAppID ?? []));
        WriteIntField(writer, "LastPlayTime", GetIntFromBytes(shortcut.LastPlayTime ?? []));
        WriteStringField(writer, "FlatpakAppID", shortcut.FlatpakAppID ?? "");
        WriteStringField(writer, "sortas", shortcut.SortAs ?? "");
        WriteTags(writer, shortcut.Tags);

        // End of this shortcut dictionary
        WriteVdfType(writer, VdfType.End);
    }

    /// <summary>
    /// Writes a string field to the binary writer.
    /// </summary>
    private static void WriteStringField(BinaryWriter writer, string key, string value)
    {
        WriteVdfType(writer, VdfType.String);
        writer.Write(Encoding.ASCII.GetBytes(key));
        WriteVdfType(writer, VdfType.Separator);
        writer.Write(Encoding.UTF8.GetBytes(value));
        WriteVdfType(writer, VdfType.Separator);
    }

    /// <summary>
    /// Writes a boolean field (as int32) to the binary writer.
    /// </summary>
    private static void WriteBoolField(BinaryWriter writer, string key, bool value)
    {
        WriteVdfType(writer, VdfType.Int32);
        writer.Write(Encoding.ASCII.GetBytes(key));
        WriteVdfType(writer, VdfType.Separator);
        writer.Write(value ? 1 : 0);
    }

    /// <summary>
    /// Writes an int32 field to the binary writer.
    /// </summary>
    private static void WriteIntField(BinaryWriter writer, string key, int value)
    {
        WriteVdfType(writer, VdfType.Int32);
        writer.Write(Encoding.ASCII.GetBytes(key));
        WriteVdfType(writer, VdfType.Separator);
        writer.Write(value);
    }

    /// <summary>
    /// Gets an int from a byte array.
    /// </summary>
    private static int GetIntFromBytes(byte[]? bytes)
    {
        if (bytes == null || bytes.Length < 4)
        {
            return 0;
        }
        return BitConverter.ToInt32(bytes, 0);
    }

    /// <summary>
    /// Writes the tags dictionary to the binary writer.
    /// </summary>
    private static void WriteTags(BinaryWriter writer, List<string> tags)
    {
        WriteVdfType(writer, VdfType.Dictionary);
        writer.Write(Encoding.ASCII.GetBytes("tags"));
        WriteVdfType(writer, VdfType.Separator);

        for (int i = 0; i < tags.Count; i++)
        {
            WriteVdfType(writer, VdfType.String);
            writer.Write(Encoding.ASCII.GetBytes(i.ToString()));
            WriteVdfType(writer, VdfType.Separator);
            writer.Write(Encoding.UTF8.GetBytes(tags[i]));
            WriteVdfType(writer, VdfType.Separator);
        }

        WriteVdfType(writer, VdfType.End); // End-of-tags dictionary
    }

    /// <summary>
    /// Writes a VDF type byte to the binary writer.
    /// </summary>
    private static void WriteVdfType(BinaryWriter writer, VdfType type)
    {
        writer.Write((byte)type);
    }

    /// <summary>
    /// Adds a new shortcut to the file.
    /// </summary>
    /// <param name="shortcut">The shortcut to add.</param>
    public void AddShortcut(SteamShortcut shortcut)
    {
        Shortcuts.Add(shortcut);
        Logger.Info<SteamShortcutsFile>($"Added shortcut: {shortcut.AppName}");
    }

    /// <summary>
    /// Removes a shortcut by index.
    /// </summary>
    /// <param name="index">The index of the shortcut to remove.</param>
    /// <returns>True if the shortcut was found and removed, false otherwise.</returns>
    public bool RemoveShortcutAt(int index)
    {
        if (index < 0 || index >= Shortcuts.Count)
        {
            return false;
        }

        SteamShortcut shortcut = Shortcuts[index];
        Shortcuts.RemoveAt(index);
        Logger.Info<SteamShortcutsFile>($"Removed shortcut: {shortcut.AppName}");
        return true;
    }

    /// <summary>
    /// Removes a shortcut by AppId.
    /// </summary>
    /// <param name="appId">The AppId of the shortcut to remove.</param>
    /// <returns>True if the shortcut was found and removed, false otherwise.</returns>
    public bool RemoveShortcutByAppId(uint appId)
    {
        SteamShortcut? shortcut = Shortcuts.FirstOrDefault(s => s.GetAppIdAsUint() == appId);
        if (shortcut == null)
        {
            return false;
        }

        Shortcuts.Remove(shortcut);
        Logger.Info<SteamShortcutsFile>($"Removed shortcut: {shortcut.AppName}");
        return true;
    }

    /// <summary>
    /// Gets a shortcut by AppId.
    /// </summary>
    /// <param name="appId">The AppId to search for.</param>
    /// <returns>The shortcut if found, null otherwise.</returns>
    public SteamShortcut? GetShortcutByAppId(uint appId)
    {
        return Shortcuts.FirstOrDefault(s => s.GetAppIdAsUint() == appId);
    }

    /// <summary>
    /// Quotes a string if it contains spaces or if force is true, and fixes Windows slashes.
    /// </summary>
    /// <param name="rawString">The string to process.</param>
    /// <param name="forceQuote">If true, always quote the string regardless of content.</param>
    /// <returns>The quoted and fixed string.</returns>
    private static string FixFormatting(string? rawString, bool forceQuote = false)
    {
        rawString ??= "";
        if (OperatingSystem.IsWindows())
        {
            rawString = rawString.Replace('/', '\\');
        }

        // Don't add quotes if the string is already quoted
        if (rawString.StartsWith("\"") && rawString.EndsWith("\"") && rawString.Length >= 2)
        {
            return rawString;
        }

        if (forceQuote || rawString.Contains(' '))
        {
            return $"\"{rawString}\"";
        }

        return rawString;
    }

    /// <summary>
    /// VDF binary type codes.
    /// </summary>
    private enum VdfType : byte
    {
        Dictionary = 0x00,
        Separator = Dictionary, // For readability - used between key and value
        String = 0x01,
        Int32 = 0x02,
        End = 0x08
    }
}