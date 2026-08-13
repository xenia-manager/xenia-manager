using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using XeniaManager.BigScreen.Models;
using XeniaManager.BigScreen.Services;
using XeniaManager.BigScreen.Utilities;
using XeniaManager.BigScreen.ViewModels.Items;
using XeniaManager.Core.Logging;
using XeniaManager.Core.Utilities;

namespace XeniaManager.BigScreen.ViewModels;

/// <summary>
/// Media screen state: the screenshot gallery, its sort mode, and the
/// full-screen viewer (nested sub-screen).
/// </summary>
public partial class MediaViewModel : ScreenViewModel
{
    private readonly SettingsViewModel _settings;
    private readonly IScreenshotLibraryService _screenshotLibraryService;
    private bool _screenshotsLoaded;

    /// <summary>
    /// All screenshots in the media gallery.
    /// </summary>
    public ObservableCollection<ScreenshotItemViewModel> Screenshots { get; } = [];

    /// <summary>
    /// Whether the media screen shows the "no screenshots" stub.
    /// </summary>
    public bool ShowEmptyScreenshots => Screenshots.Count == 0;

    /// <summary>
    /// The current media sort mode (cycled with Y).
    /// </summary>
    [ObservableProperty] private MediaSort _mediaSort = MediaSort.NewestFirst;

    /// <summary>
    /// Display name of the current media sort mode.
    /// </summary>
    public string MediaSortText => MediaSort switch
    {
        MediaSort.OldestFirst => LocalizationHelper.GetText("Media.Sort.OldestFirst"),
        MediaSort.ByGame => LocalizationHelper.GetText("Media.Sort.ByGame"),
        _ => LocalizationHelper.GetText("Media.Sort.NewestFirst"),
    };

    /// <summary>
    /// The screenshot viewer sub-screen, or null when it is closed.
    /// </summary>
    [ObservableProperty] private MediaViewerViewModel? _viewer;

    /// <summary>
    /// Whether the full-screen screenshot viewer is open.
    /// </summary>
    public bool IsViewerOpen => Viewer != null;

    partial void OnViewerChanged(MediaViewerViewModel? value)
    {
        OnPropertyChanged(nameof(IsViewerOpen));
    }

    public MediaViewModel(SettingsViewModel settings, IScreenshotLibraryService screenshotLibraryService)
        : base(settings)
    {
        _settings = settings;
        _screenshotLibraryService = screenshotLibraryService;
        Screenshots.CollectionChanged += (_, _) => OnPropertyChanged(nameof(ShowEmptyScreenshots));

        // Screenshot captions follow the persisted time format
        _settings.TimeFormatChanged += () =>
        {
            foreach (ScreenshotItemViewModel screenshot in Screenshots)
            {
                screenshot.TimeFormat = _settings.TimeFormat;
            }
        };
    }

    /// <summary>
    /// Re-sorts the screenshot collection. The selection follows the list position,
    /// not the element, so the selected card stays in the same spot on screen.
    /// </summary>
    private void ApplyMediaSort()
    {
        if (Screenshots.Count == 0)
        {
            return;
        }

        List<ScreenshotItemViewModel> sorted = MediaSort switch
        {
            MediaSort.OldestFirst => Screenshots.OrderBy(s => s.CapturedAt).ToList(),
            MediaSort.ByGame => Screenshots.OrderBy(s => s.GameTitle).ThenByDescending(s => s.CapturedAt).ToList(),
            _ => Screenshots.OrderByDescending(s => s.CapturedAt).ToList(),
        };

        SelectionHelper.ResortPreservingSelection(Screenshots, sorted);
    }

    partial void OnMediaSortChanged(MediaSort value)
    {
        ApplyMediaSort();
        OnPropertyChanged(nameof(MediaSortText));
        Logger.Debug<MediaViewModel>($"Media sort changed to {value}");
    }

    /// <summary>
    /// Cycles the media sort mode: Newest First → Oldest First → By Game.
    /// </summary>
    public void CycleMediaSort() => MediaSort = EnumCycleHelper.Next(MediaSort, 1);

    /// <summary>
    /// Scans the Canary screenshots folder (on a background thread) and fills the
    /// media gallery, applying the current sort. Part of the boot pipeline so the
    /// scan happens behind the splash screen.
    /// </summary>
    public async Task LoadScreenshotsAsync(
        IProgress<(string Status, double Progress)>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (_screenshotsLoaded)
        {
            return;
        }

        _screenshotsLoaded = true;
        progress?.Report(("Loading Media", 0.75));
        await Task.Run(() => _screenshotLibraryService.Load(), cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        Screenshots.Clear();
        foreach (ScreenshotItemViewModel screenshot in _screenshotLibraryService.Screenshots)
        {
            cancellationToken.ThrowIfCancellationRequested();
            screenshot.TimeFormat = _settings.TimeFormat;
            Screenshots.Add(screenshot);
        }

        ApplyMediaSort();
        Logger.Info<MediaViewModel>($"Loaded {Screenshots.Count} screenshots");
    }

    /// <summary>
    /// Opens the modal viewer for the given screenshot.
    /// </summary>
    public void OpenScreenshot(ScreenshotItemViewModel screenshot)
    {
        Viewer = new MediaViewerViewModel(screenshot, Screenshots);
        Logger.Debug<MediaViewModel>($"Opening screenshot viewer for '{screenshot.Title}'");
    }

    /// <summary>
    /// Closes the modal screenshot viewer.
    /// </summary>
    public void CloseMediaViewer() => Viewer = null;
}