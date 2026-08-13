using System;
using System.Net.NetworkInformation;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentIcons.Common;
using XeniaManager.BigScreen.Constants;
using XeniaManager.BigScreen.Factories;
using XeniaManager.BigScreen.Models;
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
    /// Current network connection status (drives the header network icon).
    /// </summary>
    [ObservableProperty] private NetworkStatus _networkStatus = NetworkStatus.Wifi;

    /// <summary>
    /// Current time string
    /// </summary>
    [ObservableProperty] private string _time = DateTime.Now.ToString(FormatConstants.ClockFormat);

    /// <summary>
    /// Fluent icon for the current network state (wifi / wired / off).
    /// </summary>
    public Symbol NetworkIcon => IconFactory.GetNetworkIcon(NetworkStatus);

    /// <summary>
    /// Fluent icon for the current controller battery state.
    /// BatteryWarning when no controller is connected or the level is unknown;
    /// a tiered battery icon otherwise (charging-aware).
    /// </summary>
    public Symbol BatteryIcon => IconFactory.GetBatteryIcon(BatteryLevel, IsCharging);

    partial void OnNetworkStatusChanged(NetworkStatus value)
    {
        OnPropertyChanged(nameof(NetworkIcon));
        Logger.Debug<HeaderViewModel>($"Network status: {value}");
    }

    partial void OnBatteryLevelChanged(int value) => OnPropertyChanged(nameof(BatteryIcon));

    partial void OnIsChargingChanged(bool value) => OnPropertyChanged(nameof(BatteryIcon));

    partial void OnControllerConnectedChanged(bool value) => OnPropertyChanged(nameof(BatteryIcon));

    private readonly DispatcherTimer _clockTimer;
    private readonly DispatcherTimer _networkTimer;

    public HeaderViewModel()
    {
        _clockTimer = new DispatcherTimer
        {
            Interval = TimingConstants.ClockUpdateInterval,
        };
        _clockTimer.Tick += (_, _) => Time = DateTime.Now.ToString(FormatConstants.ClockFormat);
        _clockTimer.Start();

        _networkTimer = new DispatcherTimer
        {
            Interval = TimingConstants.WifiPollInterval,
        };
        _networkTimer.Tick += (_, _) => CheckNetwork();
        _networkTimer.Start();
        CheckNetwork();
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
    /// Re-checks the network connection state from the network interfaces:
    /// wireless-up wins, then ethernet-up, otherwise disconnected.
    /// Keeps the last state on failure so the icon doesn't flicker.
    /// </summary>
    private void CheckNetwork()
    {
        try
        {
            foreach (NetworkInterface networkInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (networkInterface.OperationalStatus != OperationalStatus.Up)
                {
                    continue;
                }

                if (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
                {
                    NetworkStatus = NetworkStatus.Wifi;
                    return;
                }

                if (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                {
                    NetworkStatus = NetworkStatus.Ethernet;
                    return;
                }
            }

            NetworkStatus = NetworkStatus.Disconnected;
        }
        catch (Exception ex)
        {
            Logger.Error<HeaderViewModel>("Failed to query network connection state");
            Logger.LogExceptionDetails<HeaderViewModel>(ex);
        }
    }
}