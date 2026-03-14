namespace XeniaManager.Core.Models.Files.Config;

/// <summary>
/// Defines the UI schema for displaying configuration files.
/// Use this to customize which options are shown and how they appear.
/// </summary>
public class ConfigUiDefinition
{
    /// <summary>
    /// Gets or sets the list of section definitions.
    /// </summary>
    public List<ConfigSectionDefinition> Sections { get; set; } = [];

    /// <summary>
    /// Gets or sets the title for the config editor dialog.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Creates a new UI definition.
    /// </summary>
    public ConfigUiDefinition()
    {
    }

    /// <summary>
    /// Creates a new UI definition with sections.
    /// </summary>
    /// <param name="sections">The section definitions.</param>
    public ConfigUiDefinition(params ConfigSectionDefinition[] sections)
    {
        Sections.AddRange(sections);
    }

    /// <summary>
    /// Adds a section definition.
    /// </summary>
    /// <param name="section">The section to add.</param>
    /// <returns>This UI definition for chaining.</returns>
    public ConfigUiDefinition AddSection(ConfigSectionDefinition section)
    {
        Sections.Add(section);
        return this;
    }

    /// <summary>
    /// Adds a section with options.
    /// </summary>
    /// <param name="sectionName">The section name.</param>
    /// <param name="options">The option definitions.</param>
    /// <returns>This UI definition for chaining.</returns>
    public ConfigUiDefinition AddSection(string sectionName, params ConfigOptionDefinition[] options)
    {
        Sections.Add(new ConfigSectionDefinition(sectionName, options));
        return this;
    }
}