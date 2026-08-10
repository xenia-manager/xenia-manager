using System;
using Avalonia;
using XeniaManager.BigScreen.Services;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Utilities;

namespace XeniaManager.BigScreen;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        // Share the base Xenia Manager's data folders (library, games, artwork, profiles)
        string? baseDirectory = BaseAppLocator.Resolve(args);
        AppPathResolver.SetBaseDirectory(baseDirectory ?? string.Empty);
        Logger.Info<Program>($"Starting BigScreen (base directory: {baseDirectory ?? "own folder"})");
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}
