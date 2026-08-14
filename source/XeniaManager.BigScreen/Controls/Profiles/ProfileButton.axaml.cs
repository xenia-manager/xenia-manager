using Avalonia.Controls;
using XeniaManager.BigScreen.ViewModels.Shell;

namespace XeniaManager.BigScreen.Controls.Profiles;

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