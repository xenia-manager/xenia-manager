using CommunityToolkit.Mvvm.ComponentModel;
using XeniaManager.BigScreen.Utilities;
using XeniaManager.Core.Utilities;

namespace XeniaManager.BigScreen.ViewModels.Items;

/// <summary>
/// The "Create New Profile" stub row shown under the profile list in
/// Manage Profiles. Participates in the single-selection list so it is
/// reachable by controller.
/// </summary>
public partial class CreateProfileStubViewModel : ObservableObject, ISelectable
{
    /// <summary>
    /// The stub's label.
    /// </summary>
    public string Title => LocalizationHelper.GetText("ManageProfiles.CreateNewProfile");

    /// <summary>
    /// Whether the stub currently has selection.
    /// </summary>
    [ObservableProperty] private bool _isSelected;
}