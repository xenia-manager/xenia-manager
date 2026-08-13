using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using XeniaManager.BigScreen.Services;
using XeniaManager.BigScreen.Views.Shell;
using XeniaManager.Core.Constants;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Models;
using XeniaManager.Core.Services;
using XeniaManager.Core.Settings;
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

            // Keep the SDL controller database current silently in the background
            _ = UpdateSdlDatabaseSilentlyAsync();

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

    /// <summary>
    /// Silently keeps the SDL game controller database current for every
    /// installed emulator, then reloads the live mappings. Runs in the
    /// background after boot; failures are logged and ignored.
    /// </summary>
    private static async Task UpdateSdlDatabaseSilentlyAsync()
    {
        try
        {
            Settings settings = new();
            List<XeniaVersion> installedVersions = settings.GetInstalledVersions(settings);
            if (installedVersions.Count == 0)
            {
                return;
            }

            Logger.Info<App>("Updating SDL game controller database");
            DownloadManager downloadManager = new();
            await downloadManager.DownloadFileFromMultipleUrlsAsync(Urls.GameControllerDatabase,
                "gamecontrollerdb.txt");
            string downloaded = Path.Combine(downloadManager.DownloadPath, "gamecontrollerdb.txt");

            foreach (XeniaVersion version in installedVersions)
            {
                string emulatorDir =
                    AppPathResolver.GetFullPath(XeniaVersionInfo.GetXeniaVersionInfo(version).EmulatorDir);
                File.Copy(downloaded, Path.Combine(emulatorDir, "gamecontrollerdb.txt"), true);
                Logger.Info<App>($"Updated SDL database for Xenia {version}");
            }

            Services.GetRequiredService<IGamepadInputService>().ReloadMappings();
            Logger.Info<App>("SDL game controller database updated");
        }
        catch (Exception ex)
        {
            Logger.Warning<App>("Failed to update SDL game controller database");
            Logger.LogExceptionDetails<App>(ex);
        }
    }
}