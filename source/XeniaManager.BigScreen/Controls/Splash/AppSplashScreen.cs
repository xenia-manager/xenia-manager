using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Threading;
using FluentAvalonia.UI.Windowing;
using Microsoft.Extensions.DependencyInjection;
using XeniaManager.BigScreen.Constants;
using XeniaManager.BigScreen.ViewModels.Shell;
using XeniaManager.Logging;

namespace XeniaManager.BigScreen.Controls.Splash;

/// <summary>
/// FluentAvalonia's built-in splash screen: hosts the splash visuals and
/// runs the boot pipeline (profile, settings, library, gallery) with live
/// status/progress while the main window is still hidden.
/// </summary>
internal class AppSplashScreen : IFAApplicationSplashScreen
{
    /// <summary>
    /// No window icon - the custom splash content covers the screen.
    /// </summary>
    public IImage AppIcon
    {
        get
        {
            return null!;
        }
    }

    /// <summary>
    /// No app name shown - the custom splash content covers the screen.
    /// </summary>
    public string AppName
    {
        get
        {
            return null!;
        }
    }

    /// <summary>
    /// Custom splash visuals: logo, live status, progress bar.
    /// </summary>
    public object SplashScreenContent { get; }

    /// <summary>
    /// Minimum time the splash stays visible, even on a fast boot.
    /// </summary>
    public int MinimumShowTime
    {
        get
        {
            return (int)TimingConstants.SplashMinimumShowTime.TotalMilliseconds;
        }
    }

    public AppSplashScreen()
    {
        SplashScreenContent = new SplashScreenView();
    }

    /// <summary>
    /// Runs the boot pipeline behind the splash. Called by FAAppWindow on a
    /// background thread, so the pipeline (which mutates UI-bound collections)
    /// is dispatched back onto the UI thread.
    /// </summary>
    public async Task RunTasks(CancellationToken token)
    {
        MainWindowViewModel viewModel = App.Services.GetRequiredService<MainWindowViewModel>();
        SplashScreenView view = (SplashScreenView)SplashScreenContent;

        try
        {
            await Task.Run(async () =>
            {
                await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    IProgress<(string Status, double Progress)> progress =
                        new Progress<(string, double)>(p => view.SetProgress(p.Item1, p.Item2));
                    await viewModel.InitializeAsync(progress, token);
                });
            }, token);
        }
        catch (Exception ex)
        {
            Logger.Error<AppSplashScreen>("Failed to initialize BigScreen");
            Logger.LogExceptionDetails<AppSplashScreen>(ex);
        }
    }
}