using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentIcons.Common;
using XeniaManager.BigScreen.Constants;
using XeniaManager.BigScreen.Factories;
using XeniaManager.BigScreen.Models;
using XeniaManager.BigScreen.Models.Settings;
using XeniaManager.BigScreen.Services;
using XeniaManager.Logging;
using XeniaManager.Core.Models;
using XeniaManager.Core.Utilities;

namespace XeniaManager.BigScreen.ViewModels.Dashboard;

/// <summary>
/// Header state: profile identity, live clock, wifi and controller battery status.
/// The profile identity shows the active version's profile, and - when the
/// rotate-profiles setting is on and several versions have an active profile -
/// automatically cycles through every version (display only; the active version
/// and pickers are untouched).
/// </summary>
public partial class HeaderViewModel : ViewModelBase
{
    private readonly IBackgroundService _backgroundService;
    private readonly DispatcherTimer _clockTimer;
    private readonly DispatcherTimer _networkTimer;
    private readonly DispatcherTimer _profileTimer;
    private IProfileService? _profileService;
    private XeniaVersion? _displayedVersion;

    /// <summary>
    /// Whether the profile row (avatar chip) is currently selected.
    /// </summary>
    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    /// <summary>
    /// Gamertag of the displayed profile (the active version's, or the version
    /// currently shown by the rotation).
    /// </summary>
    [ObservableProperty]
    public partial string Gamertag { get; set; } = LocalizationHelper.GetText("Header.Guest");

    /// <summary>
    /// Total gamerscore of the displayed profile
    /// </summary>
    [ObservableProperty]
    public partial string Gamerscore { get; set; } = "0";

    /// <summary>
    /// Icon of the displayed emulator version, shown in the profile chip.
    /// </summary>
    [ObservableProperty]
    public partial Symbol VersionIcon { get; set; } = Symbol.XboxController;

    /// <summary>
    /// Whether the version icon is shown (hidden when no emulator version is
    /// installed or none has profiles).
    /// </summary>
    [ObservableProperty]
    public partial bool HasVersionIcon { get; set; }

    /// <summary>
    /// Whether a controller is connected
    /// </summary>
    [ObservableProperty]
    public partial bool ControllerConnected { get; set; } = true;

    /// <summary>
    /// Controller battery level in percent. -1 when unknown/no controller.
    /// </summary>
    [ObservableProperty]
    public partial int BatteryLevel { get; set; } = -1;

    /// <summary>
    /// Whether the controller battery is charging
    /// </summary>
    [ObservableProperty]
    public partial bool IsCharging { get; set; }

    /// <summary>
    /// Current network connection status (drives the header network icon).
    /// </summary>
    [ObservableProperty]
    public partial NetworkStatus NetworkStatus { get; set; } = NetworkStatus.Wifi;

    /// <summary>
    /// The hour format used by the clock, following the persisted setting.
    /// </summary>
    [ObservableProperty]
    public partial TimeFormat TimeFormat { get; set; } = TimeFormat.TwelveHour;

    /// <summary>
    /// Minimum clock width so the status area never jiggles as digits change;
    /// follows the time format (24-hour is narrower).
    /// </summary>
    [ObservableProperty]
    public partial double ClockMinWidth { get; set; } = LayoutConstants.ClockMinWidth12H;

    /// <summary>
    /// Current time string
    /// </summary>
    [ObservableProperty]
    public partial string Time { get; set; } = DateTime.Now.ToString(FormatConstants.ClockFormat12H);

    /// <summary>
    /// Fluent icon for the current network state (wifi / wired / off).
    /// </summary>
    public Symbol NetworkIcon
    {
        get
        {
            return IconFactory.GetNetworkIcon(NetworkStatus);
        }
    }

    /// <summary>
    /// Fluent icon for the current controller battery state.
    /// BatteryWarning when no controller is connected or the level is unknown;
    /// a tiered battery icon otherwise (charging-aware).
    /// </summary>
    public Symbol BatteryIcon
    {
        get
        {
            return IconFactory.GetBatteryIcon(BatteryLevel, IsCharging);
        }
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

    /// <summary>
    /// Shows the identity of the given version (or the guest defaults when no
    /// version is given).
    /// </summary>
    private void ShowVersion(XeniaVersion? version)
    {
        _displayedVersion = version;
        if (version == null || _profileService == null)
        {
            Gamertag = LocalizationHelper.GetText("Header.Guest");
            Gamerscore = "0";
            HasVersionIcon = false;
            return;
        }

        VersionProfileState state = _profileService.StateFor(version.Value);
        Gamertag = state.Gamertag;
        Gamerscore = state.Gamerscore;
        VersionIcon = IconFactory.GetVersionIcon(version.Value);
        HasVersionIcon = true;
    }

    /// <summary>
    /// Resets the displayed identity to the active version's profile.
    /// </summary>
    private void SyncToActive() => ShowVersion(_profileService?.ActiveVersion);

    /// <summary>
    /// Advances the displayed identity to the next version with an active
    /// profile. Falls back to the active version when the setting is off, fewer
    /// than two versions have profiles, or the displayed version is gone.
    /// </summary>
    private void RotateProfile()
    {
        if (_profileService is not { } profileService)
        {
            return;
        }

        IReadOnlyList<XeniaVersion> versions = profileService.VersionsWithProfiles;
        if (!_backgroundService.Settings.RotateProfiles || versions.Count < 2)
        {
            SyncToActive();
            return;
        }

        int index = _displayedVersion is { } displayed ? versions.ToList().IndexOf(displayed) : -1;
        if (index < 0)
        {
            SyncToActive();
            return;
        }

        ShowVersion(versions[(index + 1) % versions.Count]);
    }

    /// <summary>
    /// Applies the loaded profile's identity. Called once the profile has been
    /// loaded during the boot pipeline (the constructor stays cheap so the
    /// splash screen can appear immediately) and after every profile change.
    /// </summary>
    public void ApplyProfile(IProfileService profileService)
    {
        _profileService = profileService;
        SyncToActive();
        Logger.Debug<HeaderViewModel>($"Profile loaded: {Gamertag} ({Gamerscore}G)");
    }

    /// <summary>
    /// Applies the persisted time format to the clock.
    /// </summary>
    public void ApplyTimeFormat(TimeFormat timeFormat)
    {
        TimeFormat = timeFormat;
        Logger.Debug<HeaderViewModel>($"Time format applied: {timeFormat}");
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

    partial void OnNetworkStatusChanged(NetworkStatus value)
    {
        OnPropertyChanged(nameof(NetworkIcon));
        Logger.Debug<HeaderViewModel>($"Network status: {value}");
    }

    partial void OnBatteryLevelChanged(int value) => OnPropertyChanged(nameof(BatteryIcon));

    partial void OnIsChargingChanged(bool value) => OnPropertyChanged(nameof(BatteryIcon));

    partial void OnControllerConnectedChanged(bool value) => OnPropertyChanged(nameof(BatteryIcon));

    partial void OnTimeFormatChanged(TimeFormat value)
    {
        Time = DateTime.Now.ToString(FormatConstants.GetClockFormat(value));
        ClockMinWidth = value == TimeFormat.TwentyFourHour
            ? LayoutConstants.ClockMinWidth24H
            : LayoutConstants.ClockMinWidth12H;
        Logger.Debug<HeaderViewModel>($"Clock format: {value}");
    }

    public HeaderViewModel(IBackgroundService backgroundService)
    {
        _backgroundService = backgroundService;

        _clockTimer = new DispatcherTimer
        {
            Interval = TimingConstants.ClockUpdateInterval
        };
        _clockTimer.Tick += (_, _) => Time = DateTime.Now.ToString(FormatConstants.GetClockFormat(TimeFormat));
        _clockTimer.Start();

        _networkTimer = new DispatcherTimer
        {
            Interval = TimingConstants.WifiPollInterval
        };
        _networkTimer.Tick += (_, _) => CheckNetwork();
        _networkTimer.Start();
        CheckNetwork();

        _profileTimer = new DispatcherTimer
        {
            Interval = TimingConstants.ProfileCycleInterval
        };
        _profileTimer.Tick += (_, _) => RotateProfile();
        _profileTimer.Start();
    }
}