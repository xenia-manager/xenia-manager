using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using XeniaManager.BigScreen.ViewModels;
using XeniaManager.BigScreen.ViewModels.Modals;
using XeniaManager.BigScreen.ViewModels.Items;

namespace XeniaManager.BigScreen.Views.Modals;

/// <summary>
/// Full-screen profile picker: lists the Canary profiles and switches the
/// active one. A selects, Y opens Manage Profiles, B closes.
/// </summary>
public partial class ProfilePickerView : UserControl
{
    public ProfilePickerView()
    {
        InitializeComponent();

        // A mouse click on a row switches to that profile and closes the picker
        ProfileList.AddHandler(Button.ClickEvent, OnProfileRowClick, RoutingStrategies.Bubble);
    }

    private void OnProfileRowClick(object? sender, RoutedEventArgs e)
    {
        if (e.Source is not Button { DataContext: ProfileItemViewModel item } ||
            DataContext is not ProfilePickerViewModel vm)
        {
            return;
        }

        vm.SelectProfile(item);
    }
}