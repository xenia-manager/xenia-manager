using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using XeniaManager.BigScreen.Constants;
using XeniaManager.BigScreen.Models;
using XeniaManager.BigScreen.Services;
using XeniaManager.BigScreen.Utilities;
using XeniaManager.BigScreen.ViewModels.Items;
using XeniaManager.BigScreen.ViewModels.Modals;
using XeniaManager.Core.Converters;
using XeniaManager.Logging;
using XeniaManager.Core.Models;
using XeniaManager.Core.Utilities;

namespace XeniaManager.BigScreen.ViewModels.Screens;

/// <summary>
/// Gallery screen state: the screenshot gallery and its sort mode. The
/// full-screen viewer opens as a modal on the modal stack.
/// </summary>
public partial class GalleryViewModel : ScreenViewModel
{
    private readonly SettingsViewModel _settings;
    private readonly IScreenshotLibraryService _screenshotLibraryService;
    private readonly IModalService _modalService;
    private bool _screenshotsLoaded;

    /// <summary>
    /// All screenshots in the gallery (the current filter's view of
    /// <see cref="_allScreenshots"/>).
    /// </summary>
    public ObservableCollection<ScreenshotItemViewModel> Screenshots { get; } = [];

    /// <summary>
    /// Every screenshot loaded from disk, unfiltered; the filter selects which
    /// of these the <see cref="Screenshots"/> collection shows.
    /// </summary>
    private readonly List<ScreenshotItemViewModel> _allScreenshots = [];

    /// <summary>
    /// Whether the gallery screen shows the "no screenshots" stub.
    /// </summary>
    public bool ShowEmptyScreenshots
    {
        get
        {
            return Screenshots.Count == 0;
        }
    }

    /// <summary>
    /// The current gallery sort mode (cycled with X).
    /// </summary>
    [ObservableProperty]
    public partial GallerySort GallerySort { get; set; } = GallerySort.NewestFirst;

    /// <summary>
    /// Display name of the current gallery sort mode.
    /// </summary>
    public string GallerySortText
    {
        get
        {
            return GallerySort switch
            {
                GallerySort.OldestFirst => LocalizationHelper.GetText("Gallery.Sort.OldestFirst"),
                GallerySort.ByGame => LocalizationHelper.GetText("Gallery.Sort.ByGame"),
                _ => LocalizationHelper.GetText("Gallery.Sort.NewestFirst")
            };
        }
    }

    /// <summary>
    /// The available version filters: "All" plus every installed emulator version.
    /// </summary>
    public ObservableCollection<GalleryVersionFilter> VersionFilters { get; } = [];

    /// <summary>
    /// The currently active version filter (cycled with View; X stays on sort).
    /// </summary>
    [ObservableProperty]
    public partial GalleryVersionFilter? SelectedVersionFilter { get; set; }

    /// <summary>
    /// Display name of the active version filter (e.g. "All", "Xenia Canary").
    /// </summary>
    public string VersionFilterText
    {
        get
        {
            return SelectedVersionFilter?.DisplayName ?? string.Empty;
        }
    }

    public GalleryViewModel(SettingsViewModel settings, IScreenshotLibraryService screenshotLibraryService,
        IModalService modalService)
        : base(settings, modalService)
    {
        _settings = settings;
        _screenshotLibraryService = screenshotLibraryService;
        _modalService = modalService;
        Screenshots.CollectionChanged += (_, _) => OnPropertyChanged(nameof(ShowEmptyScreenshots));

        VersionFilters.Add(new GalleryVersionFilter(null, LocalizationHelper.GetText("Gallery.Filter.All")));
        foreach (XeniaVersion version in settings.InstalledVersions)
        {
            VersionFilters.Add(new GalleryVersionFilter(version,
                (string)XeniaVersionToStringConverter.Instance.Convert(
                    version, typeof(string), null, CultureInfo.InvariantCulture)!));
        }

        SelectedVersionFilter = VersionFilters[0];

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
    private void ApplyGallerySort()
    {
        if (Screenshots.Count == 0)
        {
            return;
        }

        List<ScreenshotItemViewModel> sorted = GallerySort switch
        {
            GallerySort.OldestFirst => Screenshots.OrderBy(s => s.CapturedAt).ToList(),
            GallerySort.ByGame => Screenshots.OrderBy(s => s.GameTitle).ThenByDescending(s => s.CapturedAt).ToList(),
            _ => Screenshots.OrderByDescending(s => s.CapturedAt).ToList()
        };

        SelectionHelper.ResortPreservingSelection(Screenshots, sorted);
    }

    /// <summary>
    /// Rebuilds the visible collection from the unfiltered screenshots under the
    /// active version filter, then applies the sort. The selection follows the
    /// list position so the viewport stays put.
    /// </summary>
    private void ApplyGalleryFilter()
    {
        List<ScreenshotItemViewModel> filtered = _allScreenshots
            .Where(s => SelectedVersionFilter?.Matches(s) ?? true)
            .ToList();

        List<ScreenshotItemViewModel> sorted = GallerySort switch
        {
            GallerySort.OldestFirst => filtered.OrderBy(s => s.CapturedAt).ToList(),
            GallerySort.ByGame => filtered.OrderBy(s => s.GameTitle).ThenByDescending(s => s.CapturedAt).ToList(),
            _ => filtered.OrderByDescending(s => s.CapturedAt).ToList()
        };

        SelectionHelper.ResortPreservingSelection(Screenshots, sorted);
    }

    partial void OnSelectedVersionFilterChanged(GalleryVersionFilter? value)
    {
        ApplyGalleryFilter();
        OnPropertyChanged(nameof(VersionFilterText));
        Logger.Debug<GalleryViewModel>($"Gallery version filter changed to {value?.DisplayName}");
    }

    partial void OnGallerySortChanged(GallerySort value)
    {
        ApplyGallerySort();
        OnPropertyChanged(nameof(GallerySortText));
        Logger.Debug<GalleryViewModel>($"Gallery sort changed to {value}");
    }

    /// <summary>
    /// Cycles the gallery sort mode: Newest First → Oldest First → By Game.
    /// </summary>
    public void CycleGallerySort() => GallerySort = EnumCycleHelper.Next(GallerySort, 1);

    /// <summary>
    /// Cycles the version filter: All → each installed version → back to All.
    /// </summary>
    public void CycleVersionFilter()
    {
        if (VersionFilters.Count == 0)
        {
            return;
        }

        int index = VersionFilters.IndexOf(SelectedVersionFilter ?? VersionFilters[0]);
        SelectedVersionFilter = VersionFilters[(index + 1) % VersionFilters.Count];
    }

    /// <summary>
    /// Scans every installed emulator version's screenshots folder (on a
    /// background thread) and fills the gallery, applying the current filter and
    /// sort. Part of the boot pipeline so the scan happens behind the splash screen.
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
        progress?.Report((LocalizationHelper.GetText("Splash.LoadingGallery"), SplashStages.LoadingGallery));
        await Task.Run(() => _screenshotLibraryService.Load(), cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        foreach (ScreenshotItemViewModel screenshot in _allScreenshots)
        {
            screenshot.Thumbnail?.Dispose();
        }

        _allScreenshots.Clear();
        foreach (ScreenshotItemViewModel screenshot in _screenshotLibraryService.Screenshots)
        {
            cancellationToken.ThrowIfCancellationRequested();
            screenshot.TimeFormat = _settings.TimeFormat;
            _allScreenshots.Add(screenshot);
        }

        ApplyGalleryFilter();
        Logger.Info<GalleryViewModel>($"Loaded {_allScreenshots.Count} screenshots");
    }

    /// <summary>
    /// Re-scans every installed emulator version's screenshots folder (on a
    /// background thread) and refreshes the gallery, reapplying the current
    /// filter and sort with the selection position preserved. Runs every time
    /// the gallery screen opens so new artwork appears without restarting.
    /// </summary>
    public async Task RefreshAsync()
    {
        await Task.Run(_screenshotLibraryService.Load);
        foreach (ScreenshotItemViewModel screenshot in _allScreenshots)
        {
            screenshot.Thumbnail?.Dispose();
        }

        _allScreenshots.Clear();
        foreach (ScreenshotItemViewModel screenshot in _screenshotLibraryService.Screenshots)
        {
            screenshot.TimeFormat = _settings.TimeFormat;
            _allScreenshots.Add(screenshot);
        }

        ApplyGalleryFilter();
        Logger.Info<GalleryViewModel>($"Refreshed gallery with {_allScreenshots.Count} screenshots");
    }

    /// <summary>
    /// Opens the full-screen screenshot viewer as a modal on the modal stack.
    /// </summary>
    public void OpenScreenshot(ScreenshotItemViewModel screenshot)
    {
        Logger.Debug<GalleryViewModel>($"Opening screenshot viewer for '{screenshot.Title}'");
        TaskUtilities.RunSafely<GalleryViewModel>(
            () => _modalService.ShowAsync(new ScreenshotViewerViewModel(screenshot, Screenshots)),
            "Opening screenshot viewer");
    }
}