namespace XeniaManager.Core.Models.Files.Config;

/// <summary>
/// Represents a single configuration option in a Xenia config file.
/// </summary>
public class ConfigOption
{
    /// <summary>
    /// Gets or sets the name of the configuration option.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the value of the configuration option.
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// Gets or sets the comment/description for this option.
    /// </summary>
    public string? Comment { get; set; }

    /// <summary>
    /// Gets or sets whether this option is commented out.
    /// </summary>
    public bool IsCommented { get; set; }

    /// <summary>
    /// Gets or sets the type of the value.
    /// </summary>
    public ConfigOptionType Type { get; set; }

    /// <summary>
    /// Gets or sets the original padding between the value and comment (for preserving alignment).
    /// </summary>
    public string? Padding { get; set; }

    /// <summary>
    /// Gets or sets whether this option has a multi-line comment block.
    /// </summary>
    public bool HasMultiLineComment { get; set; }

    /// <summary>
    /// Creates a new configuration option.
    /// </summary>
    /// <param name="name">The name of the option.</param>
    /// <param name="value">The value of the option.</param>
    /// <param name="comment">Optional comment/description.</param>
    /// <param name="isCommented">Whether the option is commented out.</param>
    /// <param name="type">The type of the value.</param>
    /// <param name="padding">Optional padding string for alignment preservation.</param>
    public ConfigOption(string name, object? value, string? comment = null, bool isCommented = false, ConfigOptionType type = ConfigOptionType.Unknown,
        string? padding = null)
    {
        Name = name;
        Value = value;
        Comment = comment;
        IsCommented = isCommented;
        Type = type;
        Padding = padding;
    }
}