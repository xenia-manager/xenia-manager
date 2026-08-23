using System;
using FluentIcons.Common;
using XeniaManager.BigScreen.Models;
using XeniaManager.Core.Models;

namespace XeniaManager.BigScreen.Factories;

/// <summary>
/// Maps live status values (battery, network) to Fluent icons.
/// Pure value-to-icon mapping - status detection lives in the callers.
/// </summary>
public static class IconFactory
{
    /// <summary>
    /// Battery percentage covered by one icon tier.
    /// </summary>
    private const int BatteryTierPercent = 10;

    /// <summary>
    /// Discharge icons, one tier per 10% (index 0-10).
    /// </summary>
    private static readonly Symbol[] BatteryTierIcons =
    [
        Symbol.Battery0,
        Symbol.Battery1,
        Symbol.Battery2,
        Symbol.Battery3,
        Symbol.Battery4,
        Symbol.Battery5,
        Symbol.Battery6,
        Symbol.Battery7,
        Symbol.Battery8,
        Symbol.Battery9,
        Symbol.Battery10
    ];

    /// <summary>
    /// Charging icons, one tier per 10% (index 0-10).
    /// </summary>
    private static readonly Symbol[] BatteryChargingTierIcons =
    [
        Symbol.BatteryCharge0,
        Symbol.BatteryCharge1,
        Symbol.BatteryCharge2,
        Symbol.BatteryCharge3,
        Symbol.BatteryCharge4,
        Symbol.BatteryCharge5,
        Symbol.BatteryCharge6,
        Symbol.BatteryCharge7,
        Symbol.BatteryCharge8,
        Symbol.BatteryCharge9,
        Symbol.BatteryCharge10
    ];

    /// <summary>
    /// Returns the battery icon for the given percentage and charging state.
    /// Unknown/no-battery (-1) shows the warning icon.
    /// </summary>
    /// <param name="batteryPercent">Battery percentage (0-100), or -1 when unknown.</param>
    /// <param name="isCharging">Whether the battery is currently charging.</param>
    public static Symbol GetBatteryIcon(int batteryPercent, bool isCharging)
    {
        if (batteryPercent < 0)
        {
            return Symbol.BatteryWarning;
        }

        int tier = Math.Clamp(batteryPercent / BatteryTierPercent, 0, BatteryTierIcons.Length - 1);
        return isCharging ? BatteryChargingTierIcons[tier] : BatteryTierIcons[tier];
    }

    /// <summary>
    /// Returns the network icon for the given status.
    /// </summary>
    public static Symbol GetNetworkIcon(NetworkStatus status)
    {
        return status switch
        {
            NetworkStatus.Wifi => Symbol.WiFi,
            NetworkStatus.Ethernet => Symbol.PlugConnected,
            _ => Symbol.WiFiOff
        };
    }

    /// <summary>
    /// Returns the icon representing the given emulator version.
    /// </summary>
    public static Symbol GetVersionIcon(XeniaVersion version) => version switch
    {
        XeniaVersion.Canary => Symbol.XboxController,
        XeniaVersion.Mousehook => Symbol.Keyboard,
        XeniaVersion.Netplay => Symbol.Globe,
        XeniaVersion.Custom => Symbol.AppFolder,
        _ => Symbol.XboxController
    };
}