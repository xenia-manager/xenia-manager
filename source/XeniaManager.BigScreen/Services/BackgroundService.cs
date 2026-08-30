using System;
using System.IO;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using XeniaManager.BigScreen.Constants;
using XeniaManager.BigScreen.Converters;
using XeniaManager.BigScreen.Factories;
using XeniaManager.BigScreen.Models.Settings;
using XeniaManager.Logging;

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

    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
        Converters =
        {
            new ColorJsonConverter()
        }
    };

    /// <summary>
    /// The currently loaded dashboard settings.
    /// </summary>
    public DashboardSettings Settings { get; private set; } = new DashboardSettings();

    /// <summary>
    /// The cached decoded background image bitmap, reused while the image path
    /// is unchanged so background rebuilds don't re-decode the file on disk.
    /// </summary>
    private Bitmap? _backgroundBitmap;

    /// <summary>
    /// The image path the cached bitmap was loaded from, so a path change
    /// triggers a re-decode and disposal of the previous bitmap.
    /// </summary>
    private string? _backgroundBitmapPath;

    /// <summary>
    /// Lightens (positive) or darkens (negative) the accent colour by the given amount.
    /// </summary>
    private Color AdjustAccent(double amount)
    {
        Color c = Settings.AccentColor;
        if (amount >= 0)
        {
            return BackgroundBrushFactory.Mix(c, Colors.White, amount);
        }

        return BackgroundBrushFactory.Mix(c, Colors.Black, -amount);
    }

    /// <summary>
    /// Creates an image brush from the configured image path, reusing the cached
    /// decoded bitmap while the path is unchanged. Returns null when no image is
    /// configured, the file is missing, or decoding fails.
    /// </summary>
    private IBrush? CreateImageBrush()
    {
        try
        {
            if (!string.IsNullOrEmpty(Settings.ImagePath) && File.Exists(Settings.ImagePath))
            {
                if (_backgroundBitmap == null || _backgroundBitmapPath != Settings.ImagePath)
                {
                    _backgroundBitmap?.Dispose();
                    _backgroundBitmap = new Bitmap(Settings.ImagePath);
                    _backgroundBitmapPath = Settings.ImagePath;
                }

                return new ImageBrush(_backgroundBitmap)
                {
                    Stretch = Stretch.UniformToFill
                };
            }
        }
        catch (Exception ex)
        {
            Logger.Warning<BackgroundService>($"Failed to load background image '{Settings.ImagePath}'");
            Logger.LogExceptionDetails<BackgroundService>(ex);
        }

        _backgroundBitmap?.Dispose();
        _backgroundBitmap = null;
        _backgroundBitmapPath = null;
        return null;
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
        for (int i = 1; i <= LayoutConstants.AccentVariantCount; i++)
        {
            double amount = i * LayoutConstants.AccentTintStep;
            resources[$"SystemAccentColorLight{i}"] = AdjustAccent(amount);
            resources[$"SystemAccentColorDark{i}"] = AdjustAccent(-amount);
        }

        resources["BackgroundVignette"] = BackgroundBrushFactory.CreateVignette(Settings.VignetteOpacity);
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
            BackgroundMode.Solid => BackgroundBrushFactory.CreateSolid(Settings.PrimaryColor),
            BackgroundMode.LinearGradient => BackgroundBrushFactory.CreateLinear(Settings.PrimaryColor),
            BackgroundMode.RadialGradient => BackgroundBrushFactory.CreateRadial(Settings.PrimaryColor),
            BackgroundMode.Dynamic => selectedGameArt != null
                ? new ImageBrush(selectedGameArt)
                {
                    Stretch = Stretch.UniformToFill
                }
                : BackgroundBrushFactory.CreateRadial(Settings.PrimaryColor),
            _ => BackgroundBrushFactory.CreateLinear(Settings.PrimaryColor)
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
            Logger.Error<BackgroundService>("Failed to save dashboard settings");
            Logger.LogExceptionDetails<BackgroundService>(ex);
        }

        ApplyResources();
    }
}