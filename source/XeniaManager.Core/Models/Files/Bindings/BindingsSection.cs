namespace XeniaManager.Core.Models.Files.Bindings;

/// <summary>
/// Represents a section in a bindings file (e.g., [4D5307D3 Default - Perfect Dark Zero]).
/// Sections can have title IDs and a type description.
/// </summary>
public class BindingsSection
{
    /// <summary>
    /// Gets or sets the name of the section (includes title IDs and type).
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the list of title IDs associated with this section.
    /// </summary>
    public List<uint> TitleIds { get; set; } = [];

    /// <summary>
    /// Gets or sets the type/description of the section (e.g., "Default", "Vehicle", "Inventory").
    /// This is only the type part, not including the game title.
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Gets or sets the title/game name (e.g., "Perfect Dark Zero" from "Default - Perfect Dark Zero").
    /// </summary>
    public string? TitleName { get; set; }

    /// <summary>
    /// Gets the list of bindings entries in this section.
    /// </summary>
    public List<BindingsEntry> Entries { get; set; } = new List<BindingsEntry>();

    /// <summary>
    /// Gets a read-only list of bindings entries.
    /// </summary>
    public IReadOnlyList<BindingsEntry> EntriesReadOnly => Entries.AsReadOnly();

    /// <summary>
    /// Creates a new bindings section.
    /// </summary>
    public BindingsSection(string name, string? type = null)
    {
        Name = name;
        Type = type;
    }

    /// <summary>
    /// Adds a bindings entry to this section.
    /// </summary>
    public BindingsEntry AddEntry(string key, string value, string? comment = null, bool isCommented = false)
    {
        BindingsEntry entry = new BindingsEntry(key, value, comment, isCommented);
        Entries.Add(entry);
        return entry;
    }

    /// <summary>
    /// Gets an entry by key.
    /// </summary>
    public BindingsEntry? GetEntry(string key)
    {
        return Entries.FirstOrDefault(e => e.Key == key);
    }

    /// <summary>
    /// Gets an entry by value.
    /// </summary>
    public BindingsEntry? GetEntryByValue(string value)
    {
        return Entries.FirstOrDefault(e => e.Value?.ToString() == value);
    }

    /// <summary>
    /// Gets all entries with the specified value.
    /// </summary>
    public List<BindingsEntry> GetEntriesByValue(string value)
    {
        return Entries.Where(e => e.Value?.ToString() == value).ToList();
    }

    /// <summary>
    /// Gets the value of an entry by key.
    /// </summary>
    public T GetValue<T>(string key, T defaultValue = default!)
    {
        BindingsEntry? entry = GetEntry(key);
        if (entry?.Value == null)
        {
            return defaultValue;
        }

        if (entry.Value is T directValue)
        {
            return directValue;
        }

        if (typeof(T) == typeof(string))
        {
            return (T)(object)entry.Value.ToString()!;
        }

        if (typeof(T) == typeof(int))
        {
            return (T)(object)Convert.ToInt32(entry.Value);
        }

        if (typeof(T) == typeof(uint))
        {
            return (T)(object)Convert.ToUInt32(entry.Value);
        }

        return defaultValue;
    }

    /// <summary>
    /// Sets the value of an entry by key. Creates the entry if it doesn't exist.
    /// </summary>
    public void SetValue(string key, object? value)
    {
        BindingsEntry? entry = GetEntry(key);
        if (entry != null)
        {
            entry.Value = value;
        }
        else
        {
            AddEntry(key, value?.ToString() ?? string.Empty);
        }
    }

    /// <summary>
    /// Removes an entry by key.
    /// </summary>
    public bool RemoveEntry(string key)
    {
        BindingsEntry? entry = GetEntry(key);
        if (entry != null)
        {
            Entries.Remove(entry);
            return true;
        }
        return false;
    }
}