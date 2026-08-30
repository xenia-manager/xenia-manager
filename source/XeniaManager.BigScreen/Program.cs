using System;
using Avalonia;
using XeniaManager.BigScreen.Services;
using XeniaManager.Logging;
using XeniaManager.Core.Utilities;

namespace XeniaManager.BigScreen;

sealed class Program
{
    /// <summary>
    /// Configures the Avalonia application builder. Also used by the visual designer.
    /// </summary>
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();

    [STAThread]
    public static void Main(string[] args)
    {
        string? baseDirectory = BaseAppLocator.Resolve(args);
        AppPathResolver.SetBaseDirectory(baseDirectory ?? string.Empty);
        Logger.Initialize(AppPathResolver.GetFullPath("Logs"));
        Logger.Info<Program>($"Starting BigScreen (base directory: {baseDirectory ?? "own folder"})");
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }
}