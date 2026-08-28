namespace XeniaManager.Core.Models.Files.Config;

/// <summary>
/// Represents the entire configuration document containing all sections.
/// </summary>
public class ConfigDocument
{
    /// <summary>
    /// Gets the list of configuration sections.
    /// </summary>
    public List<ConfigSection> Sections { get; set; } = new List<ConfigSection>();

    /// <summary>
    /// Gets a read-only list of configuration sections.
    /// </summary>
    public IReadOnlyList<ConfigSection> SectionsReadOnly => Sections.AsReadOnly();

    /// <summary>
    /// Gets or sets the header comment for the config file.
    /// </summary>
    public string? HeaderComment { get; set; }

    /// <summary>
    /// Creates a new configuration document.
    /// </summary>
    public ConfigDocument()
    {
    }

    /// <summary>
    /// Adds a section to the document.
    /// </summary>
    /// <param name="name">The name of the section.</param>
    /// <param name="description">Optional description for the section.</param>
    /// <returns>The created ConfigSection.</returns>
    public ConfigSection AddSection(string name, string? description = null)
    {
        ConfigSection section = new ConfigSection(name, description);
        Sections.Add(section);
        return section;
    }

    /// <summary>
    /// Gets a section by name.
    /// </summary>
    /// <param name="name">The name of the section to find.</param>
    /// <returns>The ConfigSection if found, null otherwise.</returns>
    public ConfigSection? GetSection(string name)
    {
        return Sections.FirstOrDefault(s => s.Name == name);
    }

    /// <summary>
    /// Gets or creates a section by name.
    /// </summary>
    /// <param name="name">The name of the section.</param>
    /// <returns>The existing or newly created ConfigSection.</returns>
    public ConfigSection GetOrCreateSection(string name)
    {
        ConfigSection section = GetSection(name) ?? AddSection(name);
        return section;
    }

    /// <summary>
    /// Removes a section by name.
    /// </summary>
    /// <param name="name">The name of the section to remove.</param>
    /// <returns>True if the section was found and removed, false otherwise.</returns>
    public bool RemoveSection(string name)
    {
        ConfigSection? section = GetSection(name);
        if (section != null)
        {
            Sections.Remove(section);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Gets the value of an option from a section.
    /// </summary>
    /// <typeparam name="T">The type to cast the value to.</typeparam>
    /// <param name="sectionName">The name of the section.</param>
    /// <param name="optionName">The name of the option.</param>
    /// <param name="defaultValue">The default value if not found.</param>
    /// <returns>The value of the option, or the default value if not found.</returns>
    public T GetValue<T>(string sectionName, string optionName, T defaultValue = default!)
    {
        ConfigSection? section = GetSection(sectionName);
        return section == null ? defaultValue : section.GetValue(optionName, defaultValue);
    }

    /// <summary>
    /// Sets the value of an option in a section. Creates the section and option if they don't exist.
    /// </summary>
    /// <param name="sectionName">The name of the section.</param>
    /// <param name="optionName">The name of the option.</param>
    /// <param name="value">The value to set.</param>
    public void SetValue(string sectionName, string optionName, object? value)
    {
        ConfigSection section = GetOrCreateSection(sectionName);
        section.SetValue(optionName, value);
    }
}