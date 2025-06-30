using System.IO;
using System.Windows;

// Imported Libraries
using XeniaManager.Core;
using XeniaManager.Core.Constants;
using XeniaManager.Core.Game;
using XeniaManager.Core.Settings;
using XeniaManager.Desktop.Components;
using XeniaManager.Desktop.Utilities;
using XeniaManager.Desktop.Views.Windows;

namespace XeniaManager.Desktop;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App
{
    #region Variables

    /// <summary>
    /// Global application settings manager instance.
    /// Provides centralized access to all application configuration data,
    /// including UI preferences, emulator settings, and user customizations.
    /// This instance handles both loading settings from storage and persisting changes.
    /// </summary>
    /// <remarks>
    /// Marked as readonly to prevent accidental reassignment while allowing
    /// modification of the settings data through the instance methods.
    /// </remarks>
    public static readonly ApplicationSettings AppSettings = new ApplicationSettings();

    /// <summary>
    /// Convenient accessor for the current application settings store.
    /// Provides direct access to the settings data without requiring
    /// interaction with the ApplicationSettings wrapper methods.
    /// </summary>
    /// <remarks>
    /// This property simplifies settings access throughout the application
    /// by providing a shorter, more readable syntax for common operations.
    /// </remarks>
    public static ApplicationSettings.ApplicationSettingsStore Settings => AppSettings.Settings;

    private static MainWindow _mainWindow { get; set; }

    #endregion

    #region Functions

    /// <summary>
    /// Handles application startup initialization and command-line argument processing.
    /// Performs essential system initialization in the correct order to ensure
    /// all dependencies are properly configured before the main window appears.
    /// Also supports headless game launching for direct game execution scenarios.
    /// </summary>
    /// <remarks>
    /// Initialization Order:
    /// <para>
    /// 1. Logger initialization (enables debugging throughout startup)
    /// </para>
    /// 2. Game library loading (populates available games)
    /// <para>
    /// 3. Command-line argument processing (handles direct game launches)
    /// </para>
    /// 4. Localization setup (loads user's preferred language)
    /// <para>
    /// 5. Theme application (applies user's visual preferences)
    /// </para>
    /// If a game is specified via command-line arguments, the application will
    /// launch the game directly and exit without showing the main UI, enabling
    /// integration with external game launchers and desktop shortcuts.
    /// </remarks>
    protected override async void OnStartup(StartupEventArgs e)
    {
        // Initialize Logger
        Logger.Initialize(e.Args.HasConsoleArgument());
        Logger.Info($"Xenia Manager v{Settings.GetInformationalVersion()}");

        // Load game library
        GameManager.LoadLibrary();

        // Check Launch Arguments
        Game game = e.Args.CheckLaunchArguments();
        if (game != null)
        {
            if (Settings.Ui.ShowGameLoadingBackground)
            {
                FullscreenImageWindow fullscreenImageWindow = new FullscreenImageWindow(Path.Combine(DirectoryPaths.Base, game.Artwork.Background), true);
                fullscreenImageWindow.Show();
                Task.Run(async () =>
                {
                    await Task.Delay(2000);
                    fullscreenImageWindow.Dispatcher.Invoke(() => fullscreenImageWindow.Visibility = Visibility.Hidden);
                });
            }
            // Launching the game without showing the window
            await Launcher.LaunchGameASync(game, Settings.Emulator.Settings.Profile.AutomaticSaveBackup, Settings.Emulator.Settings.Profile.ProfileSlot);
            GameManager.SaveLibrary(); // Save any changes to the game library (e.g. playtime)
            Current.Shutdown(); // Exit the application after launching the game
            return; // Exit early to prevent normal startup continuation
        }

        // Load and apply the user's preferred language settings
        LocalizationHelper.LoadLanguage(Settings.Ui.Language);

        // Apply the user's preferred visual theme (Light/Dark/System)
        ThemeManager.ApplyTheme(Settings.Ui.Theme);

        // Continue with normal application startup if no direct game launch was requested
        _mainWindow = new MainWindow();
        _mainWindow.Show();
        base.OnStartup(e);
    }

    /// <summary>
    /// Handles application shutdown procedures to ensure clean exit.
    /// Performs necessary cleanup operations including settings persistence
    /// and proper shutdown of all initialized subsystems.
    /// </summary>
    /// <remarks>
    /// Shutdown Order:
    /// <para>
    /// 1. Settings persistence (saves all user preferences and application state)
    /// </para>
    /// 2. Logger shutdown (flushes any pending log entries and closes log files)
    /// <para>
    /// 3. Base shutdown (allows WPF framework to complete its cleanup)
    /// </para>
    /// This method ensures that user data is never lost due to improper shutdown
    /// and that system resources are properly released.
    /// </remarks>
    protected override void OnExit(ExitEventArgs e)
    {
        // Save settings
        AppSettings.SaveSettings();

        // Shutdown logger
        Logger.Shutdown();

        // Continue with shutdown
        base.OnExit(e);
    }

    #endregion
}