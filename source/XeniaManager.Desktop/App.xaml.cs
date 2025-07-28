using System.IO;
using System.Windows;
using System.Windows.Threading;

// Imported Libraries
using XeniaManager.Core;
using XeniaManager.Core.Constants;
using XeniaManager.Core.Extensions;
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
        try
        {
            // Initialize Logger first (before anything else!)
#if EXPERIMENTAL_BUILD
            bool showConsole = true;
#else
            bool showConsole = e.Args.HasConsoleArgument() || App.Settings.UpdateChecks.UseExperimentalBuild;
#endif
            Logger.Initialize(showConsole);
            Logger.Info($"Xenia Manager {Settings.GetManagerVersion()} starting up...");

            // Set up global exception handlers after logger is initialized
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            Logger.Debug("Global exception handlers registered");
            Logger.Debug("Startup arguments: {Args}", string.Join(" ", e.Args));

            // Load game library
            Logger.Debug("Loading game library...");
            GameManager.LoadLibrary();
            Logger.Info("Game library loaded successfully");

            // Check Launch Arguments
            Logger.Debug("Checking launch arguments...");
            Game? game = e.Args.CheckLaunchArguments();
            if (game != null)
            {
                Logger.Info("Direct game launch requested: {GameTitle}", game.Title);

                if (Settings.Ui.ShowGameLoadingBackground)
                {
                    Logger.Debug("Showing fullscreen loading background");
                    FullscreenImageWindow fullscreenImageWindow = new FullscreenImageWindow(ArtworkManager.PreloadImage(Path.Combine(DirectoryPaths.Base, game.Artwork.Background)), true);
                    fullscreenImageWindow.Show();
                    Task.Run(async () =>
                    {
                        await Task.Delay(2000);
                        fullscreenImageWindow.Dispatcher.Invoke(() => fullscreenImageWindow.Visibility = Visibility.Hidden);
                    });
                }

                // Launching the game without showing the window
                Logger.Info("Launching game directly...");
                await Launcher.LaunchGameASync(game, Settings.Emulator.Settings.Profile.AutomaticSaveBackup, Settings.Emulator.Settings.Profile.ProfileSlot);
                GameManager.SaveLibrary(); // Save any changes to the game library (e.g. playtime)
                Logger.Info("Game launched successfully, shutting down application");
                Current.Shutdown(); // Exit the application after launching the game
                return; // Exit early to prevent normal startup continuation
            }

            // Load and apply the user's preferred language settings
            Logger.Debug("Loading localization...");
            LocalizationHelper.LoadLanguage(Settings.Ui.Language);
            Logger.Debug("Localization loaded: {Language}", Settings.Ui.Language);

            // Apply the user's preferred visual theme (Light/Dark/System)
            Logger.Debug("Applying theme and accent...");
            ThemeManager.ApplyTheme(Settings.Ui.Theme);
            ThemeManager.ApplyAccent(Settings.Ui.AccentColor);
            Logger.Debug("Theme applied: {Theme}, Accent: {Accent}", Settings.Ui.Theme, Settings.Ui.AccentColor);

            // Continue with normal application startup if no direct game launch was requested
            Logger.Debug("Creating main window...");
            _mainWindow = new MainWindow();
            _mainWindow.Show();
            Logger.Info("Application startup completed successfully");

            base.OnStartup(e);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Critical error during application startup");

            CustomMessageBox.Show("Startup Error", $"Failed to start Xenia Manager.\n\nError: {ex.Message}\n\nCheck the log files in the application folder for more details.");

            Logger.Shutdown();
            Environment.Exit(1);
        }
    }

    /// <summary>
    /// Handles unhandled exceptions in the WPF dispatcher thread
    /// </summary>
    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Logger.Error(e.Exception, "Unhandled dispatcher exception occurred");

        // Log additional context for debugging
        LogExceptionDetails(e.Exception, "DispatcherUnhandledException");

        // Determine if we should continue or terminate
        Logger.Error("Critical exception detected. Application will terminate.");
        CustomMessageBox.Show("Critical Error", $"A critical error occurred and Xenia Manager must close.\n\n{e.Exception.Message}\n\nCheck the log files for details.");

        Logger.Shutdown();
        Environment.Exit(1);
    }

    /// <summary>
    /// Handles unhandled exceptions in non-UI threads
    /// </summary>
    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        Exception? exception = e.ExceptionObject as Exception;
        Logger.Error(exception, "Unhandled domain exception. Terminating: {IsTerminating}");

        if (exception != null)
        {
            LogExceptionDetails(exception, "UnhandledException");
        }

        // Always show message for unhandled domain exceptions
        CustomMessageBox.Show("Fatal Error", $"A fatal error occurred in Xenia Manager.\n\n{exception?.Message ?? "Unknown error"}\n\nThe application will now close.");

        Logger.Shutdown();
    }

    /// <summary>
    /// Logs detailed exception information for debugging
    /// </summary>
    private void LogExceptionDetails(Exception exception, string context)
    {
        Logger.Error("=== Exception Details ({Context}) ===", context);
        Logger.Error("Exception Type: {ExceptionType}", exception.GetType().FullName);
        Logger.Error("Message: {Message}", exception.Message);
        Logger.Error("Source: {Source}", exception.Source ?? "Unknown");
        Logger.Error("Target Site: {TargetSite}", exception.TargetSite?.Name ?? "Unknown");

        // Log inner exceptions
        Exception? innerEx = exception.InnerException;
        int depth = 1;
        while (innerEx != null)
        {
            Logger.Error("Inner Exception {Depth}: {Type} - {Message}",
                depth, innerEx.GetType().Name, innerEx.Message);
            innerEx = innerEx.InnerException;
            depth++;
        }

        Logger.Error("Full Stack Trace:\n{StackTrace}", exception.StackTrace);
        Logger.Error("=== End Exception Details ===");
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
        Logger.Info("Application shutting down with exit code: {ExitCode}", e.ApplicationExitCode);

        try
        {
            // Save settings
            Logger.Debug("Saving application settings...");
            AppSettings.SaveSettings();
            Logger.Debug("Settings saved successfully");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to save settings during shutdown");
        }

        // Shutdown logger
        Logger.Info("Shutdown complete");
        Logger.Shutdown();

        // Continue with shutdown
        base.OnExit(e);
    }

    #endregion
}