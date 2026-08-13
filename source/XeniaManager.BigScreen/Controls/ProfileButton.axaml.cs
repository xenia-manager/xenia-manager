using Avalonia.Controls;
using Avalonia.Interactivity;
using XeniaManager.BigScreen.ViewModels;

namespace XeniaManager.BigScreen.Controls;

/// <summary>
/// Header profile chip (avatar + gamertag + gamerscore). Focusable and clickable;
/// activation opens the profile picker.
/// </summary>
public partial class ProfileButton : Button
{
    public ProfileButton()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Opens the profile picker on click.
    /// </summary>
    protected override void OnClick()
    {
        base.OnClick();

        if (DataContext is MainWindowViewModel vm)
        {
            vm.OpenProfilePicker();
        }
    }
}