using System.Windows.Media;

// Imported Libraries
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using XeniaManager.Core;

namespace XeniaManager.Desktop.Utilities;

/// <summary>
/// Applies selected themes
/// </summary>
public static class ThemeManager
{
    private static readonly ApplicationTheme _defaultTheme = ApplicationTheme.Light;
    private static ApplicationTheme _currentTheme { get; set; } = _defaultTheme;

    private static readonly WindowBackdropType _defaultBackdropType = WindowBackdropType.None;
    private static WindowBackdropType _currentBackdropType { get; set; } = _defaultBackdropType;

    private static readonly Color _defaultAccentColor = Colors.DarkGreen;
    private static Color _currentAccentColor { get; set; } = _defaultAccentColor;

    /// <summary>
    /// Applies the specified theme to the application.
    /// </summary>
    /// <remarks>This method updates the application's visual appearance by applying the selected theme and
    /// associated settings. It modifies the application's theme, backdrop type, and accent color based on the provided
    /// <paramref name="selectedTheme"/>.</remarks>
    /// <param name="selectedTheme">The theme to apply. Must be either <see cref="Theme.Dark"/> or <see cref="Theme.Light"/>.</param>
    public static void ApplyTheme(Theme selectedTheme)
    {
        Logger.Info($"Applying {selectedTheme} theme");
        _currentTheme = selectedTheme switch
        {
            Theme.Dark => ApplicationTheme.Dark,
            Theme.Light => ApplicationTheme.Light,
            _ => ApplicationTheme.Light
        };

        Reload();
    }
    public static void Reload()
    {
        ApplicationThemeManager.Apply(_currentTheme, _currentBackdropType);
        ApplyDefaultAccent();
    }

    /// <summary>
    /// Applies the specified accent color to the application's theme.
    /// </summary>
    /// <remarks>This method updates the application's theme and accent color settings. The specified accent
    /// color is applied to the theme, and additional adjustments may be made to ensure consistency with the current
    /// backdrop type and theme.</remarks>
    /// <param name="accentColor">The color to use as the accent for the application theme.</param>
    public static void ApplyAccent(Color accentColor)
    {
        Logger.Info($"Applying {accentColor} color to accent");
        _currentAccentColor = accentColor;
        ApplicationAccentColorManager.Apply(_currentAccentColor, _currentTheme);
    }

    /// <summary>
    /// Applies the current accent color and theme to the application.
    /// </summary>
    /// <remarks>This method updates the application's accent color to a predefined value and applies it along
    /// with the current theme. It is typically used to refresh the application's visual appearance.</remarks>
    public static void ApplyDefaultAccent()
    {
        ApplyAccent(_defaultAccentColor);
    }
}