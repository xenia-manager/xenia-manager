using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using XeniaManager.BigScreen.ViewModels.Items;
using XeniaManager.BigScreen.ViewModels.Modals;

namespace XeniaManager.BigScreen.Views.Modals;

/// <summary>
/// The game modal's patches pane: modify (entries + command editor) and
/// remove flows; the download flow opens as its own modal.
/// </summary>
public partial class PatchesPaneView : UserControl
{
    public PatchesPaneView()
    {
        InitializeComponent();
        PatchList.AddHandler(TappedEvent, OnPatchRowTapped, RoutingStrategies.Bubble, true);
        CommandList.AddHandler(TappedEvent, OnCommandRowTapped, RoutingStrategies.Bubble, true);
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

    /// <summary>
    /// Opens a command in the editor on click.
    /// </summary>
    private void OnCommandRowTapped(object? sender, TappedEventArgs e)
    {
        if (e.Source is not Control control || DataContext is not PatchesPaneViewModel vm)
        {
            return;
        }

        if (control.GetSelfAndVisualAncestors().OfType<Border>()
            .FirstOrDefault(b => b.Classes.Contains("command-row"))
            is { DataContext: PatchCommandItemViewModel command })
        {
            vm.SelectCommand(command);
        }
    }

    /// <summary>
    /// Adds a new command to the selected entry (opens it in the editor).
    /// </summary>
    private void OnAddCommandClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is PatchesPaneViewModel vm)
        {
            vm.AddCommand();
        }
    }

    /// <summary>
    /// Saves the edited command back to the patch file.
    /// </summary>
    private void OnSaveCommandClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is PatchesPaneViewModel vm)
        {
            vm.SaveCommand();
        }
    }

    /// <summary>
    /// Deletes the edited command and saves the patch file.
    /// </summary>
    private void OnDeleteCommandClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is PatchesPaneViewModel vm)
        {
            vm.DeleteEditingCommand();
        }
    }
}
