using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using XeniaManager.BigScreen.Controls.Settings;
using XeniaManager.BigScreen.ViewModels.Modals;

namespace XeniaManager.BigScreen.Views.Modals;

/// <summary>
/// The game modal's game settings pane: searchable config sections with the
/// five control types, saved back to the game's config file.
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
            vm.FocusOptionRequested += OnFocusOptionRequested;
        }
    }

    /// <summary>
    /// Scrolls the requested option into view and focuses its editor control.
    /// </summary>
    private void OnFocusOptionRequested(ConfigOptionViewModel option)
    {
        Dispatcher.UIThread.Post(() =>
        {
            ConfigOptionRow? row = SvSections.GetVisualDescendants().OfType<ConfigOptionRow>()
                .FirstOrDefault(r => ReferenceEquals(r.DataContext, option));
            row?.FocusEditor();
        });
    }
}
