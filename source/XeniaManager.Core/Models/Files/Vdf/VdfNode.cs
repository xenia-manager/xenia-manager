namespace XeniaManager.Core.Models.Files.Vdf;

/// <summary>
/// Represents a single node (key-value pair) in a VDF file.
/// Nodes can have either a value or child nodes, but not both.
/// </summary>
public class VdfNode
{
    /// <summary>
    /// Gets or sets the key/name of the node.
    /// </summary>
    public string Key { get; set; }

    /// <summary>
    /// Gets or sets the value of the node.
    /// Null if this node has child nodes.
    /// </summary>
    public string? Value { get; set; }

    /// <summary>
    /// Gets the list of child nodes.
    /// Empty if this node has a value.
    /// </summary>
    public List<VdfNode> Children { get; set; } = [];

    /// <summary>
    /// Gets a read-only list of child nodes.
    /// </summary>
    public IReadOnlyList<VdfNode> ChildrenReadOnly => Children.AsReadOnly();

    /// <summary>
    /// Gets whether this node has child nodes.
    /// </summary>
    public bool HasChildren => Children.Count > 0;

    /// <summary>
    /// Gets whether this node has a value (not a container node).
    /// </summary>
    public bool HasValue => Value != null;

    /// <summary>
    /// Creates a new VDF node.
    /// </summary>
    /// <param name="key">The key/name of the node.</param>
    /// <param name="value">Optional value. If null, this node is a container for child nodes.</param>
    public VdfNode(string key, string? value = null)
    {
        Key = key;
        Value = value;
    }

    /// <summary>
    /// Adds a child node to this node.
    /// </summary>
    /// <param name="key">The key/name of the child node.</param>
    /// <param name="value">Optional value. If null, creates a container node.</param>
    /// <returns>The created child node.</returns>
    public VdfNode AddChild(string key, string? value = null)
    {
        VdfNode child = new VdfNode(key, value);
        Children.Add(child);
        return child;
    }

    /// <summary>
    /// Gets a child node by key.
    /// </summary>
    /// <param name="key">The key/name of the child node to find.</param>
    /// <returns>The child node if found, null otherwise.</returns>
    public VdfNode? GetChild(string key)
    {
        return Children.FirstOrDefault(c => c.Key == key);
    }

    /// <summary>
    /// Gets or creates a child node by key.
    /// </summary>
    /// <param name="key">The key/name of the child node.</param>
    /// <returns>The existing or newly created child node.</returns>
    public VdfNode GetOrCreateChild(string key)
    {
        VdfNode child = GetChild(key) ?? AddChild(key);
        return child;
    }

    /// <summary>
    /// Removes a child node by key.
    /// </summary>
    /// <param name="key">The key/name of the child node to remove.</param>
    /// <returns>True if the child was found and removed, false otherwise.</returns>
    public bool RemoveChild(string key)
    {
        VdfNode? child = GetChild(key);
        if (child == null)
        {
            return false;
        }
        Children.Remove(child);
        return true;
    }

    /// <summary>
    /// Gets the value of a child node by key.
    /// </summary>
    /// <param name="key">The key/name of the child node.</param>
    /// <param name="defaultValue">The default value if not found.</param>
    /// <returns>The value of the child node, or the default value if not found.</returns>
    public string? GetValue(string key, string? defaultValue = null)
    {
        VdfNode? child = GetChild(key);
        return child?.Value ?? defaultValue;
    }

    /// <summary>
    /// Sets the value of a child node by key. Creates the child if it doesn't exist.
    /// </summary>
    /// <param name="key">The key/name of the child node.</param>
    /// <param name="value">The value to set.</param>
    public void SetValue(string key, string value)
    {
        VdfNode? child = GetChild(key);
        if (child != null)
        {
            child.Value = value;
        }
        else
        {
            AddChild(key, value);
        }
    }

    /// <summary>
    /// Gets an integer value from a child node by key.
    /// </summary>
    /// <param name="key">The key/name of the child node.</param>
    /// <param name="defaultValue">The default value if not found or parsing fails.</param>
    /// <returns>The integer value of the child node, or the default value if not found.</returns>
    public int GetIntValue(string key, int defaultValue = 0)
    {
        VdfNode? child = GetChild(key);
        if (child?.Value != null && int.TryParse(child.Value, out int result))
        {
            return result;
        }
        return defaultValue;
    }

    /// <summary>
    /// Gets a boolean value from a child node by key.
    /// </summary>
    /// <param name="key">The key/name of the child node.</param>
    /// <param name="defaultValue">The default value if not found or parsing fails.</param>
    /// <returns>The boolean value of the child node, or the default value if not found.</returns>
    public bool GetBoolValue(string key, bool defaultValue = false)
    {
        VdfNode? child = GetChild(key);
        if (child?.Value == null)
        {
            return defaultValue;
        }
        // VDF uses "1" for true and "0" for false
        if (child.Value == "1")
        {
            return true;
        }
        if (child.Value == "0")
        {
            return false;
        }
        if (bool.TryParse(child.Value, out bool result))
        {
            return result;
        }
        return defaultValue;
    }

    /// <summary>
    /// Returns a string representation of this node.
    /// </summary>
    /// <returns>The key-value pair as a string, or just the key if it's a container node.</returns>
    public override string ToString()
    {
        return HasValue ? $"\"{Key}\" = \"{Value}\"" : $"\"{Key}\" (container)";
    }
}