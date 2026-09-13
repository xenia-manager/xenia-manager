using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using XeniaManager.BigScreen.ViewModels.Items;
using XeniaManager.BigScreen.ViewModels.Modals;

namespace XeniaManager.BigScreen.Controls.Modals;

/// <summary>
/// Unified fetch picker: header, message and a vertical list of fetch
/// options (Up/Down selects, A activates the selection, B cancels).
/// </summary>
public partial class AchievementFetchModal : UserControl
{
    /// <summary>
    /// Moves focus to the selected option so keyboard Enter activates it.
    /// </summary>
    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (DataContext is AchievementFetchViewModel vm)
            {
                int index = vm.Options.FindIndex(o => o.IsSelected);
                if (OptionList.ContainerFromIndex(index < 0 ? 0 : index) is Control control)
                {
                    control.Focus();
                }
            }
        });
    }

    /// <summary>
    /// Confirms the clicked option (mouse path; the controller path goes
    /// through the modal VM's HandleInput).
    /// </summary>
    private void OnOptionClick(object? sender, RoutedEventArgs e)
    {
        if (e.Source is Control { DataContext: AchievementFetchOptionItemViewModel option }
            && DataContext is AchievementFetchViewModel vm)
        {
            vm.SelectOption(option);
        }
    }

    public AchievementFetchModal()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        OptionList.AddHandler(Button.ClickEvent, OnOptionClick, RoutingStrategies.Bubble);
    }
}