using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Windowing;
using Microsoft.Extensions.DependencyInjection;
using XeniaManager.Core.Installation;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Models;
using XeniaManager.Core.Services;
using XeniaManager.Core.Settings.Sections;
using XeniaManager.Core.Utilities;
using XeniaManager.ViewModels;

namespace XeniaManager.Views;

public partial class MainWindow : AppWindow
{
    // Properties
    private MainWindowViewModel _viewModel { get; set; }
    private IReleaseService _releaseService { get; set; }
    private InfoBar? _infoBar;

    // Constructor
    public MainWindow()
    {
        InitializeComponent();
        _viewModel = App.Services.GetRequiredService<MainWindowViewModel>();
        _releaseService = App.Services.GetRequiredService<IReleaseService>();
        DataContext = _viewModel;

        // Get the InfoBar control
        _infoBar = this.FindControl<InfoBar>("InfoBar");

        // Subscribe to EventManager for window state changes
        EventManager.Instance.WindowDisabled += OnWindowDisabled;
    }

    private async void Control_OnLoaded(object? sender, RoutedEventArgs e)
    {
        try
        {
            // TODO: Add checking for updates
            await CheckForEmulatorUpdates();
        }
        catch
        {
            // ignored
        }
    }

    /// <summary>
    /// Checks for emulator updates and notifies the user if updates are available.
    /// </summary>
    private async Task CheckForEmulatorUpdates()
    {
        Logger.Debug<MainWindow>("Checking for emulator updates");

        List<XeniaVersion> updatesAvailable = [];
        Dictionary<XeniaVersion, EmulatorInfo?> emulators = new Dictionary<XeniaVersion, EmulatorInfo?>
        {
            { XeniaVersion.Canary, _viewModel.Settings.Settings.Emulator.Canary }
            // TODO: Add Mousehook & Netplay Update Checking
        };

        Core.Manage.Launcher.XeniaUpdating = true;

        foreach ((XeniaVersion version, EmulatorInfo? emulator) in emulators)
        {
            try
            {
                if (emulator == null)
                {
                    Logger.Debug<MainWindow>($"Xenia {version} emulator info is null, skipping update check");
                    continue;
                }

                ReleaseType releaseType = version switch
                {
                    XeniaVersion.Canary => ReleaseType.XeniaCanary,
                    XeniaVersion.Mousehook => ReleaseType.MousehookStandard,
                    XeniaVersion.Netplay => emulator.UseNightlyBuild ? ReleaseType.NetplayNightly : ReleaseType.NetplayStable,
                    _ => throw new Exception($"Unknown Xenia version: {version}")
                };

                bool needsUpdate = false;

                if (emulator.UpdateAvailable)
                {
                    Logger.Debug<MainWindow>($"Xenia {version} update already flagged as available");
                    needsUpdate = true;
                }
                else if ((DateTime.Now - emulator.LastUpdateCheckDate).TotalDays >= 1)
                {
                    Logger.Info<MainWindow>($"Checking for Xenia {version} updates");
                    (needsUpdate, _) = await XeniaService.CheckForUpdatesAsync(_releaseService, emulator, releaseType, true);
                }
                else
                {
                    Logger.Debug<MainWindow>($"Xenia {version} was checked recently, skipping");
                    continue;
                }

                if (needsUpdate)
                {
                    updatesAvailable.Add(version);
                    emulator.UpdateAvailable = true;
                    Logger.Info<MainWindow>($"Xenia {version} update is available");
                }
            }
            catch (Exception ex)
            {
                Logger.Error<MainWindow>($"Failed to check for Xenia {version} updates");
                Logger.LogExceptionDetails<MainWindow>(ex);
            }
        }

        Core.Manage.Launcher.XeniaUpdating = false;

        if (updatesAvailable.Count > 0)
        {
            string xeniaVersions = string.Join(", ", updatesAvailable);
            Logger.Info<MainWindow>($"Updates available for: {xeniaVersions}");
            string message = string.Format(LocalizationHelper.GetText("InfoBar.UpdatesAvailable"), xeniaVersions);
            _viewModel.ShowInfoBar(message, InfoBarSeverity.Informational);
        }
        else
        {
            Logger.Debug<MainWindow>("No emulator updates available");
        }

        await _viewModel.Settings.SaveSettingsAsync();
        Logger.Debug<MainWindow>("Finished checking for emulator updates");
    }

    private void OnWindowDisabled(bool isDisabled)
    {
        _viewModel.DisableWindow = isDisabled;
    }

    /// <summary>
    /// Animates the InfoBar sliding in from the top.
    /// </summary>
    public async Task SlideInInfoBar()
    {
        if (_infoBar == null)
        {
            return;
        }

        // Set the initial state
        _infoBar.Opacity = 0;
        TranslateTransform transform = new TranslateTransform(0, -24);
        _infoBar.RenderTransform = transform;
        _infoBar.IsVisible = true;

        // Animate both opacity and translation
        await Task.WhenAll(
            AnimateOpacity(_infoBar, 0.0, 1.0, TimeSpan.FromMilliseconds(300), new QuadraticEaseOut()),
            AnimateTranslateY(transform, -20, 0, TimeSpan.FromMilliseconds(300), new QuadraticEaseOut())
        );
    }

    /// <summary>
    /// Animates the InfoBar sliding out to the top.
    /// </summary>
    public async Task SlideOutInfoBar()
    {
        if (_infoBar == null)
        {
            return;
        }

        TranslateTransform transform = _infoBar.RenderTransform as TranslateTransform ?? new TranslateTransform(0, 0);

        // Animate both opacity and translation
        await Task.WhenAll(
            AnimateOpacity(_infoBar, 1.0, 0.0, TimeSpan.FromMilliseconds(300), new QuadraticEaseIn()),
            AnimateTranslateY(transform, transform.Y, -20, TimeSpan.FromMilliseconds(300), new QuadraticEaseIn())
        );

        _infoBar.IsVisible = false;
    }

    /// <summary>
    /// Animates the opacity of a control.
    /// </summary>
    private async Task AnimateOpacity(Control control, double from, double to, TimeSpan duration, Easing easing)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        while (stopwatch.Elapsed < duration)
        {
            double progress = Math.Min(1.0, stopwatch.Elapsed.TotalMilliseconds / duration.TotalMilliseconds);
            double easedProgress = easing.Ease(progress);
            control.Opacity = from + (to - from) * easedProgress;
            await Task.Delay(16); // ~60 FPS
        }

        control.Opacity = to;
    }

    /// <summary>
    /// Animates the Y property of a TranslateTransform.
    /// </summary>
    private async Task AnimateTranslateY(TranslateTransform transform, double from, double to, TimeSpan duration, Easing easing)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        while (stopwatch.Elapsed < duration)
        {
            double progress = Math.Min(1.0, stopwatch.Elapsed.TotalMilliseconds / duration.TotalMilliseconds);
            double easedProgress = easing.Ease(progress);
            transform.Y = from + (to - from) * easedProgress;
            await Task.Delay(16); // ~60 FPS
        }

        transform.Y = to;
    }
}