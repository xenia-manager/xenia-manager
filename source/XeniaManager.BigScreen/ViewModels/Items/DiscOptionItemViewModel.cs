using CommunityToolkit.Mvvm.ComponentModel;
using XeniaManager.BigScreen.Utilities;
using XeniaManager.Core.Utilities;

namespace XeniaManager.BigScreen.ViewModels.Items;

/// <summary>
/// ViewModel for a single disc card in the disc selection modal.
/// </summary>
public partial class DiscOptionItemViewModel : ObservableObject, ISelectable
{
    /// <summary>
    /// The 1-based disc number this card represents.
    /// </summary>
    public int DiscNumber { get; }

    /// <summary>
    /// The disc's display label (custom label when set, otherwise "Disc N").
    /// </summary>
    public string Label { get; }

    /// <summary>
    /// Whether this disc was the one played last.
    /// </summary>
    public bool IsLastPlayed { get; }

    /// <summary>
    /// Whether this disc's file currently exists on disk.
    /// </summary>
    public bool IsPathValid { get; }

    /// <summary>
    /// Whether this card currently has selection in the disc row.
    /// </summary>
    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    /// <summary>
    /// Whether the disc file is missing (dimmed card, skipped by navigation).
    /// </summary>
    public bool IsMissing
    {
        get
        {
            return !IsPathValid;
        }
    }

    /// <summary>
    /// Whether the status line (last played / file missing) shows under the label.
    /// </summary>
    public bool IsStatusVisible
    {
        get
        {
            return IsLastPlayed || IsMissing;
        }
    }

    /// <summary>
    /// The status line text: the last-played marker, the file-missing marker, or both.
    /// </summary>
    public string StatusText
    {
        get
        {
            if (IsLastPlayed)
            {
                return IsMissing
                    ? $"{LocalizationHelper.GetText("DiscSelection.LastPlayed")} · {LocalizationHelper.GetText("DiscSelection.FileMissing")}"
                    : LocalizationHelper.GetText("DiscSelection.LastPlayed");
            }

            return LocalizationHelper.GetText("DiscSelection.FileMissing");
        }
    }

    public DiscOptionItemViewModel(int discNumber, string label, bool isLastPlayed, bool isPathValid)
    {
        DiscNumber = discNumber;
        Label = label;
        IsLastPlayed = isLastPlayed;
        IsPathValid = isPathValid;
    }
}