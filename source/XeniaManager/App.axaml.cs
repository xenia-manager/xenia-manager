using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Manage;
using XeniaManager.Core.Models.Game;
using XeniaManager.Core.Services;
using XeniaManager.Core.Settings;
using XeniaManager.Core.Utilities;
using XeniaManager.Services;
using XeniaManager.Views;

namespace XeniaManager;

public partial class App : Application
{
    /// <summary>
    /// Desktop instance
    /// </summary>
    public static readonly IClassicDesktopStyleApplicationLifetime? Desktop = Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;

    /// <summary>
    /// Main Window instance
    /// </summary>
    public static Window? MainWindow => Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop ? desktop.MainWindow : null;

    /// <summary>
    /// DI Services
    /// </summary>
    public static IServiceProvider Services { get; private set; } = null!;

    public override void Initialize()
    {
        Logger.Debug<App>("Initializing Avalonia application");
        AvaloniaXamlLoader.Load(this);
        Logger.Debug<App>("Avalonia XAML loaded successfully");
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (Desktop is { } desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit.
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            Logger.Trace<App>("Disabling Avalonia data annotation validation");
            DisableAvaloniaDataAnnotationValidation();

            // Global exception handlers
            Logger.Trace<App>("Registering global exception handlers");
            RegisterGlobalExceptionHandlers();

            // Configure services
            Logger.Debug<App>("Configuring dependency injection services");
            try
            {
                Services = ServiceConfigurator.ConfigureServices();
                Logger.Info<App>("Services configured successfully");
            }
            catch (Exception ex)
            {
                Logger.Error<App>("Failed to configure services");
                Logger.LogExceptionDetails<App>(ex);
                throw;
            }

            // Initial loading and saving of settings
            Settings settings = Services.GetRequiredService<Settings>();
            Logger.SetLogLevel(settings.Settings.Debug.LogLevel);
            settings.SaveSettings();

            // Load the library
            GameManager.LoadLibrary();

            // Localization initialization
            LocalizationHelper.Initialize("avares://XeniaManager/Resources/Language/");
            LocalizationHelper.LoadLanguage(settings.Settings.Ui.Language);

            // Get MainWindow
            Logger.Debug<App>("Resolving MainWindow from services");
            MainWindow mainWindow = Services.GetRequiredService<MainWindow>();
            desktop.MainWindow = mainWindow;

            // Check for arguments first (before showing MainWindow)
            Game? gameFromArgs = ArgumentParser.GetGameFromArgs(desktop.Args);

            if (gameFromArgs != null)
            {
                // Game is being launched directly from arguments
                Logger.Debug<App>("Game launch detected via arguments");

                // Set window state to minimized and disable it
                mainWindow.WindowState = WindowState.Minimized;
                EventManager.Instance.DisableWindow();

                // Wire up Application exit handler for argument-based launch
                desktop.Exit += (_, _) =>
                {
                    Logger.Info<App>("Exiting Xenia Manager after game session");
                    Logger.Debug<App>("Shutting down logger");
                    Logger.Shutdown();
                };

                // Launch the game with a loading screen
                ArgumentChecker(gameFromArgs, settings, mainWindow);
            }
            else
            {
                // No game launch via arguments, show MainWindow as normal
                SetupMainWindow(mainWindow, settings);
            }

            Logger.Info<App>("Application initialization completed successfully");
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// Registers global exception handlers for unhandled exceptions
    /// </summary>
    private void RegisterGlobalExceptionHandlers()
    {
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            args.SetObserved();
            Logger.Error<App>("Unobserved task exception occurred");
            HandleFatalException(args.Exception);
        };

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            bool isTerminating = args.IsTerminating;
            Logger.Error<App>($"Unhandled exception in AppDomain (Terminating: {isTerminating})");

            if (args.ExceptionObject is Exception ex)
            {
                HandleFatalException(ex);
            }
            else
            {
                Logger.Error<App>($"Non-exception object thrown: {args.ExceptionObject?.GetType().FullName ?? "null"}");
            }
        };

        Dispatcher.UIThread.UnhandledException += (_, args) =>
        {
            args.Handled = true;
            Logger.Error<App>("Unhandled exception on UI thread");
            HandleFatalException(args.Exception);
        };
    }

    /// <summary>
    /// Sets up the MainWindow with event handlers and window properties
    /// </summary>
    private void SetupMainWindow(MainWindow mainWindow, Settings settings)
    {
        // Restore window properties
        settings.RestoreWindowProperties(settings, mainWindow);

        // Wire up window events
        mainWindow.Opened += (_, _) =>
        {
            Logger.Info<App>("Launching Xenia Manager");
            Logger.Debug<App>("Main window opened");
        };

        mainWindow.Closing += async (_, _) =>
        {
            // Run cleanup in the background to not block app shutdown
            await Task.Run(() =>
            {
                int deletedCount = ArtworkManager.ClearUnusedCachedArtwork();
                Logger.Info<App>($"Cleaned up {deletedCount} unused cached artwork files");
            });

            settings.SaveWindowProperties(settings, mainWindow);
            Logger.Info<App>("Main window closing");
            Logger.Debug<App>("Flushing logs before shutdown");
            Logger.Flush();
        };

        // Application exit handler
        Desktop?.Exit += (_, _) =>
        {
            Logger.Info<App>("Closing Xenia Manager");
            Logger.Debug<App>("Shutting down logger");
            Logger.Shutdown();
        };
    }

    private static void HandleFatalException(Exception ex)
    {
        try
        {
            Logger.Error<App>("=== Fatal Exception Encountered ===");
            Logger.LogExceptionDetails<App>(ex, includeEnvironmentInfo: true);

            // Ensure logs are written before a potential crash
            Logger.Flush();
        }
        catch
        {
            // If logging fails, we can do little about it
            // Just ensure we don't throw from the exception handler
        }
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        DataAnnotationsValidationPlugin[] dataValidationPluginsToRemove = BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (DataAnnotationsValidationPlugin plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }

    private async void ArgumentChecker(Game game, Settings settings, MainWindow mainWindow)
    {
        Logger.Info<App>($"Launching game '{game.Title}' directly from desktop shortcut.");

        LoadingScreenWindow? loadingScreen = null;

        try
        {
            // Check if loading screen should be shown
            bool showLoadingScreen = settings.Settings.Ui.Window.LoadingScreen;

            if (showLoadingScreen && game.Artwork.CachedBackground != null)
            {
                loadingScreen = new LoadingScreenWindow();
                loadingScreen.SetBackground(game.Artwork.CachedBackground);
                loadingScreen.SetLoadingText(game.Title);
            }

            // Create a callback to notify when the game starts loading
            Action? onGameLoadingStarted = () =>
            {
                if (loadingScreen != null)
                {
                    Logger.Info<App>("Game loading started");
                    loadingScreen.OnGameLoadingStarted();
                }
            };

            // Show the loading screen before launching the game
            if (loadingScreen != null)
            {
                Logger.Info<App>("Showing loading screen");
                await loadingScreen.ShowAsync();
            }

            // Launch the game asynchronously
            await Launcher.LaunchGameASync(game, settings, onGameLoadingStarted: onGameLoadingStarted);
            Logger.Info<App>($"Game session ended for '{game.Title}'");
        }
        catch (Exception ex)
        {
            Logger.Error<App>($"Error occurred while launching game '{game.Title}': {ex.Message}");
            Logger.LogExceptionDetails<App>(ex);
        }
        finally
        {
            // Close loading screen if still open (cleanup in case callback didn't fire)
            if (loadingScreen != null)
            {
                Dispatcher.UIThread.Post(async () => await loadingScreen.CloseAsync());
            }

            // Ensure the game library is saved
            try
            {
                GameManager.SaveLibrary();
                Logger.Debug<App>("Game library saved successfully");
            }
            catch (Exception ex)
            {
                Logger.Error<App>($"Failed to save game library: {ex.Message}");
            }

            // Flush logs before exit
            try
            {
                Logger.Flush();
                Logger.Debug<App>("Logs flushed before shutdown");
            }
            catch (Exception)
            {
                // Logging failed, nothing we can do
            }

            // Clean shutdown
            Logger.Debug<App>("Shutting down logger");
            Logger.Shutdown();

            Logger.Info<App>("Exiting Xenia Manager");
            Environment.Exit(0);
        }
    }
}