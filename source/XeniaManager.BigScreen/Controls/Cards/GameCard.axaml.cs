using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using XeniaManager.BigScreen.ViewModels.Items;

namespace XeniaManager.BigScreen.Controls.Cards;

/// <summary>
/// Game card with bottom-anchored box art.
/// </summary>
public partial class GameCard : UserControl
{
    public GameCard()
    {
        InitializeComponent();
        ArtClip.SizeChanged += (_, e) =>
        {
            ArtClip.Clip = new RectangleGeometry
            {
                Rect = new Rect(0, 0, e.NewSize.Width, e.NewSize.Height),
                RadiusX = 12,
                RadiusY = 12
            };
            UpdateBoxArtHeight();
        };
        DataContextChanged += (_, _) =>
        {
            if (DataContext is GameCardViewModel vm)
            {
                vm.PropertyChanged += (_, _) => UpdateBoxArtHeight();
            }

            UpdateBoxArtHeight();
        };
    }

    private void UpdateBoxArtHeight()
    {
        if (DataContext is not GameCardViewModel vm || vm.BoxArt == null)
        {
            return;
        }

        double width = ArtClip.Bounds.Width;
        if (width <= 0)
        {
            width = Bounds.Width - 4;
        }

        if (width <= 0)
        {
            return;
        }

        double height = width * vm.BoxArt.PixelSize.Height / vm.BoxArt.PixelSize.Width;
        BoxArtHost.Height = height;
    }
}