using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using XeniaManager.BigScreen.Models;
using XeniaManager.BigScreen.Services;
using XeniaManager.BigScreen.Utilities;
using XeniaManager.BigScreen.ViewModels.Items;

namespace XeniaManager.BigScreen.ViewModels;

/// <summary>
/// Media screen state: the screenshot gallery, its sort mode, and the
/// full-screen viewer (nested sub-screen).
/// </summary>
public partial class MediaViewModel : ScreenViewModel
{
    private readonly ScreenshotLibraryService _screenshotLibraryService;
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
        MediaSort.OldestFirst => "Oldest First",
        MediaSort.ByGame => "By Game",
        _ => "Newest First",
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

    public MediaViewModel(SettingsViewModel settings, ScreenshotLibraryService screenshotLibraryService)
        : base(settings)
    {
        _screenshotLibraryService = screenshotLibraryService;
        Screenshots.CollectionChanged += (_, _) => OnPropertyChanged(nameof(ShowEmptyScreenshots));
    }

    /// <summary>
    /// Re-sorts the screenshot collection. The selection follows the list position,
    /// not the element, so the selected card stays in the same spot on screen.
    /// </summary>
    public void ApplyMediaSort()
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
    }

    /// <summary>
    /// Cycles the media sort mode: Newest First → Oldest First → By Game.
    /// </summary>
    public void CycleMediaSort() => MediaSort = EnumCycleHelper.Next(MediaSort, 1);

    /// <summary>
    /// Scans the Canary screenshots folder once (recursively, per-game subfolders)
    /// and fills the media gallery, applying the current sort.
    /// </summary>
    public void EnsureScreenshotsLoaded()
    {
        if (_screenshotsLoaded)
        {
            return;
        }

        _screenshotsLoaded = true;
        _screenshotLibraryService.Load();

        Screenshots.Clear();
        foreach (ScreenshotItemViewModel screenshot in _screenshotLibraryService.Screenshots)
        {
            Screenshots.Add(screenshot);
        }

        ApplyMediaSort();
    }

    /// <summary>
    /// Opens the modal viewer for the given screenshot.
    /// </summary>
    public void OpenScreenshot(ScreenshotItemViewModel screenshot) =>
        Viewer = new MediaViewerViewModel(screenshot, Screenshots);

    /// <summary>
    /// Closes the modal screenshot viewer.
    /// </summary>
    public void CloseMediaViewer() => Viewer = null;
}
