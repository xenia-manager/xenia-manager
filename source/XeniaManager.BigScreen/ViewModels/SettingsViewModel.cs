using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using XeniaManager.BigScreen.Models;
using XeniaManager.BigScreen.Services;
using XeniaManager.BigScreen.Utilities;

namespace XeniaManager.BigScreen.ViewModels;

/// <summary>
/// Settings screen state: dashboard appearance options and quit behaviour,
/// persisted through the background service.
/// </summary>
public partial class SettingsViewModel : ViewModelBase
{
    private readonly BackgroundService _backgroundService;

    /// <summary>
    /// Raised after a persisted appearance option changed, so the dashboard can
    /// rebuild its background.
    /// </summary>
    public event Action? AppearanceChanged;

    /// <summary>
    /// Options shown in the settings background-type dropdown.
    /// </summary>
    public ObservableCollection<BackgroundModeOption> BackgroundModeOptions { get; } =
    [
        new(BackgroundMode.Image, "Image"),
        new(BackgroundMode.Solid, "Solid Colour"),
        new(BackgroundMode.LinearGradient, "Linear Gradient"),
        new(BackgroundMode.RadialGradient, "Radial Gradient"),
        new(BackgroundMode.Dynamic, "Dynamic (Selected Game)"),
    ];

    /// <summary>
    /// The selected option in the background-type dropdown.
    /// </summary>
    [ObservableProperty] private BackgroundModeOption? _selectedBackgroundMode;

    /// <summary>
    /// The active background mode.
    /// </summary>
    [ObservableProperty] private BackgroundMode _mode = BackgroundMode.LinearGradient;

    /// <summary>
    /// The primary color; gradients are derived from it.
    /// </summary>
    [ObservableProperty] private Color _primaryColor;

    /// <summary>
    /// The dashboard's accent color (selected card border).
    /// </summary>
    [ObservableProperty] private Color _accentColor;

    /// <summary>
    /// Vignette edge opacity (0-1).
    /// </summary>
    [ObservableProperty] private double _vignetteOpacity;

    /// <summary>
    /// Whether Quit returns to Xenia Manager (launching it if it isn't running).
    /// Off = just close BigScreen.
    /// </summary>
    [ObservableProperty] private bool _returnToXeniaOnQuit = true;

    public SettingsViewModel(BackgroundService backgroundService)
    {
        _backgroundService = backgroundService;
        _backgroundService.Load();
        Mode = _backgroundService.Settings.Mode;
        SelectedBackgroundMode = BackgroundModeOptions.FirstOrDefault(o => o.Mode == Mode);
        PrimaryColor = _backgroundService.Settings.PrimaryColor;
        AccentColor = _backgroundService.Settings.AccentColor;
        VignetteOpacity = _backgroundService.Settings.VignetteOpacity;
        ReturnToXeniaOnQuit = _backgroundService.Settings.ReturnToXeniaOnQuit;
    }

    /// <summary>
    /// Brush used as the overlay/menu background, derived from the primary colour
    /// so menus match the dashboard instead of being pitch black.
    /// </summary>
    public IBrush ScreenBackground => new SolidColorBrush(PrimaryColor);

    /// <summary>
    /// Display name of the current background mode.
    /// </summary>
    public string ModeText => Mode switch
    {
        BackgroundMode.Image => "Image",
        BackgroundMode.Solid => "Solid Colour",
        BackgroundMode.LinearGradient => "Linear Gradient",
        BackgroundMode.RadialGradient => "Radial Gradient",
        BackgroundMode.Dynamic => "Dynamic (Selected Game)",
        _ => "Linear Gradient",
    };

    /// <summary>
    /// Display text for the vignette opacity as a percentage.
    /// </summary>
    public string VignetteText => $"{Math.Round(VignetteOpacity * 100)}%";

    /// <summary>
    /// Display text for the currently configured background image.
    /// </summary>
    public string ImageDisplayText => string.IsNullOrEmpty(_backgroundService.Settings.ImagePath)
        ? "None"
        : Path.GetFileName(_backgroundService.Settings.ImagePath);

    /// <summary>
    /// Cycles the background mode by the given step.
    /// </summary>
    public void CycleMode(int delta) => Mode = EnumCycleHelper.Next(Mode, delta);

    /// <summary>
    /// Cycles the primary color through the given palette by the given step.
    /// </summary>
    public void CyclePrimaryColor(int delta, Color[] palette) =>
        PrimaryColor = EnumCycleHelper.NextColor(palette, PrimaryColor, delta, 0);

    /// <summary>
    /// Cycles the accent color through the given palette by the given step.
    /// </summary>
    public void CycleAccentColor(int delta, Color[] palette) =>
        AccentColor = EnumCycleHelper.NextColor(palette, AccentColor, delta, 1);

    /// <summary>
    /// Steps the vignette opacity by the given direction (0.05 per step), clamped to 0-1.
    /// </summary>
    public void AdjustVignette(int delta) => VignetteOpacity = Math.Clamp(VignetteOpacity + delta * 0.05, 0, 1);

    /// <summary>
    /// Sets a custom image path and switches to image background mode.
    /// </summary>
    public void SetBackgroundImage(string path)
    {
        _backgroundService.Settings.ImagePath = path;
        _backgroundService.Settings.Mode = BackgroundMode.Image;
        _backgroundService.Save();
        Mode = BackgroundMode.Image;
        OnPropertyChanged(nameof(ImageDisplayText));
        AppearanceChanged?.Invoke();
    }

    partial void OnModeChanged(BackgroundMode value)
    {
        _backgroundService.Settings.Mode = value;
        _backgroundService.Save();
        SelectedBackgroundMode = BackgroundModeOptions.FirstOrDefault(o => o.Mode == value);
        AppearanceChanged?.Invoke();
    }

    partial void OnSelectedBackgroundModeChanged(BackgroundModeOption? value)
    {
        if (value != null)
        {
            Mode = value.Mode;
        }
    }

    partial void OnPrimaryColorChanged(Color value)
    {
        _backgroundService.Settings.PrimaryColor = value;
        _backgroundService.Save();
        OnPropertyChanged(nameof(ScreenBackground));
        AppearanceChanged?.Invoke();
    }

    partial void OnAccentColorChanged(Color value)
    {
        _backgroundService.Settings.AccentColor = value;
        _backgroundService.Save();
    }

    partial void OnVignetteOpacityChanged(double value)
    {
        _backgroundService.Settings.VignetteOpacity = value;
        _backgroundService.Save();
        OnPropertyChanged(nameof(VignetteText));
    }

    partial void OnReturnToXeniaOnQuitChanged(bool value)
    {
        _backgroundService.Settings.ReturnToXeniaOnQuit = value;
        _backgroundService.Save();
    }
}
