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
        string? currentCommandTypeComment = null;
        bool isInPatchSection = false;

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            string trimmedLine = line.Trim();

            if (string.IsNullOrEmpty(trimmedLine))
            {
                continue;
            }

            // Special handling for [[patch.type]] - extract comment BEFORE stripping
            if (isInPatchSection && trimmedLine.StartsWith("[[patch.", StringComparison.OrdinalIgnoreCase))
            {
                // Extract command type from raw line
                Match typeMatch = Regex.Match(trimmedLine, @"^\[\[patch\.([^\]]+)\]\]", RegexOptions.IgnoreCase);
                if (typeMatch.Success)
                {
                    currentCommandType = typeMatch.Groups[1].Value;
                }

                // Note: This regex has TWO groups - group 1 is type, group 2 is comment
                Match inlineCommentMatch = Regex.Match(trimmedLine, @"^\[\[patch\.([^\]]+)\]\]\s*#\s*(.+)$", RegexOptions.IgnoreCase);
                if (inlineCommentMatch.Success)
                {
                    currentCommandTypeComment = inlineCommentMatch.Groups[2].Value.Trim();
                    Logger.Debug<PatchFile>($"Command type '{currentCommandType}' has inline comment: {currentCommandTypeComment}");
                }

                continue;
            }

            // Special handling for header fields - extract comments BEFORE stripping
            if (trimmedLine.StartsWith("title_name", StringComparison.OrdinalIgnoreCase))
            {
                ParseTitleNameWithComment(trimmedLine, patchFile);
                continue;
            }

            if (trimmedLine.StartsWith("title_id", StringComparison.OrdinalIgnoreCase))
            {
                ParseTitleIdWithComment(trimmedLine, patchFile);
                continue;
            }

            // Parse hash before stripping to preserve comments
            if (trimmedLine.StartsWith("hash", StringComparison.OrdinalIgnoreCase))
            {
                ParseHashValue(trimmedLine, lines, i, patchFile);
                continue;
            }

            if (trimmedLine.StartsWith("#media_id", StringComparison.OrdinalIgnoreCase) ||
                trimmedLine.StartsWith("media_id", StringComparison.OrdinalIgnoreCase))
            {
                ParseMediaIdArray(trimmedLine, lines, i, patchFile);
                continue;
            }

            // Strip inline comments for all other lines (preserve # inside quoted strings)
            string processedLine = StripInlineComment(trimmedLine);

            string lineToProcess = processedLine;

            if (lineToProcess.StartsWith("[[patch]]", StringComparison.OrdinalIgnoreCase))
            {
                // Capture [[patch]] header comment if present
                Match patchHeaderMatch = Regex.Match(lineToProcess, @"^\[\[patch\]\]\s*#\s*(.*)", RegexOptions.IgnoreCase);
                string patchHeaderComment = patchHeaderMatch.Success ? patchHeaderMatch.Groups[1].Value.Trim() : string.Empty;

                if (currentPatch != null && !string.IsNullOrEmpty(currentPatch.Name) && !string.IsNullOrEmpty(currentPatch.Author))
                {
                    patchFile.Document.Patches.Add(currentPatch);
                }
                currentPatch = new PatchEntry();
                currentCommandType = null;
                isInPatchSection = true;
                continue;
            }

            if (currentPatch != null && currentCommandType != null && lineToProcess.StartsWith("address", StringComparison.OrdinalIgnoreCase))
            {
                ParsePatchCommand(lines, i, currentPatch, currentCommandType, currentCommandTypeComment);
                currentCommandTypeComment = null;
                continue;
            }

            if (currentPatch != null && (lineToProcess.StartsWith("name", StringComparison.OrdinalIgnoreCase)))
            {
                Match m = Regex.Match(lineToProcess, @"^name\s*=\s*""([^""]*)""", RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    currentPatch.Name = m.Groups[1].Value;
                }
                continue;
            }

            if (currentPatch != null && (lineToProcess.StartsWith("author", StringComparison.OrdinalIgnoreCase)))
            {
                Match m = Regex.Match(lineToProcess, @"^author\s*=\s*""([^""]*)""", RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    currentPatch.Author = m.Groups[1].Value;
                }
                continue;
            }

            if (currentPatch != null && (lineToProcess.StartsWith("desc", StringComparison.OrdinalIgnoreCase) || lineToProcess.StartsWith("Desc", StringComparison.OrdinalIgnoreCase)))
            {
                Match m = Regex.Match(lineToProcess, @"^(desc|Desc)\s*=\s*""([^""]*)""", RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    currentPatch.Description = m.Groups[2].Value;
                }
                continue;
            }

            if (currentPatch != null && lineToProcess.StartsWith("is_enabled", StringComparison.OrdinalIgnoreCase))
            {
                Match m = Regex.Match(lineToProcess, @"^is_enabled\s*=\s*(true|false)", RegexOptions.IgnoreCase);
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
    /// Parses the title_name field from a line (with inline comment extraction).
    /// </summary>
    private static void ParseTitleNameWithComment(string rawLine, PatchFile patchFile)
    {
        // Extract comment first
        Match commentMatch = Regex.Match(rawLine, @"^title_name\s*=\s*""[^""]+""\s*#\s*(.+)$");
        string? comment = commentMatch.Success ? commentMatch.Groups[1].Value.Trim() : null;

        // Now strip comment and extract title name
        string strippedLine = StripInlineComment(rawLine);
        Match m = Regex.Match(strippedLine, @"^title_name\s*=\s*""([^""]+)""");
        if (m.Success)
        {
            patchFile.Document.TitleName = m.Groups[1].Value;
            patchFile.Document.TitleNameComment = comment ?? string.Empty;
            Logger.Debug<PatchFile>($"Title Name: {patchFile.Document.TitleName}");
        }
    }

    /// <summary>
    /// Parses the title_id field from a line (with inline comment extraction).
    /// </summary>
    private static void ParseTitleIdWithComment(string rawLine, PatchFile patchFile)
    {
        // Extract comment first
        Match commentMatch = Regex.Match(rawLine, @"^title_id\s*=\s*""[A-Fa-f0-9]+""\s*#\s*(.+)$");
        string? comment = commentMatch.Success ? commentMatch.Groups[1].Value.Trim() : null;

        // Now strip comment and extract title id
        string strippedLine = StripInlineComment(rawLine);
        Match m = Regex.Match(strippedLine, @"^title_id\s*=\s*""([A-Fa-f0-9]+)""");
        if (m.Success)
        {
            patchFile.Document.TitleId = m.Groups[1].Value.ToUpper();
            patchFile.Document.TitleIdComment = comment ?? string.Empty;
            Logger.Debug<PatchFile>($"Title ID: {patchFile.Document.TitleId}");
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
    /// Parses the hash field from the TOML content (handles raw line with comment).
    /// </summary>
    private static void ParseHashValue(string rawLine, string[] lines, int lineIndex, PatchFile patchFile)
    {
        // Single hash with optional comment
        Match m = Regex.Match(rawLine, @"^hash\s*=\s*""([A-Fa-f0-9]+)""\s*#\s*(.*)");
        if (m.Success)
        {
            patchFile.Document.Hashes.Add(m.Groups[1].Value.ToUpper());
            patchFile.Document.HashComment = m.Groups[2].Value.Trim();
            Logger.Debug<PatchFile>($"Hash: {patchFile.Document.Hashes[0]}");
            return;
        }

        // Single hash without comment
        m = Regex.Match(rawLine, @"^hash\s*=\s*""([A-Fa-f0-9]+)""");
        if (m.Success)
        {
            patchFile.Document.Hashes.Add(m.Groups[1].Value.ToUpper());
            Logger.Debug<PatchFile>($"Hash: {patchFile.Document.Hashes[0]}");
            return;
        }

        // Hash array format
        m = Regex.Match(rawLine, @"^hash\s*=\s*\[");
        if (m.Success)
        {
            for (int i = lineIndex + 1; i < lines.Length; i++)
            {
                string rawLineArray = lines[i];
                string trimmed = rawLineArray.Trim();

                if (trimmed == "]")
                {
                    break;
                }

                // Skip commented-out hash entries (lines starting with #)
                if (trimmed.StartsWith("#"))
                {
                    // Check if it's a commented hash entry for logging/marking disabled
                    Match commentedMatch = Regex.Match(trimmed, @"^#\s*""([A-Fa-f0-9]+)""\s*,?\s*(?:#\s*(.*))?$");
                    if (commentedMatch.Success)
                    {
                        Logger.Debug<PatchFile>($"Hash (disabled): {commentedMatch.Groups[1].Value}");
                    }
                    continue;
                }

                // Strip inline comment before extracting hash
                string processedLine = StripInlineComment(trimmed);
                m = Regex.Match(processedLine, @"""([A-Fa-f0-9]+)""");
                if (m.Success)
                {
                    patchFile.Document.Hashes.Add(m.Groups[1].Value.ToUpper());
                    Logger.Debug<PatchFile>($"Hash: {m.Groups[1].Value}");
                }
            }
        }
    }

    /// <summary>
    /// Parses the media_id field from the TOML content (handles raw line with comment).
    /// </summary>
    private static void ParseMediaIdArray(string rawLine, string[] lines, int lineIndex, PatchFile patchFile)
    {
        // The rawLine still has inline comment
        // First extract the comment from the header line if present
        string? headerComment = null;
        Match headerCommentMatch = Regex.Match(rawLine, @"^#media_id\s*=\s*""[A-Fa-f0-9]+""\s*#\s*(.+)$");
        if (headerCommentMatch.Success)
        {
            headerComment = headerCommentMatch.Groups[1].Value.Trim();
        }

        bool isCommented = rawLine.StartsWith("#");
        bool hasBracket = rawLine.Contains("[");

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
            // Single media_id (not commented) - use rawLine with comment
            Match m = Regex.Match(rawLine, @"^media_id\s*=\s*""([A-Fa-f0-9]+)""\s*#\s*(.*)");
            if (m.Success)
            {
                string mediaId = m.Groups[1].Value.ToUpper();
                string comment = m.Groups[2].Success ? m.Groups[2].Value.Trim() : (headerComment ?? string.Empty);
                patchFile.Document.MediaIds.Add(new MediaIdEntry(mediaId, comment, false));
                Logger.Debug<PatchFile>($"Media ID: {mediaId}");
                return;
            }
            m = Regex.Match(rawLine, @"^media_id\s*=\s*""([A-Fa-f0-9]+)""");
            if (m.Success)
            {
                string mediaId = m.Groups[1].Value.ToUpper();
                patchFile.Document.MediaIds.Add(new MediaIdEntry(mediaId, headerComment ?? string.Empty, false));
                Logger.Debug<PatchFile>($"Media ID: {mediaId}");
            }
        }
        else
        {
            // Single commented media_id - use rawLine with comment
            Match m = Regex.Match(rawLine, @"^#media_id\s*=\s*""([A-Fa-f0-9]+)""\s*#\s*(.*)");
            if (m.Success)
            {
                string mediaId = m.Groups[1].Value.ToUpper();
                string comment = m.Groups[2].Success ? m.Groups[2].Value.Trim() : (headerComment ?? string.Empty);
                patchFile.Document.MediaIds.Add(new MediaIdEntry(mediaId, comment, true));
                Logger.Debug<PatchFile>($"Media ID: {mediaId}");
                return;
            }
            m = Regex.Match(rawLine, @"^#media_id\s*=\s*""([A-Fa-f0-9]+)""");
            if (m.Success)
            {
                string mediaId = m.Groups[1].Value.ToUpper();
                patchFile.Document.MediaIds.Add(new MediaIdEntry(mediaId, headerComment ?? string.Empty, true));
                Logger.Debug<PatchFile>($"Media ID: {mediaId}");
            }
        }
    }

    /// <summary>
    /// Parses a patch command from the TOML content.
    /// </summary>
    private static void ParsePatchCommand(string[] lines, int lineIndex, PatchEntry patch, string commandType, string? typeComment)
    {
        string rawAddressLine = lines[lineIndex].Trim();

        // Extract address comment - more flexible regex to handle whitespace
        string? addressComment = null;
        Match addressCommentMatch = Regex.Match(rawAddressLine, @"address\s*=\s*0x[0-9a-fA-F]+\s*#\s*(.+?)\s*$");
        if (addressCommentMatch.Success)
        {
            addressComment = addressCommentMatch.Groups[1].Value.Trim();
        }

        // Strip inline comment from address line (preserve # inside quotes)
        string addressLine = StripInlineComment(rawAddressLine);

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

        string rawValueLine = lines[lineIndex + 1].Trim();

        // Extract value comment - more flexible regex to handle whitespace
        string? valueComment = null;
        Match valueCommentMatch = Regex.Match(rawValueLine, @"value\s*=\s*.+?\s*#\s*(.+?)\s*$");
        if (valueCommentMatch.Success)
        {
            valueComment = valueCommentMatch.Groups[1].Value.Trim();
        }

        // Strip inline comment from value line (preserve # inside quotes)
        string valueLine = StripInlineComment(rawValueLine);

        Match valueMatch = Regex.Match(valueLine, @"value\s*=\s*(.+)");
        if (!valueMatch.Success)
        {
            Logger.Warning<PatchFile>($"Failed to parse value at line {lineIndex + 2}");
            return;
        }

        string valueStr = valueMatch.Groups[1].Value.Trim().TrimEnd(',');
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
            Type = ParsePatchType(commandType),
            TypeComment = typeComment,
            AddressComment = addressComment,
            ValueComment = valueComment
        };

        patch.Commands.Add(cmd);
        Logger.Debug<PatchFile>($"Parsed {commandType} command: address=0x{address:x}, value={value}");
    }

    /// <summary>
    /// Strips inline TOML comments from a value string, preserving # inside quoted strings.
    /// Handles escaped quotes (\").
    /// </summary>
    private static string StripInlineComment(string valueStr)
    {
        bool inQuotes = false;
        bool escaped = false;

        for (int i = 0; i < valueStr.Length; i++)
        {
            char c = valueStr[i];

            if (escaped)
            {
                escaped = false;
                continue;
            }

            if (c == '\\')
            {
                escaped = true;
                continue;
            }

            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == '#' && !inQuotes)
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
        if (!string.IsNullOrEmpty(Document.TitleNameComment))
        {
            sb.AppendLine($"title_name = \"{Document.TitleName}\" # {Document.TitleNameComment}");
        }
        else
        {
            sb.AppendLine($"title_name = \"{Document.TitleName}\"");
        }

        if (!string.IsNullOrEmpty(Document.TitleIdComment))
        {
            sb.AppendLine($"title_id = \"{Document.TitleId.ToUpper()}\" # {Document.TitleIdComment}");
        }
        else
        {
            sb.AppendLine($"title_id = \"{Document.TitleId.ToUpper()}\"");
        }
        ;

        // Write hashes
        if (Document.Hashes.Count == 1)
        {
            if (!string.IsNullOrEmpty(Document.HashComment))
            {
                sb.AppendLine($"hash = \"{Document.Hashes[0].ToUpper()}\" # {Document.HashComment}");
            }
            else
            {
                sb.AppendLine($"hash = \"{Document.Hashes[0].ToUpper()}\"");
            }
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
                if (!string.IsNullOrEmpty(entry.Comment))
                {
                    sb.AppendLine($"#media_id = \"{entry.Id}\" # {entry.Comment}");
                }
                else
                {
                    sb.AppendLine($"#media_id = \"{entry.Id}\"");
                }
            }
            else
            {
                sb.AppendLine("#media_id = [");
                foreach (MediaIdEntry entry in Document.MediaIds)
                {
                    if (!string.IsNullOrEmpty(entry.Comment))
                    {
                        sb.AppendLine($"#    \"{entry.Id}\" # {entry.Comment}");
                    }
                    else
                    {
                        sb.AppendLine($"#    \"{entry.Id}\"");
                    }
                }
                sb.AppendLine("#]");
            }
        }

        // Empty line between header (and media_ids if present) and patches
        sb.AppendLine();

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
                bool hasTypeComment = !string.IsNullOrWhiteSpace(command.TypeComment);
                if (hasTypeComment)
                {
                    sb.AppendLine($"    [[patch.{typeString}]] # {command.TypeComment}");
                }
                else
                {
                    sb.AppendLine($"    [[patch.{typeString}]]");
                }

                bool hasAddressComment = !string.IsNullOrWhiteSpace(command.AddressComment);
                if (hasAddressComment)
                {
                    sb.AppendLine($"        address = 0x{command.Address:x} # {command.AddressComment}");
                }
                else
                {
                    sb.AppendLine($"        address = 0x{command.Address:x}");
                }

                string valueStr = command.GetValueAsString() ?? "0x00";
                bool hasValueComment = !string.IsNullOrWhiteSpace(command.ValueComment);
                if (hasValueComment)
                {
                    sb.AppendLine($"        value = {valueStr} # {command.ValueComment}");
                }
                else
                {
                    sb.AppendLine($"        value = {valueStr}");
                }
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