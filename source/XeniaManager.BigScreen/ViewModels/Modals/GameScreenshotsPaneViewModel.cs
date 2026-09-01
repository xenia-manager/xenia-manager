using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using XeniaManager.BigScreen.Models;
using XeniaManager.BigScreen.Models.Settings;
using XeniaManager.BigScreen.Services;
using XeniaManager.BigScreen.Utilities;
using XeniaManager.BigScreen.ViewModels.Items;
using XeniaManager.BigScreen.Views.Screens;
using XeniaManager.Logging;
using XeniaManager.Core.Constants;
using XeniaManager.Core.Models;
using XeniaManager.Core.Models.Game;
using XeniaManager.Core.Utilities;

namespace XeniaManager.BigScreen.ViewModels.Modals;

/// <summary>
/// The game modal's screenshots pane: the game's own screenshots folder
/// ({GameId} under the emulator's screenshots directory) as a 4-across grid;
/// the full-screen viewer opens as a modal on the modal stack.
/// </summary>
public partial class GameScreenshotsPaneViewModel : ViewModelBase, IGameModalPane, IDisposable
{
    private readonly Game _game;
    private readonly IModalService _modalService;

    /// <summary>
    /// Whether this pane created its screenshot items itself (self-scan path)
    /// and therefore owns - and must dispose - their thumbnails. Items reused
    /// from the gallery cache stay owned by the gallery.
    /// </summary>
    private bool _ownsItems;

    /// <summary>
    /// Whether the pane has been disposed (the game modal closed). Guards the
    /// in-flight self-scan: results landing after disposal are released
    /// immediately instead of leaking.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Whether the pane shows the empty state (scan finished, no screenshots).
    /// </summary>
    public bool ShowEmpty
    {
        get
        {
            return !IsLoading && Rows.Count == 0;
        }
    }

    /// <summary>
    /// The screenshots found for this game.
    /// </summary>
    public ObservableCollection<ScreenshotItemViewModel> Rows { get; } = [];

    /// <summary>
    /// Whether the screenshot scan is still running (loading state).
    /// </summary>
    [ObservableProperty]
    public partial bool IsLoading { get; set; } = true;

    /// <summary>
    /// The screenshot count shown in the pane header.
    /// </summary>
    public string CountText
    {
        get
        {
            return string.Format(LocalizationHelper.GetText("GameModal.Screenshots.Count"), Rows.Count);
        }
    }

    /// <summary>
    /// Resolves the game's screenshots folder: the standard "{GameId}" subfolder
    /// of the emulator's screenshots directory for installed versions; for Custom
    /// games, the "screenshots" folder next to the custom executable. Returns
    /// null when the folder cannot be derived (no custom executable set).
    /// </summary>
    private string? ResolveScreenshotsFolder()
    {
        if (_game.XeniaVersion != XeniaVersion.Custom)
        {
            return AppPathResolver.GetFullPath(
                XeniaVersionInfo.GetXeniaVersionInfo(_game.XeniaVersion).EmulatorDir,
                XeniaPaths.ScreenshotsFolderName,
                _game.GameId.ToUpperInvariant());
        }

        string? executable = _game.FileLocations.CustomEmulatorExecutable;
        if (string.IsNullOrEmpty(executable))
        {
            return null;
        }

        string emulatorDir = Path.IsPathRooted(executable)
            ? Path.GetDirectoryName(executable) ?? string.Empty
            : AppPathResolver.GetFullPath(Path.GetDirectoryName(executable) ?? string.Empty);
        return emulatorDir.Length == 0
            ? null
            : Path.Combine(emulatorDir, XeniaPaths.ScreenshotsFolderName, _game.GameId.ToUpperInvariant());
    }

    /// <summary>
    /// Enumerates the game's screenshots folder (newest first), decoding each
    /// image; unreadable files are skipped with a warning.
    /// </summary>
    private List<ScreenshotItemViewModel> ScanScreenshots()
    {
        string? folder = ResolveScreenshotsFolder();

        List<ScreenshotItemViewModel> screenshots = [];
        if (folder == null || !Directory.Exists(folder))
        {
            return screenshots;
        }

        foreach (string file in Directory.EnumerateFiles(folder)
                     .Where(f => ImageFormats.ScreenshotExtensions.Contains(Path.GetExtension(f)
                         .ToLowerInvariant())))
        {
            try
            {
                string fileName = Path.GetFileName(file);
                DateTime capturedAt = ScreenshotFileNameParser.ExtractCapturedAt(fileName)
                                      ?? File.GetLastWriteTime(file);
                using FileStream imageStream = File.OpenRead(file);
                screenshots.Add(new ScreenshotItemViewModel(
                    _game.XeniaVersion,
                    file,
                    fileName,
                    capturedAt,
                    _game.Title,
                    Bitmap.DecodeToHeight(imageStream, ScreenshotGridLayout.ThumbnailHeight)));
            }
            catch (Exception ex)
            {
                Logger.Warning<GameScreenshotsPaneViewModel>($"Failed to load screenshot '{file}', skipping");
                Logger.LogExceptionDetails<GameScreenshotsPaneViewModel>(ex);
            }
        }

        return screenshots.OrderByDescending(s => s.CapturedAt).ToList();
    }

    /// <summary>
    /// Filters the gallery's preloaded (already decoded) screenshots for this
    /// game by the source version and parent-folder game ID.
    /// </summary>
    private void PopulateFromGalleryCache(TimeFormat timeFormat)
    {
        _ownsItems = false;
        IScreenshotLibraryService library = App.Services.GetRequiredService<IScreenshotLibraryService>();
        foreach (ScreenshotItemViewModel screenshot in library.Screenshots)
        {
            string folder = Path.GetFileName(Path.GetDirectoryName(screenshot.Path) ?? string.Empty);
            if (screenshot.Version == _game.XeniaVersion
                && folder.Equals(_game.GameId, StringComparison.OrdinalIgnoreCase))
            {
                screenshot.TimeFormat = timeFormat;
                Rows.Add(screenshot);
            }
        }

        IsLoading = false;
        Logger.Info<GameScreenshotsPaneViewModel>(
            $"Loaded {Rows.Count} screenshots for '{_game.Title}' from the gallery cache");
    }

    /// <summary>
    /// Scans the screenshots folder on a background thread and populates the grid.
    /// </summary>
    private async Task LoadAsync(TimeFormat timeFormat)
    {
        _ownsItems = true;
        List<ScreenshotItemViewModel> loaded = await Task.Run(ScanScreenshots);
        if (_disposed)
        {
            foreach (ScreenshotItemViewModel screenshot in loaded)
            {
                screenshot.Thumbnail?.Dispose();
            }

            return;
        }

        IsLoading = false;

        foreach (ScreenshotItemViewModel screenshot in loaded)
        {
            screenshot.TimeFormat = timeFormat;
            Rows.Add(screenshot);
        }

        Logger.Info<GameScreenshotsPaneViewModel>($"Loaded {Rows.Count} screenshots for '{_game.Title}'");
    }

    /// <summary>
    /// Opens the full-screen screenshot viewer modal for the selected screenshot.
    /// </summary>
    private void OpenScreenshot()
    {
        ScreenshotItemViewModel? selected = Rows.FirstOrDefault(s => s.IsSelected);
        if (selected != null)
        {
            Logger.Debug<GameScreenshotsPaneViewModel>($"Opening screenshot viewer for '{selected.Title}'");
            TaskUtilities.RunSafely<GameScreenshotsPaneViewModel>(
                () => _modalService.ShowAsync(new ScreenshotViewerViewModel(selected, Rows)),
                "Opening screenshot viewer");
        }
    }

    /// <summary>
    /// Handles pane input: Up/Down moves the grid (one row), Right steps into
    /// the row, A opens the full-screen viewer modal. Left/Back return to the
    /// nav list (not consumed here).
    /// </summary>
    public bool HandleInput(NavigationCommand command)
    {
        switch (command)
        {
            case NavigationCommand.MoveUp:
                SelectionHelper.MoveSelection(Rows, -GalleryView.CardsPerRow);
                return true;
            case NavigationCommand.MoveDown:
                SelectionHelper.MoveSelection(Rows, GalleryView.CardsPerRow);
                return true;
            case NavigationCommand.MoveRight:
                SelectionHelper.MoveSelection(Rows, 1);
                return true;
            case NavigationCommand.Activate:
                OpenScreenshot();
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Selects the first screenshot when the pane becomes active.
    /// </summary>
    public void OnPaneEntered() => SelectionHelper.SelectOnlyAt(Rows, 0);

    /// <summary>
    /// Clears the screenshot selection when the pane loses focus.
    /// </summary>
    public void OnPaneExited() => SelectionHelper.ClearSelection(Rows);

    /// <summary>
    /// Releases the pane's self-scanned thumbnails when the game modal closes.
    /// Gallery-cache items are skipped - the gallery owns and recycles those.
    /// </summary>
    public void Dispose()
    {
        _disposed = true;
        if (!_ownsItems)
        {
            return;
        }

        foreach (ScreenshotItemViewModel screenshot in Rows)
        {
            screenshot.Thumbnail?.Dispose();
        }
    }

    /// <summary>
    /// Fills the grid from the boot-time gallery cache for any game whose
    /// version has cached screenshots (already scanned and decoded - no
    /// re-decode); other games (e.g. Custom, or versions the gallery skipped)
    /// scan their own folder off the UI thread.
    /// </summary>
    public GameScreenshotsPaneViewModel(Game game)
    {
        _game = game;
        _modalService = App.Services.GetRequiredService<IModalService>();
        Rows.CollectionChanged += (_, _) => OnPropertyChanged(nameof(CountText));
        TimeFormat timeFormat = App.Services.GetRequiredService<IBackgroundService>().Settings.TimeFormat;
        IScreenshotLibraryService library = App.Services.GetRequiredService<IScreenshotLibraryService>();
        if (library.Screenshots.Any(s => s.Version == game.XeniaVersion))
        {
            PopulateFromGalleryCache(timeFormat);
        }
        else
        {
            TaskUtilities.RunSafely<GameScreenshotsPaneViewModel>(
                () => LoadAsync(timeFormat), "Loading game screenshots");
        }
    }
}