using System;
using System.IO;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using XeniaManager.BigScreen.Models;

namespace XeniaManager.BigScreen.Services;

/// <summary>
/// Manages the dashboard styling: persistence, user-facing options, and brush construction.
/// </summary>
public class BackgroundService
{
    /// <summary>
    /// Path of the persisted settings file (next to the executable).
    /// </summary>
    private static readonly string SettingsPath = Path.Combine(
        AppContext.BaseDirectory,
        "dashboard-settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new ColorJsonConverter() },
    };

    /// <summary>
    /// The currently loaded dashboard settings.
    /// </summary>
    public DashboardSettings Settings { get; private set; } = new();

    /// <summary>
    /// Loads the persisted settings, falling back to defaults when the file is missing or corrupt,
    /// then applies the style tokens to the application resources.
    /// </summary>
    public void Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                DashboardSettings? loaded = JsonSerializer.Deserialize<DashboardSettings>(
                    File.ReadAllText(SettingsPath), JsonOptions);
                if (loaded != null)
                {
                    Settings = loaded;
                }
            }
        }
        catch (Exception)
        {
            // Corrupt settings - fall back to defaults
        }

        ApplyResources();
    }

    /// <summary>
    /// Saves the current settings to disk and re-applies the style tokens.
    /// </summary>
    public void Save()
    {
        try
        {
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(Settings, JsonOptions));
        }
        catch (Exception)
        {
            // Ignore save failures - the styling just won't persist
        }

        ApplyResources();
    }

    /// <summary>
    /// Pushes the user-facing style tokens into the application resources so
    /// DynamicResource bindings pick them up. Called on load and after every change.
    /// </summary>
    public void ApplyResources()
    {
        IResourceDictionary? resources = Application.Current?.Resources;
        if (resources == null)
        {
            return;
        }

        resources["AccentColor"] = new SolidColorBrush(Settings.AccentColor);
        resources["BackgroundVignette"] = CreateVignetteBrush();
    }

    /// <summary>
    /// Builds the brush for the current settings and the optionally selected game artwork.
    /// </summary>
    /// <param name="selectedGameArt">Artwork of the currently selected game (Dynamic mode only).</param>
    public IBrush? GetBackground(Bitmap? selectedGameArt)
    {
        return Settings.Mode switch
        {
            BackgroundMode.Image => CreateImageBrush(),
            BackgroundMode.Solid => new SolidColorBrush(Settings.PrimaryColor),
            BackgroundMode.LinearGradient => CreateLinearBrush(),
            BackgroundMode.RadialGradient => CreateRadialBrush(),
            BackgroundMode.Dynamic => selectedGameArt != null
                ? new ImageBrush(selectedGameArt)
                {
                    Stretch = Stretch.UniformToFill,
                }
                : CreateLinearBrush(),
            _ => CreateLinearBrush(),
        };
    }

    /// <summary>
    /// Creates the vignette brush: transparent center fading to black at the edges,
    /// with the configured opacity applied to the edge.
    /// </summary>
    private IBrush CreateVignetteBrush()
    {
        RadialGradientBrush brush = new()
        {
            Center = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
            GradientOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
        };

        Color edge = Color.FromArgb(
            (byte)Math.Round(Settings.VignetteOpacity * 255),
            0, 0, 0);

        // Explicit transparent black (#00000000), NOT Colors.Transparent (#00FFFFFF):
        // interpolating white-tinted transparency toward the black edge produces a
        // bright halo in the middle of the screen.
        Color transparent = Color.FromArgb(0, 0, 0, 0);
        brush.GradientStops.Add(new GradientStop(transparent, 0));
        brush.GradientStops.Add(new GradientStop(transparent, 0.75));
        brush.GradientStops.Add(new GradientStop(edge, 1));
        return brush;
    }

    /// <summary>
    /// Creates an image brush from the configured image path, or null when unavailable.
    /// </summary>
    private IBrush? CreateImageBrush()
    {
        try
        {
            if (!string.IsNullOrEmpty(Settings.ImagePath) && File.Exists(Settings.ImagePath))
            {
                return new ImageBrush(new Bitmap(Settings.ImagePath))
                {
                    Stretch = Stretch.UniformToFill,
                };
            }
        }
        catch (Exception)
        {
            // Unreadable image - fall through to null
        }

        return null;
    }

    /// <summary>
    /// Creates a vertical linear gradient from shades of the primary color:
    /// lighter at the top, darker at the bottom.
    /// </summary>
    private IBrush CreateLinearBrush()
    {
        LinearGradientBrush brush = new()
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
        };
        AddShades(brush, [1.18, 1.10, 1.02, 0.94, 0.86]);
        return brush;
    }

    /// <summary>
    /// Creates a radial gradient from shades of the primary color:
    /// lighter at the center, darker at the edges.
    /// </summary>
    private IBrush CreateRadialBrush()
    {
        RadialGradientBrush brush = new()
        {
            Center = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
            GradientOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
        };
        AddShades(brush, [1.12, 1.0, 0.88]);
        return brush;
    }

    /// <summary>
    /// Spreads shade factors of the primary color evenly across the brush's gradient stops.
    /// </summary>
    private void AddShades(GradientBrush brush, double[] factors)
    {
        double step = 1.0 / Math.Max(1, factors.Length - 1);
        for (int i = 0; i < factors.Length; i++)
        {
            brush.GradientStops.Add(new GradientStop(Shade(Settings.PrimaryColor, factors[i]), i * step));
        }
    }

    /// <summary>
    /// Multiplies the RGB channels of a color by the given factor, clamping to byte range.
    /// </summary>
    private static Color Shade(Color color, double factor)
    {
        return Color.FromRgb(
            (byte)Math.Clamp(Math.Round(color.R * factor), 0, 255),
            (byte)Math.Clamp(Math.Round(color.G * factor), 0, 255),
            (byte)Math.Clamp(Math.Round(color.B * factor), 0, 255));
    }
}
