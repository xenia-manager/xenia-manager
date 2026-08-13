using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using XeniaManager.BigScreen.Constants;
using XeniaManager.BigScreen.Services;
using XeniaManager.BigScreen.ViewModels;
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

    /// <summary>
    /// Shows the main window behind the splash and runs the boot pipeline,
    /// closing the splash when it completes. Runs deferred so the splash
    /// gets a chance to paint before any loading work blocks the UI thread.
    /// </summary>
    private static async void StartApp(
        IClassicDesktopStyleApplicationLifetime desktop,
        SplashWindow splash,
        MainWindowViewModel viewModel)
    {
        DateTime started = DateTime.Now;
        try
        {
            Logger.Info<App>("StartApp: resolving MainWindow");
            MainWindow mainWindow = Services.GetRequiredService<MainWindow>();
            desktop.MainWindow = mainWindow;
            mainWindow.WindowState = Avalonia.Controls.WindowState.FullScreen;
            mainWindow.Show();
            Logger.Info<App>("StartApp: MainWindow shown");

            IProgress<(string Status, double Progress)> progress =
                new Progress<(string, double)>(p => splash.SetProgress(p.Item1, p.Item2));
            Logger.Info<App>("StartApp: running boot pipeline");
            await viewModel.InitializeAsync(progress);
            Logger.Info<App>("StartApp: boot pipeline complete");
        }
        catch (Exception ex)
        {
            Logger.Error<App>("Failed to initialize BigScreen");
            Logger.LogExceptionDetails<App>(ex);
        }
        finally
        {
            // Hold the splash so it's perceptible even on a fast boot
            double elapsed = (DateTime.Now - started).TotalMilliseconds;
            if (elapsed < TimingConstants.SplashMinimumShowTime.TotalMilliseconds)
            {
                await Task.Delay(TimingConstants.SplashMinimumShowTime - TimeSpan.FromMilliseconds(elapsed));
            }

            Logger.Info<App>("StartApp: closing splash");
            splash.Close();
        }
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

            // Show the splash first, deferring the boot pipeline so the splash
            // paints before any loading work runs (see StartApp)
            MainWindowViewModel viewModel = Services.GetRequiredService<MainWindowViewModel>();
            SplashWindow splash = new SplashWindow();
            splash.Show();
            Logger.Info<App>("Splash window shown, deferring boot");
            Dispatcher.UIThread.Post(() => StartApp(desktop, splash, viewModel), DispatcherPriority.Background);

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