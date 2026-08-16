using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using XeniaManager.BigScreen.Controls.Settings;
using XeniaManager.BigScreen.Constants;
using XeniaManager.BigScreen.Factories;
using XeniaManager.BigScreen.Models;
using XeniaManager.BigScreen.Models.Settings;
using XeniaManager.BigScreen.Services;
using XeniaManager.BigScreen.Utilities;
using XeniaManager.BigScreen.ViewModels.Items;
using XeniaManager.BigScreen.ViewModels.Modals;
using XeniaManager.Core.Converters;
using XeniaManager.Core.Files;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Manage;
using XeniaManager.Core.Models;
using XeniaManager.Core.Models.Files.XConfig;
using XeniaManager.Core.Services;
using XeniaManager.Core.Utilities;

namespace XeniaManager.BigScreen.ViewModels.Screens;

/// <summary>
/// Settings screen state: dashboard appearance options and quit behaviour,
/// persisted through the background service.
/// </summary>
public partial class SettingsViewModel : ViewModelBase
{
    private readonly IBackgroundService _backgroundService;
    private readonly IProfileService _profileService;
    private readonly IGamepadInputService _gamepadService;

    /// <summary>
    /// The base Xenia Manager's settings (same file the desktop app uses), so
    /// the "Start in Big Screen" toggle shares the desktop app's state.
    /// </summary>
    private readonly Core.Settings.Settings _desktopSettings = new();

    /// <summary>
    /// The emulator versions currently installed on this machine.
    /// </summary>
    public IReadOnlyList<XeniaVersion> InstalledVersions { get; }

    /// <summary>
    /// The loaded XConfig file (resolution card), or null when none exists.
    /// </summary>
    private XConfigFile? _xconfigFile;

    /// <summary>
    /// Suppresses the resolution save while the dropdown is being re-synced
    /// after a version switch (the selection would otherwise write the new
    /// version's file with the old version's resolution).
    /// </summary>
    private bool _syncingXConfig;

    /// <summary>
    /// Every controller-navigable row in display order (fixed rows followed by
    /// the connected gamepads), used for the row selection movement.
    /// </summary>
    private readonly List<ISelectable> _rows = [];

    /// <summary>
    /// Value snapshots taken when a row editor opens, restored on cancel.
    /// </summary>
    private BackgroundMode _originalMode;

    private LibraryViewMode _originalLibraryView;
    private CardImageMode _originalCardImage;
    private TimeFormat _originalTimeFormat;
    private Color _originalPrimary;
    private Color _originalAccent;
    private XConfigResolutionOption? _originalXConfigResolution;
    private XConfigVersionOption? _originalXConfigVersion;

    /// <summary>
    /// Builds a settings dropdown option per enum member, in declaration order
    /// (the default leads). Each option's display name comes from the key
    /// "{keyPrefix}.{MemberName}".
    /// </summary>
    private static ObservableCollection<TOption> BuildOptions<TEnum, TOption>(
        string keyPrefix, Func<TEnum, string, TOption> create)
        where TEnum : struct, Enum
    {
        IEnumerable<TOption> options = Enum.GetValues<TEnum>()
            .Select(value => create(value, LocalizationHelper.GetText($"{keyPrefix}.{value}")));
        return new ObservableCollection<TOption>(options);
    }

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
    /// Raised after the time format changed, so the clock and capture dates reformat.
    /// </summary>
    public event Action? TimeFormatChanged;

    /// <summary>
    /// Raised after the controller moved to a row, so the view can scroll it into view.
    /// </summary>
    public event Action<ISelectable>? RowSelectionChanged;

    /// <summary>
    /// Raised after a row's editor opened (dropdown, palette or slider), so the
    /// view can open the matching native control.
    /// </summary>
    public event Action<SettingsRowKind>? EditorOpened;

    /// <summary>
    /// Raised after a row's editor closed (commit or cancel), so the view can
    /// close the native control.
    /// </summary>
    public event Action? EditorClosed;

    /// <summary>
    /// Raised when the controller activates the Background Image row, so the
    /// view can open the file picker.
    /// </summary>
    public event Action? SelectImageRequested;

    /// <summary>
    /// Whether no gamepads are currently connected.
    /// </summary>
    public bool HasNoControllers => Controllers.Count == 0;

    /// <summary>
    /// Whether any installed version has an XConfig file (the resolution card
    /// shows only then).
    /// </summary>
    public bool HasXConfig => XConfigVersions.Count > 0;

    /// <summary>
    /// Whether a row's editor is open and takes value input.
    /// </summary>
    public bool IsEditorOpen => ActiveEditor.HasValue;

    /// <summary>
    /// Whether the open editor cycles its value with Up/Down (the dropdown
    /// rows) instead of Left/Right (colours and the slider).
    /// </summary>
    private bool IsComboEditor =>
        ActiveEditor is SettingsRowKind.LibraryView
            or SettingsRowKind.CardImage
            or SettingsRowKind.TimeFormat
            or SettingsRowKind.BackgroundMode
            or SettingsRowKind.XConfigVersion
            or SettingsRowKind.XConfig;

    /// <summary>
    /// Connected gamepads shown in the Controllers section.
    /// </summary>
    public ObservableCollection<GamepadItemViewModel> Controllers { get; } = [];

    /// <summary>
    /// Gamertag of the active profile, shown in the Profiles section.
    /// </summary>
    public string ActiveGamertag => _profileService.Gamertag;

    /// <summary>
    /// Row for the Manage Profiles card.
    /// </summary>
    public SettingsRowViewModel RowManageProfiles { get; } = new(SettingsRowKind.ManageProfiles);

    /// <summary>
    /// Row for the library view dropdown card.
    /// </summary>
    public SettingsRowViewModel RowLibraryView { get; } = new(SettingsRowKind.LibraryView);

    /// <summary>
    /// Row for the card image dropdown card.
    /// </summary>
    public SettingsRowViewModel RowCardImage { get; } = new(SettingsRowKind.CardImage);

    /// <summary>
    /// Row for the time format dropdown card.
    /// </summary>
    public SettingsRowViewModel RowTimeFormat { get; } = new(SettingsRowKind.TimeFormat);

    /// <summary>
    /// Row for the quit behaviour toggle card.
    /// </summary>
    public SettingsRowViewModel RowQuitToggle { get; } = new(SettingsRowKind.QuitToggle);

    /// <summary>
    /// Row for the "launch games in fullscreen" toggle.
    /// </summary>
    public SettingsRowViewModel RowFullscreenToggle { get; } = new(SettingsRowKind.FullscreenToggle);

    /// <summary>
    /// Row for the "start in Big Screen" toggle (writes the desktop app's setting).
    /// </summary>
    public SettingsRowViewModel RowStartInBigScreenToggle { get; } = new(SettingsRowKind.StartInBigScreenToggle);

    /// <summary>
    /// Row for the "rotate profiles" toggle.
    /// </summary>
    public SettingsRowViewModel RowRotateProfiles { get; } = new(SettingsRowKind.RotateProfiles);

    /// <summary>
    /// Row for the background type dropdown card.
    /// </summary>
    public SettingsRowViewModel RowBackgroundMode { get; } = new(SettingsRowKind.BackgroundMode);

    /// <summary>
    /// Row for the primary colour card.
    /// </summary>
    public SettingsRowViewModel RowPrimaryColour { get; } = new(SettingsRowKind.PrimaryColour);

    /// <summary>
    /// Row for the accent colour card.
    /// </summary>
    public SettingsRowViewModel RowAccentColour { get; } = new(SettingsRowKind.AccentColour);

    /// <summary>
    /// Row for the vignette slider card.
    /// </summary>
    public SettingsRowViewModel RowVignette { get; } = new(SettingsRowKind.Vignette);

    /// <summary>
    /// Row for the background image picker card.
    /// </summary>
    public SettingsRowViewModel RowBackgroundImage { get; } = new(SettingsRowKind.BackgroundImage);

    /// <summary>
    /// Row for the XConfig resolution card (bottom of the screen).
    /// </summary>
    public SettingsRowViewModel RowXConfig { get; } = new(SettingsRowKind.XConfig);

    /// <summary>
    /// Row for the XConfig version card (bottom of the screen).
    /// </summary>
    public SettingsRowViewModel RowXConfigVersion { get; } = new(SettingsRowKind.XConfigVersion);

    /// <summary>
    /// Options shown in the settings background-type dropdown, in enum order
    /// (the default leads).
    /// </summary>
    public ObservableCollection<BackgroundModeOption> BackgroundModeOptions { get; } =
        BuildOptions<BackgroundMode, BackgroundModeOption>(
            "Settings.BackgroundMode", (mode, name) => new BackgroundModeOption(mode, name));

    /// <summary>
    /// Options shown in the settings library-view dropdown, in enum order
    /// (the default leads).
    /// </summary>
    public ObservableCollection<LibraryViewModeOption> LibraryViewModeOptions { get; } =
        BuildOptions<LibraryViewMode, LibraryViewModeOption>(
            "Settings.LibraryView", (mode, name) => new LibraryViewModeOption(mode, name));

    /// <summary>
    /// Options shown in the settings card-image dropdown, in enum order
    /// (the default leads).
    /// </summary>
    public ObservableCollection<CardImageModeOption> CardImageModeOptions { get; } =
        BuildOptions<CardImageMode, CardImageModeOption>(
            "Settings.CardImage", (mode, name) => new CardImageModeOption(mode, name));

    /// <summary>
    /// Options shown in the settings time-format dropdown, in enum order
    /// (the default leads).
    /// </summary>
    public ObservableCollection<TimeFormatOption> TimeFormatOptions { get; } =
        BuildOptions<TimeFormat, TimeFormatOption>(
            "Settings.TimeFormat", (format, name) => new TimeFormatOption(format, name));

    /// <summary>
    /// Options shown in the XConfig resolution dropdown (the "R" enum prefix
    /// is stripped for display).
    /// </summary>
    public ObservableCollection<XConfigResolutionOption> XConfigResolutions { get; } =
        new(Enum.GetValues<XConfigResolution>()
            .Select(value => new XConfigResolutionOption(value, value.ToString().TrimStart('R'))));

    /// <summary>
    /// Options shown in the XConfig version dropdown: installed versions that
    /// have an XConfig file (Custom never appears - it has no standard config).
    /// </summary>
    public ObservableCollection<XConfigVersionOption> XConfigVersions { get; } = [];

    /// <summary>
    /// Brush used as the overlay/menu background, derived from the primary colour
    /// so menus match the dashboard instead of being pitch black.
    /// </summary>
    public IBrush ScreenBackground => BackgroundBrushFactory.CreateSolid(PrimaryColor);

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
        _ => LocalizationHelper.GetText("Settings.BackgroundMode.LinearGradient")
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
    /// The selected option in the XConfig resolution dropdown.
    /// </summary>
    [ObservableProperty]
    public partial XConfigResolutionOption? SelectedXConfigResolution { get; set; }

    /// <summary>
    /// The selected option in the XConfig version dropdown.
    /// </summary>
    [ObservableProperty]
    public partial XConfigVersionOption? SelectedXConfigVersion { get; set; }

    /// <summary>
    /// The selected option in the background-type dropdown.
    /// </summary>
    [ObservableProperty]
    public partial BackgroundModeOption? SelectedBackgroundMode { get; set; }

    /// <summary>
    /// The active background mode.
    /// </summary>
    [ObservableProperty]
    public partial BackgroundMode Mode { get; set; } = BackgroundMode.Dynamic;

    /// <summary>
    /// The selected option in the library-view dropdown.
    /// </summary>
    [ObservableProperty]
    public partial LibraryViewModeOption? SelectedLibraryViewMode { get; set; }

    /// <summary>
    /// The active library view mode.
    /// </summary>
    [ObservableProperty]
    public partial LibraryViewMode LibraryViewMode { get; set; } = LibraryViewMode.Carousel;

    /// <summary>
    /// The selected option in the card-image dropdown.
    /// </summary>
    [ObservableProperty]
    public partial CardImageModeOption? SelectedCardImageMode { get; set; }

    /// <summary>
    /// The active dashboard card image mode.
    /// </summary>
    [ObservableProperty]
    public partial CardImageMode CardImageMode { get; set; } = CardImageMode.Icon;

    /// <summary>
    /// The selected option in the time-format dropdown.
    /// </summary>
    [ObservableProperty]
    public partial TimeFormatOption? SelectedTimeFormat { get; set; }

    /// <summary>
    /// The active time format for the clock and capture dates.
    /// </summary>
    [ObservableProperty]
    public partial TimeFormat TimeFormat { get; set; } = TimeFormat.TwelveHour;

    /// <summary>
    /// The primary color; gradients are derived from it.
    /// </summary>
    [ObservableProperty]
    public partial Color PrimaryColor { get; set; }

    /// <summary>
    /// The dashboard's accent color (selected card border).
    /// </summary>
    [ObservableProperty]
    public partial Color AccentColor { get; set; }

    /// <summary>
    /// Vignette edge opacity (0-1).
    /// </summary>
    [ObservableProperty]
    public partial double VignetteOpacity { get; set; }

    /// <summary>
    /// Whether Quit returns to Xenia Manager (launching it if it isn't running).
    /// Off = just close BigScreen.
    /// </summary>
    [ObservableProperty]
    public partial bool ReturnToXeniaOnQuit { get; set; } = true;

    /// <summary>
    /// Whether launched games start in fullscreen (Display.fullscreen is forced
    /// for the session and restored after).
    /// </summary>
    [ObservableProperty]
    public partial bool LaunchGamesFullscreen { get; set; } = true;

    /// <summary>
    /// Whether the base Xenia Manager starts in Big Screen on startup
    /// (persisted through the desktop app's settings file).
    /// </summary>
    [ObservableProperty]
    public partial bool StartInBigScreen { get; set; }

    /// <summary>
    /// Whether the header identity automatically cycles through every version
    /// that has an active profile.
    /// </summary>
    [ObservableProperty]
    public partial bool RotateProfiles { get; set; } = true;

    /// <summary>
    /// Whether this screen's hint bar is visible - hidden while any modal is
    /// open, so only the top modal's hints show.
    /// </summary>
    [ObservableProperty]
    public partial bool IsHintBarVisible { get; set; } = true;

    /// <summary>
    /// The kind of the row whose editor is currently open (dropdown or palette),
    /// or null when rows navigate normally. The vignette slider steps directly
    /// with Left/Right - it has no editor.
    /// </summary>
    [ObservableProperty]
    public partial SettingsRowKind? ActiveEditor { get; set; }

    /// <summary>
    /// Closes the editor and notifies the view to close the native control.
    /// </summary>
    private void CloseEditor()
    {
        ActiveEditor = null;
        EditorClosed?.Invoke();
    }

    /// <summary>
    /// Restores the value the open editor started from.
    /// </summary>
    private void RestoreEditorOriginal()
    {
        switch (ActiveEditor)
        {
            case SettingsRowKind.LibraryView:
                LibraryViewMode = _originalLibraryView;
                break;
            case SettingsRowKind.CardImage:
                CardImageMode = _originalCardImage;
                break;
            case SettingsRowKind.TimeFormat:
                TimeFormat = _originalTimeFormat;
                break;
            case SettingsRowKind.BackgroundMode:
                Mode = _originalMode;
                break;
            case SettingsRowKind.PrimaryColour:
                PrimaryColor = _originalPrimary;
                break;
            case SettingsRowKind.AccentColour:
                AccentColor = _originalAccent;
                break;
            case SettingsRowKind.XConfig:
                SelectedXConfigResolution = _originalXConfigResolution;
                break;
            case SettingsRowKind.XConfigVersion:
                SelectedXConfigVersion = _originalXConfigVersion;
                break;
        }
    }

    /// <summary>
    /// Cycles the XConfig resolution option by the given step, wrapping at both ends.
    /// </summary>
    private void CycleXConfigResolution(int delta)
    {
        int count = XConfigResolutions.Count;
        if (count == 0)
        {
            return;
        }

        int current = XConfigResolutions.IndexOf(SelectedXConfigResolution!);
        if (current < 0)
        {
            current = 0;
        }

        SelectedXConfigResolution = XConfigResolutions[(current + delta + count) % count];
    }

    /// <summary>
    /// Steps the vignette opacity by the given direction (0.05 per step), clamped to 0-1.
    /// </summary>
    public void AdjustVignette(int delta) =>
        VignetteOpacity = Math.Clamp(VignetteOpacity + delta * LayoutConstants.VignetteStep, 0, 1);

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
    /// Sets the given gamepad as primary and persists its device GUID.
    /// </summary>
    public void SetPrimary(GamepadItemViewModel item)
    {
        _gamepadService.SetPrimary(item.Source);
        _backgroundService.Settings.PrimaryControllerGuid = item.Source.Guid;
        _backgroundService.Save();
        Logger.Info<SettingsViewModel>($"Primary controller set to '{item.Name}' ({item.Source.Guid})");
    }

    /// <summary>
    /// Rebuilds the XConfig version list from the installed versions that have
    /// an XConfig file, keeping the previously selected version when it still
    /// exists (falling back to the first otherwise) and loads its file.
    /// </summary>
    private void LoadXConfig()
    {
        XConfigVersions.Clear();
        foreach (XeniaVersion version in InstalledVersions)
        {
            if (XConfigManager.XConfigExists(version))
            {
                XConfigVersions.Add(new XConfigVersionOption(version,
                    (string)XeniaVersionToStringConverter.Instance.Convert(
                        version, typeof(string), null, CultureInfo.InvariantCulture)!));
            }
        }

        OnPropertyChanged(nameof(HasXConfig));
        if (XConfigVersions.Count == 0)
        {
            _xconfigFile = null;
            SelectedXConfigVersion = null;
            return;
        }

        XConfigVersionOption? previous = SelectedXConfigVersion;
        SelectedXConfigVersion =
            XConfigVersions.FirstOrDefault(v => v.Version == previous?.Version) ?? XConfigVersions[0];
    }

    /// <summary>
    /// Loads the given version's XConfig file and syncs the resolution
    /// dropdown to it (without persisting - the sync is suppressed).
    /// </summary>
    private void LoadXConfigFile(XeniaVersion version)
    {
        _syncingXConfig = true;
        try
        {
            _xconfigFile = XConfigManager.LoadXConfig(version);
            SelectedXConfigResolution =
                XConfigResolutions.FirstOrDefault(r => r.Value == _xconfigFile?.AvHdmiScreenSize);
        }
        finally
        {
            _syncingXConfig = false;
        }
    }

    /// <summary>
    /// Rebuilds the controller-navigable row list (fixed rows in display order,
    /// then the connected gamepads).
    /// </summary>
    private void RebuildRows()
    {
        _rows.Clear();
        _rows.Add(RowManageProfiles);
        _rows.Add(RowLibraryView);
        _rows.Add(RowCardImage);
        _rows.Add(RowTimeFormat);
        _rows.Add(RowQuitToggle);
        _rows.Add(RowFullscreenToggle);
        _rows.Add(RowStartInBigScreenToggle);
        _rows.Add(RowRotateProfiles);
        _rows.Add(RowBackgroundMode);
        _rows.Add(RowPrimaryColour);
        _rows.Add(RowAccentColour);
        _rows.Add(RowVignette);
        _rows.Add(RowBackgroundImage);
        foreach (GamepadItemViewModel controller in Controllers)
        {
            _rows.Add(controller);
        }

        if (HasXConfig)
        {
            _rows.Add(RowXConfigVersion);
            _rows.Add(RowXConfig);
        }
    }

    /// <summary>
    /// Rebuilds the connected-gamepad list from the gamepad service, keeping
    /// the selection on the same gamepad (or falling back to the Background
    /// Image row when it disconnected). Battery polls rebuild this list, so
    /// the selection must survive them.
    /// </summary>
    public void RefreshControllers()
    {
        string? selectedGuid = Controllers.FirstOrDefault(c => c.IsSelected)?.Guid;
        Controllers.Clear();
        foreach (GamepadInfo gamepad in _gamepadService.ConnectedGamepads)
        {
            Controllers.Add(new GamepadItemViewModel(gamepad));
        }

        RebuildRows();
        if (selectedGuid != null)
        {
            GamepadItemViewModel? match = Controllers.FirstOrDefault(c => c.Guid == selectedGuid);
            SelectionHelper.SelectOnly(_rows, (ISelectable?)match ?? RowBackgroundImage);
        }

        OnPropertyChanged(nameof(HasNoControllers));
        Logger.Debug<SettingsViewModel>($"Controllers refreshed: {Controllers.Count} connected");
    }

    /// <summary>
    /// Refreshes the controller list when the gamepad service state changes
    /// (connect, disconnect, primary switch, battery).
    /// </summary>
    private void OnGamepadStateChanged() => RefreshControllers();

    /// <summary>
    /// Commits the open editor's value and closes it.
    /// </summary>
    public void CommitEditor()
    {
        SettingsRowKind? kind = ActiveEditor;
        CloseEditor();
        Logger.Debug<SettingsViewModel>($"Committed {kind} editor");
    }

    /// <summary>
    /// Restores the open editor's original value and closes it.
    /// </summary>
    public void CancelEditor()
    {
        RestoreEditorOriginal();
        SettingsRowKind? kind = ActiveEditor;
        CloseEditor();
        Logger.Debug<SettingsViewModel>($"Cancelled {kind} editor");
    }

    /// <summary>
    /// Opens the editor for the given row kind, snapshotting its value so a
    /// cancel can restore it.
    /// </summary>
    private void OpenEditor(SettingsRowKind kind)
    {
        switch (kind)
        {
            case SettingsRowKind.LibraryView:
                _originalLibraryView = LibraryViewMode;
                break;
            case SettingsRowKind.CardImage:
                _originalCardImage = CardImageMode;
                break;
            case SettingsRowKind.TimeFormat:
                _originalTimeFormat = TimeFormat;
                break;
            case SettingsRowKind.BackgroundMode:
                _originalMode = Mode;
                break;
            case SettingsRowKind.PrimaryColour:
                _originalPrimary = PrimaryColor;
                break;
            case SettingsRowKind.AccentColour:
                _originalAccent = AccentColor;
                break;
            case SettingsRowKind.XConfig:
                _originalXConfigResolution = SelectedXConfigResolution;
                break;
            case SettingsRowKind.XConfigVersion:
                _originalXConfigVersion = SelectedXConfigVersion;
                break;
        }

        ActiveEditor = kind;
        EditorOpened?.Invoke(kind);
        Logger.Debug<SettingsViewModel>($"Opened {kind} editor");
    }

    /// <summary>
    /// Opens the Manage Profiles overlay as a modal on top of the settings screen.
    /// Skipped when a modal is already open (stray Enter on the still-focused
    /// button would otherwise double-open the overlay).
    /// </summary>
    public static void OpenManageProfiles()
    {
        IModalService modalService = App.Services.GetRequiredService<IModalService>();
        if (modalService.IsOpen)
        {
            return;
        }

        XeniaVersion version = App.Services.GetRequiredService<IProfileService>().ActiveVersion
                               ?? XeniaVersion.Canary;
        Logger.Info<SettingsViewModel>($"Opening manage profiles for {version}");
        TaskUtilities.RunSafely<SettingsViewModel>(
            () => modalService.ShowAsync(new ManageProfilesViewModel(version)), "Opening manage profiles");
    }

    /// <summary>
    /// Cycles the open editor's value by the given step (dropdown option, palette
    /// colour or vignette step), wrapping at both ends.
    /// </summary>
    private void CycleValue(int delta)
    {
        switch (ActiveEditor)
        {
            case SettingsRowKind.LibraryView:
                LibraryViewMode = EnumCycleHelper.Next(LibraryViewMode, delta);
                break;
            case SettingsRowKind.CardImage:
                CardImageMode = EnumCycleHelper.Next(CardImageMode, delta);
                break;
            case SettingsRowKind.TimeFormat:
                TimeFormat = EnumCycleHelper.Next(TimeFormat, delta);
                break;
            case SettingsRowKind.BackgroundMode:
                Mode = EnumCycleHelper.Next(Mode, delta);
                break;
            case SettingsRowKind.PrimaryColour:
                PrimaryColor = EnumCycleHelper.NextColor(ColorPickerField.BackgroundPalette, PrimaryColor, delta, 0);
                break;
            case SettingsRowKind.AccentColour:
                AccentColor = EnumCycleHelper.NextColor(ColorPickerField.AccentPalette, AccentColor, delta, 1);
                break;
            case SettingsRowKind.XConfig:
                CycleXConfigResolution(delta);
                break;
            case SettingsRowKind.XConfigVersion:
                CycleXConfigVersion(delta);
                break;
        }
    }

    /// <summary>
    /// Handles gamepad input while a row editor is open: dropdown rows cycle
    /// with Up/Down, palette colours with Left/Right, A commits.
    /// </summary>
    private bool HandleEditorInput(NavigationCommand command)
    {
        switch (command)
        {
            case NavigationCommand.MoveUp:
                if (IsComboEditor)
                {
                    CycleValue(-1);
                }

                return true;
            case NavigationCommand.MoveDown:
                if (IsComboEditor)
                {
                    CycleValue(1);
                }

                return true;
            case NavigationCommand.MoveLeft:
                if (!IsComboEditor)
                {
                    CycleValue(-1);
                }

                return true;
            case NavigationCommand.MoveRight:
                if (!IsComboEditor)
                {
                    CycleValue(1);
                }

                return true;
            case NavigationCommand.Activate:
                CommitEditor();
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Steps the selected row's immediate-commit value directly (vignette
    /// slider); returns false so other rows fall through (Left/Right stays unused).
    /// </summary>
    private bool StepSelectedRow(int delta)
    {
        if (_rows.FirstOrDefault(r => r.IsSelected) is SettingsRowViewModel { Kind: SettingsRowKind.Vignette })
        {
            AdjustVignette(delta);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Runs the fixed row's action for its kind.
    /// </summary>
    private void ActivateFixedRow(SettingsRowKind kind)
    {
        switch (kind)
        {
            case SettingsRowKind.ManageProfiles:
                OpenManageProfiles();
                break;
            case SettingsRowKind.QuitToggle:
                ReturnToXeniaOnQuit = !ReturnToXeniaOnQuit;
                break;
            case SettingsRowKind.FullscreenToggle:
                LaunchGamesFullscreen = !LaunchGamesFullscreen;
                break;
            case SettingsRowKind.StartInBigScreenToggle:
                StartInBigScreen = !StartInBigScreen;
                break;
            case SettingsRowKind.RotateProfiles:
                RotateProfiles = !RotateProfiles;
                break;
            case SettingsRowKind.BackgroundImage:
                SelectImageRequested?.Invoke();
                break;
            case SettingsRowKind.LibraryView:
            case SettingsRowKind.CardImage:
            case SettingsRowKind.TimeFormat:
            case SettingsRowKind.BackgroundMode:
            case SettingsRowKind.PrimaryColour:
            case SettingsRowKind.AccentColour:
            case SettingsRowKind.XConfig:
            case SettingsRowKind.XConfigVersion:
                OpenEditor(kind);
                break;
        }
    }

    /// <summary>
    /// Activates the selected row: sets the primary controller on a gamepad row,
    /// or runs the fixed row's action (toggle, editor, modal or picker).
    /// </summary>
    private void ActivateRow()
    {
        switch (_rows.FirstOrDefault(r => r.IsSelected))
        {
            case SettingsRowViewModel row:
                ActivateFixedRow(row.Kind);
                break;
            case GamepadItemViewModel gamepad:
                SetPrimary(gamepad);
                break;
        }
    }

    /// <summary>
    /// Moves the row selection by the given step, clamped at both ends. Rows
    /// start unselected - the first move selects the first row.
    /// </summary>
    public void MoveSelection(int delta)
    {
        int index = SelectionHelper.MoveSelection(_rows, delta);
        if (index >= 0)
        {
            RowSelectionChanged?.Invoke(_rows[index]);
        }
    }

    /// <summary>
    /// Cycles the XConfig version by the given step, wrapping at both ends.
    /// Immediate-commit: reloads the selected version's file and re-syncs the
    /// resolution dropdown.
    /// </summary>
    private void CycleXConfigVersion(int delta)
    {
        int count = XConfigVersions.Count;
        if (count == 0 || SelectedXConfigVersion == null)
        {
            return;
        }

        int current = XConfigVersions.IndexOf(SelectedXConfigVersion);
        if (current < 0)
        {
            current = 0;
        }

        SelectedXConfigVersion = XConfigVersions[(current + delta + count) % count];
    }

    /// <summary>
    /// Handles a gamepad navigation command: row movement while no editor is
    /// open, value cycling while one is. Returns whether the command was consumed.
    /// </summary>
    public bool HandleInput(NavigationCommand command)
    {
        if (IsEditorOpen)
        {
            return HandleEditorInput(command);
        }

        switch (command)
        {
            case NavigationCommand.MoveUp:
                MoveSelection(-1);
                return true;
            case NavigationCommand.MoveDown:
                MoveSelection(1);
                return true;
            case NavigationCommand.MoveLeft:
                return StepSelectedRow(-1);
            case NavigationCommand.MoveRight:
                return StepSelectedRow(1);
            case NavigationCommand.Activate:
                ActivateRow();
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Handles Back: cancels the open row editor first, returning whether Back
    /// was consumed (the router closes the settings screen otherwise).
    /// </summary>
    public bool HandleBack()
    {
        if (IsEditorOpen)
        {
            CancelEditor();
            return true;
        }

        return false;
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
        LaunchGamesFullscreen = _backgroundService.Settings.LaunchGamesFullscreen;
        StartInBigScreen = _desktopSettings.Settings.General.StartInBigScreen;
        RotateProfiles = _backgroundService.Settings.RotateProfiles;
        LibraryViewMode = _backgroundService.Settings.LibraryViewMode;
        SelectedLibraryViewMode = LibraryViewModeOptions.FirstOrDefault(o => o.Mode == LibraryViewMode);
        CardImageMode = _backgroundService.Settings.CardImageMode;
        SelectedCardImageMode = CardImageModeOptions.FirstOrDefault(o => o.Mode == CardImageMode);
        TimeFormat = _backgroundService.Settings.TimeFormat;
        SelectedTimeFormat = TimeFormatOptions.FirstOrDefault(o => o.Format == TimeFormat);
        LoadXConfig();
        RefreshControllers();
    }

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

    /// <summary>
    /// Applies the given change to the persisted dashboard settings and saves
    /// them to disk.
    /// </summary>
    private void SaveAppearance(Action<DashboardSettings> apply)
    {
        apply(_backgroundService.Settings);
        _backgroundService.Save();
    }

    partial void OnActiveEditorChanged(SettingsRowKind? value) =>
        OnPropertyChanged(nameof(IsEditorOpen));

    partial void OnSelectedXConfigResolutionChanged(XConfigResolutionOption? value)
    {
        if (_syncingXConfig || value == null || _xconfigFile == null || SelectedXConfigVersion == null)
        {
            return;
        }

        _xconfigFile.AvHdmiScreenSize = value.Value;
        XConfigManager.SaveXConfig(_xconfigFile, SelectedXConfigVersion.Version);
        Logger.Info<SettingsViewModel>($"XConfig resolution set to {value.Value} ({SelectedXConfigVersion.DisplayName})");
    }

    partial void OnSelectedXConfigVersionChanged(XConfigVersionOption? value)
    {
        if (value == null)
        {
            return;
        }

        LoadXConfigFile(value.Version);
        Logger.Info<SettingsViewModel>($"XConfig version set to {value.DisplayName}");
    }

    partial void OnModeChanged(BackgroundMode value)
    {
        SaveAppearance(s => s.Mode = value);
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
        SaveAppearance(s => s.PrimaryColor = value);
        OnPropertyChanged(nameof(ScreenBackground));
        AppearanceChanged?.Invoke();
        Logger.Info<SettingsViewModel>($"Primary color changed to {value}");
    }

    partial void OnAccentColorChanged(Color value)
    {
        SaveAppearance(s => s.AccentColor = value);
        Logger.Info<SettingsViewModel>($"Accent color changed to {value}");
    }

    partial void OnVignetteOpacityChanged(double value)
    {
        SaveAppearance(s => s.VignetteOpacity = value);
        OnPropertyChanged(nameof(VignetteText));
        Logger.Debug<SettingsViewModel>($"Vignette opacity changed to {value:0.00}");
    }

    partial void OnReturnToXeniaOnQuitChanged(bool value)
    {
        SaveAppearance(s => s.ReturnToXeniaOnQuit = value);
        Logger.Info<SettingsViewModel>($"Return to Xenia Manager on quit: {value}");
    }

    partial void OnLaunchGamesFullscreenChanged(bool value)
    {
        SaveAppearance(s => s.LaunchGamesFullscreen = value);
        Logger.Info<SettingsViewModel>($"Launch games in fullscreen: {value}");
    }

    partial void OnStartInBigScreenChanged(bool value)
    {
        _desktopSettings.Settings.General.StartInBigScreen = value;
        _desktopSettings.SaveSettings();
        Logger.Info<SettingsViewModel>($"Start in Big Screen: {value}");
    }

    partial void OnRotateProfilesChanged(bool value)
    {
        SaveAppearance(s => s.RotateProfiles = value);
        Logger.Info<SettingsViewModel>($"Rotate profiles in header: {value}");
    }

    partial void OnLibraryViewModeChanged(LibraryViewMode value)
    {
        SaveAppearance(s => s.LibraryViewMode = value);
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
        SaveAppearance(s => s.CardImageMode = value);
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

    partial void OnTimeFormatChanged(TimeFormat value)
    {
        SaveAppearance(s => s.TimeFormat = value);
        SelectedTimeFormat = TimeFormatOptions.FirstOrDefault(o => o.Format == value);
        TimeFormatChanged?.Invoke();
        Logger.Info<SettingsViewModel>($"Time format changed to {value}");
    }

    partial void OnSelectedTimeFormatChanged(TimeFormatOption? value)
    {
        if (value != null)
        {
            TimeFormat = value.Format;
        }
    }

    public SettingsViewModel(IBackgroundService backgroundService, IProfileService profileService,
        IGamepadInputService gamepadService, IModalService modalService)
    {
        _backgroundService = backgroundService;
        _profileService = profileService;
        _gamepadService = gamepadService;
        InstalledVersions = _desktopSettings.GetInstalledVersions(_desktopSettings);
        modalService.StackChanged += () => IsHintBarVisible = !modalService.IsOpen;

        _profileService.ProfileChanged += () => OnPropertyChanged(nameof(ActiveGamertag));

        if (_gamepadService.IsActive)
        {
            _gamepadService.StateChanged += OnGamepadStateChanged;
        }

        RebuildRows();
    }
}