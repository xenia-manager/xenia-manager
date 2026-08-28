using CommunityToolkit.Mvvm.ComponentModel;
using XeniaManager.BigScreen.Utilities;
using XeniaManager.Files.Models.Patches;

namespace XeniaManager.BigScreen.ViewModels.Items;

/// <summary>
/// ViewModel for a single patch entry: name, author, enabled state and its
/// command count, plus the conversion back to a Core patch entry.
/// </summary>
public partial class PatchEntryItemViewModel : ObservableObject, ISelectable
{
    private readonly PatchEntry _originalEntry;

    /// <summary>
    /// Whether this row currently has selection in the patch list.
    /// </summary>
    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    /// <summary>
    /// The patch's display name.
    /// </summary>
    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    /// <summary>
    /// The patch's author.
    /// </summary>
    [ObservableProperty]
    public partial string Author { get; set; } = string.Empty;

    /// <summary>
    /// The patch's description, or null when none.
    /// </summary>
    [ObservableProperty]
    public partial string? Description { get; set; }

    /// <summary>
    /// Whether the patch is enabled (A toggles this on the list).
    /// </summary>
    [ObservableProperty]
    public partial bool IsEnabled { get; set; }

    /// <summary>
    /// "X commands" summary shown on the list row.
    /// </summary>
    public string CommandCountText => _originalEntry.Commands.Count == 1
        ? "1 command"
        : $"{_originalEntry.Commands.Count} commands";

    public PatchEntryItemViewModel(PatchEntry entry)
    {
        _originalEntry = entry;
        Name = entry.Name;
        Author = entry.Author;
        Description = entry.Description;
        IsEnabled = entry.IsEnabled;
    }

    /// <summary>
    /// Converts this view model back to a Core patch entry, preserving the
    /// original command list (commands are not editable in BigScreen).
    /// </summary>
    public PatchEntry ToPatchEntry()
    {
        PatchEntry entry = new PatchEntry
        {
            Name = Name,
            Author = Author,
            Description = Description,
            IsEnabled = IsEnabled,
            HeaderComment = _originalEntry.HeaderComment
        };

        foreach (PatchCommand command in _originalEntry.Commands)
        {
            entry.Commands.Add(command);
        }

        return entry;
    }
}