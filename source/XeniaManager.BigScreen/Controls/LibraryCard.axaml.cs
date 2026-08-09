using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace XeniaManager.BigScreen.Controls;

/// <summary>
/// A full-size library card: box art, title and per-game stats.
/// </summary>
public partial class LibraryCard : UserControl
{
    public LibraryCard()
    {
        InitializeComponent();
        ArtClip.SizeChanged += (_, e) => ArtClip.Clip = new RectangleGeometry
        {
            Rect = new Rect(0, 0, e.NewSize.Width, e.NewSize.Height),
            RadiusX = 14,
            RadiusY = 14,
        };
    }
}
