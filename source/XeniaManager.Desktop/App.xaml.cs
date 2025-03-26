using System.Windows;

using XeniaManager.Core;
using XeniaManager.Core.Settings;
using XeniaManager.Desktop.Utilities;

namespace XeniaManager.Desktop;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    // Variables
    private static ApplicationSettings _appSettings = new ApplicationSettings();
    public static ApplicationSettings.ApplicationSettingsStore Settings => _appSettings.Settings;

    // Functions
    protected override void OnStartup(StartupEventArgs e)
    {
        // Initialize Logger
        Logger.Initialize(e.Args.HasConsoleArgument());

        // Load language
        LocalizationHelper.LoadLanguage(Settings.UI.Language);

        // Load theme
        ThemeManager.ApplyTheme(Settings.UI.Theme);

        // Continue with startup
        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // Shutdown logger
        Logger.Shutdown();

        // Save settings
        _appSettings.SaveSettings();

        // Continue with shutdown
        base.OnExit(e);
    }
}