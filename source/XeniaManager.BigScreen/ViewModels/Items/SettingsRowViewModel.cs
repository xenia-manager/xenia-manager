using CommunityToolkit.Mvvm.ComponentModel;
using XeniaManager.BigScreen.Models;
using XeniaManager.BigScreen.Utilities;

namespace XeniaManager.BigScreen.ViewModels.Items;

/// <summary>
/// A fixed settings screen row (one card): carries the row's kind and its
/// controller selection state. The card layout stays in XAML - only the
/// navigation state lives here.
/// </summary>
public partial class SettingsRowViewModel : ObservableObject, ISelectable
{
    /// <summary>
    /// Which settings card this row represents (drives activation behaviour).
    /// </summary>
    public SettingsRowKind Kind { get; }

    /// <summary>
    /// Whether the row is selected (controller focus).
    /// </summary>
    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    public SettingsRowViewModel(SettingsRowKind kind)
    {
        Kind = kind;
    }
}
