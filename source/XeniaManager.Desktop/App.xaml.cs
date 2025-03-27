using System.Windows;

using XeniaManager.Core;
using XeniaManager.Core.Settings;
using XeniaManager.Desktop.Utilities;

namespace XeniaManager.Desktop;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App
{
    // Variables
    public static readonly ApplicationSettings AppSettings = new ApplicationSettings();
    public static ApplicationSettings.ApplicationSettingsStore Settings => AppSettings.Settings;

    // Functions
    protected override void OnStartup(StartupEventArgs e)
    {
        // Initialize Logger
        Logger.Initialize(e.Args.HasConsoleArgument());

        // Load language
        LocalizationHelper.LoadLanguage(Settings.Ui.Language);

        // Load theme
        ThemeManager.ApplyTheme(Settings.Ui.Theme);

        // Continue with startup
        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // Shutdown logger
        Logger.Shutdown();

        // Save settings
        AppSettings.SaveSettings();

        // Continue with shutdown
        base.OnExit(e);
    }
}