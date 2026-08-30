using CommunityToolkit.Mvvm.ComponentModel;
using FluentIcons.Common;
using XeniaManager.BigScreen.Factories;
using XeniaManager.BigScreen.Utilities;
using XeniaManager.Core.Models;
using XeniaManager.Core.Utilities;

namespace XeniaManager.BigScreen.ViewModels.Items;

/// <summary>
/// A connected gamepad shown in the settings Controllers section.
/// </summary>
public partial class GamepadItemViewModel : ObservableObject, ISelectable
{
    /// <summary>
    /// The live gamepad status snapshot this item wraps.
    /// </summary>
    public GamepadInfo Source { get; }

    /// <summary>
    /// Human-readable gamepad name.
    /// </summary>
    public string Name
    {
        get
        {
            return Source.Name;
        }
    }

    /// <summary>
    /// Battery display text (percentage or "Unknown").
    /// </summary>
    public string BatteryText
    {
        get
        {
            return Source.BatteryPercent < 0
                ? LocalizationHelper.GetText("Settings.Controllers.BatteryUnknown")
                : $"{Source.BatteryPercent}%";
        }
    }

    /// <summary>
    /// Battery icon for the current level and charging state.
    /// </summary>
    public Symbol BatteryIcon
    {
        get
        {
            return IconFactory.GetBatteryIcon(Source.BatteryPercent, Source.IsCharging);
        }
    }

    /// <summary>
    /// "Status:" label for the row (stays white for both states).
    /// </summary>
    public static string StatusLabel
    {
        get
        {
            return LocalizationHelper.GetText("Settings.Controllers.StatusLabel");
        }
    }

    /// <summary>
    /// Status value: "Primary" (accent) or "Secondary" (faded).
    /// </summary>
    public string StatusValue
    {
        get
        {
            return LocalizationHelper.GetText(
                IsPrimary ? "Settings.Controllers.Primary" : "Settings.Controllers.Secondary");
        }
    }

    /// <summary>
    /// Whether the battery is currently charging.
    /// </summary>
    public bool IsCharging
    {
        get
        {
            return Source.IsCharging;
        }
    }

    /// <summary>
    /// Whether this gamepad drives navigation input.
    /// </summary>
    public bool IsPrimary
    {
        get
        {
            return Source.IsPrimary;
        }
    }

    /// <summary>
    /// Device GUID (hex), used for persistence.
    /// </summary>
    public string Guid
    {
        get
        {
            return Source.Guid;
        }
    }

    /// <summary>
    /// Whether the row is selected (controller focus).
    /// </summary>
    [ObservableProperty]
    public partial bool IsSelected { get; set; }

    public GamepadItemViewModel(GamepadInfo source)
    {
        Source = source;
    }
}