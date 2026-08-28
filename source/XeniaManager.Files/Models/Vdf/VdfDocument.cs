namespace XeniaManager.Core.Models.Files.Vdf;

/// <summary>
/// Represents the entire VDF document containing the root node.
/// VDF (Valve Data File) is a file format used by Steam and Source games.
/// </summary>
public class VdfDocument
{
    /// <summary>
    /// Gets or sets the root node of the VDF document.
    /// The root node typically contains a single top-level key with all other nodes as children.
    /// </summary>
    public VdfNode? Root { get; set; }

    /// <summary>
    /// Gets or sets the header comment for the VDF file.
    /// Note: The VDF format doesn't natively support comments, but some files may have them.
    /// </summary>
    public string? HeaderComment { get; set; }

    /// <summary>
    /// Creates a new VDF document.
    /// </summary>
    public VdfDocument()
    {
    }

    /// <summary>
    /// Sets the root node of the document.
    /// </summary>
    /// <param name="key">The key/name of the root node.</param>
    /// <returns>The created root node.</returns>
    public VdfNode SetRoot(string key)
    {
        Root = new VdfNode(key);
        return Root;
    }

    /// <summary>
    /// Gets the root node's value.
    /// </summary>
    /// <returns>The root node's value, or null if no root exists.</returns>
    public string? GetRootValue()
    {
        return Root?.Value;
    }

    /// <summary>
    /// Gets a child node from the root by key.
    /// </summary>
    /// <param name="key">The key/name of the child node to find.</param>
    /// <returns>The child node if found, null otherwise.</returns>
    public VdfNode? GetChild(string key)
    {
        return Root?.GetChild(key);
    }

    /// <summary>
    /// Gets or creates a child node from the root by key.
    /// </summary>
    /// <param name="key">The key/name of the child node.</param>
    /// <returns>The existing or newly created child node.</returns>
    public VdfNode GetOrCreateChild(string key)
    {
        Root ??= new VdfNode("root");
        return Root.GetOrCreateChild(key);
    }

    /// <summary>
    /// Gets a value from a child node of the root.
    /// </summary>
    /// <param name="key">The key/name of the child node.</param>
    /// <param name="defaultValue">The default value if not found.</param>
    /// <returns>The value of the child node, or the default value if not found.</returns>
    public string? GetValue(string key, string? defaultValue = null)
    {
        return Root?.GetValue(key, defaultValue);
    }

    /// <summary>
    /// Sets a value in a child node of the root. Creates the child if it doesn't exist.
    /// </summary>
    /// <param name="key">The key/name of the child node.</param>
    /// <param name="value">The value to set.</param>
    public void SetValue(string key, string value)
    {
        Root ??= new VdfNode("root");
        Root.SetValue(key, value);
    }

    /// <summary>
    /// Gets an integer value from a child node of the root.
    /// </summary>
    /// <param name="key">The key/name of the child node.</param>
    /// <param name="defaultValue">The default value if not found or parsing fails.</param>
    /// <returns>The integer value of the child node, or the default value if not found.</returns>
    public int GetIntValue(string key, int defaultValue = 0)
    {
        return Root?.GetIntValue(key, defaultValue) ?? defaultValue;
    }

    /// <summary>
    /// Gets a boolean value from a child node of the root.
    /// </summary>
    /// <param name="key">The key/name of the child node.</param>
    /// <param name="defaultValue">The default value if not found or parsing fails.</param>
    /// <returns>The boolean value of the child node, or the default value if not found.</returns>
    public bool GetBoolValue(string key, bool defaultValue = false)
    {
        return Root?.GetBoolValue(key, defaultValue) ?? defaultValue;
    }

    /// <summary>
    /// Gets a nested value by traversing a path of keys.
    /// </summary>
    /// <param name="path">The path of keys to traverse (e.g., "actions", "GameControls").</param>
    /// <returns>The value at the path, or null if not found.</returns>
    public string? GetNestedValue(params string[]? path)
    {
        if (Root == null || path == null || path.Length == 0)
        {
            return null;
        }

        VdfNode? current = Root;
        foreach (string t in path)
        {
            current = current.GetChild(t);
            if (current == null)
            {
                return null;
            }
        }

        return current.Value;
    }

    /// <summary>
    /// Sets a nested value by traversing a path of keys, creating nodes as needed.
    /// </summary>
    /// <param name="value">The value to set at the final path.</param>
    /// <param name="path">The path of keys to traverse (e.g., "actions", "GameControls").</param>
    public void SetNestedValue(string value, params string[]? path)
    {
        if (Root == null || path == null || path.Length == 0)
        {
            return;
        }

        VdfNode current = Root;
        for (int i = 0; i < path.Length - 1; i++)
        {
            current = current.GetOrCreateChild(path[i]);
        }

        current.SetValue(path[path.Length - 1], value);
    }

    /// <summary>
    /// Gets a nested node by traversing a path of keys.
    /// </summary>
    /// <param name="path">The path of keys to traverse.</param>
    /// <returns>The node at the path, or null if not found.</returns>
    public VdfNode? GetNestedNode(params string[]? path)
    {
        if (Root == null || path == null || path.Length == 0)
        {
            return null;
        }

        VdfNode? current = Root;
        foreach (string key in path)
        {
            current = current.GetChild(key);
            if (current == null)
            {
                return null;
            }
        }

        return current;
    }
}