using Avalonia.Controls;

namespace XeniaManager.BigScreen.Controls.Cards;

/// <summary>
/// Disc card in the disc selection modal: disc icon, label header and a
/// last-played / file-missing status line. Accent border on selection/hover,
/// dimmed when the disc's file is missing.
/// </summary>
public partial class DiscOptionCard : Button
{
    public DiscOptionCard()
    {
        InitializeComponent();
    }
}