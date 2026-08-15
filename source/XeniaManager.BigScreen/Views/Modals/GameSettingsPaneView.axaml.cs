using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using XeniaManager.BigScreen.ViewModels.Items;
using XeniaManager.BigScreen.ViewModels.Modals;

namespace XeniaManager.BigScreen.Views.Modals;

/// <summary>
/// The game modal's game settings pane: curated config rows as scannable
/// settings cards. Opens/focuses the native editor controls on demand and
/// commits slider edits when the mouse releases them.
/// </summary>
public partial class GameSettingsPaneView : UserControl
{
    public GameSettingsPaneView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is GameSettingsPaneViewModel vm)
        {
            vm.EditorOpened += OnEditorOpened;
            vm.EditorClosed += OnEditorClosed;
            vm.RowSelectionChanged += OnRowSelectionChanged;
            Core.Logging.Logger.Debug<GameSettingsPaneView>(
                $"Game settings pane attached: {vm.Rows.Count} rows");
        }
    }

    /// <summary>
    /// Scrolls the newly selected row into view (controller navigation).
    /// </summary>
    private void OnRowSelectionChanged(ConfigRowViewModel row)
    {
        Border? card = SvSettings.GetVisualDescendants().OfType<Border>()
            .FirstOrDefault(b => ReferenceEquals(b.DataContext, row));
        card?.BringIntoView();
    }

    /// <summary>
    /// Opens the editor control for the given row: the combo box opens its
    /// native dropdown, the slider takes focus.
    /// </summary>
    private void OnEditorOpened(ConfigRowViewModel row)
    {
        foreach (ComboBox combo in SvSettings.GetVisualDescendants().OfType<ComboBox>())
        {
            if (ReferenceEquals(combo.DataContext, row))
            {
                combo.IsDropDownOpen = true;
                combo.Focus();
                return;
            }
        }

        foreach (Slider slider in SvSettings.GetVisualDescendants().OfType<Slider>())
        {
            if (ReferenceEquals(slider.DataContext, row))
            {
                slider.Focus();
                slider.BringIntoView();
                return;
            }
        }
    }

    /// <summary>
    /// Closes the editor control (commit or cancel).
    /// </summary>
    private void OnEditorClosed()
    {
        foreach (ComboBox combo in SvSettings.GetVisualDescendants().OfType<ComboBox>())
        {
            combo.IsDropDownOpen = false;
        }
    }
}