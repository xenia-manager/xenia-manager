namespace XeniaManager.Core.Models.Files.Patches;

/// <summary>
/// Represents a single patch entry with metadata and commands.
/// Corresponds to a [[patch]] table in the TOML file.
/// </summary>
public class PatchEntry
{
    /// <summary>
    /// The name of the patch (e.g., "Disable Motion Blur").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The author of the patch.
    /// </summary>
    public string Author { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of what the patch does.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether the patch is enabled.
    /// Must be false for PRs to the game-patches repository.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// List of patch commands for this patch entry.
    /// Each command specifies an address and value to patch.
    /// </summary>
    public List<PatchCommand> Commands { get; set; } = [];

    /// <summary>
    /// Creates a new instance of PatchEntry.
    /// </summary>
    public PatchEntry()
    {
    }

    /// <summary>
    /// Creates a new instance of PatchEntry with the specified parameters.
    /// </summary>
    /// <param name="name">The name of the patch.</param>
    /// <param name="author">The author of the patch.</param>
    /// <param name="isEnabled">Whether the patch is enabled.</param>
    /// <param name="description">Optional description of the patch.</param>
    public PatchEntry(string name, string author, bool isEnabled = false, string? description = null)
    {
        Name = name;
        Author = author;
        IsEnabled = isEnabled;
        Description = description;
    }

    /// <summary>
    /// Adds a patch command to this entry.
    /// </summary>
    /// <param name="address">The memory address to patch.</param>
    /// <param name="value">The value to write.</param>
    /// <param name="type">The type of patch command.</param>
    public void AddCommand(uint address, object? value, PatchType type)
    {
        Commands.Add(new PatchCommand(address, value, type));
    }
}