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
using XeniaManager.BigScreen.ViewModels.Modals;
using XeniaManager.Core.Logging;
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
    /// All screenshots in the gallery.
    /// </summary>
    public ObservableCollection<ScreenshotItemViewModel> Screenshots { get; } = [];

    /// <summary>
    /// Whether the gallery screen shows the "no screenshots" stub.
    /// </summary>
    public bool ShowEmptyScreenshots => Screenshots.Count == 0;

    /// <summary>
    /// The current gallery sort mode (cycled with X).
    /// </summary>
    [ObservableProperty]
    public partial GallerySort GallerySort { get; set; } = GallerySort.NewestFirst;

    /// <summary>
    /// Display name of the current gallery sort mode.
    /// </summary>
    public string GallerySortText => GallerySort switch
    {
        GallerySort.OldestFirst => LocalizationHelper.GetText("Gallery.Sort.OldestFirst"),
        GallerySort.ByGame => LocalizationHelper.GetText("Gallery.Sort.ByGame"),
        _ => LocalizationHelper.GetText("Gallery.Sort.NewestFirst")
    };

    public GalleryViewModel(SettingsViewModel settings, IScreenshotLibraryService screenshotLibraryService,
        IModalService modalService)
        : base(settings, modalService)
    {
        _settings = settings;
        _screenshotLibraryService = screenshotLibraryService;
        _modalService = modalService;
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
    /// Scans the Canary screenshots folder (on a background thread) and fills the
    /// gallery, applying the current sort. Part of the boot pipeline so the
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
        progress?.Report((LocalizationHelper.GetText("Splash.LoadingGallery"), 0.85));
        await Task.Run(() => _screenshotLibraryService.Load(), cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        Screenshots.Clear();
        foreach (ScreenshotItemViewModel screenshot in _screenshotLibraryService.Screenshots)
        {
            cancellationToken.ThrowIfCancellationRequested();
            screenshot.TimeFormat = _settings.TimeFormat;
            Screenshots.Add(screenshot);
        }

        ApplyGallerySort();
        Logger.Info<GalleryViewModel>($"Loaded {Screenshots.Count} screenshots");
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