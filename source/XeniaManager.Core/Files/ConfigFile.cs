using System.Collections;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Models.Files.Config;

namespace XeniaManager.Core.Files;

/// <summary>
/// Handles loading, saving, and manipulation of Xenia configuration files (.config.toml).
/// Config files use the TOML format and contain emulator settings organized in sections.
/// </summary>
public class ConfigFile
{
    /// <summary>
    /// The configuration document containing all sections and options.
    /// </summary>
    public ConfigDocument Document { get; private set; }

    /// <summary>
    /// The file path of this config file.
    /// </summary>
    public string? FilePath { get; private set; }

    /// <summary>
    /// Gets the list of configuration sections.
    /// </summary>
    public IReadOnlyList<ConfigSection> Sections => Document.SectionsReadOnly;

    /// <summary>
    /// Gets or sets the header comment for the config file.
    /// </summary>
    public string? HeaderComment
    {
        get => Document.HeaderComment;
        set => Document.HeaderComment = value;
    }

    /// <summary>
    /// Private constructor to enforce factory methods.
    /// </summary>
    private ConfigFile()
    {
        Document = new ConfigDocument();
    }

    /// <summary>
    /// Creates a new empty configuration file.
    /// </summary>
    /// <param name="headerComment">Optional header comment for the config file.</param>
    /// <returns>A new ConfigFile instance.</returns>
    public static ConfigFile Create(string? headerComment = null)
    {
        Logger.Info<ConfigFile>("Creating new configuration file");

        ConfigFile configFile = new ConfigFile
        {
            Document = new ConfigDocument { HeaderComment = headerComment }
        };

        Logger.Trace<ConfigFile>("ConfigFile created successfully");
        return configFile;
    }

    /// <summary>
    /// Loads a configuration file from the specified path.
    /// </summary>
    /// <param name="filePath">The path to the .config.toml file to load.</param>
    /// <returns>A new ConfigFile instance.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
    /// <exception cref="ArgumentException">Thrown when the file has an invalid format.</exception>
    public static ConfigFile Load(string filePath)
    {
        Logger.Debug<ConfigFile>($"Loading configuration file from {filePath}");

        if (!File.Exists(filePath))
        {
            Logger.Error<ConfigFile>($"Configuration file does not exist: {filePath}");
            throw new FileNotFoundException($"Configuration file does not exist at {filePath}", filePath);
        }

        string content = File.ReadAllText(filePath);
        Logger.Info<ConfigFile>($"Loaded configuration file: {filePath} ({content.Length} bytes)");

        ConfigFile configFile = FromString(content);
        configFile.FilePath = filePath;

        return configFile;
    }

    /// <summary>
    /// Parses a configuration file from a TOML string.
    /// </summary>
    /// <param name="content">The TOML content of the configuration file.</param>
    /// <returns>A new ConfigFile instance.</returns>
    /// <exception cref="ArgumentException">Thrown when the TOML content is invalid.</exception>
    public static ConfigFile FromString(string content)
    {
        Logger.Trace<ConfigFile>($"Parsing configuration file from TOML string ({content.Length} bytes)");

        try
        {
            ConfigFile configFile = new ConfigFile();

            // Parse the raw content to preserve comments
            ParseRawContent(content, configFile);

            Logger.Info<ConfigFile>($"Successfully parsed configuration file with {configFile.Document.Sections.Count} sections");
            return configFile;
        }
        catch (Exception ex)
        {
            Logger.Error<ConfigFile>($"Failed to parse configuration file");
            Logger.LogExceptionDetails<ConfigFile>(ex);
            throw new ArgumentException($"Invalid TOML content.", nameof(content), ex);
        }
    }

    /// <summary>
    /// Parses the raw TOML content to preserve comments and formatting.
    /// </summary>
    private static void ParseRawContent(string rawToml, ConfigFile configFile)
    {
        string[] lines = rawToml.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);

        ConfigSection? currentSection = null;
        ConfigOption? currentOption = null;
        List<string>? pendingCommentLines = null;

        foreach (string line in lines)
        {
            string trimmedLine = line.Trim();

            // Check for header comment (lines before the first section)
            if (trimmedLine.StartsWith("#") && currentSection == null && configFile.Sections.Count == 0)
            {
                if (configFile.HeaderComment == null)
                {
                    configFile.HeaderComment = trimmedLine.Substring(1).Trim();
                }
                else
                {
                    configFile.HeaderComment += "\n" + trimmedLine.Substring(1).Trim();
                }
                continue;
            }

            // Check for the section header [SectionName]
            if (trimmedLine.StartsWith("["))
            {
                Match sectionMatch = Regex.Match(trimmedLine, @"^\[([^\]]+)\]");
                if (sectionMatch.Success)
                {
                    string sectionName = sectionMatch.Groups[1].Value.Trim();
                    currentSection = configFile.Document.AddSection(sectionName);
                    currentOption = null;
                    pendingCommentLines = null;
                    Logger.Debug<ConfigFile>($"Parsing section: {sectionName}");
                }
                continue;
            }

            // Skip empty lines
            if (string.IsNullOrEmpty(trimmedLine))
            {
                continue;
            }

            // Check if this is a continuation line (starts with whitespace and #)
            if (currentOption != null && !string.IsNullOrEmpty(line) && (line.StartsWith(" ") || line.StartsWith("\t")) && trimmedLine.StartsWith("#"))
            {
                // This is a continuation of the previous option's comment
                string commentText = trimmedLine.Substring(1).Trim();

                if (currentOption.Comment == null)
                {
                    currentOption.Comment = commentText;
                }
                else
                {
                    currentOption.Comment += "\n" + commentText;
                }
                currentOption.HasMultiLineComment = true;
                continue;
            }

            // Check for option line
            if (currentSection != null && trimmedLine.Contains("="))
            {
                // First, attach any pending comments to this option if they exist
                if (pendingCommentLines != null && pendingCommentLines.Count > 0)
                {
                    // These are standalone comments before the option, not part of it
                    pendingCommentLines = null;
                }

                ParseOptionLine(trimmedLine, currentSection, ref currentOption);
            }
            else if (currentSection != null && trimmedLine.StartsWith("#"))
            {
                // Standalone comment line - could be a description for the next option
                // Store it temporarily
                if (pendingCommentLines == null)
                {
                    pendingCommentLines = [];
                }
                pendingCommentLines.Add(trimmedLine.Substring(1).Trim());
            }
        }
    }

    /// <summary>
    /// Parses a single option line.
    /// </summary>
    private static void ParseOptionLine(string trimmedLine, ConfigSection section, ref ConfigOption? currentOption)
    {
        // Pattern: option_name = value # comment
        // or: #option_name = value # comment (commented option)

        bool isCommented = trimmedLine.StartsWith("#");
        string lineWithoutLeadingComment = isCommented ? trimmedLine.Substring(1).TrimStart() : trimmedLine;

        // Split by first = to get name and value+comment
        int equalsIndex = lineWithoutLeadingComment.IndexOf('=');
        if (equalsIndex == -1)
        {
            return;
        }

        string optionName = lineWithoutLeadingComment.Substring(0, equalsIndex).Trim();
        string valueAndComment = lineWithoutLeadingComment.Substring(equalsIndex + 1);

        // Extract inline comment - look for # that's after the value
        // Need to handle quoted strings - # inside quotes is part of the value
        string? inlineComment = null;
        string valuePart;
        string padding = string.Empty;

        // Find the comment marker but skip # inside quoted strings
        int commentIndex = -1;
        bool inQuotes = false;
        char quoteChar = '\0';

        for (int i = 0; i < valueAndComment.Length; i++)
        {
            char c = valueAndComment[i];

            // Track if we're inside a quoted string
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

            // Look for # outside of quotes
            if (!inQuotes && c == '#')
            {
                // Check if there's a whitespace or tab before the #
                if (i > 0 && (char.IsWhiteSpace(valueAndComment[i - 1]) || valueAndComment[i - 1] == '\t'))
                {
                    commentIndex = i;
                    break;
                }
            }
        }

        if (commentIndex >= 0)
        {
            // Extract padding (whitespace between value and #)
            string valueAndPadding = valueAndComment.Substring(0, commentIndex);
            valuePart = valueAndPadding.Trim();

            // Padding is only the whitespace (spaces/tabs) between value and #
            // We need to strip any trailing quotes from the value first
            padding = valueAndPadding.Substring(valuePart.Length);

            // Extract comment (everything after #)
            string commentText = valueAndComment.Substring(commentIndex + 1).Trim();

            // Check if this is a multi-line comment (contains tab before #)
            bool hasMultiLineComment = commentIndex > 0 && valueAndComment[commentIndex - 1] == '\t';

            inlineComment = commentText;

            // Store the option with padding info
            currentOption = section.AddOption(optionName, ParseValue(valuePart, out ConfigOptionType type), inlineComment, isCommented, type, padding);
            currentOption.HasMultiLineComment = hasMultiLineComment;
        }
        else
        {
            valuePart = valueAndComment.Trim();
            currentOption = section.AddOption(optionName, ParseValue(valuePart, out ConfigOptionType type), null, isCommented, type, "");
        }

        Logger.Debug<ConfigFile>($"Parsed option: {optionName} = {valuePart} (commented: {isCommented})");
    }

    /// <summary>
    /// Parses a value string to the appropriate type.
    /// </summary>
    private static object? ParseValue(string valueStr, out ConfigOptionType type)
    {
        valueStr = valueStr.Trim();

        // Empty string
        if (string.IsNullOrEmpty(valueStr))
        {
            type = ConfigOptionType.String;
            return string.Empty;
        }

        // Boolean
        if (valueStr.Equals("true", StringComparison.OrdinalIgnoreCase))
        {
            type = ConfigOptionType.Boolean;
            return true;
        }
        if (valueStr.Equals("false", StringComparison.OrdinalIgnoreCase))
        {
            type = ConfigOptionType.Boolean;
            return false;
        }

        // String (quoted)
        if ((valueStr.StartsWith("\"") && valueStr.EndsWith("\"")) || (valueStr.StartsWith("'") && valueStr.EndsWith("'")))
        {
            type = ConfigOptionType.String;
            return valueStr.Substring(1, valueStr.Length - 2);
        }

        // Array
        if (valueStr.StartsWith("[") && valueStr.EndsWith("]"))
        {
            type = ConfigOptionType.Array;
            return ParseArrayValue(valueStr);
        }

        // Integer or Float
        if (long.TryParse(valueStr, out long longValue))
        {
            type = ConfigOptionType.Integer;
            return longValue;
        }

        if (double.TryParse(valueStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double doubleValue))
        {
            type = ConfigOptionType.Float;
            return doubleValue;
        }

        // Default to string
        type = ConfigOptionType.String;
        return valueStr;
    }

    /// <summary>
    /// Parses an array value.
    /// </summary>
    private static List<object> ParseArrayValue(string arrayStr)
    {
        List<object> result = new List<object>();

        // Remove brackets
        string inner = arrayStr.Substring(1, arrayStr.Length - 2).Trim();

        if (string.IsNullOrEmpty(inner))
        {
            return result;
        }

        // Simple parsing - split by comma (doesn't handle nested arrays or strings with commas)
        string[] parts = inner.Split(',');
        foreach (string part in parts)
        {
            string trimmed = part.Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                continue;
            }

            // Try to parse as a number or boolean, otherwise keep as string
            if (trimmed.Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                result.Add(true);
            }
            else if (trimmed.Equals("false", StringComparison.OrdinalIgnoreCase))
            {
                result.Add(false);
            }
            else if ((trimmed.StartsWith("\"") && trimmed.EndsWith("\"")) || (trimmed.StartsWith("'") && trimmed.EndsWith("'")))
            {
                result.Add(trimmed.Substring(1, trimmed.Length - 2));
            }
            else if (long.TryParse(trimmed, out long longValue))
            {
                result.Add(longValue);
            }
            else if (double.TryParse(trimmed, NumberStyles.Any, CultureInfo.InvariantCulture, out double doubleValue))
            {
                result.Add(doubleValue);
            }
            else
            {
                result.Add(trimmed);
            }
        }

        return result;
    }

    /// <summary>
    /// Saves the configuration file to the specified path.
    /// If no path is specified, saves to the original loaded path.
    /// </summary>
    /// <param name="filePath">Optional path to save to. If null, uses the original path.</param>
    /// <exception cref="InvalidOperationException">Thrown when no path is specified and the file was not loaded from a path.</exception>
    public void Save(string? filePath = null)
    {
        string savePath = filePath ?? FilePath!;

        if (string.IsNullOrEmpty(savePath))
        {
            Logger.Error<ConfigFile>("No save path specified and the file was not loaded from a path");
            throw new InvalidOperationException("No save path specified and the file was not loaded from a path");
        }

        Logger.Debug<ConfigFile>($"Saving configuration file to {savePath}");

        try
        {
            string content = ToTomlString();

            // Ensure directory exists
            string? directory = Path.GetDirectoryName(savePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Logger.Info<ConfigFile>($"Creating directory: {directory}");
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(savePath, content);
            FilePath = savePath;
            Logger.Info<ConfigFile>($"Successfully saved configuration file to {savePath} ({content.Length} bytes)");
        }
        catch (Exception ex)
        {
            Logger.Error<ConfigFile>($"Failed to save configuration file to {savePath}: {ex.Message}");
            Logger.LogExceptionDetails<ConfigFile>(ex);
            throw;
        }
    }

    /// <summary>
    /// Converts the configuration document to a TOML string, preserving comments.
    /// </summary>
    /// <returns>The TOML string representation of the configuration file.</returns>
    public string ToTomlString()
    {
        Logger.Trace<ConfigFile>("Converting configuration document to TOML string");

        StringBuilder sb = new StringBuilder();

        // Write header comment if present
        if (!string.IsNullOrEmpty(Document.HeaderComment))
        {
            string[] headerLines = Document.HeaderComment.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
            foreach (string line in headerLines)
            {
                sb.AppendLine($"# {line}");
            }
            sb.AppendLine();
        }

        // Write each section
        foreach (ConfigSection section in Document.Sections)
        {
            // Write section header
            sb.AppendLine($"[{section.Name}]");

            // Write options
            foreach (ConfigOption option in section.Options)
            {
                WriteOption(sb, option);
            }

            sb.AppendLine();
        }

        string result = sb.ToString();
        Logger.Debug<ConfigFile>($"Generated TOML string: {result.Length} bytes");
        return result;
    }

    /// <summary>
    /// Writes a single option to the string builder.
    /// </summary>
    private static void WriteOption(StringBuilder sb, ConfigOption option)
    {
        string valueStr = FormatValue(option.Value, option.Type);
        string commentPrefix = option.IsCommented ? "#" : "";

        // Build the option part (name = value)
        string optionPart = $"{commentPrefix}{option.Name} = {valueStr}";

        // Add inline comment if present
        if (!string.IsNullOrEmpty(option.Comment))
        {
            // Format multi-line comments
            string[] commentLines = option.Comment.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);

            // Xenia uses value_alignment of 50 characters
            const int valueAlignment = 50;

            if (commentLines.Length == 1)
            {
                // Single line comment - pad to alignment, then tab + # + comment
                if (optionPart.Length < valueAlignment)
                {
                    sb.Append(optionPart);
                    sb.Append(' ', valueAlignment - optionPart.Length);
                }
                else
                {
                    sb.Append(optionPart);
                    sb.Append(' ');
                }
                sb.AppendLine($"\t# {commentLines[0]}");
            }
            else
            {
                // Multi-line comment - first line inline, rest as continuation
                if (optionPart.Length < valueAlignment)
                {
                    sb.Append(optionPart);
                    sb.Append(' ', valueAlignment - optionPart.Length);
                }
                else
                {
                    sb.Append(optionPart);
                    sb.Append(' ');
                }
                sb.AppendLine($"\t# {commentLines[0]}");

                // Continuation lines: pad to value_alignment, then tab + # + comment
                string continuationPadding = new string(' ', valueAlignment);

                for (int i = 1; i < commentLines.Length; i++)
                {
                    sb.Append(continuationPadding);
                    sb.AppendLine($"\t# {commentLines[i]}");
                }
            }
        }
        else
        {
            sb.AppendLine(optionPart);
        }
    }

    /// <summary>
    /// Formats a value for TOML output.
    /// </summary>
    private static string FormatValue(object? value, ConfigOptionType type)
    {
        if (value == null)
        {
            return "\"\"";
        }

        // If the type is unknown, infer it from the value
        if (type == ConfigOptionType.Unknown)
        {
            if (value is bool)
            {
                return (bool)value ? "true" : "false";
            }
            if (value is string str)
            {
                return $"\"{str}\"";
            }
            if (value is IEnumerable && value is not string)
            {
                return FormatArrayValue(value);
            }
            if (value is double d)
            {
                return d.ToString(CultureInfo.InvariantCulture);
            }
            return value.ToString() ?? "\"\"";
        }

        return type switch
        {
            ConfigOptionType.Boolean => (bool)value ? "true" : "false",
            ConfigOptionType.Integer => value.ToString()!,
            ConfigOptionType.Float => Convert.ToDouble(value).ToString(CultureInfo.InvariantCulture),
            ConfigOptionType.String => $"\"{value}\"",
            ConfigOptionType.Array => FormatArrayValue(value),
            _ => value is string ? $"\"{value}\"" : (value.ToString() ?? "\"\"")
        };
    }

    /// <summary>
    /// Formats an array value for TOML output.
    /// </summary>
    private static string FormatArrayValue(object? value)
    {
        if (value is not IEnumerable enumerable || value is string)
        {
            return "[]";
        }

        StringBuilder sb = new StringBuilder("[");
        bool first = true;

        foreach (object? item in enumerable)
        {
            if (!first)
            {
                sb.Append(", ");
            }

            if (item is bool boolValue)
            {
                sb.Append(boolValue.ToString().ToLower());
            }
            else if (item is string stringValue)
            {
                sb.Append($"\"{stringValue}\"");
            }
            else if (item is double doubleValue)
            {
                sb.Append(doubleValue.ToString(CultureInfo.InvariantCulture));
            }
            else
            {
                sb.Append(item?.ToString() ?? "null");
            }

            first = false;
        }

        sb.Append("]");
        return sb.ToString();
    }

    /// <summary>
    /// Gets a value from the configuration.
    /// </summary>
    /// <typeparam name="T">The type to cast the value to.</typeparam>
    /// <param name="sectionName">The name of the section.</param>
    /// <param name="optionName">The name of the option.</param>
    /// <param name="defaultValue">The default value if not found.</param>
    /// <returns>The value of the option, or the default value if not found.</returns>
    public T GetValue<T>(string sectionName, string optionName, T defaultValue = default!)
    {
        return Document.GetValue<T>(sectionName, optionName, defaultValue);
    }

    /// <summary>
    /// Sets a value in the configuration.
    /// </summary>
    /// <param name="sectionName">The name of the section.</param>
    /// <param name="optionName">The name of the option.</param>
    /// <param name="value">The value to set.</param>
    public void SetValue(string sectionName, string optionName, object? value)
    {
        Document.SetValue(sectionName, optionName, value);
    }

    /// <summary>
    /// Gets a section by name.
    /// </summary>
    /// <param name="name">The name of the section to find.</param>
    /// <returns>The ConfigSection if found, null otherwise.</returns>
    public ConfigSection? GetSection(string name)
    {
        return Document.GetSection(name);
    }

    /// <summary>
    /// Gets or creates a section by name.
    /// </summary>
    /// <param name="name">The name of the section.</param>
    /// <returns>The existing or newly created ConfigSection.</returns>
    public ConfigSection GetOrCreateSection(string name)
    {
        return Document.GetOrCreateSection(name);
    }

    /// <summary>
    /// Adds a new section to the configuration.
    /// </summary>
    /// <param name="name">The name of the section.</param>
    /// <param name="description">Optional description for the section.</param>
    /// <returns>The created ConfigSection.</returns>
    public ConfigSection AddSection(string name, string? description = null)
    {
        Logger.Info<ConfigFile>($"Adding new section: {name}");
        return Document.AddSection(name, description);
    }

    /// <summary>
    /// Removes a section by name.
    /// </summary>
    /// <param name="name">The name of the section to remove.</param>
    /// <returns>True if the section was found and removed, false otherwise.</returns>
    public bool RemoveSection(string name)
    {
        Logger.Info<ConfigFile>($"Removing section: {name}");
        return Document.RemoveSection(name);
    }
}