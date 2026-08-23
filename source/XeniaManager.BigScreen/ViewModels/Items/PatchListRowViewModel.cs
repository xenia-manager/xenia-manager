using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using XeniaManager.BigScreen.Utilities;

namespace XeniaManager.BigScreen.ViewModels.Items;

/// <summary>
/// The type of a patches-pane action row (download or remove).
/// </summary>
public enum PatchActionType
{
    /// <summary>
    /// Opens the patch download search.
    /// </summary>
    Download,

    /// <summary>
    /// Removes the installed patch file.
    /// </summary>
    Remove
}

/// <summary>
/// A row in the patches list: either a patch entry or a pinned action
/// (download / remove).
/// </summary>
public partial class PatchListRowViewModel : ObservableObject, ISelectable
{
    /// <summary>
    /// Whether this row currently has selection in the patch list.
    /// </summary>
    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    /// <summary>
    /// The patch entry behind this row, or null for action rows.
    /// </summary>
    public PatchEntryItemViewModel? Entry { get; }

    /// <summary>
    /// The action type for action rows.
    /// </summary>
    public PatchActionType ActionType { get; }

    /// <summary>
    /// The row title: the action label or the patch name.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Whether this row is an action row rather than a patch entry.
    /// </summary>
    public bool IsAction => Entry == null;

    /// <summary>
    /// Whether this row is the download action.
    /// </summary>
    public bool IsDownloadAction => IsAction && ActionType == PatchActionType.Download;

    /// <summary>
    /// Whether this row is the remove action.
    /// </summary>
    public bool IsRemoveAction => IsAction && ActionType == PatchActionType.Remove;

    /// <summary>
    /// The patch author and description for entry rows.
    /// </summary>
    public string Subtitle => Entry?.Author ?? string.Empty;

    /// <summary>
    /// Whether the subtitle line is shown (hidden when empty, so action rows
    /// and author-less entries stay vertically centered).
    /// </summary>
    public bool HasSubtitle => !string.IsNullOrEmpty(Subtitle);

    /// <summary>
    /// The "X commands" summary for entry rows.
    /// </summary>
    public string CommandCountText => Entry?.CommandCountText ?? string.Empty;

    /// <summary>
    /// Whether the patch entry is enabled (entry rows only).
    /// </summary>
    public bool IsEnabled => Entry?.IsEnabled ?? false;

    public PatchListRowViewModel(string actionTitle, PatchActionType actionType)
    {
        ActionType = actionType;
        Title = actionTitle;
    }

    public PatchListRowViewModel(PatchEntryItemViewModel entry)
    {
        Entry = entry;
        Title = entry.Name;
        entry.PropertyChanged += OnEntryPropertyChanged;
    }

    /// <summary>
    /// Forwards the entry's enabled state so the row's toggle marker updates
    /// immediately when the patch is toggled.
    /// </summary>
    private void OnEntryPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PatchEntryItemViewModel.IsEnabled))
        {
            OnPropertyChanged(nameof(IsEnabled));
        }
    }
}