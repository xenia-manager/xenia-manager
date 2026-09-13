using CommunityToolkit.Mvvm.ComponentModel;
using XeniaManager.BigScreen.Models;
using XeniaManager.BigScreen.Utilities;

namespace XeniaManager.BigScreen.ViewModels.Items;

/// <summary>
/// ViewModel for a single option row in the achievement fetch picker.
/// </summary>
public partial class AchievementFetchOptionItemViewModel(AchievementFetchOption option, string label)
    : ObservableObject, ISelectable
{
    /// <summary>
    /// The fetch option this row represents.
    /// </summary>
    public AchievementFetchOption Option { get; } = option;

    /// <summary>
    /// The row's display label.
    /// </summary>
    public string Label { get; } = label;

    /// <summary>
    /// Whether this row currently has selection in the options list.
    /// </summary>
    [ObservableProperty]
    public partial bool IsSelected { get; set; }
}