using CommunityToolkit.Mvvm.ComponentModel;
using XeniaManager.BigScreen.Models;
using XeniaManager.BigScreen.Utilities;

namespace XeniaManager.BigScreen.ViewModels.Items;

/// <summary>
/// A fixed edit-panel row in the Manage Profiles modal: carries the row's
/// kind and its controller selection state. The field layout stays in XAML -
/// only the navigation state lives here.
/// </summary>
public partial class ManageProfilesRowViewModel : ObservableObject, ISelectable
{
    /// <summary>
    /// Which edit-panel field this row represents (drives activation behaviour).
    /// </summary>
    public ManageProfilesRowKind Kind { get; }

    /// <summary>
    /// Whether the row is selected (controller focus).
    /// </summary>
    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    public ManageProfilesRowViewModel(ManageProfilesRowKind kind)
    {
        Kind = kind;
    }
}