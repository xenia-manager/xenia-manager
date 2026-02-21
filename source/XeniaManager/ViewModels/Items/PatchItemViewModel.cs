using CommunityToolkit.Mvvm.ComponentModel;
using XeniaManager.Core.Models.Database.Patches;

namespace XeniaManager.ViewModels.Items;

/// <summary>
/// Represents a patch item in the patch selection dialog.
/// This ViewModel wraps a <see cref="PatchInfo"/> object for display in the UI.
/// </summary>
public partial class PatchItemViewModel : ViewModelBase
{
    /// <summary>
    /// The display name of the patch.
    /// </summary>
    [ObservableProperty] private string _name = string.Empty;

    /// <summary>
    /// The type of patch database this patch comes from (Canary or Netplay).
    /// </summary>
    [ObservableProperty] private PatchDatabaseType _patchType;

    /// <summary>
    /// Indicates whether this patch item is currently selected.
    /// </summary>
    [ObservableProperty] private bool _isSelected;

    /// <summary>
    /// The underlying patch information containing metadata about the patch.
    /// </summary>
    public PatchInfo PatchInfo { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PatchItemViewModel"/> class.
    /// </summary>
    /// <param name="patchInfo">The patch information to wrap.</param>
    /// <param name="patchType">The type of patch database this patch comes from.</param>
    public PatchItemViewModel(PatchInfo patchInfo, PatchDatabaseType patchType = PatchDatabaseType.Canary)
    {
        PatchInfo = patchInfo;
        PatchType = patchType;
        Name = patchInfo.Name?.Replace(".patch.toml", "") ?? string.Empty;
    }
}