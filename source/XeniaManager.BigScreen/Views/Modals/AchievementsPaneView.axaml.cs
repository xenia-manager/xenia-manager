using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using XeniaManager.BigScreen.Controls.Cards;
using XeniaManager.BigScreen.ViewModels.Items;
using XeniaManager.BigScreen.ViewModels.Modals;

namespace XeniaManager.BigScreen.Views.Modals;

/// <summary>
/// The game modal's achievements pane: stats header, sort indicator and
/// scrollable achievement rows.
/// </summary>
public partial class AchievementsPaneView : UserControl
{
    /// <summary>
    /// Scrolls the selected achievement row into view (controller navigation).
    /// </summary>
    private void OnScrollRequested()
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (DataContext is not AchievementsPaneViewModel vm)
            {
                return;
            }

            AchievementItemViewModel? selected = vm.Rows.FirstOrDefault(r => r.IsSelected);
            if (selected == null)
            {
                return;
            }

            SvAchievements.GetVisualDescendants().OfType<AchievementRow>()
                .FirstOrDefault(r => ReferenceEquals(r.DataContext, selected))?.BringIntoView();
        });
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is AchievementsPaneViewModel vm)
        {
            vm.ScrollRequested += OnScrollRequested;
        }
    }

    public AchievementsPaneView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }
}