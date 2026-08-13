using Avalonia.Controls;

namespace XeniaManager.BigScreen.Controls;

/// <summary>
/// A single connected gamepad row: name, battery icon and percentage.
/// Selection/hover state is carried on the row border (accent).
/// </summary>
public partial class GamepadCard : Button
{
    public GamepadCard()
    {
        InitializeComponent();
    }
}