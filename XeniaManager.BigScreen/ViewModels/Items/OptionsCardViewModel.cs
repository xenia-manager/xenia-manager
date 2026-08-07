using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace XeniaManager.BigScreen.ViewModels.Items;

/// <summary>
/// ViewModel for a single option/menu tile on the dashboard's bottom row.
/// </summary>
public partial class OptionsCardViewModel : ObservableObject
{
    /// <summary>
    /// The option's display title.
    /// </summary>
    [ObservableProperty] private string _title;

    /// <summary>
    /// The fluent symbol name rendered on the tile.
    /// </summary>
    [ObservableProperty] private string _icon;

    /// <summary>
    /// Whether this card currently has focus/selection on the dashboard.
    /// </summary>
    [ObservableProperty] private bool _isSelected;

    public OptionsCardViewModel(string title, string icon)
    {
        _title = title;
        _icon = icon;
    }

    /// <summary>
    /// Activates the option. Stub for future wiring.
    /// </summary>
    [RelayCommand]
    private void Select()
    {
    }
}
