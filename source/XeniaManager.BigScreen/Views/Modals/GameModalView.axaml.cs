using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using XeniaManager.BigScreen.ViewModels.Items;
using XeniaManager.BigScreen.ViewModels.Modals;

namespace XeniaManager.BigScreen.Views.Modals;

/// <summary>
/// The game modal modal: game info + options list on the left, the active
/// pane on the right.
/// </summary>
public partial class GameModalView : UserControl
{
    public GameModalView()
    {
        InitializeComponent();
        OptionsList.AddHandler(Button.ClickEvent, OnActionRowClick, RoutingStrategies.Bubble);
    }

    /// <summary>
    /// Opens the option's pane when its row is clicked (mouse path; the
    /// controller path goes through the dialog VM's HandleInput).
    /// </summary>
    private void OnActionRowClick(object? sender, RoutedEventArgs e)
    {
        if (e.Source is Control { DataContext: GameActionItemViewModel option }
            && DataContext is GameModalViewModel vm)
        {
            vm.OpenOption(option);
        }
    }
}
