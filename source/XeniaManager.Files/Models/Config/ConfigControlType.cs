namespace XeniaManager.Core.Models.Files.Config;

/// <summary>
/// Defines the type of control to use for displaying a configuration option.
/// </summary>
public enum ConfigControlType
{
    /// <summary>
    /// Automatically determine the control type based on the value type.
    /// </summary>
    Auto,

    /// <summary>
    /// ToggleSwitch for boolean values.
    /// </summary>
    ToggleSwitch,

    /// <summary>
    /// Slider for numeric values with a range.
    /// </summary>
    Slider,

    /// <summary>
    /// NumberBox for integer values.
    /// </summary>
    NumberBox,

    /// <summary>
    /// TextBox for string values.
    /// </summary>
    TextBox,

    /// <summary>
    /// ComboBox for selecting from predefined options.
    /// </summary>
    ComboBox
}