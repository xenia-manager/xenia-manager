using CommunityToolkit.Mvvm.ComponentModel;
using XeniaManager.BigScreen.Utilities;
using XeniaManager.Core.Models.Database.Patches;

namespace XeniaManager.BigScreen.ViewModels.Items;

/// <summary>
/// A downloadable patch result row: name, source (Canary/Netplay) and the
/// underlying patch info used to download.
/// </summary>
public partial class PatchDownloadItemViewModel : ObservableObject, ISelectable
{
    /// <summary>
    /// Whether this row currently has selection in the download list.
    /// </summary>
    [ObservableProperty] private bool _isSelected;

    /// <summary>
    /// The patch's display name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The patch source: "Canary" or "Netplay".
    /// </summary>
    public string Source { get; }

    /// <summary>
    /// The Core patch info used by <see cref="Core.Manage.PatchManager"/>.
    /// </summary>
    public PatchInfo PatchInfo { get; }

    public PatchDownloadItemViewModel(PatchInfo patchInfo, string source)
    {
        PatchInfo = patchInfo;
        Source = source;
        Name = patchInfo.Name ?? "Unknown Patch";
    }
}
