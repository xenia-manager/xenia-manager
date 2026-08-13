using System;
using System.Linq;
using System.Net.NetworkInformation;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using XeniaManager.BigScreen.Constants;
using XeniaManager.BigScreen.Services;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Utilities;

namespace XeniaManager.BigScreen.ViewModels;

/// <summary>
/// Header state: profile identity, live clock, wifi and controller battery status.
/// </summary>
public partial class HeaderViewModel : ViewModelBase
{
    /// <summary>
    /// Gamertag of the active profile (Canary)
    /// </summary>
    [ObservableProperty] private string _gamertag = LocalizationHelper.GetText("Header.Guest");

    /// <summary>
    /// Total gamerscore of the active profile
    /// </summary>
    [ObservableProperty] private string _gamerscore = "0";

    /// <summary>
    /// Whether a controller is connected
    /// </summary>
    [ObservableProperty] private bool _controllerConnected = true;

    /// <summary>
    /// Controller battery level in percent. -1 when unknown/no controller.
    /// </summary>
    [ObservableProperty] private int _batteryLevel = -1;

    /// <summary>
    /// Whether the controller battery is charging
    /// </summary>
    [ObservableProperty] private bool _isCharging;

    /// <summary>
    /// Whether wifi is connected
    /// </summary>
    [ObservableProperty] private bool _isWifiConnected = true;

    /// <summary>
    /// Current time string
    /// </summary>
    [ObservableProperty] private string _time = DateTime.Now.ToString(FormatConstants.ClockFormat);

    /// <summary>
    /// Fluent icon name for the current wifi state
    /// </summary>
    public string WifiIcon => IsWifiConnected ? "WiFi" : "WiFiOff";

    /// <summary>
    /// Fluent icon name for the current controller battery state.
    /// BatteryWarning when no controller is connected or the level is unknown;
    /// full battery when wired/charging.
    /// </summary>
    public string BatteryIcon => BatteryLevel < 0
        ? "BatteryWarning"
        : IsCharging
            ? "Battery10"
            : BatteryLevel switch
            {
                <= 0 => "Battery0",
                <= 20 => "Battery1",
                <= 40 => "Battery3",
                <= 60 => "Battery5",
                <= 80 => "Battery7",
                _ => "Battery10",
            };

    partial void OnIsWifiConnectedChanged(bool value)
    {
        OnPropertyChanged(nameof(WifiIcon));
        Logger.Debug<HeaderViewModel>($"Wifi connected: {value}");
    }

    partial void OnBatteryLevelChanged(int value) => OnPropertyChanged(nameof(BatteryIcon));

    partial void OnIsChargingChanged(bool value) => OnPropertyChanged(nameof(BatteryIcon));

    partial void OnControllerConnectedChanged(bool value) => OnPropertyChanged(nameof(BatteryIcon));

    private readonly DispatcherTimer _clockTimer;
    private readonly DispatcherTimer _wifiTimer;

    public HeaderViewModel()
    {
        _clockTimer = new DispatcherTimer
        {
            Interval = TimingConstants.ClockUpdateInterval,
        };
        _clockTimer.Tick += (_, _) => Time = DateTime.Now.ToString(FormatConstants.ClockFormat);
        _clockTimer.Start();

        _wifiTimer = new DispatcherTimer
        {
            Interval = TimingConstants.WifiPollInterval,
        };
        _wifiTimer.Tick += (_, _) => CheckWifi();
        _wifiTimer.Start();
        CheckWifi();
    }

    /// <summary>
    /// Applies the loaded profile's identity. Called once the profile has been
    /// loaded during the boot pipeline (the constructor stays cheap so the
    /// splash screen can appear immediately).
    /// </summary>
    public void ApplyProfile(IProfileService profileService)
    {
        Gamertag = profileService.Gamertag;
        Gamerscore = profileService.Gamerscore;
        Logger.Debug<HeaderViewModel>($"Profile loaded: {Gamertag} ({Gamerscore}G)");
    }

    /// <summary>
    /// Applies the live gamepad connection/battery state from the gamepad service.
    /// </summary>
    public void ApplyGamepadState(bool connected, int batteryPercent, bool charging)
    {
        ControllerConnected = connected;
        BatteryLevel = batteryPercent;
        IsCharging = charging;
        Logger.Debug<HeaderViewModel>(
            $"Gamepad state: connected={connected}, battery={batteryPercent}%, charging={charging}");
    }

    /// <summary>
    /// Re-checks the Wi-Fi connection state from the network interfaces.
    /// Keeps the last state on failure so the icon doesn't flicker.
    /// </summary>
    private void CheckWifi()
    {
        try
        {
            IsWifiConnected = NetworkInterface.GetAllNetworkInterfaces()
                .Any(i => i is
                {
                    NetworkInterfaceType: NetworkInterfaceType.Wireless80211, OperationalStatus: OperationalStatus.Up
                });
        }
        catch (Exception ex)
        {
            Logger.Error<HeaderViewModel>("Failed to query wifi connection state");
            Logger.LogExceptionDetails<HeaderViewModel>(ex);
        }
    }
}