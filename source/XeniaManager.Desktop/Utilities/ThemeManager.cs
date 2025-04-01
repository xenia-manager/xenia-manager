// Imported
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using XeniaManager.Core;

namespace XeniaManager.Desktop.Utilities;

/// <summary>
/// Applies selected themes
/// </summary>
// TODO: Add support for different backdrops and accent colors
public static class ThemeManager
{
    private static ApplicationTheme _currentTheme { get; set; }

    /// <summary>
    /// Applies the selected theme to the Xenia Manager UI
    /// </summary>
    /// <param name="selectedTheme">Selected theme</param>
    public static void ApplyTheme(Theme selectedTheme)
    {
        Logger.Info($"Applying {selectedTheme} theme");
        // Apply theme
        // Dark/Light
        if (selectedTheme == Theme.Dark)
        {
            _currentTheme = ApplicationTheme.Dark;
            ApplicationThemeManager.Apply(_currentTheme, WindowBackdropType.None, true);
        }
        else
        {
            _currentTheme = ApplicationTheme.Light;
            ApplicationThemeManager.Apply(_currentTheme, WindowBackdropType.None, true);
        }
    }
}