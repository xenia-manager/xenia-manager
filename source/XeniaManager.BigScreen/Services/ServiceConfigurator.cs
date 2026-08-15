using System;
using Microsoft.Extensions.DependencyInjection;
using XeniaManager.BigScreen.ViewModels.Shell;
using XeniaManager.BigScreen.Views.Shell;
using XeniaManager.Core.Services;

namespace XeniaManager.BigScreen.Services;

/// <summary>
/// Provides centralized service configuration and registration for the BigScreen
/// application, managing the dependency injection container setup.
/// </summary>
public class ServiceConfigurator
{
    /// <summary>
    /// Registers all application services with the dependency injection container
    /// and returns a built service provider ready for use.
    /// </summary>
    /// <returns>An <see cref="IServiceProvider"/> instance with all configured services.</returns>
    public static IServiceProvider ConfigureServices()
    {
        ServiceCollection services = new ServiceCollection();

        services.AddSingleton<IBackgroundService, BackgroundService>();
        services.AddSingleton<IProfileService, ProfileService>();
        services.AddSingleton<IGameLibraryService, GameLibraryService>();
        services.AddSingleton<IScreenshotLibraryService, ScreenshotLibraryService>();
        services.AddSingleton<IModalService, ModalService>();
        services.AddSingleton<DashboardNavigationController>();
        services.AddSingleton<InputRouter>();
        services.AddSingleton<IGamepadInputService, GamepadInputService>();

        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<MainWindow>();

        return services.BuildServiceProvider();
    }
}