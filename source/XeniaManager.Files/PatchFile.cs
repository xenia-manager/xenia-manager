using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using XeniaManager.Logging;
using XeniaManager.Files.Models.Patches;
using XeniaManager.Files.Utilities;

namespace XeniaManager.Files;

/// <summary>
/// Handles loading, saving, and manipulation of Xenia game patch files (.patch.toml).
/// Patch files use the TOML format and contain game metadata and patch commands.
/// </summary>
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
    /// Original TOML content used to preserve formatting when only patch
    /// enable states change.
    /// </summary>
    private string? _sourceContent;

    /// <summary>
    /// Fingerprint of <see cref="Document"/> excluding enable states.
    /// </summary>
    private string? _sourceFingerprint;

    /// <summary>
    /// Whether the source file used a UTF-8 byte order mark.
    /// </summary>
    private bool _hasUtf8Bom;

    /// <summary>
    /// Gets the title name from the document.
    /// </summary>
    public string TitleName
    {
        get
        {
            return Document.TitleName;
        }
    }

    /// <summary>
    /// Gets the title ID from the document.
    /// </summary>
    public string TitleId
    {
        get
        {
            return Document.TitleId;
        }
    }

    /// <summary>
    /// Gets the list of module hashes from the document.
    /// </summary>
    public IReadOnlyList<string> Hashes
    {
        get
        {
            return Document.Hashes.AsReadOnly();
        }
    }

    /// <summary>
    /// Gets the list of media IDs from the document.
    /// </summary>
    public IReadOnlyList<MediaIdEntry> MediaIds
    {
        get
        {
            return Document.MediaIds.AsReadOnly();
        }
    }

    /// <summary>
    /// Gets all patch entries in this file.
    /// </summary>
    public IReadOnlyList<PatchEntry> Patches
    {
        get
        {
            return Document.Patches.AsReadOnly();
        }
    }

    /// <summary>
    /// Gets only the enabled patch entries.
    /// </summary>
    public IEnumerable<PatchEntry> EnabledPatches
    {
        get
        {
            return Document.Patches.Where(p => p.IsEnabled);
        }
    }

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

        byte[] bytes = File.ReadAllBytes(filePath);
        bool hasUtf8Bom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
        int contentOffset = hasUtf8Bom ? 3 : 0;
        string content = new UTF8Encoding(false).GetString(bytes, contentOffset, bytes.Length - contentOffset);
        Logger.Info<PatchFile>($"Loaded patch file: {filePath} ({content.Length} bytes)");

        PatchFile patchFile = FromString(content);
        patchFile.FilePath = filePath;
        patchFile._hasUtf8Bom = hasUtf8Bom;

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
        if (content == null)
        {
            throw new ArgumentException("Content cannot be null.", nameof(content));
        }

        Logger.Trace<PatchFile>($"Parsing patch file from TOML string ({content.Length} bytes)");

        try
        {
            PatchFile patchFile = new PatchFile();
            ParseContent(content, patchFile);
            if (string.IsNullOrWhiteSpace(patchFile.Document.TitleName) ||
                string.IsNullOrWhiteSpace(patchFile.Document.TitleId))
            {
                throw new ArgumentException("Patch file is missing title_name or title_id.");
            }

            patchFile._sourceContent = content;
            patchFile._sourceFingerprint = patchFile.ComputeFingerprint();
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
        if (lines.Length > 0)
        {
            lines[0] = lines[0].TrimStart('\uFEFF');
        }

        PatchEntry? currentPatch = null;
        string? currentCommandType = null;
        string? currentCommandTypeComment = null;
        bool isInPatchSection = false;
        List<string> pendingComments = [];
        List<string> currentCommandLeadingComments = [];

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
                currentCommandTypeComment = null;
                Match inlineCommentMatch = Regex.Match(trimmedLine, @"^\[\[patch\.([^\]]+)\]\]\s*#\s*(.+)$", RegexOptions.IgnoreCase);
                if (inlineCommentMatch.Success)
                {
                    currentCommandTypeComment = inlineCommentMatch.Groups[2].Value.Trim();
                    Logger.Debug<PatchFile>($"Command type '{currentCommandType}' has inline comment: {currentCommandTypeComment}");
                }

                currentCommandLeadingComments.AddRange(pendingComments);
                pendingComments.Clear();
                continue;
            }

            // Special handling for header fields - extract comments BEFORE stripping
            if (trimmedLine.StartsWith("title_name", StringComparison.OrdinalIgnoreCase))
            {
                ParseTitleNameWithComment(lines, ref i, trimmedLine, patchFile);
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
                ParseHashValue(trimmedLine, lines, ref i, patchFile);
                continue;
            }

            if (trimmedLine.StartsWith("#media_id", StringComparison.OrdinalIgnoreCase) ||
                trimmedLine.StartsWith("media_id", StringComparison.OrdinalIgnoreCase))
            {
                ParseMediaIdArray(trimmedLine, lines, ref i, patchFile);
                continue;
            }

            // Keep standalone comments so a full rewrite does not discard them.
            if (trimmedLine.StartsWith('#'))
            {
                if (currentPatch == null && !isInPatchSection && patchFile.Document.TitleName.Length == 0)
                {
                    patchFile.Document.LeadingComments.Add(line);
                }
                else
                {
                    pendingComments.Add(line);
                }

                continue;
            }

            // Strip inline comments for all other lines (preserve # inside quoted strings)
            string processedLine = StripInlineComment(trimmedLine);

            string lineToProcess = processedLine;

            // Extract [[patch]] header comment from raw trimmed line BEFORE stripping
            string? patchHeaderComment = null;
            if (trimmedLine.StartsWith("[[patch]]", StringComparison.OrdinalIgnoreCase))
            {
                Match patchHeaderMatch = Regex.Match(trimmedLine, @"^\[\[patch\]\]\s*#\s*(.+)$", RegexOptions.IgnoreCase);
                patchHeaderComment = patchHeaderMatch.Success ? patchHeaderMatch.Groups[1].Value.Trim() : null;
            }

            if (lineToProcess.StartsWith("[[patch]]", StringComparison.OrdinalIgnoreCase))
            {
                if (currentPatch != null)
                {
                    patchFile.Document.Patches.Add(currentPatch);
                }

                currentPatch = new PatchEntry
                {
                    HeaderComment = patchHeaderComment,
                    LeadingComments = [.. pendingComments]
                };
                pendingComments.Clear();
                currentCommandType = null;
                currentCommandTypeComment = null;
                currentCommandLeadingComments.Clear();
                isInPatchSection = true;
                continue;
            }

            if (currentPatch != null && currentCommandType != null && lineToProcess.StartsWith("address", StringComparison.OrdinalIgnoreCase))
            {
                currentCommandLeadingComments.AddRange(pendingComments);
                pendingComments.Clear();
                ParsePatchCommand(lines, ref i, currentPatch, currentCommandType, currentCommandTypeComment,
                    currentCommandLeadingComments);
                currentCommandTypeComment = null;
                currentCommandLeadingComments.Clear();
                continue;
            }

            if (currentPatch != null)
            {
                Match metadataMatch = Regex.Match(lineToProcess, @"^(name|author|desc|note)\s*=", RegexOptions.IgnoreCase);
                if (metadataMatch.Success)
                {
                    if (pendingComments.Count > 0)
                    {
                        currentPatch.PostMetadataComments.AddRange(pendingComments);
                        pendingComments.Clear();
                    }

                    int firstMetadataLine = i;
                    if (TomlString.TryParseAssignedValue(lines, ref i, trimmedLine, out string metadataValue))
                    {
                        string? lineSuffix = i == firstMetadataLine ? ExtractValueSuffix(trimmedLine) : null;
                        switch (metadataMatch.Groups[1].Value.ToLowerInvariant())
                        {
                            case "name":
                                currentPatch.Name = metadataValue;
                                currentPatch.NameLineSuffix = lineSuffix;
                                break;
                            case "author":
                                currentPatch.Author = metadataValue;
                                currentPatch.AuthorLineSuffix = lineSuffix;
                                break;
                            case "desc":
                                currentPatch.Description = metadataValue;
                                currentPatch.DescriptionLineSuffix = lineSuffix;
                                break;
                            case "note":
                                currentPatch.Note = metadataValue;
                                currentPatch.NoteLineSuffix = lineSuffix;
                                break;
                        }
                    }

                    continue;
                }
            }

            if (currentPatch != null && lineToProcess.StartsWith("is_enabled", StringComparison.OrdinalIgnoreCase))
            {
                if (pendingComments.Count > 0)
                {
                    currentPatch.PostMetadataComments.AddRange(pendingComments);
                    pendingComments.Clear();
                }

                Match m = Regex.Match(lineToProcess, @"^is_enabled\s*=\s*(true|false)", RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    currentPatch.IsEnabled = m.Groups[1].Value.Equals("true", StringComparison.OrdinalIgnoreCase);
                    currentPatch.SourceIsEnabledLineIndex = i;
                }

                continue;
            }
        }

        if (currentPatch != null)
        {
            patchFile.Document.Patches.Add(currentPatch);
        }

        patchFile.Document.TrailingComments.AddRange(pendingComments);

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
    private static void ParseTitleNameWithComment(string[] lines, ref int lineIndex, string rawLine, PatchFile patchFile)
    {
        // Extract comment first
        Match commentMatch = Regex.Match(rawLine, @"^title_name\s*=\s*""[^""]*""\s*#\s*(.+)$");
        string? comment = commentMatch.Success ? commentMatch.Groups[1].Value.Trim() : null;

        if (TomlString.TryParseAssignedValue(lines, ref lineIndex, rawLine, out string titleName))
        {
            patchFile.Document.TitleName = titleName;
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
    /// Parses the hash field from the TOML content (handles raw line with comment).
    /// </summary>
    private static void ParseHashValue(string rawLine, string[] lines, ref int lineIndex, PatchFile patchFile)
    {
        // Single hash with optional comment
        Match m = Regex.Match(rawLine, @"^hash\s*=\s*""([A-Fa-f0-9]+)""\s*#\s*(.*)");
        if (m.Success)
        {
            string hash = m.Groups[1].Value.ToUpper();
            string comment = m.Groups[2].Value.Trim();
            patchFile.Document.Hashes.Add(hash);
            patchFile.Document.HashComment = comment;
            patchFile.Document.HashEntries.Add(new HashEntry(hash, comment));
            Logger.Debug<PatchFile>($"Hash: {hash}");
            return;
        }

        // Single hash without comment
        m = Regex.Match(rawLine, @"^hash\s*=\s*""([A-Fa-f0-9]+)""");
        if (m.Success)
        {
            string hash = m.Groups[1].Value.ToUpper();
            patchFile.Document.Hashes.Add(hash);
            patchFile.Document.HashEntries.Add(new HashEntry(hash));
            Logger.Debug<PatchFile>($"Hash: {hash}");
            return;
        }

        // Hash array format
        m = Regex.Match(rawLine, @"^hash\s*=\s*\[");
        if (m.Success)
        {
            // Entries closed on the same line: hash = ["A", "B"]
            if (TryParseInlineArrayHashes(rawLine, patchFile))
            {
                return;
            }

            patchFile.Document.HashArrayHeaderComment = ExtractInlineComment(rawLine);

            int i;
            for (i = lineIndex + 1; i < lines.Length; i++)
            {
                string rawLineArray = lines[i];
                string trimmed = rawLineArray.Trim();

                if (trimmed.StartsWith("]"))
                {
                    break;
                }

                // Keep commented-out hash entries so a rewrite does not drop
                // alternate module versions.
                if (trimmed.StartsWith("#"))
                {
                    Match commentedMatch = Regex.Match(trimmed, @"^#\s*""([A-Fa-f0-9]+)""\s*,?\s*(?:#\s*(.*))?$");
                    if (commentedMatch.Success)
                    {
                        string disabledHash = commentedMatch.Groups[1].Value.ToUpper();
                        string? disabledComment = commentedMatch.Groups[2].Success
                            ? commentedMatch.Groups[2].Value.Trim()
                            : null;
                        patchFile.Document.HashEntries.Add(new HashEntry(disabledHash, disabledComment, true));
                        Logger.Debug<PatchFile>($"Hash (disabled): {disabledHash}");
                    }

                    continue;
                }

                string? inlineComment = ExtractInlineComment(trimmed);

                // Strip inline comment before extracting hash
                string processedLine = StripInlineComment(trimmed);
                m = Regex.Match(processedLine, @"""([A-Fa-f0-9]+)""");
                if (m.Success)
                {
                    string hash = m.Groups[1].Value.ToUpper();
                    patchFile.Document.Hashes.Add(hash);
                    patchFile.Document.HashComments.Add(inlineComment ?? string.Empty);
                    patchFile.Document.HashEntries.Add(new HashEntry(hash, inlineComment));
                    Logger.Debug<PatchFile>($"Hash: {hash}" + (inlineComment != null ? $" - {inlineComment}" : ""));
                }
            }

            lineIndex = i;
        }
    }

    /// <summary>
    /// Parses hash entries closed on the same line (e.g. <c>hash = ["A", "B"]</c>).
    /// </summary>
    /// <returns>True when the array closes on this line (nothing more to read), false otherwise.</returns>
    private static bool TryParseInlineArrayHashes(string rawLine, PatchFile patchFile)
    {
        string code = StripInlineComment(rawLine);
        int open = code.IndexOf('[');
        int close = open >= 0 ? code.IndexOf(']', open) : -1;
        if (open < 0 || close < 0)
        {
            return false;
        }

        foreach (Match hm in Regex.Matches(code.Substring(open, close - open + 1), @"""([A-Fa-f0-9]+)"""))
        {
            string hash = hm.Groups[1].Value.ToUpper();
            patchFile.Document.Hashes.Add(hash);
            patchFile.Document.HashComments.Add(string.Empty);
            patchFile.Document.HashEntries.Add(new HashEntry(hash));
            Logger.Debug<PatchFile>($"Hash: {hash}");
        }

        return true;
    }

    /// <summary>
    /// Parses media IDs closed on the same line (e.g. <c>media_id = ["A", "B"]</c>).
    /// </summary>
    /// <returns>True when the array closes on this line (nothing more to read), false otherwise.</returns>
    private static bool TryParseInlineMediaIds(string rawLine, bool isCommented, PatchFile patchFile)
    {
        string code = StripInlineComment(rawLine);
        int open = code.IndexOf('[');
        int close = open >= 0 ? code.IndexOf(']', open) : -1;
        if (open < 0 || close < 0)
        {
            return false;
        }

        foreach (Match mm in Regex.Matches(code.Substring(open, close - open + 1), @"""([A-Fa-f0-9]+)"""))
        {
            string mediaId = mm.Groups[1].Value.ToUpper();
            patchFile.Document.MediaIds.Add(new MediaIdEntry(mediaId, string.Empty, isCommented));
            Logger.Debug<PatchFile>($"Media ID: {mediaId}");
        }

        return true;
    }

    /// <summary>
    /// Parses the media_id field from the TOML content (handles raw line with comment).
    /// </summary>
    private static void ParseMediaIdArray(string rawLine, string[] lines, ref int lineIndex, PatchFile patchFile)
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
            // Entries closed on the same line: media_id = ["A", "B"]
            string inlineCandidate = isCommented
                ? rawLine.TrimStart()[1..].TrimStart()
                : rawLine;
            if (TryParseInlineMediaIds(inlineCandidate, isCommented, patchFile))
            {
                return;
            }

            // Array format (commented or not)
            int i;
            for (i = lineIndex + 1; i < lines.Length; i++)
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
                        string? comment = ExtractInlineComment(trimmed);
                        patchFile.Document.MediaIds.Add(new MediaIdEntry(mediaId, comment ?? string.Empty, false));
                        Logger.Debug<PatchFile>($"Media ID: {mediaId}");
                    }
                }
            }

            lineIndex = i;
        }
        else if (!isCommented)
        {
            // Single media_id (not commented) - use rawLine with comment
            Match m = Regex.Match(rawLine, @"^media_id\s*=\s*""([A-Fa-f0-9]+)""\s*#\s*(.*)");
            if (m.Success)
            {
                string mediaId = m.Groups[1].Value.ToUpper();
                string comment = m.Groups[2].Success ? m.Groups[2].Value.Trim() : headerComment ?? string.Empty;
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
                string comment = m.Groups[2].Success ? m.Groups[2].Value.Trim() : headerComment ?? string.Empty;
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
    private static void ParsePatchCommand(string[] lines, ref int lineIndex, PatchEntry patch, string commandType,
        string? typeComment, IReadOnlyList<string> leadingComments)
    {
        string rawAddressLine = lines[lineIndex].Trim();

        // Extract address comment - more flexible regex to handle whitespace
        string? addressComment = null;
        Match addressCommentMatch = Regex.Match(rawAddressLine, @"address\s*=\s*[^\s#]+\s*#\s*(.+?)\s*$");
        if (addressCommentMatch.Success)
        {
            addressComment = addressCommentMatch.Groups[1].Value.Trim();
        }

        // Strip inline comment from address line (preserve # inside quotes)
        string addressLine = StripInlineComment(rawAddressLine);

        Match addressMatch = Regex.Match(addressLine, @"address\s*=\s*([^\s#]+)");
        if (!addressMatch.Success || !PatchValueParser.TryParseIntegerLiteral(addressMatch.Groups[1].Value, out long addressValue))
        {
            Logger.Warning<PatchFile>($"Failed to parse address at line {lineIndex + 1}");
            return;
        }

        ulong address = unchecked((ulong)addressValue);

        // Skip blank lines and comments between address and value. Upstream
        // patch files can contain a commented-out value there.
        List<string> valueLeadingComments = [];
        int valueLineIndex = lineIndex + 1;
        while (valueLineIndex < lines.Length)
        {
            string candidate = lines[valueLineIndex].Trim();
            if (candidate.Length == 0)
            {
                valueLineIndex++;
                continue;
            }

            if (candidate.StartsWith('#'))
            {
                valueLeadingComments.Add(lines[valueLineIndex]);
                valueLineIndex++;
                continue;
            }

            break;
        }

        if (valueLineIndex >= lines.Length)
        {
            Logger.Warning<PatchFile>($"No value line found after address at line {lineIndex + 1}");
            return;
        }

        string rawValueLine = lines[valueLineIndex].Trim();
        if (!Regex.IsMatch(rawValueLine, @"^value\s*=", RegexOptions.IgnoreCase))
        {
            Logger.Warning<PatchFile>($"No value line found after address at line {lineIndex + 1}");
            return;
        }

        string? valueComment = ExtractInlineComment(rawValueLine);

        PatchCommand cmd = new PatchCommand
        {
            Address = address,
            Type = ParsePatchType(commandType),
            TypeComment = typeComment,
            AddressComment = addressComment,
            ValueComment = valueComment,
            LeadingComments = [.. leadingComments],
            ValueLeadingComments = valueLeadingComments
        };

        if (commandType.Equals("string", StringComparison.OrdinalIgnoreCase) ||
            commandType.Equals("u16string", StringComparison.OrdinalIgnoreCase) ||
            commandType.Equals("array", StringComparison.OrdinalIgnoreCase))
        {
            int firstValueLine = valueLineIndex;
            if (!TomlString.TryParseAssignedValue(lines, ref valueLineIndex, rawValueLine, out string stringValue))
            {
                Logger.Warning<PatchFile>($"Failed to parse value at line {valueLineIndex + 1}");
                return;
            }

            if (valueLineIndex == firstValueLine)
            {
                cmd.RawValueLineSuffix = ExtractValueSuffix(rawValueLine);
            }

            if (commandType.Equals("array", StringComparison.OrdinalIgnoreCase))
            {
                if (!PatchValueParser.TryParseArray(stringValue, out byte[]? bytes, out bool usePrefix) || bytes == null)
                {
                    Logger.Warning<PatchFile>($"Failed to parse array value at line {valueLineIndex + 1}");
                    return;
                }

                cmd.Value = bytes;
                cmd.UseArrayPrefix = usePrefix;
                Logger.Debug<PatchFile>($"Array parsed: usePrefix={usePrefix}, bytes={bytes.Length}");
            }
            else
            {
                cmd.Value = stringValue;
            }
        }
        else
        {
            // Strip inline comment from value line (preserve # inside quotes)
            string valueLine = StripInlineComment(rawValueLine);

            Match valueMatch = Regex.Match(valueLine, @"value\s*=\s*(.+)");
            if (!valueMatch.Success)
            {
                Logger.Warning<PatchFile>($"Failed to parse value at line {valueLineIndex + 1}");
                return;
            }

            string valueStr = valueMatch.Groups[1].Value.Trim().TrimEnd(',');
            if (!PatchValueParser.TryParse(valueStr, cmd.Type, out object? parsedValue) || parsedValue == null)
            {
                Logger.Warning<PatchFile>($"Failed to parse value '{valueStr}' for type {commandType} at line {valueLineIndex + 1}");
                return;
            }

            cmd.Value = parsedValue;
            if (PatchValueParser.IsTomlRepresentableLiteral(valueStr, cmd.Type))
            {
                cmd.RawValue = valueStr;
                cmd.RawValueLineSuffix = ExtractValueSuffix(rawValueLine);
            }
        }

        patch.Commands.Add(cmd);
        lineIndex = valueLineIndex;
        Logger.Debug<PatchFile>($"Parsed {commandType} command: address=0x{address:x}, value={cmd.Value}");
    }

    /// <summary>
    /// Returns the exact text after "value = " on a source line, including
    /// the inline comment and its spacing.
    /// </summary>
    private static string? ExtractValueSuffix(string rawValueLine)
    {
        int equalsIndex = rawValueLine.IndexOf('=');
        return equalsIndex >= 0 ? rawValueLine[(equalsIndex + 1)..].TrimStart() : null;
    }

    /// <summary>
    /// Returns the text after the first TOML comment marker that is outside a
    /// string, or null when the line has no inline comment.
    /// </summary>
    private static string? ExtractInlineComment(string line)
    {
        int commentIndex = FindInlineCommentIndex(line);
        return commentIndex >= 0 ? line[(commentIndex + 1)..].Trim() : null;
    }

    /// <summary>
    /// Finds the first TOML comment marker that is outside a basic or literal
    /// string. Handles escaped quotes in basic strings.
    /// </summary>
    private static int FindInlineCommentIndex(string line)
    {
        bool inBasicString = false;
        bool inLiteralString = false;
        bool escaped = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (escaped)
            {
                escaped = false;
                continue;
            }

            if (inBasicString)
            {
                if (c == '\\')
                {
                    escaped = true;
                }
                else if (c == '"')
                {
                    inBasicString = false;
                }

                continue;
            }

            if (inLiteralString)
            {
                if (c == '\'')
                {
                    inLiteralString = false;
                }

                continue;
            }

            if (c == '"')
            {
                inBasicString = true;
            }
            else if (c == '\'')
            {
                inLiteralString = true;
            }
            else if (c == '#')
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Strips inline TOML comments from a value string, preserving # inside quoted strings.
    /// Handles escaped quotes (\").
    /// </summary>
    private static string StripInlineComment(string valueStr)
    {
        int commentIndex = FindInlineCommentIndex(valueStr);
        return commentIndex >= 0 ? valueStr[..commentIndex].TrimEnd() : valueStr;
    }

    /// <summary>
    /// Parses a patch type string to PatchType enum.
    /// </summary>
    private static PatchType ParsePatchType(string typeString)
    {
        return typeString.ToLowerInvariant() switch
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

            // Refuse to overwrite a file that was edited outside this instance
            // since it was loaded. The captured source is only safe to reuse
            // while it still matches what is on disk.
            if (_sourceContent != null &&
                !string.IsNullOrEmpty(FilePath) &&
                string.Equals(Path.GetFullPath(savePath), Path.GetFullPath(FilePath), StringComparison.OrdinalIgnoreCase) &&
                File.Exists(savePath) &&
                !string.Equals(File.ReadAllText(savePath), _sourceContent, StringComparison.Ordinal))
            {
                throw new IOException($"Patch file has changed on disk since it was loaded: {savePath}");
            }

            string currentFingerprint = ComputeFingerprint();
            string content;
            bool usedInPlaceUpdate = false;

            if (_sourceContent != null &&
                string.Equals(_sourceFingerprint, currentFingerprint, StringComparison.Ordinal) &&
                TryApplyEnabledStateChanges(_sourceContent, out string updatedSource))
            {
                content = updatedSource;
                usedInPlaceUpdate = true;
            }
            else
            {
                content = ToTomlString();
            }

            string? directory = Path.GetDirectoryName(savePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Logger.Info<PatchFile>($"Creating directory: {directory}");
                Directory.CreateDirectory(directory);
            }

            // Write to a temporary file first so a crash or full disk cannot
            // leave the user's patch file truncated.
            string temporaryPath = savePath + ".tmp";
            try
            {
                File.WriteAllText(temporaryPath, content, new UTF8Encoding(_hasUtf8Bom));
                File.Move(temporaryPath, savePath, true);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }

            _sourceContent = content;
            _sourceFingerprint = currentFingerprint;
            if (!usedInPlaceUpdate)
            {
                RefreshSourceLineIndices(content);
            }

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
    /// Creates a fingerprint of all serializable document content except patch
    /// enable states, so enable-only edits can be applied in place.
    /// </summary>
    private string ComputeFingerprint()
    {
        StringBuilder builder = new StringBuilder();
        builder.Append(Document.Hashes.Count).Append(':')
            .Append(Document.HashComments.Count).Append(':')
            .Append(Document.HashEntries.Count).Append(':')
            .Append(Document.MediaIds.Count).Append(':')
            .Append(Document.Patches.Count).Append('\u001F');

        void Add(string? value)
        {
            builder.Append(value?.Length ?? -1).Append(':').Append(value).Append('\u001F');
        }

        Add(Document.TitleName);
        Add(Document.TitleId);
        Add(Document.TitleNameComment);
        Add(Document.TitleIdComment);
        Add(Document.HashComment);
        Add(Document.HashArrayHeaderComment);
        foreach (string comment in Document.LeadingComments)
        {
            Add(comment);
        }

        foreach (string comment in Document.TrailingComments)
        {
            Add(comment);
        }

        foreach (string hash in Document.Hashes)
        {
            Add(hash);
        }

        foreach (string comment in Document.HashComments)
        {
            Add(comment);
        }

        foreach (HashEntry hashEntry in Document.HashEntries)
        {
            Add(hashEntry.Hash);
            Add(hashEntry.Comment);
            Add(hashEntry.IsCommented ? "1" : "0");
        }

        foreach (MediaIdEntry mediaId in Document.MediaIds)
        {
            Add(mediaId.Id);
            Add(mediaId.Comment);
            Add(mediaId.IsCommented ? "1" : "0");
        }

        foreach (PatchEntry patch in Document.Patches)
        {
            Add(patch.Name);
            Add(patch.Author);
            Add(patch.Description);
            Add(patch.Note);
            Add(patch.HeaderComment);
            Add(patch.NameLineSuffix);
            Add(patch.AuthorLineSuffix);
            Add(patch.DescriptionLineSuffix);
            Add(patch.NoteLineSuffix);
            foreach (string comment in patch.LeadingComments)
            {
                Add(comment);
            }

            foreach (string comment in patch.PostMetadataComments)
            {
                Add(comment);
            }

            foreach (PatchCommand command in patch.Commands)
            {
                Add(command.Type.ToString());
                Add(command.Address.ToString(CultureInfo.InvariantCulture));
                Add(command.GetValueAsString());
                Add(command.TypeComment);
                Add(command.AddressComment);
                Add(command.ValueComment);
                Add(command.UseArrayPrefix?.ToString());
                foreach (string comment in command.LeadingComments)
                {
                    Add(comment);
                }

                foreach (string comment in command.ValueLeadingComments)
                {
                    Add(comment);
                }
            }
        }

        return builder.ToString();
    }

    /// <summary>
    /// Applies only patch enable-state changes to the original source text,
    /// preserving all comments, whitespace and value formatting.
    /// </summary>
    private bool TryApplyEnabledStateChanges(string source, out string updated)
    {
        updated = source;
        string[] lines = Regex.Split(source, "(\r\n|\r|\n)");
        bool changed = false;

        foreach (PatchEntry patch in Document.Patches)
        {
            if (patch.SourceIsEnabledLineIndex < 0)
            {
                return false;
            }

            int lineIndex = patch.SourceIsEnabledLineIndex * 2;
            if (lineIndex >= lines.Length)
            {
                return false;
            }

            string line = lines[lineIndex];
            Match match = Regex.Match(line, @"^([ \t]*is_enabled[ \t]*=[ \t]*)(true|false)", RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                return false;
            }

            Group valueGroup = match.Groups[2];
            bool currentValue = valueGroup.Value.Equals("true", StringComparison.OrdinalIgnoreCase);
            if (currentValue != patch.IsEnabled)
            {
                string desiredValue = patch.IsEnabled ? "true" : "false";
                lines[lineIndex] = line[..valueGroup.Index] + desiredValue + line[(valueGroup.Index + valueGroup.Length)..];
                changed = true;
            }
        }

        if (changed)
        {
            updated = string.Concat(lines);
        }

        return true;
    }

    /// <summary>
    /// Re-parses freshly written content so later enable-only saves can still
    /// be applied in place.
    /// </summary>
    private void RefreshSourceLineIndices(string content)
    {
        try
        {
            PatchFile reparsed = FromString(content);
            if (reparsed.Document.Patches.Count != Document.Patches.Count)
            {
                foreach (PatchEntry patch in Document.Patches)
                {
                    patch.SourceIsEnabledLineIndex = -1;
                }

                return;
            }

            for (int i = 0; i < Document.Patches.Count; i++)
            {
                Document.Patches[i].SourceIsEnabledLineIndex = reparsed.Document.Patches[i].SourceIsEnabledLineIndex;
            }
        }
        catch
        {
            foreach (PatchEntry patch in Document.Patches)
            {
                patch.SourceIsEnabledLineIndex = -1;
            }
        }
    }

    /// <summary>
    /// Returns true when the source hash entry list still matches the active
    /// hash list, so the full list can be written without losing entries.
    /// </summary>
    private bool CanWriteModeledHashEntries()
    {
        if (Document.HashEntries.Count == 0)
        {
            return false;
        }

        List<HashEntry> activeEntries = Document.HashEntries.Where(entry => !entry.IsCommented).ToList();
        if (activeEntries.Count != Document.Hashes.Count)
        {
            return false;
        }

        for (int i = 0; i < activeEntries.Count; i++)
        {
            if (!string.Equals(activeEntries[i].Hash, Document.Hashes[i], StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Writes preserved standalone comment lines verbatim.
    /// </summary>
    private static void AppendCommentLines(StringBuilder builder, IEnumerable<string> comments)
    {
        foreach (string comment in comments)
        {
            string line = comment.TrimEnd();
            if (line.Length > 0)
            {
                builder.AppendLine(line);
            }
        }
    }

    /// <summary>
    /// Formats a memory address as a TOML integer. Addresses above the signed
    /// 64-bit range are written as negative decimals so toml++ accepts them.
    /// </summary>
    private static string FormatAddress(ulong address) =>
        address <= long.MaxValue
            ? $"0x{address:x}"
            : unchecked((long)address).ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// Converts the patch document to a TOML string.
    /// </summary>
    /// <returns>The TOML string representation of the patch file.</returns>
    public string ToTomlString()
    {
        Logger.Trace<PatchFile>("Converting patch document to TOML string");

        StringBuilder sb = new StringBuilder();

        AppendCommentLines(sb, Document.LeadingComments);

        // Write header
        if (!string.IsNullOrEmpty(Document.TitleNameComment))
        {
            sb.AppendLine($"title_name = {TomlString.Encode(Document.TitleName)} # {Document.TitleNameComment}");
        }
        else
        {
            sb.AppendLine($"title_name = {TomlString.Encode(Document.TitleName)}");
        }

        if (!string.IsNullOrEmpty(Document.TitleIdComment))
        {
            sb.AppendLine($"title_id = \"{Document.TitleId.ToUpper()}\" # {Document.TitleIdComment}");
        }
        else
        {
            sb.AppendLine($"title_id = \"{Document.TitleId.ToUpper()}\"");
        }

        // Write hashes. Use the full source list when it is consistent with
        // the active hashes so commented-out module alternatives survive.
        bool hasCommentedHashes = Document.HashEntries.Any(entry => entry.IsCommented);
        bool useModeledHashEntries = CanWriteModeledHashEntries() &&
                                     (Document.Hashes.Count > 1 ||
                                      hasCommentedHashes ||
                                      !string.IsNullOrEmpty(Document.HashArrayHeaderComment));

        if (Document.Hashes.Count == 1 && !useModeledHashEntries)
        {
            string? hashComment = Document.HashComment;
            if (string.IsNullOrEmpty(hashComment) && Document.HashComments.Count > 0)
            {
                hashComment = Document.HashComments[0];
            }

            if (!string.IsNullOrEmpty(hashComment))
            {
                sb.AppendLine($"hash = \"{Document.Hashes[0].ToUpper()}\" # {hashComment}");
            }
            else
            {
                sb.AppendLine($"hash = \"{Document.Hashes[0].ToUpper()}\"");
            }
        }
        else if (useModeledHashEntries)
        {
            sb.Append("hash = [");
            if (!string.IsNullOrEmpty(Document.HashArrayHeaderComment))
            {
                sb.Append($" # {Document.HashArrayHeaderComment}");
            }

            sb.AppendLine();
            int activeHashCount = Document.HashEntries.Count(entry => !entry.IsCommented);
            int activeHashIndex = 0;
            foreach (HashEntry entry in Document.HashEntries)
            {
                string hash = entry.Hash.ToUpper();

                if (entry.IsCommented)
                {
                    string? disabledComment = entry.Comment;
                    sb.AppendLine(string.IsNullOrEmpty(disabledComment)
                        ? $"#    \"{hash}\""
                        : $"#    \"{hash}\" # {disabledComment}");
                    continue;
                }

                activeHashIndex++;
                string separator = activeHashIndex < activeHashCount ? "," : string.Empty;
                string? comment = activeHashIndex - 1 < Document.HashComments.Count
                    ? Document.HashComments[activeHashIndex - 1]
                    : entry.Comment;
                sb.AppendLine(string.IsNullOrEmpty(comment)
                    ? $"    \"{hash}\"{separator}"
                    : $"    \"{hash}\"{separator} # {comment}");
            }

            sb.AppendLine("]");
        }
        else
        {
            sb.AppendLine("hash = [");
            for (int i = 0; i < Document.Hashes.Count; i++)
            {
                string hash = Document.Hashes[i].ToUpper();
                string separator = i < Document.Hashes.Count - 1 ? "," : string.Empty;
                string? comment = i < Document.HashComments.Count ? Document.HashComments[i] : null;
                if (!string.IsNullOrEmpty(comment))
                {
                    sb.AppendLine($"    \"{hash}\"{separator} # {comment}");
                }
                else
                {
                    sb.AppendLine($"    \"{hash}\"{separator}");
                }
            }

            sb.AppendLine("]");
        }

        // Write media IDs
        if (Document.MediaIds is { Count: > 0 })
        {
            if (Document.MediaIds.Count == 1)
            {
                MediaIdEntry entry = Document.MediaIds[0];
                string id = entry.Id.ToUpperInvariant();
                if (!string.IsNullOrEmpty(entry.Comment))
                {
                    sb.AppendLine($"#media_id = \"{id}\" # {entry.Comment}");
                }
                else
                {
                    sb.AppendLine($"#media_id = \"{id}\"");
                }
            }
            else
            {
                sb.AppendLine("#media_id = [");
                foreach (MediaIdEntry entry in Document.MediaIds)
                {
                    string id = entry.Id.ToUpperInvariant();
                    if (!string.IsNullOrEmpty(entry.Comment))
                    {
                        sb.AppendLine($"#    \"{id}\" # {entry.Comment}");
                    }
                    else
                    {
                        sb.AppendLine($"#    \"{id}\"");
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
            AppendCommentLines(sb, patch.LeadingComments);
            if (!string.IsNullOrEmpty(patch.HeaderComment))
            {
                sb.AppendLine($"[[patch]] # {patch.HeaderComment}");
            }
            else
            {
                sb.AppendLine("[[patch]]");
            }

            sb.AppendLine(patch.NameLineSuffix != null
                ? $"    name = {patch.NameLineSuffix}"
                : $"    name = {TomlString.Encode(patch.Name)}");

            if (patch.NoteLineSuffix != null)
            {
                sb.AppendLine($"    note = {patch.NoteLineSuffix}");
            }
            else if (!string.IsNullOrEmpty(patch.Note))
            {
                sb.AppendLine($"    note = {TomlString.Encode(patch.Note)}");
            }

            if (patch.DescriptionLineSuffix != null)
            {
                sb.AppendLine($"    desc = {patch.DescriptionLineSuffix}");
            }
            else if (!string.IsNullOrEmpty(patch.Description))
            {
                sb.AppendLine($"    desc = {TomlString.Encode(patch.Description)}");
            }

            // Some upstream patches intentionally omit the author; omitting an
            // empty value keeps that layout and is equivalent for Xenia.
            if (patch.AuthorLineSuffix != null)
            {
                sb.AppendLine($"    author = {patch.AuthorLineSuffix}");
            }
            else if (!string.IsNullOrEmpty(patch.Author))
            {
                sb.AppendLine($"    author = {TomlString.Encode(patch.Author)}");
            }

            sb.AppendLine($"    is_enabled = {patch.IsEnabled.ToString().ToLower()}");
            AppendCommentLines(sb, patch.PostMetadataComments);
            sb.AppendLine();

            // Write commands in original order
            foreach (PatchCommand command in patch.Commands)
            {
                AppendCommentLines(sb, command.LeadingComments);
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

                string address = FormatAddress(command.Address);
                bool hasAddressComment = !string.IsNullOrWhiteSpace(command.AddressComment);
                if (hasAddressComment)
                {
                    sb.AppendLine($"        address = {address} # {command.AddressComment}");
                }
                else
                {
                    sb.AppendLine($"        address = {address}");
                }

                AppendCommentLines(sb, command.ValueLeadingComments);
                if (!string.IsNullOrEmpty(command.RawValueLineSuffix))
                {
                    sb.AppendLine($"        value = {command.RawValueLineSuffix}");
                }
                else
                {
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
            }

            sb.AppendLine();
        }

        AppendCommentLines(sb, Document.TrailingComments);

        // The game-patches repository uses LF line endings and a single final
        // newline; Xenia accepts either style, so normalize on every platform.
        string result = sb.ToString().Replace("\r\n", "\n").TrimEnd('\n') + "\n";
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
    /// Validates that the file name follows Xenia's patch file naming rule:
    /// an 8-character hexadecimal title ID followed by ".patch.toml".
    /// </summary>
    /// <param name="fileName">The file name to validate.</param>
    /// <exception cref="ArgumentException">Thrown when the file name does not follow the convention.</exception>
    private static void ValidateFileName(string fileName)
    {
        string pattern = @"^[A-Fa-f0-9]{8}.*\.patch\.toml$";

        if (!Regex.IsMatch(fileName, pattern))
        {
            Logger.Error<PatchFile>($"Invalid file name format: {fileName}. Expected format: 'Title ID - Game Title.patch.toml'");
            throw new ArgumentException(
                $"Invalid file name format: {fileName}. File name must start with an 8-character hexadecimal title ID and end with '.patch.toml' (e.g., '4D5307DF - Blue Dragon.patch.toml')",
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
    public PatchEntry? GetPatch(string name) => Document.Patches.FirstOrDefault(p => p.Name == name);
}