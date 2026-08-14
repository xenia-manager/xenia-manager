using CommunityToolkit.Mvvm.ComponentModel;
using XeniaManager.BigScreen.Models;
using XeniaManager.BigScreen.Utilities;

namespace XeniaManager.BigScreen.ViewModels.Items;

/// <summary>
/// ViewModel for a single option row in the game modal's options list.
/// </summary>
public partial class GameActionItemViewModel(string title, string icon, GameModalPane pane)
    : ObservableObject, ISelectable
{
    /// <summary>
    /// The option's display title.
    /// </summary>
    [ObservableProperty] private string _title = title;

    /// <summary>
    /// The fluent symbol name rendered on the row.
    /// </summary>
    [ObservableProperty] private string _icon = icon;

    /// <summary>
    /// Whether this row currently has selection in the options list.
    /// </summary>
    [ObservableProperty] private bool _isSelected;

    /// <summary>
    /// The pane opened when this option is activated.
    /// </summary>
    public GameModalPane Pane { get; } = pane;
}
