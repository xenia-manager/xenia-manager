namespace XeniaManager.Core.Models.Files.Config;

/// <summary>
/// Configuration for how a section and its options should be displayed in the UI.
/// </summary>
public class ConfigSectionDefinition
{
    /// <summary>
    /// Gets or sets the name of the section (as it appears in the config file).
    /// </summary>
    public string SectionName { get; set; }

    /// <summary>
    /// Gets or sets the display title for the section. If null, the section name is used.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the custom description for the section. If null, the original description is used.
    /// </summary>
    public string? CustomDescription { get; set; }

    /// <summary>
    /// Gets or sets whether this section is expanded by default.
    /// </summary>
    public bool IsExpandedByDefault { get; set; } = true;

    /// <summary>
    /// Gets or sets whether this section is visible in the UI.
    /// </summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// Gets or sets the list of option definitions for this section.
    /// </summary>
    public List<ConfigOptionDefinition> Options { get; set; } = [];

    /// <summary>
    /// Creates a new section definition.
    /// </summary>
    /// <param name="sectionName">The name of the section in the config file.</param>
    public ConfigSectionDefinition(string sectionName)
    {
        SectionName = sectionName;
    }

    /// <summary>
    /// Creates a new section definition with options.
    /// </summary>
    /// <param name="sectionName">The name of the section in the config file.</param>
    /// <param name="options">The option definitions for this section.</param>
    public ConfigSectionDefinition(string sectionName, params ConfigOptionDefinition[] options)
    {
        SectionName = sectionName;
        Options.AddRange(options);
    }

    /// <summary>
    /// Adds an option definition to this section.
    /// </summary>
    /// <param name="option">The option definition to add.</param>
    /// <returns>This section definition for chaining.</returns>
    public ConfigSectionDefinition AddOption(ConfigOptionDefinition option)
    {
        Options.Add(option);
        return this;
    }

    /// <summary>
    /// Adds a toggle option to this section.
    /// </summary>
    public ConfigSectionDefinition AddToggle(string optionName, string? displayName = null, string? comment = null)
    {
        Options.Add(ConfigOptionDefinition.Toggle(optionName, displayName, comment));
        return this;
    }

    /// <summary>
    /// Adds a slider option to this section.
    /// </summary>
    public ConfigSectionDefinition AddSlider(string optionName, double minimum, double maximum, string? displayName = null, string? comment = null)
    {
        Options.Add(ConfigOptionDefinition.Slider(optionName, minimum, maximum, displayName, comment));
        return this;
    }

    /// <summary>
    /// Adds a slider option with custom step/increment value to this section.
    /// </summary>
    public ConfigSectionDefinition AddSlider(string optionName, double minimum, double maximum, double step, string? displayName = null, string? comment = null, string? valueFormat = null)
    {
        ConfigOptionDefinition definition = ConfigOptionDefinition.Slider(optionName, minimum, maximum, displayName, comment);
        definition.Step = step;
        definition.ValueFormat = valueFormat;
        Options.Add(definition);
        return this;
    }

    /// <summary>
    /// Adds a NumberBox to this section with custom minimum and maximum values.
    /// </summary>
    public ConfigSectionDefinition AddNumberBox(string optionName, double minimum, double maximum, string? displayName = null, string? comment = null)
    {
        ConfigOptionDefinition definition = ConfigOptionDefinition.Slider(optionName, minimum, maximum, displayName, comment);
        definition.ControlType = ConfigControlType.NumberBox;
        Options.Add(definition);
        return this;
    }

    /// <summary>
    /// Adds a combo box option to this section.
    /// </summary>
    public ConfigSectionDefinition AddComboBox(string optionName, Dictionary<object, string> options, string? displayName = null, string? comment = null)
    {
        Options.Add(ConfigOptionDefinition.ComboBox(optionName, options, displayName, comment));
        return this;
    }

    /// <summary>
    /// Adds a text box option to this section.
    /// </summary>
    public ConfigSectionDefinition AddTextBox(string optionName, string? displayName = null, string? comment = null)
    {
        Options.Add(ConfigOptionDefinition.TextBox(optionName, displayName, comment));
        return this;
    }
}