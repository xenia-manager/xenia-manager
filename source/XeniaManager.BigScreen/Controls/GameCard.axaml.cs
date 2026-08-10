using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace XeniaManager.BigScreen.Controls;

/// <summary>
/// A dashboard game tile: box art with a title bar and border overlay on selection.
/// </summary>
public partial class GameCard : UserControl
{
    public GameCard()
    {
        InitializeComponent();
        ArtClip.SizeChanged += (_, e) => ArtClip.Clip = new RectangleGeometry
        {
            Rect = new Rect(0, 0, e.NewSize.Width, e.NewSize.Height),
            RadiusX = 12,
            RadiusY = 12,
        };
    }
}
