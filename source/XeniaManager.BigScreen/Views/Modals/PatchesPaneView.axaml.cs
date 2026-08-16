using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
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
    /// Scrolls the selected patch row into view (controller navigation).
    /// </summary>
    private void OnScrollRequested()
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (DataContext is not PatchesPaneViewModel vm)
            {
                return;
            }

            PatchListRowViewModel? selected = vm.Rows.FirstOrDefault(r => r.IsSelected);
            if (selected == null)
            {
                return;
            }

            PatchList.GetVisualDescendants().OfType<Border>()
                .FirstOrDefault(b => ReferenceEquals(b.DataContext, selected))?.BringIntoView();
        });
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is PatchesPaneViewModel vm)
        {
            vm.ScrollRequested += OnScrollRequested;
        }
    }

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
        DataContextChanged += OnDataContextChanged;
    }
}