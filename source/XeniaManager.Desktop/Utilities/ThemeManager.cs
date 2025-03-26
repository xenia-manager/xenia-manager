
// Imported
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using XeniaManager.Core;

namespace XeniaManager.Desktop.Utilities;

public static class ThemeManager
{
    private static ApplicationTheme _currentTheme { get; set; }

    public static void ApplyTheme(Theme selectedTheme)
    {
        Logger.Info($"Applying {selectedTheme} theme");
        // Apply theme
        if (selectedTheme != Theme.System)
        {
            // Dark/Light
            if (selectedTheme == Theme.Dark)
            {
                _currentTheme = ApplicationTheme.Dark;
                ApplicationThemeManager.Apply(_currentTheme, WindowBackdropType.Acrylic, true);
            }
            else
            {
                _currentTheme = ApplicationTheme.Light;
                ApplicationThemeManager.Apply(_currentTheme, WindowBackdropType.None, true);
            }
        }
        else
        {
            // System Default
            ApplicationTheme _currentTheme = ApplicationTheme.Light;
            if (ApplicationThemeManager.GetSystemTheme() is SystemTheme.Dark or SystemTheme.CapturedMotion or SystemTheme.Glow)
            {
                _currentTheme = ApplicationTheme.Dark;
            }
            ApplicationThemeManager.ApplySystemTheme(true);
        }
    }
}