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
    public string Name => Source.Name;

    /// <summary>
    /// Battery display text (percentage or "Unknown").
    /// </summary>
    public string BatteryText => Source.BatteryPercent < 0
        ? LocalizationHelper.GetText("Settings.Controllers.BatteryUnknown")
        : $"{Source.BatteryPercent}%";

    /// <summary>
    /// Battery icon for the current level and charging state.
    /// </summary>
    public Symbol BatteryIcon => IconFactory.GetBatteryIcon(Source.BatteryPercent, Source.IsCharging);

    /// <summary>
    /// "Status:" label for the row (stays white for both states).
    /// </summary>
    public string StatusLabel => LocalizationHelper.GetText("Settings.Controllers.StatusLabel");

    /// <summary>
    /// Status value: "Primary" (accent) or "Secondary" (faded).
    /// </summary>
    public string StatusValue => LocalizationHelper.GetText(
        IsPrimary ? "Settings.Controllers.Primary" : "Settings.Controllers.Secondary");

    /// <summary>
    /// Whether the battery is currently charging.
    /// </summary>
    public bool IsCharging => Source.IsCharging;

    /// <summary>
    /// Whether this gamepad drives navigation input.
    /// </summary>
    public bool IsPrimary => Source.IsPrimary;

    /// <summary>
    /// Device GUID (hex), used for persistence.
    /// </summary>
    public string Guid => Source.Guid;

    /// <summary>
    /// Whether the row is selected (controller focus).
    /// </summary>
    [ObservableProperty] private bool _isSelected;

    public GamepadItemViewModel(GamepadInfo source)
    {
        Source = source;
    }
}