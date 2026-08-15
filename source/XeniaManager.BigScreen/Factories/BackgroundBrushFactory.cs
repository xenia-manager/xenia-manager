using System;
using Avalonia;
using Avalonia.Media;
using XeniaManager.BigScreen.Constants;

namespace XeniaManager.BigScreen.Factories;

/// <summary>
/// Builds the dashboard/splash brushes from a colour: solid, linear and radial
/// gradients derived from a primary colour, plus the vignette overlay.
/// Single home for the colour-mixing math and gradient stop offsets.
/// </summary>
public static class BackgroundBrushFactory
{
    /// <summary>
    /// Linearly interpolates between two colors.
    /// </summary>
    public static Color Mix(Color from, Color to, double amount)
    {
        return Color.FromRgb(
            (byte)Math.Round(from.R + (to.R - from.R) * amount),
            (byte)Math.Round(from.G + (to.G - from.G) * amount),
            (byte)Math.Round(from.B + (to.B - from.B) * amount));
    }

    /// <summary>
    /// Blends the colour toward black by the given amount (0-1).
    /// </summary>
    private static Color MixWithBlack(Color color, double amount) => Mix(color, Colors.Black, amount);

    /// <summary>
    /// Creates a smooth vertical linear gradient from the primary colour:
    /// the colour itself at the top, fading to a slightly darker slate at the bottom.
    /// </summary>
    public static IBrush CreateLinear(Color primary)
    {
        return new LinearGradientBrush
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(primary, 0),
                new GradientStop(MixWithBlack(primary, LayoutConstants.GradientMixAmount),
                    LayoutConstants.LinearMidOffset),
                new GradientStop(MixWithBlack(primary, LayoutConstants.GradientEndMixAmount), 1)
            }
        };
    }

    /// <summary>
    /// Creates a radial gradient from the primary colour:
    /// the colour itself in the top-left corner fading to a darker slate at the bottom-right.
    /// </summary>
    public static IBrush CreateRadial(Color primary)
    {
        return new RadialGradientBrush
        {
            Center = new RelativePoint(0, 0, RelativeUnit.Relative),
            GradientOrigin = new RelativePoint(0, 0, RelativeUnit.Relative),
            RadiusX = new RelativeScalar(1.5, RelativeUnit.Relative),
            RadiusY = new RelativeScalar(1.5, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(primary, 0),
                new GradientStop(MixWithBlack(primary, LayoutConstants.GradientMixAmount),
                    LayoutConstants.RadialMidOffset),
                new GradientStop(MixWithBlack(primary, LayoutConstants.GradientEndMixAmount), 1)
            }
        };
    }

    /// <summary>
    /// Creates a solid brush from the given colour.
    /// </summary>
    public static IBrush CreateSolid(Color primary) => new SolidColorBrush(primary);

    /// <summary>
    /// Creates the vignette brush: transparent center fading to black at the edges,
    /// with the configured opacity applied to the edge.
    /// </summary>
    /// <param name="opacity">Edge opacity (0-1).</param>
    public static IBrush CreateVignette(double opacity)
    {
        Color edge = Color.FromArgb((byte)Math.Round(opacity * 255), 0, 0, 0);

        Color transparent = Color.FromArgb(0, 0, 0, 0);
        return new RadialGradientBrush
        {
            Center = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
            GradientOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(transparent, 0),
                new GradientStop(transparent, LayoutConstants.VignetteInnerStop),
                new GradientStop(edge, 1)
            }
        };
    }
}