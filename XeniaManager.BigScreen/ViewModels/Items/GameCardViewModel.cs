using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace XeniaManager.BigScreen.ViewModels.Items;

/// <summary>
/// ViewModel for a single game tile on the dashboard.
/// </summary>
public partial class GameCardViewModel : ObservableObject
{
    /// <summary>
    /// The game's display title.
    /// </summary>
    [ObservableProperty] private string _title;

    /// <summary>
    /// Whether this card currently has focus/selection on the dashboard.
    /// </summary>
    [ObservableProperty] private bool _isSelected;

    /// <summary>
    /// The game's artwork, used by the dynamic background. Null until real library data is wired in.
    /// </summary>
    [ObservableProperty] private Bitmap? _backgroundArt;

    public GameCardViewModel(string title)
    {
        _title = title;
    }

    /// <summary>
    /// Activates the card (launching the game). Stub for future wiring.
    /// </summary>
    [RelayCommand]
    private void Select()
    {
    }
}
