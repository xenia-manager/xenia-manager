using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Interactivity;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Windowing;
using Microsoft.Extensions.DependencyInjection;
using XeniaManager.Core.Installation;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Models;
using XeniaManager.Core.Services;
using XeniaManager.Core.Settings.Sections;
using XeniaManager.Services;
using XeniaManager.Core.Utilities;
using XeniaManager.ViewModels;

namespace XeniaManager.Views;

public partial class MainWindow : AppWindow
{
    // Properties
    private MainWindowViewModel _viewModel { get; set; }
    private IReleaseService _releaseService { get; set; }
    private INotificationService _notificationService { get; set; }

    // Constructor
    public MainWindow()
    {
        InitializeComponent();
        _viewModel = App.Services.GetRequiredService<MainWindowViewModel>();
        _releaseService = App.Services.GetRequiredService<IReleaseService>();
        _notificationService = App.Services.GetRequiredService<INotificationService>();
        DataContext = _viewModel;

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
            { XeniaVersion.Canary, _viewModel.Settings.Settings.Emulator.Canary },
            { XeniaVersion.Mousehook, _viewModel.Settings.Settings.Emulator.Mousehook },
            { XeniaVersion.Netplay, _viewModel.Settings.Settings.Emulator.Netplay }
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
            _notificationService.Show(message, InfoBarSeverity.Informational);
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
}