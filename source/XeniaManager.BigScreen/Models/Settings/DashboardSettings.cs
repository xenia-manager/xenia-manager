using System.Collections.Generic;
using System.Text.Json.Serialization;
using Avalonia.Media;
using XeniaManager.Core.Models;

namespace XeniaManager.BigScreen.Models.Settings;

/// <summary>
/// User-facing dashboard styling options.
/// </summary>
public class DashboardSettings
{
    /// <summary>
    /// The active background mode.
    /// </summary>
    [JsonPropertyName("mode")]
    public BackgroundMode Mode { get; set; } = BackgroundMode.Dynamic;

    /// <summary>
    /// Path to the background image used in <see cref="BackgroundMode.Image"/>.
    /// </summary>
    [JsonPropertyName("image_path")]
    public string ImagePath { get; set; } = string.Empty;

    /// <summary>
    /// The primary color. Used directly for solid backgrounds and as the base
    /// for derived linear/radial gradient shades.
    /// </summary>
    [JsonPropertyName("primary_color")]
    public Color PrimaryColor { get; set; } = Color.FromRgb(0x1C, 0x1F, 0x25);

    /// <summary>
    /// Accent colour used for the selected card border.
    /// </summary>
    [JsonPropertyName("accent_color")]
    public Color AccentColor { get; set; } = Color.FromRgb(0x10, 0x7C, 0x41);

    /// <summary>
    /// Vignette edge opacity (0-1). 0 disables the vignette.
    /// </summary>
    [JsonPropertyName("vignette_opacity")]
    public double VignetteOpacity { get; set; } = 0.2;

    /// <summary>
    /// Whether Quit returns to Xenia Manager (launching it if it isn't running).
    /// Off = just close BigScreen.
    /// </summary>
    [JsonPropertyName("return_to_xenia")]
    public bool ReturnToXeniaOnQuit { get; set; } = true;

    /// <summary>
    /// Whether launched games start in fullscreen (sets the game's
    /// Display.fullscreen for the session, restoring the original after).
    /// </summary>
    [JsonPropertyName("launch_games_fullscreen")]
    public bool LaunchGamesFullscreen { get; set; } = true;

    /// <summary>
    /// The layout used by the library screen.
    /// </summary>
    [JsonPropertyName("library_view_mode")]
    public LibraryViewMode LibraryViewMode { get; set; } = LibraryViewMode.Carousel;

    /// <summary>
    /// The image shown on dashboard game cards.
    /// </summary>
    [JsonPropertyName("card_image_mode")]
    public CardImageMode CardImageMode { get; set; } = CardImageMode.Icon;

    /// <summary>
    /// The hour format used by the header clock and capture dates.
    /// </summary>
    [JsonPropertyName("time_format")]
    public TimeFormat TimeFormat { get; set; } = TimeFormat.TwelveHour;

    /// <summary>
    /// Device GUID (hex) of the primary gamepad, restored at boot.
    /// </summary>
    [JsonPropertyName("primary_controller_guid")]
    public string? PrimaryControllerGuid { get; set; }

    /// <summary>
    /// Hex PathXUID of the active profile per emulator version, restored at boot.
    /// </summary>
    [JsonPropertyName("active_profiles")]
    public Dictionary<XeniaVersion, string> ActiveProfiles { get; set; } = [];

    /// <summary>
    /// The emulator version whose profile is considered active (drives the
    /// header identity and pickers), restored at boot.
    /// </summary>
    [JsonPropertyName("active_version")]
    public XeniaVersion? ActiveVersion { get; set; }

    /// <summary>
    /// Whether the header identity automatically cycles through every version
    /// that has an active profile (display only).
    /// </summary>
    [JsonPropertyName("rotate_profiles")]
    public bool RotateProfiles { get; set; } = true;
}