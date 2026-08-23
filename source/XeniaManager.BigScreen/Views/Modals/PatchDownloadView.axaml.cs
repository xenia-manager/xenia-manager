using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using XeniaManager.BigScreen.ViewModels.Items;
using XeniaManager.BigScreen.ViewModels.Modals;

namespace XeniaManager.BigScreen.Views.Modals;

/// <summary>
/// The patch download modal: search box, results with source badges and the
/// download-on-click flow.
/// </summary>
public partial class PatchDownloadView : UserControl
{
    /// <summary>
    /// Downloads the clicked result (mouse path; the gamepad path goes through
    /// the modal VM's HandleInput).
    /// </summary>
    private void OnResultTapped(object? sender, TappedEventArgs e)
    {
        if (e.Source is not Control control || DataContext is not PatchDownloadViewModel vm)
        {
            return;
        }

        if (control.GetSelfAndVisualAncestors().OfType<Border>()
                .FirstOrDefault(b => b.Classes.Contains("download-row"))
            is { DataContext: PatchDownloadItemViewModel item })
        {
            vm.SelectResult(item);
        }
    }

    public PatchDownloadView()
    {
        InitializeComponent();
        ResultsList.AddHandler(TappedEvent, OnResultTapped, RoutingStrategies.Bubble, true);
    }
}