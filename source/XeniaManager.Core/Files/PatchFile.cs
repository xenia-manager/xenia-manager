using System.Text;
using System.Text.RegularExpressions;
using Tomlyn;
using Tomlyn.Model;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Models.Patches;

namespace XeniaManager.Core.Files;

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

        try
        {
            // Parse the TOML content
            TomlTable model = Toml.ToModel(content);
            PatchFile patchFile = new PatchFile();

            // Parse title_name
            if (model.TryGetValue("title_name", out object titleNameObj))
            {
                patchFile.Document.TitleName = titleNameObj.ToString() ?? string.Empty;
                Logger.Debug<PatchFile>($"Title Name: {patchFile.Document.TitleName}");
            }
            else
            {
                Logger.Warning<PatchFile>("Missing title_name in patch file");
            }

            // Parse title_id
            if (model.TryGetValue("title_id", out object titleIdObj))
            {
                patchFile.Document.TitleId = titleIdObj.ToString()?.ToUpper() ?? string.Empty;
                Logger.Debug<PatchFile>($"Title ID: {patchFile.Document.TitleId}");
            }
            else
            {
                Logger.Warning<PatchFile>("Missing title_id in patch file");
            }

            // Parse hash (can be string or array)
            ParseHashField(model, patchFile);

            // Parse media_id (optional, can be string or array, often commented out)
            ParseMediaIdField(model, patchFile, content);

            // Parse patches
            ParsePatches(model, patchFile);

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
    /// Parses the hash field from the TOML model.
    /// </summary>
    private static void ParseHashField(TomlTable model, PatchFile patchFile)
    {
        if (model.TryGetValue("hash", out object hashObj))
        {
            switch (hashObj)
            {
                case string hashString:
                    patchFile.Document.Hashes.Add(hashString.ToUpper());
                    Logger.Debug<PatchFile>($"Hash: {hashString.ToUpper()}");
                    break;
                case TomlArray hashArray:
                    foreach (object? hash in hashArray)
                    {
                        if (hash == null)
                        {
                            continue;
                        }
                        string hashStr = hash.ToString()?.ToUpper() ?? string.Empty;
                        patchFile.Document.Hashes.Add(hashStr);
                        Logger.Debug<PatchFile>($"Hash: {hashStr}");
                    }
                    break;
            }
        }
        else
        {
            Logger.Warning<PatchFile>("Missing hash field in patch file");
        }
    }

    /// <summary>
    /// Parses the media_id field from the TOML model.
    /// Also parses commented media_id lines from the raw content.
    /// </summary>
    private static void ParseMediaIdField(TomlTable model, PatchFile patchFile, string rawToml)
    {
        // First, try to parse uncommented media_id from the model
        if (model.TryGetValue("media_id", out object mediaIdObj))
        {
            switch (mediaIdObj)
            {
                case string mediaIdString:
                    patchFile.Document.MediaIds.Add(new MediaIdEntry(mediaIdString.ToUpper()));
                    Logger.Debug<PatchFile>($"Media ID: {mediaIdString.ToUpper()}");
                    break;
                case TomlArray mediaIdArray:
                    foreach (object? mediaId in mediaIdArray)
                    {
                        if (mediaId == null)
                        {
                            continue;
                        }
                        string mediaIdStr = mediaId.ToString()?.ToUpper() ?? string.Empty;
                        patchFile.Document.MediaIds.Add(new MediaIdEntry(mediaIdStr));
                        Logger.Debug<PatchFile>($"Media ID: {mediaIdStr}");
                    }
                    break;
            }
        }

        // Also parse commented media_id lines from raw TOML
        // Pattern: #media_id = "XXXXXXXX" # comment
        // or: # "XXXXXXXX" [...] # comment
        ParseCommentedMediaIds(rawToml, patchFile);
    }

    /// <summary>
    /// Parses commented media_id lines from raw TOML content.
    /// This captures media IDs that are commented out (common in patch files).
    /// </summary>
    private static void ParseCommentedMediaIds(string rawToml, PatchFile patchFile)
    {
        string[] lines = rawToml.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        bool inCommentedArray = false;

        foreach (string line in lines)
        {
            string trimmedLine = line.Trim();

            // Check to being commented media_id array: #media_id = [
            if (trimmedLine.StartsWith("#media_id", StringComparison.OrdinalIgnoreCase) && trimmedLine.Contains('['))
            {
                inCommentedArray = true;

                // Try to extract media ID from the same line if it's a single-line array
                Match singleMatch = Regex.Match(trimmedLine, @"#media_id\s*=\s*\[\s*""([A-F0-9]+)""\s*(?:#\s*(.*))?\s*\]");
                if (singleMatch.Success)
                {
                    string mediaId = singleMatch.Groups[1].Value.ToUpper();
                    string comment = singleMatch.Groups[2].Value.Trim();
                    patchFile.Document.MediaIds.Add(new MediaIdEntry(mediaId, comment, true));
                    Logger.Debug<PatchFile>($"Commented Media ID: {mediaId} - {comment}");
                    inCommentedArray = false;
                }
                continue;
            }

            // Check for the end of the commented array
            if (inCommentedArray && trimmedLine.StartsWith("#]"))
            {
                inCommentedArray = false;
                continue;
            }

            // Parse media ID entries in the commented array
            if (inCommentedArray && trimmedLine.StartsWith("#"))
            {
                // Pattern: #"XXXXXXXX", # comment or # "XXXXXXXX" # comment
                // The comma after the media ID is optional
                Match match = Regex.Match(trimmedLine, @"#\s*""([A-F0-9]+)""\s*,?\s*(?:#\s*(.*))?");
                if (match.Success)
                {
                    string mediaId = match.Groups[1].Value.ToUpper();
                    string comment = match.Groups[2].Value.Trim();

                    // Avoid duplicates from uncommented parsing
                    if (patchFile.Document.MediaIds.All(m => m.Id != mediaId))
                    {
                        patchFile.Document.MediaIds.Add(new MediaIdEntry(mediaId, comment, true));
                        Logger.Debug<PatchFile>($"Commented Media ID: {mediaId} - {comment}");
                    }
                }
            }
            else if (trimmedLine.StartsWith("#media_id = \"") || trimmedLine.StartsWith("#media_id=\""))
            {
                // Single commented media_id: #media_id = "XXXXXXXX" # comment
                Match match = Regex.Match(trimmedLine, @"#media_id\s*=\s*""([A-F0-9]+)""\s*(?:#\s*(.*))?");
                if (match.Success)
                {
                    string mediaId = match.Groups[1].Value.ToUpper();
                    string comment = match.Groups[2].Value.Trim();

                    // Avoid duplicates
                    if (patchFile.Document.MediaIds.All(m => m.Id != mediaId))
                    {
                        patchFile.Document.MediaIds.Add(new MediaIdEntry(mediaId, comment, true));
                        Logger.Debug<PatchFile>($"Commented Media ID: {mediaId} - {comment}");
                    }
                }
            }
        }
    }

    /// <summary>
    /// Parses all patch entries from the TOML model.
    /// </summary>
    private static void ParsePatches(TomlTable model, PatchFile patchFile)
    {
        if (!model.TryGetValue("patch", out object patchObj))
        {
            Logger.Warning<PatchFile>("No patches found in patch file");
            return;
        }

        if (patchObj is not TomlTableArray patchesArray)
        {
            Logger.Warning<PatchFile>("Invalid patch array format");
            return;
        }

        foreach (TomlTable patchItem in patchesArray)
        {
            if (patchItem is not { } patchTable)
            {
                continue;
            }

            PatchEntry patchEntry = new PatchEntry();

            // Parse patch metadata
            if (patchTable.TryGetValue("name", out object nameObj))
            {
                patchEntry.Name = nameObj.ToString() ?? string.Empty;
            }

            if (patchTable.TryGetValue("author", out object authorObj))
            {
                patchEntry.Author = authorObj.ToString() ?? string.Empty;
            }

            if (patchTable.TryGetValue("desc", out object descObj))
            {
                patchEntry.Description = descObj.ToString();
            }

            if (patchTable.TryGetValue("is_enabled", out object enabledObj))
            {
                patchEntry.IsEnabled = Convert.ToBoolean(enabledObj);
            }

            // Parse patch commands
            ParsePatchCommands(patchTable, patchEntry);

            patchFile.Document.Patches.Add(patchEntry);
            Logger.Debug<PatchFile>($"Parsed patch: {patchEntry.Name} by {patchEntry.Author} ({patchEntry.Commands.Count} commands)");
        }
    }

    /// <summary>
    /// Parses patch commands from a patch table.
    /// Note: Tomlyn parses [[patch.type]] as TomlTableArray within the patch table.
    /// </summary>
    private static void ParsePatchCommands(TomlTable patchTable, PatchEntry patchEntry)
    {
        // Known patch types
        string[] patchTypes = ["be8", "be16", "be32", "be64", "array", "f32", "f64", "string", "u16string"];

        foreach (string patchType in patchTypes)
        {
            if (!patchTable.TryGetValue(patchType, out object commandsObj))
            {
                continue;
            }
            PatchType type = ParsePatchType(patchType);

            // Commands can be a TomlTableArray (from [[patch.type]] syntax), TomlArray or TomlTable
            switch (commandsObj)
            {
                case TomlTableArray commandsTableArray:
                {
                    foreach (TomlTable commandItem in commandsTableArray)
                    {
                        ParseCommand(commandItem, type, patchEntry);
                    }
                    break;
                }
                case TomlArray commandsArray:
                {
                    foreach (object? commandItem in commandsArray)
                    {
                        ParseCommand(commandItem, type, patchEntry);
                    }
                    break;
                }
                case TomlTable commandTable:
                    // Single command (not an array)
                    ParseCommand(commandTable, type, patchEntry);
                    break;
            }
        }
    }

    /// <summary>
    /// Parses a single command from a command table.
    /// </summary>
    private static void ParseCommand(object? commandObj, PatchType type, PatchEntry patchEntry)
    {
        if (commandObj is not TomlTable commandTable)
        {
            return;
        }

        PatchCommand command = new PatchCommand { Type = type };

        // Parse address
        if (commandTable.TryGetValue("address", out object addressObj))
        {
            command.Address = Convert.ToUInt32(addressObj);
        }

        // Parse value
        if (commandTable.TryGetValue("value", out object valueObj))
        {
            command.Value = ParseValue(valueObj, type);
        }

        patchEntry.Commands.Add(command);
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
    /// Parses a value based on the patch type.
    /// </summary>
    private static object? ParseValue(object? valueObj, PatchType type)
    {
        if (valueObj == null)
        {
            return null;
        }

        return type switch
        {
            PatchType.Be8 => Convert.ToByte(valueObj),
            PatchType.Be16 => Convert.ToUInt16(valueObj),
            PatchType.Be32 => Convert.ToUInt32(valueObj),
            // For be64, Tomlyn can parse large hex as signed long, so we need to handle the conversion carefully
            PatchType.Be64 => valueObj switch
            {
                ulong ul => ul,
                long l => unchecked((ulong)l),
                _ => Convert.ToUInt64(valueObj)
            },
            PatchType.F32 => Convert.ToSingle(valueObj),
            PatchType.F64 => Convert.ToDouble(valueObj),
            PatchType.String or PatchType.U16String => valueObj.ToString(),
            PatchType.Array => ParseArrayValue(valueObj),
            _ => valueObj.ToString()
        };
    }

    /// <summary>
    /// Parses an array value (hex string like "0x##*").
    /// Tomlyn already strips quotes from the string value.
    /// </summary>
    private static object? ParseArrayValue(object? valueObj)
    {
        if (valueObj is not string hexString || !hexString.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return valueObj;
        }
        // Remove "0x" prefix (Tomlyn already strips quotes)
        string hex = hexString.Substring(2);
        int byteCount = hex.Length / 2;
        byte[] bytes = new byte[byteCount];

        for (int i = 0; i < byteCount; i++)
        {
            bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
        }

        return bytes;
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
            // Validate file name
            ValidateFileName(Path.GetFileName(savePath));

            string content = ToTomlString();

            // Ensure directory exists
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
        sb.AppendLine($"title_name = \"{Document.TitleName}\"");
        sb.AppendLine($"title_id = \"{Document.TitleId.ToUpper()}\"");

        // Write hashes
        if (Document.Hashes.Count == 1)
        {
            sb.AppendLine($"hash = \"{Document.Hashes[0].ToUpper()}\"");
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

        // Write media IDs (Always commented out as per convention)
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

        // Write the patches
        foreach (PatchEntry patch in Document.Patches)
        {
            sb.AppendLine("[[patch]]");
            sb.AppendLine($"    name = \"{patch.Name}\"");
            sb.AppendLine($"    author = \"{patch.Author}\"");

            if (!string.IsNullOrEmpty(patch.Description))
            {
                sb.AppendLine($"    desc = \"{patch.Description}\"");
            }

            sb.AppendLine($"    is_enabled = {patch.IsEnabled.ToString().ToLower()}");
            sb.AppendLine();

            // Write commands grouped by type
            IEnumerable<IGrouping<PatchType, PatchCommand>> commandsByType = patch.Commands.GroupBy(c => c.Type);

            foreach (IGrouping<PatchType, PatchCommand> group in commandsByType)
            {
                string typeString = GetPatchTypeString(group.Key);

                foreach (PatchCommand command in group)
                {
                    sb.AppendLine($"    [[patch.{typeString}]]");
                    sb.AppendLine($"        address = 0x{command.Address:x8}");

                    string valueStr = command.GetValueAsString() ?? "0x00";
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
        // Pattern: XXXXXXXX - Game Title.patch.toml
        // Title ID must be uppercase hex (8 characters)
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