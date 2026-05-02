using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Models.Files.Patches;

namespace XeniaManager.Core.Files;

/// <summary>
/// Handles loading, saving, and manipulation of Xenia game patch files (.patch.toml).
/// Patch files use the TOML format and contain game metadata and patch commands.
/// <summary>
public class PatchFile
{
    /// <summary>
    /// The patch document containing all metadata and patch entries.
    /// </summary>
    public PatchDocument Document { get; private set; }

    /// <summary>
    /// The file path of this patch file.
    /// </summary>
    public string? FilePath { get; private set; }

    /// <summary>
    /// Gets the title name from the document.
    /// </summary>
    public string TitleName => Document.TitleName;

    /// <summary>
    /// Gets the title ID from the document.
    /// </summary>
    public string TitleId => Document.TitleId;

    /// <summary>
    /// Gets the list of module hashes from the document.
    /// </summary>
    public IReadOnlyList<string> Hashes => Document.Hashes.AsReadOnly();

    /// <summary>
    /// Gets the list of media IDs from the document.
    /// </summary>
    public IReadOnlyList<MediaIdEntry> MediaIds => Document.MediaIds.AsReadOnly();

    /// <summary>
    /// Gets all patch entries in this file.
    /// </summary>
    public IReadOnlyList<PatchEntry> Patches => Document.Patches.AsReadOnly();

    /// <summary>
    /// Gets only the enabled patch entries.
    /// </summary>
    public IEnumerable<PatchEntry> EnabledPatches => Document.Patches.Where(p => p.IsEnabled);

    /// <summary>
    /// Private constructor to enforce factory methods.
    /// </summary>
    private PatchFile()
    {
        Document = new PatchDocument();
    }

    /// <summary>
    /// Creates a new empty patch file.
    /// </summary>
    /// <param name="titleName">The name of the game title.</param>
    /// <param name="titleId">The title ID in uppercase hex.</param>
    /// <param name="hash">The primary module hash in uppercase hex.</param>
    /// <param name="mediaIds">Optional list of media IDs.</param>
    /// <returns>A new PatchFile instance.</returns>
    public static PatchFile Create(string titleName, string titleId, string hash, List<string>? mediaIds = null)
    {
        Logger.Info<PatchFile>($"Creating new patch file for: {titleName} (Title ID: {titleId}, Hash: {hash})");

        PatchFile patchFile = new PatchFile
        {
            Document = new PatchDocument(titleName, titleId, hash, mediaIds)
        };

        Logger.Trace<PatchFile>("PatchFile created successfully");
        return patchFile;
    }

    /// <summary>
    /// Loads a patch file from the specified path.
    /// </summary>
    /// <param name="filePath">The path to the .patch.toml file to load.</param>
    /// <returns>A new PatchFile instance.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
    /// <exception cref="ArgumentException">Thrown when the file has an invalid format.</exception>
    public static PatchFile Load(string filePath)
    {
        Logger.Debug<PatchFile>($"Loading patch file from {filePath}");

        if (!File.Exists(filePath))
        {
            Logger.Error<PatchFile>($"Patch file does not exist: {filePath}");
            throw new FileNotFoundException($"Patch file does not exist at {filePath}", filePath);
        }

        string content = File.ReadAllText(filePath);
        Logger.Info<PatchFile>($"Loaded patch file: {filePath} ({content.Length} bytes)");

        PatchFile patchFile = FromString(content);
        patchFile.FilePath = filePath;

        return patchFile;
    }

    /// <summary>
    /// Parses a patch file from a TOML string.
    /// </summary>
    /// <param name="content">The TOML content of the patch file.</param>
    /// <returns>A new PatchFile instance.</returns>
    /// <exception cref="ArgumentException">Thrown when the TOML content is invalid.</exception>
    public static PatchFile FromString(string content)
    {
        Logger.Trace<PatchFile>($"Parsing patch file from TOML string ({content.Length} bytes)");

        if (content == null)
        {
            throw new ArgumentException("Content cannot be null.", nameof(content));
        }

        string trimmed = content.Trim();
        if (trimmed.Contains("[[[[") || trimmed.Contains("]]]"))
        {
            throw new ArgumentException("Invalid TOML content.", nameof(content));
        }

        try
        {
            PatchFile patchFile = new PatchFile();
            ParseContent(content, patchFile);
            Logger.Info<PatchFile>($"Successfully parsed patch file with {patchFile.Document.Patches.Count} patch entries");
            return patchFile;
        }
        catch (Exception ex)
        {
            Logger.Error<PatchFile>($"Failed to parse patch file");
            Logger.LogExceptionDetails<PatchFile>(ex);
            throw new ArgumentException($"Invalid TOML content.", nameof(content), ex);
        }
    }

    /// <summary>
    /// Parses the TOML content and populates the patch document.
    /// </summary>
    private static void ParseContent(string content, PatchFile patchFile)
    {
        string[] lines = content.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
        PatchEntry? currentPatch = null;
        string? currentCommandType = null;
        bool isInPatchSection = false;

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            string trimmedLine = line.Trim();

            // Skip empty lines and pure comment lines in most contexts
            // Exception: #media_id lines need to be processed
            if (string.IsNullOrEmpty(trimmedLine) || (trimmedLine.StartsWith("#") && !trimmedLine.StartsWith("#media_id")))
            {
                continue;
            }

            if (trimmedLine.StartsWith("title_name"))
            {
                ParseTitleName(trimmedLine, patchFile);
                continue;
            }

            if (trimmedLine.StartsWith("title_id"))
            {
                ParseTitleId(trimmedLine, patchFile);
                continue;
            }

            if (trimmedLine.StartsWith("hash"))
            {
                ParseHashValue(trimmedLine, lines, i, patchFile);
                continue;
            }

            if (trimmedLine.StartsWith("#media_id") || trimmedLine.StartsWith("media_id"))
            {
                ParseMediaIdArray(trimmedLine, lines, i, patchFile);
                continue;
            }

            if (trimmedLine.StartsWith("[[patch]]"))
            {
                if (currentPatch != null && !string.IsNullOrEmpty(currentPatch.Name) && !string.IsNullOrEmpty(currentPatch.Author))
                {
                    patchFile.Document.Patches.Add(currentPatch);
                }
                currentPatch = new PatchEntry();
                currentCommandType = null;
                isInPatchSection = true;
                continue;
            }

            if (isInPatchSection && currentPatch != null && trimmedLine.StartsWith("[[patch."))
            {
                Match m = Regex.Match(trimmedLine, @"^\[\[patch\.([^\]]+)\]\]");
                if (m.Success)
                {
                    currentCommandType = m.Groups[1].Value;
                }
                continue;
            }

            if (currentPatch != null && currentCommandType != null && trimmedLine.StartsWith("address"))
            {
                ParsePatchCommand(lines, i, currentPatch, currentCommandType);
                continue;
            }

            if (currentPatch != null && trimmedLine.StartsWith("name"))
            {
                Match m = Regex.Match(trimmedLine, @"^name\s*=\s*""([^""]*)""");
                if (m.Success)
                {
                    currentPatch.Name = m.Groups[1].Value;
                }
                continue;
            }

            if (currentPatch != null && trimmedLine.StartsWith("author"))
            {
                Match m = Regex.Match(trimmedLine, @"^author\s*=\s*""([^""]*)""");
                if (m.Success)
                {
                    currentPatch.Author = m.Groups[1].Value;
                }
                continue;
            }

            if (currentPatch != null && trimmedLine.StartsWith("desc"))
            {
                Match m = Regex.Match(trimmedLine, @"^desc\s*=\s*""([^""]*)""");
                if (m.Success)
                {
                    currentPatch.Description = m.Groups[1].Value;
                }
                continue;
            }

            if (currentPatch != null && trimmedLine.StartsWith("is_enabled"))
            {
                Match m = Regex.Match(trimmedLine, @"^is_enabled\s*=\s*(true|false)", RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    currentPatch.IsEnabled = m.Groups[1].Value.Equals("true", StringComparison.OrdinalIgnoreCase);
                }
                continue;
            }
        }

        if (currentPatch != null && !string.IsNullOrEmpty(currentPatch.Name) && !string.IsNullOrEmpty(currentPatch.Author))
        {
            patchFile.Document.Patches.Add(currentPatch);
        }

        foreach (PatchEntry patch in patchFile.Document.Patches)
        {
            if (patch.Commands.Count == 0)
            {
                Logger.Warning<PatchFile>($"Skipping patch entry '{patch.Name}' with no valid commands");
                continue;
            }
            Logger.Debug<PatchFile>($"Parsed patch: {patch.Name} by {patch.Author} ({patch.Commands.Count} commands)");
        }
    }

    /// <summary>
    /// Parses the title_name field from a line.
    /// </summary>
    private static void ParseTitleName(string line, PatchFile patchFile)
    {
        Match m = Regex.Match(line, @"^title_name\s*=\s*""([^""]+)""\s*#\s*(.*)");
        if (m.Success)
        {
            patchFile.Document.TitleName = m.Groups[1].Value;
            patchFile.Document.TitleNameComment = m.Groups[2].Value.Trim();
            Logger.Debug<PatchFile>($"Title Name: {patchFile.Document.TitleName}");
            return;
        }

        m = Regex.Match(line, @"^title_name\s*=\s*""([^""]*)""");
        if (m.Success)
        {
            patchFile.Document.TitleName = m.Groups[1].Value;
            Logger.Debug<PatchFile>($"Title Name: {patchFile.Document.TitleName}");
        }
    }

    /// <summary>
    /// Parses the title_id field from a line.
    /// </summary>
    private static void ParseTitleId(string line, PatchFile patchFile)
    {
        Match m = Regex.Match(line, @"^title_id\s*=\s*""([A-Fa-f0-9]+)""\s*#\s*(.*)");
        if (m.Success)
        {
            patchFile.Document.TitleId = m.Groups[1].Value.ToUpper();
            patchFile.Document.TitleIdComment = m.Groups[2].Value.Trim();
            Logger.Debug<PatchFile>($"Title ID: {patchFile.Document.TitleId}");
            return;
        }

        m = Regex.Match(line, @"^title_id\s*=\s*""([A-Fa-f0-9]+)""");
        if (m.Success)
        {
            patchFile.Document.TitleId = m.Groups[1].Value.ToUpper();
            Logger.Debug<PatchFile>($"Title ID: {patchFile.Document.TitleId}");
        }
    }

    /// <summary>
    /// Parses the hash field from the TOML content.
    /// </summary>
    private static void ParseHashValue(string line, string[] lines, int lineIndex, PatchFile patchFile)
    {
        // Single hash with optional comment
        Match m = Regex.Match(line, @"^hash\s*=\s*""([A-Fa-f0-9]+)""\s*#\s*(.*)");
        if (m.Success)
        {
            patchFile.Document.Hashes.Add(m.Groups[1].Value.ToUpper());
            patchFile.Document.HashComment = m.Groups[2].Value.Trim();
            Logger.Debug<PatchFile>($"Hash: {patchFile.Document.Hashes[0]}");
            return;
        }

        // Single hash without comment
        m = Regex.Match(line, @"^hash\s*=\s*""([A-Fa-f0-9]+)""");
        if (m.Success)
        {
            patchFile.Document.Hashes.Add(m.Groups[1].Value.ToUpper());
            Logger.Debug<PatchFile>($"Hash: {patchFile.Document.Hashes[0]}");
            return;
        }

        // Hash array format
        m = Regex.Match(line, @"^hash\s*=\s*\[");
        if (m.Success)
        {
            for (int i = lineIndex + 1; i < lines.Length; i++)
            {
                string trimmed = lines[i].Trim();
                if (trimmed == "]")
                {
                    break;
                }
                // Extract hash from array line (handles comments)
                m = Regex.Match(trimmed, @"""([A-Fa-f0-9]+)""");
                if (m.Success)
                {
                    patchFile.Document.Hashes.Add(m.Groups[1].Value.ToUpper());
                    Logger.Debug<PatchFile>($"Hash: {m.Groups[1].Value}");
                }
            }
        }
    }

    /// <summary>
    /// Parses the media_id field from the TOML content.
    /// </summary>
    private static void ParseMediaIdArray(string line, string[] lines, int lineIndex, PatchFile patchFile)
    {
        bool isCommented = line.StartsWith("#");
        bool hasBracket = line.Contains("[");

        if (hasBracket)
        {
            // Array format (commented or not)
            for (int i = lineIndex + 1; i < lines.Length; i++)
            {
                string trimmed = lines[i].Trim();

                // Check for end of array (either #] or ])
                if (trimmed == "#]" || trimmed == "]")
                {
                    break;
                }

                if (isCommented)
                {
                    // Parse commented media ID with optional comment
                    // Format: # "2B7A1346", # Disc (Europe, Asia): http://redump.org/disc/84331
                    Match m = Regex.Match(trimmed, @"^#\s*""([A-Fa-f0-9]+)""\s*,?\s*(?:#\s*(.*))?$");
                    if (m.Success)
                    {
                        string mediaId = m.Groups[1].Value.ToUpper();
                        string comment = m.Groups[2].Success ? m.Groups[2].Value.Trim() : string.Empty;
                        patchFile.Document.MediaIds.Add(new MediaIdEntry(mediaId, comment, true));
                        Logger.Debug<PatchFile>($"Media ID: {mediaId} (commented)");
                    }
                    else
                    {
                        Logger.Warning<PatchFile>($"Failed to parse commented media ID line: {trimmed}");
                    }
                }
                else
                {
                    // Parse uncommented media ID
                    Match m = Regex.Match(trimmed, @"""([A-Fa-f0-9]+)""\s*,?");
                    if (m.Success)
                    {
                        string mediaId = m.Groups[1].Value.ToUpper();
                        patchFile.Document.MediaIds.Add(new MediaIdEntry(mediaId, string.Empty, false));
                        Logger.Debug<PatchFile>($"Media ID: {mediaId}");
                    }
                }
            }
        }
        else if (!isCommented)
        {
            // Single media_id (not commented)
            Match m = Regex.Match(line, @"^media_id\s*=\s*""([A-Fa-f0-9]+)""\s*#\s*(.*)");
            if (m.Success)
            {
                string mediaId = m.Groups[1].Value.ToUpper();
                string comment = m.Groups[2].Value.Trim();
                patchFile.Document.MediaIds.Add(new MediaIdEntry(mediaId, comment, false));
                Logger.Debug<PatchFile>($"Media ID: {mediaId}");
                return;
            }
            m = Regex.Match(line, @"^media_id\s*=\s*""([A-Fa-f0-9]+)""");
            if (m.Success)
            {
                string mediaId = m.Groups[1].Value.ToUpper();
                patchFile.Document.MediaIds.Add(new MediaIdEntry(mediaId, string.Empty, false));
                Logger.Debug<PatchFile>($"Media ID: {mediaId}");
            }
        }
        else
        {
            // Single commented media_id
            Match m = Regex.Match(line, @"#media_id\s*=\s*""([A-Fa-f0-9]+)""\s*#\s*(.*)");
            if (m.Success)
            {
                string mediaId = m.Groups[1].Value.ToUpper();
                string comment = m.Groups[2].Value.Trim();
                patchFile.Document.MediaIds.Add(new MediaIdEntry(mediaId, comment, true));
                Logger.Debug<PatchFile>($"Media ID: {mediaId}");
                return;
            }
            m = Regex.Match(line, @"#media_id\s*=\s*""([A-Fa-f0-9]+)""");
            if (m.Success)
            {
                string mediaId = m.Groups[1].Value.ToUpper();
                patchFile.Document.MediaIds.Add(new MediaIdEntry(mediaId, string.Empty, true));
                Logger.Debug<PatchFile>($"Media ID: {mediaId}");
            }
        }
    }

    /// <summary>
    /// Parses a patch command from the TOML content.
    /// </summary>
    private static void ParsePatchCommand(string[] lines, int lineIndex, PatchEntry patch, string commandType)
    {
        string addressLine = lines[lineIndex].Trim();
        Match addressMatch = Regex.Match(addressLine, @"address\s*=\s*0x([0-9a-fA-F]+)");
        if (!addressMatch.Success)
        {
            Logger.Warning<PatchFile>($"Failed to parse address at line {lineIndex + 1}");
            return;
        }

        ulong address = Convert.ToUInt64(addressMatch.Groups[1].Value, 16);

        if (lineIndex >= lines.Length - 1)
        {
            Logger.Warning<PatchFile>($"No value line found after address at line {lineIndex + 1}");
            return;
        }

        string valueLine = lines[lineIndex + 1].Trim();
        Match valueMatch = Regex.Match(valueLine, @"value\s*=\s*(.+)");
        if (!valueMatch.Success)
        {
            Logger.Warning<PatchFile>($"Failed to parse value at line {lineIndex + 2}");
            return;
        }

        string valueStr = valueMatch.Groups[1].Value.Trim().TrimEnd(',');
        valueStr = StripInlineComment(valueStr);
        object? value = ParseValue(valueStr, commandType);
        if (value == null)
        {
            Logger.Warning<PatchFile>($"Failed to parse value '{valueStr}' for type {commandType} at line {lineIndex + 2}");
            return;
        }

        PatchCommand cmd = new PatchCommand
        {
            Address = address,
            Value = value,
            Type = ParsePatchType(commandType)
        };

        patch.Commands.Add(cmd);
        Logger.Debug<PatchFile>($"Parsed {commandType} command: address=0x{address:x}, value={value}");
    }

    /// <summary>
    /// Strips inline TOML comments from a value string, preserving # inside quoted strings.
    /// </summary>
    private static string StripInlineComment(string valueStr)
    {
        bool inQuotes = false;
        for (int i = 0; i < valueStr.Length; i++)
        {
            if (valueStr[i] == '"' && (i == 0 || valueStr[i - 1] != '\\'))
            {
                inQuotes = !inQuotes;
            }
            else if (valueStr[i] == '#' && !inQuotes)
            {
                return valueStr.Substring(0, i).TrimEnd();
            }
        }
        return valueStr;
    }

    /// <summary>
    /// Parses a value string based on the patch type.
    /// </summary>
    private static object? ParseValue(string valueStr, string patchType)
    {
        valueStr = valueStr.Trim();
        string typeLower = patchType.ToLower();

        // String types - remove quotes
        if (typeLower == "string" || typeLower == "u16string")
        {
            if (valueStr.StartsWith("\"") && valueStr.EndsWith("\""))
            {
                valueStr = valueStr.Substring(1, valueStr.Length - 2);
            }
            return valueStr;
        }

        // Hex number types
        if (typeLower is "be8" or "be16" or "be32" or "be64")
        {
            string hexValue = valueStr.Replace("0x", "").Replace("0X", "");
            try
            {
                return typeLower switch
                {
                    "be8" => Convert.ToByte(hexValue, 16),
                    "be16" => Convert.ToUInt16(hexValue, 16),
                    "be32" => Convert.ToUInt32(hexValue, 16),
                    "be64" => Convert.ToUInt64(hexValue, 16),
                    _ => null
                };
            }
            catch (Exception ex)
            {
                Logger.Warning<PatchFile>($"Failed to parse hex value '{valueStr}' for type {typeLower}: {ex.Message}");
                return null;
            }
        }

        // Float types
        if (typeLower is "f32" or "f64")
        {
            try
            {
                // Handle hex float format (0x...)
                if (valueStr.StartsWith("0x") || valueStr.StartsWith("0X"))
                {
                    // TODO: For floats in hex format, we might need special handling
                }
                return typeLower switch
                {
                    "f32" => (object)float.Parse(valueStr, CultureInfo.InvariantCulture),
                    "f64" => double.Parse(valueStr, CultureInfo.InvariantCulture),
                    _ => throw new ArgumentOutOfRangeException()
                };
            }
            catch (Exception ex)
            {
                Logger.Warning<PatchFile>($"Failed to parse float value '{valueStr}' for type {typeLower}: {ex.Message}");
                return null;
            }
        }

        // Array type
        if (typeLower == "array")
        {
            return ParseArrayValue(valueStr);
        }

        Logger.Warning<PatchFile>($"Unknown patch type: {typeLower}");
        return null;
    }

    /// <summary>
    /// Parses an array value (hex string like "0x##*").
    /// </summary>
    private static byte[]? ParseArrayValue(string valueStr)
    {
        string testStr = valueStr;
        if (testStr.StartsWith("\"") && testStr.EndsWith("\""))
        {
            testStr = testStr.Substring(1, testStr.Length - 2);
        }

        if (!testStr.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            Logger.Warning<PatchFile>($"Array value must start with 0x: {valueStr}");
            return null;
        }

        string hex = testStr.Substring(2);
        if (hex.Length % 2 != 0)
        {
            Logger.Warning<PatchFile>($"Array hex length must be even: {hex}");
            return null;
        }

        int byteCount = hex.Length / 2;
        byte[] bytes = new byte[byteCount];

        for (int i = 0; i < byteCount; i++)
        {
            if (!byte.TryParse(hex.Substring(i * 2, 2), NumberStyles.HexNumber, null, out bytes[i]))
            {
                Logger.Warning<PatchFile>($"Failed to parse array byte at position {i}: {hex.Substring(i * 2, 2)}");
                return null;
            }
        }

        return bytes;
    }

    /// <summary>
    /// Parses a patch type string to PatchType enum.
    /// </summary>
    private static PatchType ParsePatchType(string typeString)
    {
        return typeString.ToLower() switch
        {
            "be8" => PatchType.Be8,
            "be16" => PatchType.Be16,
            "be32" => PatchType.Be32,
            "be64" => PatchType.Be64,
            "array" => PatchType.Array,
            "f32" => PatchType.F32,
            "f64" => PatchType.F64,
            "string" => PatchType.String,
            "u16string" => PatchType.U16String,
            _ => PatchType.Unknown
        };
    }

    /// <summary>
    /// Saves the patch file to the specified path.
    /// If no path is specified, saves to the original loaded path.
    /// </summary>
    /// <param name="filePath">Optional path to save to. If null, uses the original path.</param>
    /// <exception cref="InvalidOperationException">Thrown when no path is specified and the file was not loaded from a path.</exception>
    /// <exception cref="ArgumentException">Thrown when the file name does not follow the naming convention.</exception>
    public void Save(string? filePath = null)
    {
        string savePath = filePath ?? FilePath!;

        if (string.IsNullOrEmpty(savePath))
        {
            Logger.Error<PatchFile>("No save path specified and the file was not loaded from a path");
            throw new InvalidOperationException("No save path specified and the file was not loaded from a path");
        }

        Logger.Debug<PatchFile>($"Saving patch file to {savePath}");

        try
        {
            ValidateFileName(Path.GetFileName(savePath));
            string content = ToTomlString();

            string? directory = Path.GetDirectoryName(savePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Logger.Info<PatchFile>($"Creating directory: {directory}");
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(savePath, content);
            FilePath = savePath;
            Logger.Info<PatchFile>($"Successfully saved patch file to {savePath} ({content.Length} bytes)");
        }
        catch (Exception ex)
        {
            Logger.Error<PatchFile>($"Failed to save patch file to {savePath}: {ex.Message}");
            Logger.LogExceptionDetails<PatchFile>(ex);
            throw;
        }
    }

    /// <summary>
    /// Converts the patch document to a TOML string.
    /// </summary>
    /// <returns>The TOML string representation of the patch file.</returns>
    public string ToTomlString()
    {
        Logger.Trace<PatchFile>("Converting patch document to TOML string");

        StringBuilder sb = new StringBuilder();

        // Write header
        string titleNameComment = !string.IsNullOrEmpty(Document.TitleNameComment)
            ? $"    # {Document.TitleNameComment}"
            : string.Empty;
        sb.AppendLine($"title_name = \"{Document.TitleName}\"{titleNameComment}");

        string titleIdComment = !string.IsNullOrEmpty(Document.TitleIdComment)
            ? $"    # {Document.TitleIdComment}"
            : string.Empty;
        sb.AppendLine($"title_id = \"{Document.TitleId.ToUpper()}\"{titleIdComment}");

        // Write hashes
        if (Document.Hashes.Count == 1)
        {
            string hashComment = !string.IsNullOrEmpty(Document.HashComment)
                ? $"    # {Document.HashComment}"
                : string.Empty;
            sb.AppendLine($"hash = \"{Document.Hashes[0].ToUpper()}\"{hashComment}");
        }
        else
        {
            sb.AppendLine("hash = [");
            foreach (string hash in Document.Hashes)
            {
                sb.AppendLine($"    \"{hash.ToUpper()}\"");
            }
            sb.AppendLine("]");
        }

        // Write media IDs
        if (Document.MediaIds is { Count: > 0 })
        {
            if (Document.MediaIds.Count == 1)
            {
                MediaIdEntry entry = Document.MediaIds[0];
                string comment = !string.IsNullOrEmpty(entry.Comment) ? entry.Comment : string.Empty;
                sb.AppendLine($"#media_id = \"{entry.Id}\"    # {comment}");
            }
            else
            {
                sb.AppendLine("#media_id = [");
                foreach (MediaIdEntry entry in Document.MediaIds)
                {
                    string commentSuffix = !string.IsNullOrEmpty(entry.Comment) ? $" # {entry.Comment}" : string.Empty;
                    sb.AppendLine($"#    \"{entry.Id}\"{commentSuffix}");
                }
                sb.AppendLine("#]");
            }
        }

        // Write patches
        foreach (PatchEntry patch in Document.Patches)
        {
            sb.AppendLine("[[patch]]");
            sb.AppendLine($"    name = \"{patch.Name}\"");

            if (!string.IsNullOrEmpty(patch.Description))
            {
                sb.AppendLine($"    desc = \"{patch.Description}\"");
            }

            sb.AppendLine($"    author = \"{patch.Author}\"");
            sb.AppendLine($"    is_enabled = {patch.IsEnabled.ToString().ToLower()}");
            sb.AppendLine();

            // Write commands in original order
            foreach (PatchCommand command in patch.Commands)
            {
                string typeString = GetPatchTypeString(command.Type);
                sb.AppendLine($"    [[patch.{typeString}]]");
                sb.AppendLine($"        address = 0x{command.Address:x}");

                string valueStr = command.GetValueAsString() ?? "0x00";
                sb.AppendLine($"        value = {valueStr}");
            }

            sb.AppendLine();
        }

        string result = sb.ToString();
        Logger.Debug<PatchFile>($"Generated TOML string: {result.Length} bytes");
        return result;
    }

    /// <summary>
    /// Gets the TOML type string for a patch type.
    /// </summary>
    private static string GetPatchTypeString(PatchType type)
    {
        return type switch
        {
            PatchType.Be8 => "be8",
            PatchType.Be16 => "be16",
            PatchType.Be32 => "be32",
            PatchType.Be64 => "be64",
            PatchType.Array => "array",
            PatchType.F32 => "f32",
            PatchType.F64 => "f64",
            PatchType.String => "string",
            PatchType.U16String => "u16string",
            _ => "be32"
        };
    }

    /// <summary>
    /// Validates that the file name follows the naming convention.
    /// Format: "Title ID - Game Title.patch.toml"
    /// </summary>
    /// <param name="fileName">The file name to validate.</param>
    /// <exception cref="ArgumentException">Thrown when the file name does not follow the convention.</exception>
    private static void ValidateFileName(string fileName)
    {
        string pattern = @"^[A-F0-9]{8} - .+\.patch\.toml$";

        if (!Regex.IsMatch(fileName, pattern))
        {
            Logger.Error<PatchFile>($"Invalid file name format: {fileName}. Expected format: 'Title ID - Game Title.patch.toml'");
            throw new ArgumentException(
                $"Invalid file name format: {fileName}. Expected format: 'Title ID - Game Title.patch.toml' (e.g., '4D5307DF - Blue Dragon.patch.toml')",
                nameof(fileName));
        }

        Logger.Debug<PatchFile>($"File name validated: {fileName}");
    }

    /// <summary>
    /// Adds a new patch entry to this file.
    /// </summary>
    /// <param name="name">The name of the patch.</param>
    /// <param name="author">The author of the patch.</param>
    /// <param name="isEnabled">Whether the patch is enabled.</param>
    /// <param name="description">Optional description of the patch.</param>
    /// <returns>The created PatchEntry for further modification.</returns>
    public PatchEntry AddPatch(string name, string author, bool isEnabled = false, string? description = null)
    {
        Logger.Info<PatchFile>($"Adding new patch: {name} by {author}");
        PatchEntry patch = Document.AddPatch(name, author, isEnabled, description);
        Logger.Debug<PatchFile>($"Patch added successfully with {patch.Commands.Count} commands");
        return patch;
    }

    /// <summary>
    /// Removes a patch entry by name.
    /// </summary>
    /// <param name="name">The name of the patch to remove.</param>
    /// <returns>True if the patch was found and removed, false otherwise.</returns>
    public bool RemovePatch(string name)
    {
        Logger.Info<PatchFile>($"Removing patch: {name}");
        PatchEntry? patch = Document.Patches.FirstOrDefault(p => p.Name == name);

        if (patch == null)
        {
            Logger.Warning<PatchFile>($"Patch not found: {name}");
            return false;
        }

        Document.Patches.Remove(patch);
        Logger.Info<PatchFile>($"Successfully removed patch: {name}");
        return true;
    }

    /// <summary>
    /// Gets a patch entry by name.
    /// </summary>
    /// <param name="name">The name of the patch to find.</param>
    /// <returns>The PatchEntry if found, null otherwise.</returns>
    public PatchEntry? GetPatch(string name)
    {
        return Document.Patches.FirstOrDefault(p => p.Name == name);
    }
}