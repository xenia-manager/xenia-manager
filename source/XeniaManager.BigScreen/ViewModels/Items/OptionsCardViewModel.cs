using CommunityToolkit.Mvvm.ComponentModel;
using XeniaManager.BigScreen.Models;
using XeniaManager.BigScreen.Utilities;

namespace XeniaManager.BigScreen.ViewModels.Items;

/// <summary>
/// ViewModel for a single option/menu tile on the dashboard's bottom row.
/// </summary>
public partial class OptionsCardViewModel(string title, string icon, OverlayScreen targetScreen)
    : ObservableObject, ISelectable
{
    /// <summary>
    /// The option's display title.
    /// </summary>
    [ObservableProperty]
    public partial string Title { get; set; } = title;

    /// <summary>
    /// The fluent symbol name rendered on the tile.
    /// </summary>
    [ObservableProperty]
    public partial string Icon { get; set; } = icon;

    /// <summary>
    /// Whether this card currently has focus/selection on the dashboard.
    /// </summary>
    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    /// <summary>
    /// The overlay screen opened when this card is activated, or None for actions
    /// handled elsewhere (e.g. Quit).
    /// </summary>
    public OverlayScreen TargetScreen { get; } = targetScreen;
}