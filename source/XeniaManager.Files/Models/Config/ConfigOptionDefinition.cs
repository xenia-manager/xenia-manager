namespace XeniaManager.Core.Models.Files.Config;

/// <summary>
/// Configuration for how a specific config option should be displayed in the UI.
/// </summary>
public class ConfigOptionDefinition
{
    /// <summary>
    /// Gets or sets the name of the configuration option (as it appears in the config file).
    /// </summary>
    public string OptionName { get; set; }

    /// <summary>
    /// Gets or sets the display name shown in the UI. If null, the option name will be formatted automatically.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the custom comment/description shown in the tooltip. If null, the original config comment is used.
    /// </summary>
    public string? CustomComment { get; set; }

    /// <summary>
    /// Gets or sets whether to hide the original comment.
    /// </summary>
    public bool HideComment { get; set; }

    /// <summary>
    /// Gets or sets the type of control to use for this option.
    /// </summary>
    public ConfigControlType ControlType { get; set; } = ConfigControlType.Auto;

    /// <summary>
    /// Gets or sets the minimum value for slider/numberbox controls.
    /// </summary>
    public double? Minimum { get; set; }

    /// <summary>
    /// Gets or sets the maximum value for slider/numberbox controls.
    /// </summary>
    public double? Maximum { get; set; }

    /// <summary>
    /// Gets or sets the step/increment value for slider controls.
    /// </summary>
    public double? Step { get; set; }

    /// <summary>
    /// Gets or sets the list of predefined options for ComboBox controls.
    /// Key is the internal value, Value is the display name.
    /// </summary>
    public Dictionary<object, string>? ComboBoxOptions { get; set; }

    /// <summary>
    /// Gets or sets whether this option is visible in the UI.
    /// </summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// Gets or sets whether this option is editable in the UI.
    /// </summary>
    public bool IsEditable { get; set; } = true;

    /// <summary>
    /// Gets or sets a format string for displaying numeric values.
    /// </summary>
    public string? ValueFormat { get; set; }

    /// <summary>
    /// Gets or sets the suffix to display after the value (e.g., "ms", "frames").
    /// </summary>
    public string? ValueSuffix { get; set; }

    /// <summary>
    /// Creates a new configuration option definition.
    /// </summary>
    /// <param name="optionName">The name of the option in the config file.</param>
    public ConfigOptionDefinition(string optionName)
    {
        OptionName = optionName;
    }

    /// <summary>
    /// Creates a new configuration option definition for a boolean toggle.
    /// </summary>
    /// <param name="optionName">The name of the option in the config file.</param>
    /// <param name="displayName">Optional display name.</param>
    /// <param name="comment">Optional custom comment.</param>
    public static ConfigOptionDefinition Toggle(string optionName, string? displayName = null, string? comment = null)
    {
        return new ConfigOptionDefinition(optionName)
        {
            DisplayName = displayName,
            CustomComment = comment,
            ControlType = ConfigControlType.ToggleSwitch
        };
    }

    /// <summary>
    /// Creates a new configuration option definition for a slider.
    /// </summary>
    /// <param name="optionName">The name of the option in the config file.</param>
    /// <param name="minimum">Minimum value.</param>
    /// <param name="maximum">Maximum value.</param>
    /// <param name="displayName">Optional display name.</param>
    /// <param name="comment">Optional custom comment.</param>
    public static ConfigOptionDefinition Slider(string optionName, double minimum, double maximum, string? displayName = null, string? comment = null)
    {
        return new ConfigOptionDefinition(optionName)
        {
            DisplayName = displayName,
            CustomComment = comment,
            ControlType = ConfigControlType.Slider,
            Minimum = minimum,
            Maximum = maximum
        };
    }

    /// <summary>
    /// Creates a new configuration option definition for a combo box.
    /// </summary>
    /// <param name="optionName">The name of the option in the config file.</param>
    /// <param name="options">Dictionary of value to display name mappings.</param>
    /// <param name="displayName">Optional display name.</param>
    /// <param name="comment">Optional custom comment.</param>
    public static ConfigOptionDefinition ComboBox(string optionName, Dictionary<object, string> options, string? displayName = null, string? comment = null)
    {
        return new ConfigOptionDefinition(optionName)
        {
            DisplayName = displayName,
            CustomComment = comment,
            ControlType = ConfigControlType.ComboBox,
            ComboBoxOptions = options
        };
    }

    /// <summary>
    /// Creates a new configuration option definition for a text box.
    /// </summary>
    /// <param name="optionName">The name of the option in the config file.</param>
    /// <param name="displayName">Optional display name.</param>
    /// <param name="comment">Optional custom comment.</param>
    public static ConfigOptionDefinition TextBox(string optionName, string? displayName = null, string? comment = null)
    {
        return new ConfigOptionDefinition(optionName)
        {
            DisplayName = displayName,
            CustomComment = comment,
            ControlType = ConfigControlType.TextBox
        };
    }
}