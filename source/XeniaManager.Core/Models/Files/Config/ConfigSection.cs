namespace XeniaManager.Core.Models.Files.Config;

/// <summary>
/// Represents a configuration section in a Xenia config file (e.g., [APU], [CPU], [GPU]).
/// </summary>
public class ConfigSection
{
    /// <summary>
    /// Gets or sets the name of the section.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the description/comment for this section.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets the list of configuration options in this section.
    /// </summary>
    public List<ConfigOption> Options { get; set; } = new List<ConfigOption>();

    /// <summary>
    /// Gets a read-only list of configuration options.
    /// </summary>
    public IReadOnlyList<ConfigOption> OptionsReadOnly => Options.AsReadOnly();

    /// <summary>
    /// Creates a new configuration section.
    /// </summary>
    /// <param name="name">The name of the section.</param>
    /// <param name="description">Optional description for the section.</param>
    public ConfigSection(string name, string? description = null)
    {
        Name = name;
        Description = description;
    }

    /// <summary>
    /// Adds a configuration option to this section.
    /// </summary>
    /// <param name="name">The name of the option.</param>
    /// <param name="value">The value of the option.</param>
    /// <param name="comment">Optional comment/description.</param>
    /// <param name="isCommented">Whether the option is commented out.</param>
    /// <param name="type">The type of the value.</param>
    /// <param name="padding">Optional padding string for alignment preservation.</param>
    /// <returns>The created ConfigOption.</returns>
    public ConfigOption AddOption(string name, object? value, string? comment = null, bool isCommented = false, ConfigOptionType type = ConfigOptionType.Unknown,
        string? padding = null)
    {
        ConfigOption option = new ConfigOption(name, value, comment, isCommented, type, padding);
        Options.Add(option);
        return option;
    }

    /// <summary>
    /// Gets an option by name.
    /// </summary>
    /// <param name="name">The name of the option to find.</param>
    /// <returns>The ConfigOption if found, null otherwise.</returns>
    public ConfigOption? GetOption(string name)
    {
        return Options.FirstOrDefault(o => o.Name == name);
    }

    /// <summary>
    /// Gets the value of an option by name.
    /// </summary>
    /// <typeparam name="T">The type to cast the value to.</typeparam>
    /// <param name="name">The name of the option.</param>
    /// <param name="defaultValue">The default value if the option is not found.</param>
    /// <returns>The value of the option, or the default value if not found.</returns>
    public T GetValue<T>(string name, T defaultValue = default!)
    {
        ConfigOption? option = GetOption(name);
        if (option?.Value == null)
        {
            return defaultValue;
        }

        // Direct type match
        if (option.Value is T directValue)
        {
            return directValue;
        }

        // Handle numeric type conversions
        if (typeof(T) == typeof(int))
        {
            return (T)(object)Convert.ToInt32(option.Value);
        }
        if (typeof(T) == typeof(long))
        {
            return (T)(object)Convert.ToInt64(option.Value);
        }
        if (typeof(T) == typeof(uint))
        {
            return (T)(object)Convert.ToUInt32(option.Value);
        }
        if (typeof(T) == typeof(ulong))
        {
            return (T)(object)Convert.ToUInt64(option.Value);
        }
        if (typeof(T) == typeof(float))
        {
            return (T)(object)Convert.ToSingle(option.Value);
        }
        if (typeof(T) == typeof(double))
        {
            return (T)(object)Convert.ToDouble(option.Value);
        }

        return defaultValue;
    }

    /// <summary>
    /// Sets the value of an option by name. Creates the option if it doesn't exist.
    /// </summary>
    /// <param name="name">The name of the option.</param>
    /// <param name="value">The value to set.</param>
    public void SetValue(string name, object? value)
    {
        ConfigOption? option = GetOption(name);
        if (option != null)
        {
            option.Value = value;
        }
        else
        {
            AddOption(name, value);
        }
    }

    /// <summary>
    /// Removes an option by name.
    /// </summary>
    /// <param name="name">The name of the option to remove.</param>
    /// <returns>True if the option was found and removed, false otherwise.</returns>
    public bool RemoveOption(string name)
    {
        ConfigOption? option = GetOption(name);
        if (option != null)
        {
            Options.Remove(option);
            return true;
        }
        return false;
    }
}