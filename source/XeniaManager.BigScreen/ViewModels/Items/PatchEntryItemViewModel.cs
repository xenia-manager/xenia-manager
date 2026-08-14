using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using XeniaManager.BigScreen.Utilities;
using XeniaManager.Core.Models.Files.Patches;

namespace XeniaManager.BigScreen.ViewModels.Items;

/// <summary>
/// ViewModel for a single patch entry: name, author, enabled state and its
/// commands, plus the conversion back to a Core patch entry.
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
    /// The patch's command list.
    /// </summary>
    public ObservableCollection<PatchCommandItemViewModel> Commands { get; } = [];

    /// <summary>
    /// "X commands" summary shown on the list row.
    /// </summary>
    public string CommandCountText => Commands.Count == 1
        ? "1 command"
        : $"{Commands.Count} commands";

    public PatchEntryItemViewModel(PatchEntry entry)
    {
        _originalEntry = entry;
        Name = entry.Name;
        Author = entry.Author;
        Description = entry.Description;
        IsEnabled = entry.IsEnabled;
        foreach (PatchCommand command in entry.Commands)
        {
            Commands.Add(new PatchCommandItemViewModel(command));
        }
    }

    /// <summary>
    /// Converts this view model back to a Core patch entry.
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

        foreach (PatchCommandItemViewModel command in Commands)
        {
            entry.Commands.Add(command.ToPatchCommand());
        }

        return entry;
    }

    /// <summary>
    /// Adds a default (be32 0x00000000) command to this entry.
    /// </summary>
    public PatchCommandItemViewModel AddCommand()
    {
        PatchCommandItemViewModel command = new();
        Commands.Add(command);
        return command;
    }

    /// <summary>
    /// Removes the given command from this entry.
    /// </summary>
    public void RemoveCommand(PatchCommandItemViewModel command)
    {
        Commands.Remove(command);
    }
}