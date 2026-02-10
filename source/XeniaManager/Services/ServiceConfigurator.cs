using System;
using Microsoft.Extensions.DependencyInjection;
using XeniaManager.Core.Settings;
using XeniaManager.ViewModels;
using XeniaManager.ViewModels.Pages;
using XeniaManager.Views;
using Logger = XeniaManager.Core.Logging.Logger;

namespace XeniaManager.Services;

/// <summary>
/// Provides centralized service configuration and registration for the Altair application,
/// managing dependency injection container setup and service lifecycle management.
/// </summary>
public class ServiceConfigurator
{
    /// <summary>
    /// Configures and registers all application services with the dependency injection container.
    /// This method sets up the service collection with all required services and returns
    /// a built service provider ready for use.
    /// </summary>
    /// <returns>An IServiceProvider instance with all configured services.</returns>
    public static IServiceProvider ConfigureServices()
    {
        ServiceCollection services = new ServiceCollection();

        // Register Services
        // Settings
        services.AddSingleton<Settings>(serviceProvider =>
        {
            Settings settings = new Settings();
            try
            {
                // Load settings at startup (this will create defaults if the file doesn't exist)
                Settings.SettingsStore loadedSettings = settings.Settings;
            }
            catch (Exception ex)
            {
                Logger.Error<ServiceConfigurator>("Failed to initialize settings during registration");
                Logger.LogExceptionDetails<ServiceConfigurator>(ex);
                // Settings will fall back to defaults, which is handled by the settings system
            }
            return settings;
        });
        // NavigationService
        services.AddSingleton<NavigationService>();

        // Register Views/ViewModels
        // Pages
        services.AddSingleton<AboutPageViewModel>();
        services.AddSingleton<LibraryPageViewModel>();
        services.AddSingleton<ManagePageViewModel>();
        services.AddSingleton<SettingsPageViewModel>();
        services.AddSingleton<XeniaSettingsPageViewModel>();
        // MainView
        services.AddSingleton<MainViewModel>();
        // MainWindow
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<MainWindow>();

        IServiceProvider serviceProvider = services.BuildServiceProvider();
        return serviceProvider;
    }
}