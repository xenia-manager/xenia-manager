using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Settings;
using XeniaManager.Core.Utilities;

namespace XeniaManager.ViewModels.Pages;

public partial class ManagePageViewModel : ViewModelBase
{
    // Variables
    private Settings _settings { get; set; }

    // Xenia Canary
    [ObservableProperty] private bool canaryInstalled;
    [ObservableProperty] private string canaryVersion = string.Empty;
    [ObservableProperty] private bool canaryInstall;
    [ObservableProperty] private bool canaryUninstall;
    [ObservableProperty] private bool canaryUpdate;

    // Constructor
    public ManagePageViewModel()
    {
        _settings = App.Services.GetRequiredService<Settings>();
        UpdateEmulatorStatus();
    }

    // Functions
    public void UpdateEmulatorStatus()
    {
        // Xenia Canary
        CanaryInstalled = _settings.Settings.Emulator.Canary != null;
        if (_settings.Settings.Emulator.Canary != null)
        {
            CanaryVersion = CanaryInstalled ? _settings.Settings.Emulator.Canary.Version : LocalizationHelper.GetText("ManagePage.Emulator.Manage.Xenia.NotInstalled");
            Logger.Info<ManagePageViewModel>($"Xenia Canary is installed ({_settings.Settings.Emulator.Canary.Version})");
        }
        else
        {
            CanaryVersion = LocalizationHelper.GetText("ManagePage.Emulator.Manage.Xenia.NotInstalled");
            Logger.Info<ManagePageViewModel>("Xenia Canary is not installed");
        }
        CanaryInstall = !CanaryInstalled;
        CanaryUpdate = _settings.Settings.Emulator.Canary is { UpdateAvailable: true };
        CanaryUninstall = CanaryInstalled;
    }
}