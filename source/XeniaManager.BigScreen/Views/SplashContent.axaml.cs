using System;
using System.IO;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using XeniaManager.BigScreen.Constants;

namespace XeniaManager.BigScreen.Views;

/// <summary>
/// Splash screen content: logo, live boot status text and a progress bar
/// over the dashboard's radial background.
/// </summary>
public partial class SplashContent : UserControl
{
    public SplashContent()
    {
        InitializeComponent();
        Background = CreateRadialBackground();

        // Use the saved BigScreen accent (not the theme default) so the splash
        // matches the dashboard once the settings load
        Color accent = LoadAccentColor();
        LogoIcon.Foreground = new SolidColorBrush(accent);
        LoadBar.Foreground = new SolidColorBrush(accent);
    }

    /// <summary>
    /// Updates the status text and progress bar (0-1).
    /// </summary>
    public void SetProgress(string status, double progress)
    {
        StatusText.Text = status;
        LoadBar.Value = progress * 100;
    }

    /// <summary>
    /// Builds the dashboard-style radial gradient from the saved primary color
    /// (same stops as the dashboard's radial background).
    /// </summary>
    private static IBrush CreateRadialBackground()
    {
        Color primary = LoadSavedColor("primary_color", Color.FromRgb(0x1C, 0x1F, 0x25));
        return new RadialGradientBrush
        {
            Center = new RelativePoint(0, 0, RelativeUnit.Relative),
            GradientOrigin = new RelativePoint(0, 0, RelativeUnit.Relative),
            RadiusX = new RelativeScalar(1.5, RelativeUnit.Relative),
            RadiusY = new RelativeScalar(1.5, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(primary, 0),
                new GradientStop(Mix(primary, Colors.Black, LayoutConstants.GradientMixAmount), LayoutConstants.RadialMidOffset),
                new GradientStop(Mix(primary, Colors.Black, LayoutConstants.GradientEndMixAmount), 1),
            },
        };
    }

    /// <summary>
    /// The saved BigScreen accent color, falling back to the theme default.
    /// </summary>
    private static Color LoadAccentColor() => LoadSavedColor("accent_color", Color.FromRgb(0x10, 0x7C, 0x41));

    /// <summary>
    /// Reads a saved color from the dashboard settings file, falling back to
    /// <paramref name="fallback"/> when unavailable.
    /// </summary>
    private static Color LoadSavedColor(string propertyName, Color fallback)
    {
        try
        {
            string path = Path.Combine(AppContext.BaseDirectory, AppConstants.SettingsFileName);
            if (File.Exists(path))
            {
                using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
                if (document.RootElement.TryGetProperty(propertyName, out JsonElement element)
                    && element.ValueKind == JsonValueKind.String
                    && Color.TryParse(element.GetString(), out Color color))
                {
                    return color;
                }
            }
        }
        catch (Exception)
        {
            // Unreadable settings - fall back to the default
        }

        return fallback;
    }

    /// <summary>
    /// Linearly interpolates between two colors.
    /// </summary>
    private static Color Mix(Color from, Color to, double amount)
    {
        return Color.FromRgb(
            (byte)Math.Round(from.R + (to.R - from.R) * amount),
            (byte)Math.Round(from.G + (to.G - from.G) * amount),
            (byte)Math.Round(from.B + (to.B - from.B) * amount));
    }
}
