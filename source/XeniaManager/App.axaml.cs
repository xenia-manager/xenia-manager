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
using XeniaManager.Core.Settings;
using XeniaManager.Core.Utilities;
using XeniaManager.Services;
using XeniaManager.ViewModels;
using XeniaManager.Views;

namespace XeniaManager;

public partial class App : Application
{
    /// <summary>
    /// Desktop instance
    /// </summary>
    public static IClassicDesktopStyleApplicationLifetime? Desktop = Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;

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
            settings.SaveSettings();

            // Localization initialization
            LocalizationHelper.Initialize("avares://XeniaManager/Resources/Language/");
            LocalizationHelper.LoadLanguage(settings.Settings.Ui.Language);

            // Get MainWindow
            Logger.Debug<App>("Resolving MainWindow from services");
            MainWindow mainWindow = Services.GetRequiredService<MainWindow>();

            // Loading window properties
            settings.RestoreWindowProperties(settings, mainWindow);
            desktop.MainWindow = mainWindow;

            // Wire up window events
            mainWindow.Opened += (_, _) =>
            {
                Logger.Info<App>("Launching Xenia Manager");
                Logger.Debug<App>("Main window opened");
            };

            mainWindow.Closing += (_, e) =>
            {
                settings.SaveWindowProperties(settings, mainWindow);
                Logger.Info<App>("Main window closing");
                Logger.Debug<App>("Flushing logs before shutdown");
                Logger.Flush();
            };

            // Application exit handler
            desktop.Exit += (_, _) =>
            {
                Logger.Info<App>("Closing Xenia Manager");
                Logger.Debug<App>("Shutting down logger");
                Logger.Shutdown();
            };

            Logger.Info<App>("Application initialization completed successfully");
        }

        base.OnFrameworkInitializationCompleted();
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
}