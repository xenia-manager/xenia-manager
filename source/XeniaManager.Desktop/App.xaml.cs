using System.Windows;

using XeniaManager.Core;
using XeniaManager.Core.Game;
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
        Logger.Info($"Xenia Manager v{Settings.GetCurrentVersion()}");
        
        // Load language
        LocalizationHelper.LoadLanguage(Settings.Ui.Language);

        // Load theme
        ThemeManager.ApplyTheme(Settings.Ui.Theme);
        
        // Load game library
        GameManager.LoadLibrary();
        
        // Check Launch Arguments
        Game game = e.Args.CheckLaunchArguments();
        if (game != null)
        {
            // Launching the game without showing the window
            Launcher.LaunchGame(game, AppSettings.Settings.Emulator.Settings.Profile.AutomaticSaveBackup, AppSettings.Settings.Emulator.Settings.Profile.ProfileSlot);
            GameManager.SaveLibrary();
            Application.Current.Shutdown();
        }

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