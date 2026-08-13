using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using XeniaManager.BigScreen.Constants;
using XeniaManager.BigScreen.Models;
using XeniaManager.BigScreen.Services;
using XeniaManager.BigScreen.Utilities;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Utilities;

namespace XeniaManager.BigScreen.ViewModels;

/// <summary>
/// Settings screen state: dashboard appearance options and quit behaviour,
/// persisted through the background service.
/// </summary>
public partial class SettingsViewModel : ViewModelBase
{
    private readonly IBackgroundService _backgroundService;

    /// <summary>
    /// Raised after a persisted appearance option changed, so the dashboard can
    /// rebuild its background.
    /// </summary>
    public event Action? AppearanceChanged;

    /// <summary>
    /// Raised after the library view mode changed, so the library can switch layouts live.
    /// </summary>
    public event Action? LibraryViewModeChanged;

    /// <summary>
    /// Raised after the dashboard card image mode changed, so the cards can swap images live.
    /// </summary>
    public event Action? CardImageChanged;

    /// <summary>
    /// Options shown in the settings background-type dropdown.
    /// </summary>
    public ObservableCollection<BackgroundModeOption> BackgroundModeOptions { get; } =
    [
        new(BackgroundMode.Image, LocalizationHelper.GetText("Settings.BackgroundMode.Image")),
        new(BackgroundMode.Solid, LocalizationHelper.GetText("Settings.BackgroundMode.Solid")),
        new(BackgroundMode.LinearGradient, LocalizationHelper.GetText("Settings.BackgroundMode.LinearGradient")),
        new(BackgroundMode.RadialGradient, LocalizationHelper.GetText("Settings.BackgroundMode.RadialGradient")),
        new(BackgroundMode.Dynamic, LocalizationHelper.GetText("Settings.BackgroundMode.Dynamic")),
    ];

    /// <summary>
    /// Options shown in the settings library-view dropdown.
    /// </summary>
    public ObservableCollection<LibraryViewModeOption> LibraryViewModeOptions { get; } =
    [
        new(LibraryViewMode.Carousel, LocalizationHelper.GetText("Settings.LibraryView.Carousel")),
        new(LibraryViewMode.List, LocalizationHelper.GetText("Settings.LibraryView.List")),
    ];

    /// <summary>
    /// Options shown in the settings card-image dropdown.
    /// </summary>
    public ObservableCollection<CardImageModeOption> CardImageModeOptions { get; } =
    [
        new(CardImageMode.BoxArt, LocalizationHelper.GetText("Settings.CardImage.BoxArt")),
        new(CardImageMode.Icon, LocalizationHelper.GetText("Settings.CardImage.Icon")),
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
    /// The selected option in the library-view dropdown.
    /// </summary>
    [ObservableProperty] private LibraryViewModeOption? _selectedLibraryViewMode;

    /// <summary>
    /// The active library view mode.
    /// </summary>
    [ObservableProperty] private LibraryViewMode _libraryViewMode = LibraryViewMode.Carousel;

    /// <summary>
    /// The selected option in the card-image dropdown.
    /// </summary>
    [ObservableProperty] private CardImageModeOption? _selectedCardImageMode;

    /// <summary>
    /// The active dashboard card image mode.
    /// </summary>
    [ObservableProperty] private CardImageMode _cardImageMode = CardImageMode.Icon;

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

    public SettingsViewModel(IBackgroundService backgroundService)
    {
        _backgroundService = backgroundService;
    }

    /// <summary>
    /// Loads the persisted settings and applies them to the bound properties.
    /// Called during the boot pipeline (Loading Settings stage) - the constructor
    /// stays cheap so the splash can appear immediately.
    /// </summary>
    public void Load()
    {
        _backgroundService.Load();
        Mode = _backgroundService.Settings.Mode;
        SelectedBackgroundMode = BackgroundModeOptions.FirstOrDefault(o => o.Mode == Mode);
        PrimaryColor = _backgroundService.Settings.PrimaryColor;
        AccentColor = _backgroundService.Settings.AccentColor;
        VignetteOpacity = _backgroundService.Settings.VignetteOpacity;
        ReturnToXeniaOnQuit = _backgroundService.Settings.ReturnToXeniaOnQuit;
        LibraryViewMode = _backgroundService.Settings.LibraryViewMode;
        SelectedLibraryViewMode = LibraryViewModeOptions.FirstOrDefault(o => o.Mode == LibraryViewMode);
        CardImageMode = _backgroundService.Settings.CardImageMode;
        SelectedCardImageMode = CardImageModeOptions.FirstOrDefault(o => o.Mode == CardImageMode);
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
        BackgroundMode.Image => LocalizationHelper.GetText("Settings.BackgroundMode.Image"),
        BackgroundMode.Solid => LocalizationHelper.GetText("Settings.BackgroundMode.Solid"),
        BackgroundMode.LinearGradient => LocalizationHelper.GetText("Settings.BackgroundMode.LinearGradient"),
        BackgroundMode.RadialGradient => LocalizationHelper.GetText("Settings.BackgroundMode.RadialGradient"),
        BackgroundMode.Dynamic => LocalizationHelper.GetText("Settings.BackgroundMode.Dynamic"),
        _ => LocalizationHelper.GetText("Settings.BackgroundMode.LinearGradient"),
    };

    /// <summary>
    /// Display text for the vignette opacity as a percentage.
    /// </summary>
    public string VignetteText => $"{Math.Round(VignetteOpacity * 100)}%";

    /// <summary>
    /// Display text for the currently configured background image.
    /// </summary>
    public string ImageDisplayText => string.IsNullOrEmpty(_backgroundService.Settings.ImagePath)
        ? LocalizationHelper.GetText("Settings.NoImage")
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
    public void AdjustVignette(int delta) =>
        VignetteOpacity = Math.Clamp(VignetteOpacity + delta * LayoutConstants.VignetteStep, 0, 1);

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
        Logger.Info<SettingsViewModel>($"Background image set to '{path}'");
    }

    partial void OnModeChanged(BackgroundMode value)
    {
        _backgroundService.Settings.Mode = value;
        _backgroundService.Save();
        SelectedBackgroundMode = BackgroundModeOptions.FirstOrDefault(o => o.Mode == value);
        AppearanceChanged?.Invoke();
        Logger.Info<SettingsViewModel>($"Background mode changed to {value}");
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
        Logger.Info<SettingsViewModel>($"Primary color changed to {value}");
    }

    partial void OnAccentColorChanged(Color value)
    {
        _backgroundService.Settings.AccentColor = value;
        _backgroundService.Save();
        Logger.Info<SettingsViewModel>($"Accent color changed to {value}");
    }

    partial void OnVignetteOpacityChanged(double value)
    {
        _backgroundService.Settings.VignetteOpacity = value;
        _backgroundService.Save();
        OnPropertyChanged(nameof(VignetteText));
        Logger.Debug<SettingsViewModel>($"Vignette opacity changed to {value:0.00}");
    }

    partial void OnReturnToXeniaOnQuitChanged(bool value)
    {
        _backgroundService.Settings.ReturnToXeniaOnQuit = value;
        _backgroundService.Save();
        Logger.Info<SettingsViewModel>($"Return to Xenia Manager on quit: {value}");
    }

    partial void OnLibraryViewModeChanged(LibraryViewMode value)
    {
        _backgroundService.Settings.LibraryViewMode = value;
        _backgroundService.Save();
        SelectedLibraryViewMode = LibraryViewModeOptions.FirstOrDefault(o => o.Mode == value);
        LibraryViewModeChanged?.Invoke();
        Logger.Info<SettingsViewModel>($"Library view mode changed to {value}");
    }

    partial void OnSelectedLibraryViewModeChanged(LibraryViewModeOption? value)
    {
        if (value != null)
        {
            LibraryViewMode = value.Mode;
        }
    }

    partial void OnCardImageModeChanged(CardImageMode value)
    {
        _backgroundService.Settings.CardImageMode = value;
        _backgroundService.Save();
        SelectedCardImageMode = CardImageModeOptions.FirstOrDefault(o => o.Mode == value);
        CardImageChanged?.Invoke();
        Logger.Info<SettingsViewModel>($"Card image mode changed to {value}");
    }

    partial void OnSelectedCardImageModeChanged(CardImageModeOption? value)
    {
        if (value != null)
        {
            CardImageMode = value.Mode;
        }
    }
}