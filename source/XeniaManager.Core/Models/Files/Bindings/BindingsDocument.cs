namespace XeniaManager.Core.Models.Files.Bindings;

/// <summary>
/// Represents the entire bindings document containing all sections.
/// </summary>
public class BindingsDocument
{
    /// <summary>
    /// Gets the list of bindings sections.
    /// </summary>
    public List<BindingsSection> Sections { get; set; } = [];

    /// <summary>
    /// Gets a read-only list of bindings sections.
    /// </summary>
    public IReadOnlyList<BindingsSection> SectionsReadOnly => Sections.AsReadOnly();

    /// <summary>
    /// Gets or sets the header comment for the bindings file.
    /// </summary>
    public string? HeaderComment { get; set; }

    /// <summary>
    /// Creates a new bindings document.
    /// </summary>
    public BindingsDocument()
    {
    }

    /// <summary>
    /// Adds a section to the document.
    /// </summary>
    public BindingsSection AddSection(string name, string? description = null)
    {
        BindingsSection section = new BindingsSection(name, description);
        Sections.Add(section);
        return section;
    }

    /// <summary>
    /// Gets a section by name.
    /// </summary>
    public BindingsSection? GetSection(string name)
    {
        return Sections.FirstOrDefault(s => s.Name == name);
    }

    /// <summary>
    /// Gets a section by title ID.
    /// If no matching section is found, creates a new section based on the "Default" section with the specified title ID.
    /// </summary>
    /// <param name="titleId">The title ID to search for.</param>
    /// <param name="type">Optional type filter (e.g., "Default", "Vehicle").</param>
    /// <param name="titleName">Optional game title name for creating a new section.</param>
    /// <returns>The matching section, or a new section created from Default if no match is found.</returns>
    public BindingsSection? GetSectionByTitleId(uint titleId, string? type = null, string? titleName = null)
    {
        // First, try to find a section matching the title ID
        BindingsSection? section = Sections.FirstOrDefault(s =>
            s.TitleIds.Contains(titleId) &&
            (type == null || (s.Type != null && s.Type.StartsWith(type))));

        // If not found and a type was specified, try again without the type filter
        if (section == null && type != null)
        {
            section = Sections.FirstOrDefault(s => s.TitleIds.Contains(titleId));
        }

        // If still not found, create a new section based on the Default section
        if (section == null)
        {
            BindingsSection? defaultSection = Sections.FirstOrDefault(s => s.Name == "Default");
            if (defaultSection != null)
            {
                // Create a new section name with the title ID and type/title
                string sectionType = type ?? "Default";
                string newSectionName = titleName != null
                    ? $"{titleId:X8} {sectionType} - {titleName}"
                    : $"{titleId:X8} {sectionType}";

                section = CreateSectionFromDefault(defaultSection, newSectionName, sectionType, titleName, titleId);
                Sections.Add(section);
            }
        }

        return section;
    }

    /// <summary>
    /// Creates a new section by copying all entries from the Default section.
    /// </summary>
    private BindingsSection CreateSectionFromDefault(BindingsSection defaultSection, string newName, string type, string? titleName, uint titleId)
    {
        BindingsSection newSection = new BindingsSection(newName, type)
        {
            TitleName = titleName,
            TitleIds = { titleId }
        };

        // Copy all entries from the Default section
        foreach (BindingsEntry entry in defaultSection.Entries)
        {
            newSection.AddEntry(
                entry.Key,
                entry.Value?.ToString() ?? "",
                entry.Comment,
                entry.IsCommented
            );
        }

        return newSection;
    }

    /// <summary>
    /// Gets all sections that contain the specified title ID.
    /// If no matching sections are found, creates a new "Default" section with the specified title ID.
    /// </summary>
    /// <param name="titleId">The title ID to search for.</param>
    /// <param name="titleName">Optional game title name for creating a new section.</param>
    /// <returns>A list of matching sections, or a new list containing a section created from Default.</returns>
    public List<BindingsSection> GetSectionsByTitleId(uint titleId, string? titleName = null)
    {
        List<BindingsSection> sections = Sections.Where(s => s.TitleIds.Contains(titleId)).ToList();

        // If no sections found, create a new section based on the Default section
        if (sections.Count == 0)
        {
            BindingsSection? defaultSection = Sections.FirstOrDefault(s => s.Name == "Default");
            if (defaultSection != null)
            {
                string newSectionName = titleName != null
                    ? $"{titleId:X8} Default - {titleName}"
                    : $"{titleId:X8} Default";

                BindingsSection newSection = CreateSectionFromDefault(
                    defaultSection,
                    newSectionName,
                    "Default",
                    titleName,
                    titleId
                );
                Sections.Add(newSection);
                sections.Add(newSection);
            }
        }

        return sections;
    }

    /// <summary>
    /// Gets or creates a section by name.
    /// </summary>
    public BindingsSection GetOrCreateSection(string name)
    {
        BindingsSection section = GetSection(name) ?? AddSection(name);
        return section;
    }

    /// <summary>
    /// Removes a section by name.
    /// </summary>
    public bool RemoveSection(string name)
    {
        BindingsSection? section = GetSection(name);
        if (section != null)
        {
            Sections.Remove(section);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Gets a section entry by value.
    /// </summary>
    public BindingsEntry? GetEntryByValue(string sectionName, string value)
    {
        BindingsSection? section = GetSection(sectionName);
        return section?.GetEntryByValue(value);
    }

    /// <summary>
    /// Gets the value of an entry from a section.
    /// </summary>
    public T GetValue<T>(string sectionName, string entryName, T defaultValue = default!)
    {
        BindingsSection? section = GetSection(sectionName);
        return section == null ? defaultValue : section.GetValue(entryName, defaultValue);
    }

    /// <summary>
    /// Sets the value of an entry in a section. Creates the section and entry if they don't exist.
    /// </summary>
    public void SetValue(string sectionName, string entryName, object? value)
    {
        BindingsSection section = GetOrCreateSection(sectionName);
        section.SetValue(entryName, value);
    }
}