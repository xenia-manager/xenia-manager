using System;
using System.IO;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using XeniaManager.BigScreen.Constants;
using XeniaManager.BigScreen.Converters;
using XeniaManager.BigScreen.Models;
using XeniaManager.Core.Logging;

namespace XeniaManager.BigScreen.Services;

/// <summary>
/// Manages the dashboard styling: persistence, user-facing options, and brush construction.
/// </summary>
public class BackgroundService : IBackgroundService
{
    /// <summary>
    /// Path of the persisted settings file (next to the executable).
    /// </summary>
    private static readonly string SettingsPath = Path.Combine(
        AppContext.BaseDirectory,
        AppConstants.SettingsFileName);

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
    /// Linearly interpolates between two colors.
    /// </summary>
    private static Color Mix(Color from, Color to, double amount)
    {
        return Color.FromRgb(
            (byte)Math.Round(from.R + (to.R - from.R) * amount),
            (byte)Math.Round(from.G + (to.G - from.G) * amount),
            (byte)Math.Round(from.B + (to.B - from.B) * amount));
    }

    /// <summary>
    /// Blends the colour toward white by the given amount (0-1).
    /// </summary>
    private static Color MixWithWhite(Color color, double amount) => Mix(color, Colors.White, amount);

    /// <summary>
    /// Blends the colour toward black by the given amount (0-1).
    /// </summary>
    private static Color MixWithBlack(Color color, double amount) => Mix(color, Colors.Black, amount);

    /// <summary>
    /// Lightens (positive) or darkens (negative) the accent colour by the given amount.
    /// </summary>
    private Color AdjustAccent(double amount)
    {
        Color c = Settings.AccentColor;
        if (amount >= 0)
        {
            return Mix(c, Colors.White, amount);
        }

        return Mix(c, Colors.Black, -amount);
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
        brush.GradientStops.Add(new GradientStop(transparent, LayoutConstants.VignetteInnerStop));
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
        catch (Exception ex)
        {
            // Unreadable image - fall through to null
            Logger.Warning<BackgroundService>($"Failed to load background image '{Settings.ImagePath}'");
            Logger.LogExceptionDetails<BackgroundService>(ex);
        }

        return null;
    }

    /// <summary>
    /// Creates a smooth vertical linear gradient from the primary color:
    /// the colour itself at the top, fading to a slightly darker slate at the bottom.
    /// </summary>
    private IBrush CreateLinearBrush()
    {
        LinearGradientBrush brush = new()
        {
            StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
            EndPoint = new RelativePoint(0, 1, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(Settings.PrimaryColor, 0),
                new GradientStop(MixWithBlack(Settings.PrimaryColor, LayoutConstants.GradientMixAmount),
                    LayoutConstants.LinearMidOffset),
                new GradientStop(MixWithBlack(Settings.PrimaryColor, LayoutConstants.GradientEndMixAmount), 1),
            },
        };
        return brush;
    }

    /// <summary>
    /// Creates a radial gradient from the primary colour:
    /// the colour itself in the top-left corner fading to a darker slate at the bottom-right.
    /// </summary>
    private IBrush CreateRadialBrush()
    {
        RadialGradientBrush brush = new()
        {
            Center = new RelativePoint(0, 0, RelativeUnit.Relative),
            GradientOrigin = new RelativePoint(0, 0, RelativeUnit.Relative),
            RadiusX = new RelativeScalar(1.5, RelativeUnit.Relative),
            RadiusY = new RelativeScalar(1.5, RelativeUnit.Relative),
            GradientStops =
            {
                new GradientStop(Settings.PrimaryColor, 0),
                new GradientStop(MixWithBlack(Settings.PrimaryColor, LayoutConstants.GradientMixAmount),
                    LayoutConstants.RadialMidOffset),
                new GradientStop(MixWithBlack(Settings.PrimaryColor, LayoutConstants.GradientEndMixAmount), 1),
            },
        };
        return brush;
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
        resources["SystemAccentColor"] = Settings.AccentColor;
        resources["SystemAccentColorBrush"] = new SolidColorBrush(Settings.AccentColor);
        for (int i = 1; i <= 3; i++)
        {
            double amount = i * LayoutConstants.AccentTintStep;
            resources[$"SystemAccentColorLight{i}"] = AdjustAccent(amount);
            resources[$"SystemAccentColorDark{i}"] = AdjustAccent(-amount);
        }

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
                : CreateRadialBrush(),
            _ => CreateLinearBrush(),
        };
    }

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
        catch (Exception ex)
        {
            // Corrupt settings - fall back to defaults
            Logger.Error<BackgroundService>("Failed to load dashboard settings, falling back to defaults");
            Logger.LogExceptionDetails<BackgroundService>(ex);
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
        catch (Exception ex)
        {
            // Ignore save failures - the styling just won't persist
            Logger.Error<BackgroundService>("Failed to save dashboard settings");
            Logger.LogExceptionDetails<BackgroundService>(ex);
        }

        ApplyResources();
    }
}