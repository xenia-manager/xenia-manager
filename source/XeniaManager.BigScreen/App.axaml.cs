using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using XeniaManager.BigScreen.Services;
using XeniaManager.BigScreen.Views;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Utilities;

namespace XeniaManager.BigScreen;

public partial class App : Application
{
    /// <summary>
    /// DI Services
    /// </summary>
    public static IServiceProvider Services { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Configure services
            Logger.Debug<App>("Configuring dependency injection services");
            Services = ServiceConfigurator.ConfigureServices();
            Logger.Info<App>("Services configured successfully");

            // Default-language resources so Core's PlaytimeFormatter can localize
            LocalizationHelper.Initialize("avares://XeniaManager.BigScreen/Resources/Language/");

            // FAAppWindow shows its built-in splash (AppSplashScreen) while the
            // boot pipeline runs, then reveals the fullscreen dashboard
            MainWindow mainWindow = Services.GetRequiredService<MainWindow>();
            desktop.MainWindow = mainWindow;

            desktop.Exit += (_, _) =>
            {
                Logger.Info<App>("Closing BigScreen");
                Logger.Debug<App>("Shutting down logger");
                Logger.Shutdown();
            };

            Logger.Info<App>("Application initialization completed successfully");
        }

        base.OnFrameworkInitializationCompleted();
    }
}
