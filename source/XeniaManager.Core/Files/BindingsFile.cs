using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Models.Files.Bindings;

namespace XeniaManager.Core.Files;

/// <summary>
/// Handles loading, saving, and manipulation of Xenia bindings files (bindings.ini).
/// Bindings files contain sections with key-value pairs representing keyboard-to-controller mappings.
/// </summary>
public class BindingsFile
{
    /// <summary>
    /// The bindings document containing all sections.
    /// </summary>
    public BindingsDocument Document { get; private set; }

    /// <summary>
    /// The file path of this bindings file.
    /// </summary>
    public string? FilePath { get; private set; }

    /// <summary>
    /// Gets the list of bindings sections.
    /// </summary>
    public IReadOnlyList<BindingsSection> Sections => Document.SectionsReadOnly;

    /// <summary>
    /// Gets or sets the header comment for the bindings file.
    /// </summary>
    public string? HeaderComment
    {
        get => Document.HeaderComment;
        set => Document.HeaderComment = value;
    }

    /// <summary>
    /// Private constructor to enforce factory methods.
    /// </summary>
    private BindingsFile()
    {
        Document = new BindingsDocument();
    }

    /// <summary>
    /// Creates a new empty bindings file.
    /// </summary>
    /// <param name="headerComment">Optional header comment for the bindings file.</param>
    /// <returns>A new BindingsFile instance.</returns>
    public static BindingsFile Create(string? headerComment = null)
    {
        Logger.Info<BindingsFile>("Creating new bindings file");

        BindingsFile bindingsFile = new BindingsFile
        {
            Document = new BindingsDocument { HeaderComment = headerComment }
        };

        Logger.Trace<BindingsFile>("BindingsFile created successfully");
        return bindingsFile;
    }

    /// <summary>
    /// Loads a bindings file from the specified path.
    /// </summary>
    /// <param name="filePath">The path to the bindings.ini file to load.</param>
    /// <returns>A new BindingsFile instance.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
    /// <exception cref="ArgumentException">Thrown when the file has an invalid format.</exception>
    public static BindingsFile Load(string filePath)
    {
        Logger.Debug<BindingsFile>($"Loading bindings file from {filePath}");

        if (!File.Exists(filePath))
        {
            Logger.Error<BindingsFile>($"Bindings file does not exist: {filePath}");
            throw new FileNotFoundException($"Bindings file does not exist at {filePath}", filePath);
        }

        string content = File.ReadAllText(filePath);
        Logger.Info<BindingsFile>($"Loaded bindings file: {filePath} ({content.Length} bytes)");

        BindingsFile bindingsFile = FromString(content);
        bindingsFile.FilePath = filePath;

        return bindingsFile;
    }

    /// <summary>
    /// Parses a bindings file from a string.
    /// </summary>
    /// <param name="content">The bindings content to parse.</param>
    /// <returns>A new BindingsFile instance.</returns>
    /// <exception cref="ArgumentException">Thrown when the bindings content is invalid.</exception>
    public static BindingsFile FromString(string content)
    {
        Logger.Trace<BindingsFile>($"Parsing bindings file from string ({content.Length} bytes)");

        try
        {
            BindingsFile bindingsFile = new BindingsFile();

            ParseRawContent(content, bindingsFile);

            Logger.Info<BindingsFile>($"Successfully parsed bindings file with {bindingsFile.Document.Sections.Count} sections");
            return bindingsFile;
        }
        catch (Exception ex)
        {
            Logger.Error<BindingsFile>($"Failed to parse bindings file");
            Logger.LogExceptionDetails<BindingsFile>(ex);
            throw new ArgumentException($"Invalid bindings content.", nameof(content), ex);
        }
    }

    /// <summary>
    /// Parses the raw bindings content to preserve comments and formatting.
    /// </summary>
    private static void ParseRawContent(string rawBindings, BindingsFile bindingsFile)
    {
        string[] lines = rawBindings.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);

        BindingsSection? currentSection = null;
        BindingsEntry? currentEntry = null;
        List<string>? pendingCommentLines = null;
        bool hasStartedSections = false;

        foreach (string line in lines)
        {
            string trimmedLine = line.Trim();

            // Check for lines starting with semicolon
            // Note: "; " followed by "=" means ";" is the key (semicolon key binding), not a comment
            // Pattern: "; = value" or "; = value ; comment" = semicolon key binding
            // Pattern: ";key = value" = commented entry
            // Pattern: "; comment" = standalone comment
            if (trimmedLine.StartsWith(";") && !hasStartedSections)
            {
                // Check if this is a semicolon key binding ("; = value")
                if (trimmedLine.Length >= 3 && trimmedLine[1] == ' ' && trimmedLine[2] == '=')
                {
                    // This is a semicolon key binding (e.g., "; = RS-Right" or "; = weapon10 ; comment")
                    if (currentSection == null)
                    {
                        currentSection = bindingsFile.Document.AddSection("Default");
                    }
                    ParseEntryLine(trimmedLine, currentSection, ref currentEntry, false);
                }
                else
                {
                    // Check if this is a commented entry (contains =)
                    string lineWithoutLeadingComment = trimmedLine.Substring(1).TrimStart();
                    if (lineWithoutLeadingComment.Contains("="))
                    {
                        // Ensure we have a default section for entries before the first section header
                        if (currentSection == null)
                        {
                            currentSection = bindingsFile.Document.AddSection("Default");
                        }
                        ParseEntryLine(lineWithoutLeadingComment, currentSection, ref currentEntry, true);
                    }
                    else
                    {
                        // This is a header comment or standalone comment
                        string commentText = lineWithoutLeadingComment.Trim();
                        if (bindingsFile.HeaderComment == null)
                        {
                            bindingsFile.HeaderComment = commentText;
                        }
                        else
                        {
                            bindingsFile.HeaderComment += "\n" + commentText;
                        }
                    }
                }
                continue;
            }

            // Check for the section header [SectionName]
            if (trimmedLine.StartsWith("["))
            {
                Match sectionMatch = Regex.Match(trimmedLine, @"^\[([^\]]+)\]");
                if (sectionMatch.Success)
                {
                    string sectionHeader = sectionMatch.Groups[1].Value.Trim();

                    // Parse section header to extract title IDs and type
                    ParseSectionHeader(sectionHeader, bindingsFile, out currentSection);

                    hasStartedSections = true;
                    currentEntry = null;
                    pendingCommentLines = null;
                    Logger.Debug<BindingsFile>($"Parsing section: {currentSection.Name}");
                }
                continue;
            }

            // Skip empty lines
            if (string.IsNullOrEmpty(trimmedLine))
            {
                continue;
            }

            // Check if this is a comment line, semicolon key binding, or commented entry
            if (trimmedLine.StartsWith(";") && hasStartedSections)
            {
                // Check if this is a semicolon key binding ("; = value")
                if (trimmedLine.Length >= 3 && trimmedLine[1] == ' ' && trimmedLine[2] == '=')
                {
                    // This is a semicolon key binding (e.g., "; = RS-Right" or "; = weapon10 ; comment")
                    if (currentSection != null)
                    {
                        ParseEntryLine(trimmedLine, currentSection, ref currentEntry, false);
                    }
                }
                else
                {
                    string lineWithoutLeadingComment = trimmedLine.Substring(1).TrimStart();

                    // Check if this is a commented entry (contains =)
                    if (currentSection != null && lineWithoutLeadingComment.Contains("="))
                    {
                        // This is a commented entry like ";key = value ; comment"
                        ParseEntryLine(lineWithoutLeadingComment, currentSection, ref currentEntry, true);
                    }
                    else
                    {
                        // This is a standalone comment
                        string commentText = lineWithoutLeadingComment.Trim();

                        // Could be a comment for the next entry or a standalone comment
                        if (pendingCommentLines == null)
                        {
                            pendingCommentLines = [];
                        }
                        pendingCommentLines.Add(commentText);
                    }
                }
                continue;
            }

            // Check for the entry line (Key = Value)
            if (trimmedLine.Contains("="))
            {
                // Ensure we have a section (create default if entries appear before the first section header)
                if (currentSection == null)
                {
                    currentSection = bindingsFile.Document.AddSection("Default");
                }

                // First, attach any pending comments to this entry if they exist
                if (pendingCommentLines != null && pendingCommentLines.Count > 0)
                {
                    // These are standalone comments before the entry, not part of it
                    pendingCommentLines = null;
                }

                ParseEntryLine(trimmedLine, currentSection, ref currentEntry);
            }
        }
    }

    /// <summary>
    /// Parses a section header to extract title IDs and type.
    /// </summary>
    private static void ParseSectionHeader(string sectionHeader, BindingsFile bindingsFile, out BindingsSection section)
    {
        List<uint> titleIds = [];
        string type = sectionHeader;
        string? titleName = null;

        // Find the first space that separates title IDs from the type description
        int firstSpaceIndex = sectionHeader.IndexOf(' ');

        if (firstSpaceIndex > 0)
        {
            string titleIdPart = sectionHeader.Substring(0, firstSpaceIndex);
            string remainingPart = sectionHeader.Substring(firstSpaceIndex + 1).Trim();

            // Check if the titleIdPart contains commas (multiple title IDs)
            if (titleIdPart.Contains(','))
            {
                string[] titleIdStrings = titleIdPart.Split(',');
                foreach (string titleIdStr in titleIdStrings)
                {
                    if (uint.TryParse(titleIdStr.Trim(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint titleId))
                    {
                        titleIds.Add(titleId);
                    }
                }
            }
            else
            {
                // Single title ID
                if (uint.TryParse(titleIdPart, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint titleId))
                {
                    titleIds.Add(titleId);
                }
            }

            // If we successfully parsed title IDs, parse the type and title name
            if (titleIds.Count > 0)
            {
                // Extract type (before " - ") and title name (after " - ")
                // e.g., "Default - Perfect Dark Zero" -> Type: "Default", TitleName: "Perfect Dark Zero"
                int dashIndex = remainingPart.IndexOf(" - ", StringComparison.Ordinal);
                if (dashIndex >= 0 && dashIndex < remainingPart.Length - 3)
                {
                    type = remainingPart.Substring(0, dashIndex).Trim();
                    titleName = remainingPart.Substring(dashIndex + 3).Trim();
                }
                else
                {
                    type = remainingPart;
                }
            }
        }

        // Create the section
        section = bindingsFile.Document.AddSection(sectionHeader);
        section.TitleIds = titleIds;
        section.Type = type;
        section.TitleName = titleName;
    }

    /// <summary>
    /// Parses a single entry line.
    /// </summary>
    private static void ParseEntryLine(string trimmedLine, BindingsSection section, ref BindingsEntry? currentEntry, bool isCommented = false)
    {
        // Split by first = to get key and value+comment
        int equalsIndex = trimmedLine.IndexOf('=');
        if (equalsIndex == -1)
        {
            return;
        }

        string key = trimmedLine.Substring(0, equalsIndex).Trim();
        string valueAndComment = trimmedLine.Substring(equalsIndex + 1).Trim();

        // Extract inline comment - look for ; that's after the value
        string? inlineComment = null;
        string valuePart;

        int commentIndex = -1;
        bool inQuotes = false;
        char quoteChar = '\0';

        for (int i = 0; i < valueAndComment.Length; i++)
        {
            char c = valueAndComment[i];

            if ((c == '"' || c == '\'') && (i == 0 || valueAndComment[i - 1] != '\\'))
            {
                if (!inQuotes)
                {
                    inQuotes = true;
                    quoteChar = c;
                }
                else if (c == quoteChar)
                {
                    inQuotes = false;
                    quoteChar = '\0';
                }
            }

            if (!inQuotes && c == ';')
            {
                commentIndex = i;
                break;
            }
        }

        if (commentIndex >= 0)
        {
            valuePart = valueAndComment.Substring(0, commentIndex).Trim();
            inlineComment = valueAndComment.Substring(commentIndex + 1).Trim();
        }
        else
        {
            // No ; comment separator found
            // Check if the value contains spaces - if so, it's a comment, not a binding
            // Valid binding values are single tokens (e.g., "weapon10", "LS-Up", "Start")
            // This handles cases like: "; = weapon10 will reload weapons independently..."
            if (valueAndComment.Contains(' '))
            {
                // Value contains spaces, so this is a comment line, not a valid binding
                // Skip this entry
                return;
            }
            else
            {
                valuePart = valueAndComment;
            }
        }

        currentEntry = section.AddEntry(key, valuePart, inlineComment, isCommented);
        Logger.Debug<BindingsFile>($"Parsed entry: {key} = {valuePart} (commented: {isCommented})");
    }

    /// <summary>
    /// Saves the bindings file to the specified path.
    /// </summary>
    public void Save(string? filePath = null)
    {
        string savePath = filePath ?? FilePath!;

        if (string.IsNullOrEmpty(savePath))
        {
            Logger.Error<BindingsFile>("No save path specified and the file was not loaded from a path");
            throw new InvalidOperationException("No save path specified and the file was not loaded from a path");
        }

        Logger.Debug<BindingsFile>($"Saving bindings file to {savePath}");

        try
        {
            string content = ToBindingsString();

            string? directory = Path.GetDirectoryName(savePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Logger.Info<BindingsFile>($"Creating directory: {directory}");
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(savePath, content);
            FilePath = savePath;
            Logger.Info<BindingsFile>($"Successfully saved bindings file to {savePath} ({content.Length} bytes)");
        }
        catch (Exception ex)
        {
            Logger.Error<BindingsFile>($"Failed to save bindings file to {savePath}: {ex.Message}");
            Logger.LogExceptionDetails<BindingsFile>(ex);
            throw;
        }
    }

    /// <summary>
    /// Converts the bindings document to a string, preserving comments.
    /// </summary>
    public string ToBindingsString()
    {
        Logger.Trace<BindingsFile>("Converting bindings document to string");

        StringBuilder sb = new StringBuilder();

        // Write header comment if present
        if (!string.IsNullOrEmpty(Document.HeaderComment))
        {
            string[] headerLines = Document.HeaderComment.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
            foreach (string line in headerLines)
            {
                sb.AppendLine($"; {line}");
            }
            sb.AppendLine();
        }

        // Write each section
        foreach (BindingsSection section in Document.Sections)
        {
            // Write section header
            sb.AppendLine($"[{section.Name}]");

            // Write entries
            foreach (BindingsEntry entry in section.Entries)
            {
                WriteEntry(sb, entry);
            }

            sb.AppendLine();
        }

        string result = sb.ToString();
        Logger.Debug<BindingsFile>($"Generated bindings string: {result.Length} bytes");
        return result;
    }

    /// <summary>
    /// Writes a single entry to the string builder.
    /// </summary>
    private static void WriteEntry(StringBuilder sb, BindingsEntry entry)
    {
        string valueStr = entry.Value?.ToString() ?? string.Empty;
        string commentPrefix = entry.IsCommented ? ";" : "";

        // Build the entry part (key = value)
        string entryPart = $"{commentPrefix}{entry.Key} = {valueStr}";

        // Add inline comment if present
        if (!string.IsNullOrEmpty(entry.Comment))
        {
            sb.AppendLine($"{entryPart} ; {entry.Comment}");
        }
        else
        {
            sb.AppendLine(entryPart);
        }
    }

    /// <summary>
    /// Gets a section by name.
    /// </summary>
    public BindingsSection? GetSection(string name)
    {
        return Document.GetSection(name);
    }

    /// <summary>
    /// Gets or creates a section by name.
    /// </summary>
    public BindingsSection GetOrCreateSection(string name)
    {
        return Document.GetOrCreateSection(name);
    }

    /// <summary>
    /// Adds a new section to the bindings file.
    /// </summary>
    public BindingsSection AddSection(string name, string? type = null)
    {
        return Document.AddSection(name, type);
    }

    /// <summary>
    /// Removes a section by name.
    /// </summary>
    public bool RemoveSection(string name)
    {
        return Document.RemoveSection(name);
    }

    /// <summary>
    /// Gets a section by title ID.
    /// If no matching section is found, creates a new section based on the "Default" section.
    /// </summary>
    /// <param name="titleId">The title ID to search for.</param>
    /// <param name="type">Optional type filter (e.g., "Default", "Vehicle").</param>
    /// <param name="titleName">Optional game title name for creating a new section.</param>
    /// <returns>The matching section, or a new section created from Default if no match is found.</returns>
    public BindingsSection? GetSectionByTitleId(uint titleId, string? type = null, string? titleName = null)
    {
        return Document.GetSectionByTitleId(titleId, type, titleName);
    }

    /// <summary>
    /// Gets all sections that contain the specified title ID.
    /// If no matching sections are found, creates a new "Default" section with the specified title ID.
    /// </summary>
    /// <param name="titleId">The title ID to search for.</param>
    /// <param name="titleName">Optional game title name for creating a new section.</param>
    /// <returns>A list of matching sections, or a new list containing a section created from Default.</returns>
    public List<BindingsSection> GetSectionsByTitleId(uint titleId, string? titleName = null)
    {
        return Document.GetSectionsByTitleId(titleId, titleName);
    }

    /// <summary>
    /// Gets a section entry by value.
    /// </summary>
    public BindingsEntry? GetEntryByValue(string sectionName, string value)
    {
        return Document.GetEntryByValue(sectionName, value);
    }

    /// <summary>
    /// Gets a value from the bindings file.
    /// </summary>
    public T GetValue<T>(string sectionName, string entryName, T defaultValue = default!)
    {
        return Document.GetValue<T>(sectionName, entryName, defaultValue);
    }

    /// <summary>
    /// Sets a value in the bindings file.
    /// </summary>
    public void SetValue(string sectionName, string entryName, object? value)
    {
        Document.SetValue(sectionName, entryName, value);
    }
}