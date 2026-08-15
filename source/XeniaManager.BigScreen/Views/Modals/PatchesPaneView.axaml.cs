using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using XeniaManager.BigScreen.ViewModels.Items;
using XeniaManager.BigScreen.ViewModels.Modals;

namespace XeniaManager.BigScreen.Views.Modals;

/// <summary>
/// The game modal's patches pane: toggle and remove flows; the download flow
/// opens as its own modal.
/// </summary>
public partial class PatchesPaneView : UserControl
{
    /// <summary>
    /// Activates a patch list row on click (mouse path; the gamepad path goes
    /// through the pane VM's HandleInput).
    /// </summary>
    private void OnPatchRowTapped(object? sender, TappedEventArgs e)
    {
        if (e.Source is not Control control || DataContext is not PatchesPaneViewModel vm)
        {
            return;
        }

        if (control.GetSelfAndVisualAncestors().OfType<Border>()
                .FirstOrDefault(b => b.Classes.Contains("patch-row"))
            is { DataContext: PatchListRowViewModel row })
        {
            vm.SelectRow(row);
        }
    }

    public PatchesPaneView()
    {
        InitializeComponent();
        PatchList.AddHandler(TappedEvent, OnPatchRowTapped, RoutingStrategies.Bubble, true);
    }
}