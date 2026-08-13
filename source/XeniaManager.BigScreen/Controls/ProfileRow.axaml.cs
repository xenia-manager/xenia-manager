using Avalonia.Controls;

namespace XeniaManager.BigScreen.Controls;

/// <summary>
/// A profile list row (avatar, gamertag, XUID, active badge) with an accent
/// border on selection/hover.
/// </summary>
public partial class ProfileRow : Button
{
    public ProfileRow()
    {
        InitializeComponent();
    }
}