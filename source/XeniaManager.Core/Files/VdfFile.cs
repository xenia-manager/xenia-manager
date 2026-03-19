using System.Text;
using System.Text.RegularExpressions;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Models.Files.Vdf;

namespace XeniaManager.Core.Files;

/// <summary>
/// Handles loading, saving, and manipulation of Steam VDF (Valve Data File) files.
/// VDF is a file format used in Source games and Steam to store metadata, configurations, and resource information.
/// VDF files use a nested key-value structure with quoted strings.
/// </summary>
public class VdfFile
{
    /// <summary>
    /// The VDF document containing all nodes.
    /// </summary>
    public VdfDocument Document { get; private set; }

    /// <summary>
    /// The file path of this VDF file.
    /// </summary>
    public string? FilePath { get; private set; }

    /// <summary>
    /// Gets the root node of the document.
    /// </summary>
    public VdfNode? Root => Document.Root;

    /// <summary>
    /// Gets or sets the header comment for the VDF file.
    /// Note: The VDF format doesn't natively support comments, but this is preserved for compatibility.
    /// </summary>
    public string? HeaderComment
    {
        get => Document.HeaderComment;
        set => Document.HeaderComment = value;
    }

    /// <summary>
    /// Private constructor to enforce factory methods.
    /// </summary>
    private VdfFile()
    {
        Document = new VdfDocument();
    }

    /// <summary>
    /// Creates a new empty VDF file.
    /// </summary>
    /// <param name="rootKey">The key/name of the root node.</param>
    /// <param name="headerComment">Optional header comment (non-standard, for compatibility).</param>
    /// <returns>A new VdfFile instance.</returns>
    public static VdfFile Create(string rootKey, string? headerComment = null)
    {
        Logger.Info<VdfFile>($"Creating new VDF file with root: {rootKey}");

        VdfFile vdfFile = new VdfFile
        {
            Document = new VdfDocument { HeaderComment = headerComment }
        };
        vdfFile.Document.SetRoot(rootKey);

        Logger.Trace<VdfFile>("VdfFile created successfully");
        return vdfFile;
    }

    /// <summary>
    /// Loads a VDF file from the specified path.
    /// </summary>
    /// <param name="filePath">The path to the .vdf file to load.</param>
    /// <returns>A new VdfFile instance.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
    /// <exception cref="ArgumentException">Thrown when the file has an invalid format.</exception>
    public static VdfFile Load(string filePath)
    {
        Logger.Debug<VdfFile>($"Loading VDF file from {filePath}");

        if (!File.Exists(filePath))
        {
            Logger.Error<VdfFile>($"VDF file does not exist: {filePath}");
            throw new FileNotFoundException($"VDF file does not exist at {filePath}", filePath);
        }

        string content = File.ReadAllText(filePath);
        Logger.Info<VdfFile>($"Loaded VDF file: {filePath} ({content.Length} bytes)");

        VdfFile vdfFile = FromString(content);
        vdfFile.FilePath = filePath;

        return vdfFile;
    }

    /// <summary>
    /// Parses a VDF file from a string.
    /// </summary>
    /// <param name="content">The VDF content to parse.</param>
    /// <returns>A new VdfFile instance.</returns>
    /// <exception cref="ArgumentException">Thrown when the VDF content is invalid.</exception>
    public static VdfFile FromString(string content)
    {
        Logger.Trace<VdfFile>($"Parsing VDF file from string ({content.Length} bytes)");

        try
        {
            VdfFile vdfFile = new VdfFile();
            ParseRawContent(content, vdfFile);

            Logger.Info<VdfFile>($"Successfully parsed VDF file with root: {vdfFile.Document.Root?.Key ?? "null"}");
            return vdfFile;
        }
        catch (Exception ex)
        {
            Logger.Error<VdfFile>($"Failed to parse VDF file");
            Logger.LogExceptionDetails<VdfFile>(ex);
            throw new ArgumentException($"Invalid VDF content.", nameof(content), ex);
        }
    }

    /// <summary>
    /// Parses the raw VDF content to build the node hierarchy.
    /// </summary>
    private static void ParseRawContent(string rawVdf, VdfFile vdfFile)
    {
        string[] lines = rawVdf.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
        int lineIndex = 0;

        // Check for header comment (lines starting with // before the first "{")
        StringBuilder? headerComment = null;
        while (lineIndex < lines.Length)
        {
            string trimmedLine = lines[lineIndex].Trim();
            if (trimmedLine.StartsWith("//"))
            {
                headerComment ??= new StringBuilder();
                if (headerComment.Length > 0)
                {
                    headerComment.AppendLine();
                }
                headerComment.Append(trimmedLine.Substring(2).Trim());
                lineIndex++;
            }
            else if (!string.IsNullOrEmpty(trimmedLine))
            {
                break;
            }
            else
            {
                lineIndex++;
            }
        }

        if (headerComment != null)
        {
            vdfFile.Document.HeaderComment = headerComment.ToString();
        }

        // Parse the VDF structure
        VdfNode? currentNode = null;
        bool hasRoot = false;

        while (lineIndex < lines.Length)
        {
            string line = lines[lineIndex];
            string trimmedLine = line.Trim();

            // Skip empty lines and comments
            if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("//"))
            {
                lineIndex++;
                continue;
            }

            // Check for the quoted key (start of a new node or key-value pair)
            if (trimmedLine.StartsWith("\""))
            {
                ParseQuotedLine(trimmedLine, lines, ref lineIndex, vdfFile, ref currentNode, ref hasRoot);
            }
            else if (trimmedLine == "}")
            {
                // End of current block - move up to parent
                if (currentNode != null)
                {
                    // Find the parent by traversing from root
                    currentNode = FindParentNode(vdfFile.Document.Root, currentNode);
                }
                lineIndex++;
            }
            else
            {
                // Unknown format - skip
                lineIndex++;
            }
        }
    }

    /// <summary>
    /// Parses a line that starts with a quoted key.
    /// </summary>
    private static void ParseQuotedLine(string trimmedLine, string[] lines, ref int lineIndex, VdfFile vdfFile, ref VdfNode? currentNode, ref bool hasRoot)
    {
        // Pattern: "key" followed by either "{" (container) or "value" (key-value pair)
        // Can also have: "key" "{" on the same line

        // Extract the key (first quoted string)
        Match keyMatch = Regex.Match(trimmedLine, @"^""([^""]*)""");
        if (!keyMatch.Success)
        {
            lineIndex++;
            return;
        }

        string key = keyMatch.Groups[1].Value;
        string remainder = trimmedLine.Substring(keyMatch.Length).Trim();

        // Create the new node
        VdfNode newNode = new VdfNode(key);

        // Determine if this is a container node or key-value pair
        if (remainder == "{" || remainder.StartsWith("{"))
        {
            // Container node - has child nodes
            if (currentNode == null)
            {
                // This is the root node
                vdfFile.Document.SetRoot(key);
                currentNode = vdfFile.Document.Root;
                hasRoot = true;
            }
            else
            {
                currentNode.Children.Add(newNode);
                currentNode = newNode;
            }
            lineIndex++;
        }
        else if (remainder.StartsWith("\"") && remainder.EndsWith("\""))
        {
            // Key-value pair on a single line: "key" "value"
            Match valueMatch = Regex.Match(remainder, @"^""([^""]*)""");
            if (valueMatch.Success)
            {
                newNode.Value = valueMatch.Groups[1].Value;

                if (currentNode == null)
                {
                    // Root node with a value (unusual but valid)
                    vdfFile.Document.SetRoot(key);
                    vdfFile.Document.Root?.Value = newNode.Value;
                    hasRoot = true;
                }
                else
                {
                    currentNode.Children.Add(newNode);
                }
            }
            lineIndex++;
        }
        else if (string.IsNullOrEmpty(remainder))
        {
            // Key on one line, value or "{" on the next line
            lineIndex++;

            // Look ahead to find the value or "{"
            while (lineIndex < lines.Length)
            {
                string nextLine = lines[lineIndex].Trim();

                if (string.IsNullOrEmpty(nextLine) || nextLine.StartsWith("//"))
                {
                    lineIndex++;
                    continue;
                }

                if (nextLine == "{")
                {
                    // Container node
                    if (currentNode == null)
                    {
                        vdfFile.Document.SetRoot(key);
                        currentNode = vdfFile.Document.Root;
                        hasRoot = true;
                    }
                    else
                    {
                        currentNode.Children.Add(newNode);
                        currentNode = newNode;
                    }
                    lineIndex++;
                    break;
                }
                else if (nextLine.StartsWith("\"") && nextLine.EndsWith("\""))
                {
                    // Key-value pair
                    Match valueMatch = Regex.Match(nextLine, @"^""([^""]*)""");
                    if (valueMatch.Success)
                    {
                        newNode.Value = valueMatch.Groups[1].Value;

                        if (currentNode == null)
                        {
                            vdfFile.Document.SetRoot(key);
                            if (vdfFile.Document.Root != null)
                            {
                                vdfFile.Document.Root.Value = newNode.Value;
                            }
                            hasRoot = true;
                        }
                        else
                        {
                            currentNode.Children.Add(newNode);
                        }
                    }
                    lineIndex++;
                    break;
                }
                else
                {
                    // Invalid format
                    lineIndex++;
                    break;
                }
            }
        }
        else
        {
            // Invalid format - skip
            lineIndex++;
        }
    }

    /// <summary>
    /// Finds the parent node of a given node by traversing from the root.
    /// </summary>
    private static VdfNode? FindParentNode(VdfNode? root, VdfNode target)
    {
        if (root == null || root == target)
        {
            return null;
        }

        // Check if the target is a direct child
        if (root.Children.Contains(target))
        {
            return root;
        }

        // Recursively search children
        foreach (VdfNode child in root.Children)
        {
            VdfNode? result = FindParentNode(child, target);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    /// <summary>
    /// Saves the VDF file to the specified path.
    /// </summary>
    /// <param name="filePath">Optional path to save to. If null, uses the original path.</param>
    /// <exception cref="InvalidOperationException">Thrown when no path is specified and the file was not loaded from a path.</exception>
    public void Save(string? filePath = null)
    {
        string savePath = filePath ?? FilePath!;

        if (string.IsNullOrEmpty(savePath))
        {
            Logger.Error<VdfFile>("No save path specified and the file was not loaded from a path");
            throw new InvalidOperationException("No save path specified and the file was not loaded from a path");
        }

        Logger.Debug<VdfFile>($"Saving VDF file to {savePath}");

        try
        {
            string content = ToVdfString();

            // Ensure directory exists
            string? directory = Path.GetDirectoryName(savePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Logger.Info<VdfFile>($"Creating directory: {directory}");
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(savePath, content);
            FilePath = savePath;
            Logger.Info<VdfFile>($"Successfully saved VDF file to {savePath} ({content.Length} bytes)");
        }
        catch (Exception ex)
        {
            Logger.Error<VdfFile>($"Failed to save VDF file to {savePath}: {ex.Message}");
            Logger.LogExceptionDetails<VdfFile>(ex);
            throw;
        }
    }

    /// <summary>
    /// Converts the VDF document to a string representation.
    /// </summary>
    /// <returns>The VDF string representation of the document.</returns>
    public string ToVdfString()
    {
        Logger.Trace<VdfFile>("Converting VDF document to string");

        StringBuilder sb = new StringBuilder();

        // Write header comment if present
        if (!string.IsNullOrEmpty(Document.HeaderComment))
        {
            string[] headerLines = Document.HeaderComment.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
            foreach (string line in headerLines)
            {
                sb.AppendLine($"// {line}");
            }
            sb.AppendLine();
        }

        // Write the VDF structure
        if (Document.Root != null)
        {
            WriteNode(sb, Document.Root, 0);
        }

        string result = sb.ToString();
        Logger.Debug<VdfFile>($"Generated VDF string: {result.Length} bytes");
        return result;
    }

    /// <summary>
    /// Writes a node and its children to the string builder.
    /// </summary>
    private static void WriteNode(StringBuilder sb, VdfNode node, int indentLevel)
    {
        string indent = new string('\t', indentLevel);
        string key = EscapeString(node.Key);

        if (node.HasChildren)
        {
            // Container node
            sb.AppendLine($"{indent}\"{key}\"");
            sb.AppendLine($"{indent}{{");

            foreach (VdfNode child in node.Children)
            {
                WriteNode(sb, child, indentLevel + 1);
            }

            sb.AppendLine($"{indent}}}");
        }
        else
        {
            // Key-value pair
            string value = EscapeString(node.Value ?? string.Empty);
            sb.AppendLine($"{indent}\"{key}\"{indent}\"{value}\"");
        }
    }

    /// <summary>
    /// Escapes special characters in a VDF string.
    /// </summary>
    private static string EscapeString(string str)
    {
        if (string.IsNullOrEmpty(str))
        {
            return str;
        }

        return str
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }

    /// <summary>
    /// Unescapes special characters in a VDF string.
    /// </summary>
    private static string UnescapeString(string str)
    {
        if (string.IsNullOrEmpty(str))
        {
            return str;
        }

        return str
            .Replace("\\\\", "\x00")
            .Replace("\\\"", "\"")
            .Replace("\\n", "\n")
            .Replace("\\r", "\r")
            .Replace("\\t", "\t")
            .Replace("\x00", "\\");
    }

    /// <summary>
    /// Gets a child node from the root by key.
    /// </summary>
    /// <param name="key">The key/name of the child node to find.</param>
    /// <returns>The child node if found, null otherwise.</returns>
    public VdfNode? GetChild(string key)
    {
        return Document.GetChild(key);
    }

    /// <summary>
    /// Gets or creates a child node from the root by key.
    /// </summary>
    /// <param name="key">The key/name of the child node.</param>
    /// <returns>The existing or newly created child node.</returns>
    public VdfNode GetOrCreateChild(string key)
    {
        return Document.GetOrCreateChild(key);
    }

    /// <summary>
    /// Gets a value from a child node of the root.
    /// </summary>
    /// <param name="key">The key/name of the child node.</param>
    /// <param name="defaultValue">The default value if not found.</param>
    /// <returns>The value of the child node, or the default value if not found.</returns>
    public string? GetValue(string key, string? defaultValue = null)
    {
        return Document.GetValue(key, defaultValue);
    }

    /// <summary>
    /// Sets a value in a child node of the root. Creates the child if it doesn't exist.
    /// </summary>
    /// <param name="key">The key/name of the child node.</param>
    /// <param name="value">The value to set.</param>
    public void SetValue(string key, string value)
    {
        Document.SetValue(key, value);
    }

    /// <summary>
    /// Gets an integer value from a child node of the root.
    /// </summary>
    /// <param name="key">The key/name of the child node.</param>
    /// <param name="defaultValue">The default value if not found or parsing fails.</param>
    /// <returns>The integer value of the child node, or the default value if not found.</returns>
    public int GetIntValue(string key, int defaultValue = 0)
    {
        return Document.GetIntValue(key, defaultValue);
    }

    /// <summary>
    /// Gets a boolean value from a child node of the root.
    /// </summary>
    /// <param name="key">The key/name of the child node.</param>
    /// <param name="defaultValue">The default value if not found or parsing fails.</param>
    /// <returns>The boolean value of the child node, or the default value if not found.</returns>
    public bool GetBoolValue(string key, bool defaultValue = false)
    {
        return Document.GetBoolValue(key, defaultValue);
    }

    /// <summary>
    /// Gets a nested value by traversing a path of keys.
    /// </summary>
    /// <param name="path">The path of keys to traverse (e.g., "actions", "GameControls").</param>
    /// <returns>The value at the path, or null if not found.</returns>
    public string? GetNestedValue(params string[] path)
    {
        return Document.GetNestedValue(path);
    }

    /// <summary>
    /// Sets a nested value by traversing a path of keys, creating nodes as needed.
    /// </summary>
    /// <param name="value">The value to set at the final path.</param>
    /// <param name="path">The path of keys to traverse.</param>
    public void SetNestedValue(string value, params string[] path)
    {
        Document.SetNestedValue(value, path);
    }

    /// <summary>
    /// Gets a nested node by traversing a path of keys.
    /// </summary>
    /// <param name="path">The path of keys to traverse.</param>
    /// <returns>The node at the path, or null if not found.</returns>
    public VdfNode? GetNestedNode(params string[] path)
    {
        return Document.GetNestedNode(path);
    }
}