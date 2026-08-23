using Avalonia.Controls;
using Avalonia.Interactivity;
using XeniaManager.BigScreen.ViewModels.Modals;
using XeniaManager.BigScreen.ViewModels.Items;

namespace XeniaManager.BigScreen.Views.Modals;

/// <summary>
/// Full-screen profile picker: lists the profiles of one emulator version and
/// switches the active one. A selects, Y opens Manage Profiles, B closes; the
/// version chips switch which version's profiles are shown.
/// </summary>
public partial class ProfilePickerView : UserControl
{
    private void OnProfileRowClick(object? sender, RoutedEventArgs e)
    {
        if (e.Source is not Button { DataContext: ProfileItemViewModel item } ||
            DataContext is not ProfilePickerViewModel vm)
        {
            return;
        }

        vm.SelectProfile(item);
    }

    private void OnVersionChipClick(object? sender, RoutedEventArgs e)
    {
        if (e.Source is not Button { DataContext: VersionChipViewModel chip } ||
            DataContext is not ProfilePickerViewModel vm)
        {
            return;
        }

        vm.SelectVersion(chip);
    }

    public ProfilePickerView()
    {
        InitializeComponent();

        ProfileList.AddHandler(Button.ClickEvent, OnProfileRowClick, RoutingStrategies.Bubble);
        VersionChipsList.AddHandler(Button.ClickEvent, OnVersionChipClick, RoutingStrategies.Bubble);
    }
}