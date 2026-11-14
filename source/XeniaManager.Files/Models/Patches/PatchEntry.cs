namespace XeniaManager.Files.Models.Patches;

/// <summary>
/// Represents a single patch entry with metadata and commands.
/// Corresponds to a [[patch]] table in the TOML file.
/// </summary>
public class PatchEntry
{
    private string _name = string.Empty;
    private string _author = string.Empty;
    private string? _description;
    private string? _note;

    /// <summary>
    /// The name of the patch (e.g., "Disable Motion Blur").
    /// </summary>
    public string Name
    {
        get
        {
            return _name;
        }

        set
        {
            if (!string.Equals(_name, value, StringComparison.Ordinal))
            {
                _name = value;
                NameLineSuffix = null;
            }
        }
    }

    /// <summary>
    /// The author of the patch.
    /// </summary>
    public string Author
    {
        get
        {
            return _author;
        }

        set
        {
            if (!string.Equals(_author, value, StringComparison.Ordinal))
            {
                _author = value;
                AuthorLineSuffix = null;
            }
        }
    }

    /// <summary>
    /// Optional description of what the patch does.
    /// </summary>
    public string? Description
    {
        get
        {
            return _description;
        }

        set
        {
            if (!string.Equals(_description, value, StringComparison.Ordinal))
            {
                _description = value;
                DescriptionLineSuffix = null;
            }
        }
    }

    /// <summary>
    /// Optional note about the patch. Xenia ignores this field, but upstream
    /// patch files use it for additional context.
    /// </summary>
    public string? Note
    {
        get
        {
            return _note;
        }

        set
        {
            if (!string.Equals(_note, value, StringComparison.Ordinal))
            {
                _note = value;
                NoteLineSuffix = null;
            }
        }
    }

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
    /// Optional inline comment from the [[patch]] header line (e.g., [[patch]] # Netplay).
    /// </summary>
    public string? HeaderComment { get; set; }

    /// <summary>
    /// Standalone comment lines before this [[patch]] table.
    /// </summary>
    public List<string> LeadingComments { get; set; } = [];

    /// <summary>
    /// Standalone comment lines between the patch metadata and its first
    /// command. Written after is_enabled.
    /// </summary>
    public List<string> PostMetadataComments { get; set; } = [];

    /// <summary>
    /// Original text after "name = " including an inline comment. Kept so a
    /// rewrite does not discard comments or the author's formatting.
    /// </summary>
    public string? NameLineSuffix { get; set; }

    /// <summary>
    /// Original text after "author = " including an inline comment.
    /// </summary>
    public string? AuthorLineSuffix { get; set; }

    /// <summary>
    /// Original text after "desc = " including an inline comment.
    /// </summary>
    public string? DescriptionLineSuffix { get; set; }

    /// <summary>
    /// Original text after "note = " including an inline comment.
    /// </summary>
    public string? NoteLineSuffix { get; set; }

    /// <summary>
    /// Zero-based line index of the is_enabled assignment in the source file.
    /// Used internally to preserve the original file formatting when only
    /// enable states change. Not serialized.
    /// </summary>
    public int SourceIsEnabledLineIndex { get; set; } = -1;

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
    public void AddCommand(ulong address, object? value, PatchType type) => Commands.Add(new PatchCommand(address, value, type));
}