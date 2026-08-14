using Avalonia.Controls;
using Avalonia.Interactivity;
using XeniaManager.BigScreen.ViewModels.Items;
using XeniaManager.BigScreen.ViewModels.Modals;

namespace XeniaManager.BigScreen.Views.Modals;

/// <summary>
/// Disc selection modal: game identity up top, box art on the left and one
/// disc card per disc on the right. A/click launches the selected disc,
/// B cancels.
/// </summary>
public partial class DiscSelectionView : UserControl
{
    public DiscSelectionView()
    {
        InitializeComponent();
        DiscList.AddHandler(Button.ClickEvent, OnDiscClick, RoutingStrategies.Bubble);
    }

    /// <summary>
    /// Launches the clicked disc (mouse path; the controller path goes through
    /// the modal VM's HandleInput).
    /// </summary>
    private void OnDiscClick(object? sender, RoutedEventArgs e)
    {
        if (e.Source is Control { DataContext: DiscOptionItemViewModel disc }
            && DataContext is DiscSelectionViewModel vm)
        {
            vm.SelectDisc(disc);
        }
    }
}