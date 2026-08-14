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
using XeniaManager.Core.Logging;
using XeniaManager.Core.Models;
using XeniaManager.Core.Models.Game;
using XeniaManager.Core.Utilities;

namespace XeniaManager.BigScreen.ViewModels.Modals;

/// <summary>
/// The game modal's screenshots pane: the game's own screenshots folder
/// ({GameId} under the emulator's screenshots directory) as a 4-across grid;
/// the full-screen viewer opens as a modal on the modal stack.
/// </summary>
public partial class GameScreenshotsPaneViewModel : ViewModelBase, IGameModalPane
{
    private readonly Game _game;
    private readonly IModalService _modalService;

    /// <summary>
    /// The screenshots found for this game.
    /// </summary>
    public ObservableCollection<ScreenshotItemViewModel> Rows { get; } = [];

    /// <summary>
    /// Whether the screenshot scan is still running (loading state).
    /// </summary>
    [ObservableProperty] private bool _isLoading = true;

    /// <summary>
    /// Whether the pane shows the empty state (scan finished, no screenshots).
    /// </summary>
    public bool ShowEmpty => !IsLoading && Rows.Count == 0;

    /// <summary>
    /// The screenshot count shown in the pane header.
    /// </summary>
    public string CountText => string.Format(LocalizationHelper.GetText("GameModal.Screenshots.Count"), Rows.Count);

    /// <summary>
    /// Fills the grid from the boot-time gallery cache for Canary games (the
    /// screenshots are already scanned and decoded - no re-decode); other
    /// emulator versions scan their own folder off the UI thread.
    /// </summary>
    public GameScreenshotsPaneViewModel(Game game)
    {
        _game = game;
        _modalService = App.Services.GetRequiredService<IModalService>();
        Rows.CollectionChanged += (_, _) => OnPropertyChanged(nameof(CountText));
        TimeFormat timeFormat = App.Services.GetRequiredService<IBackgroundService>().Settings.TimeFormat;
        if (game.XeniaVersion == XeniaVersion.Canary)
        {
            PopulateFromGalleryCache(timeFormat);
        }
        else
        {
            TaskUtilities.RunSafely<GameScreenshotsPaneViewModel>(
                () => LoadAsync(timeFormat), "Loading game screenshots");
        }
    }

    /// <summary>
    /// Filters the gallery's preloaded (already decoded) screenshots for this
    /// game by the parent-folder game ID.
    /// </summary>
    private void PopulateFromGalleryCache(TimeFormat timeFormat)
    {
        IScreenshotLibraryService library = App.Services.GetRequiredService<IScreenshotLibraryService>();
        foreach (ScreenshotItemViewModel screenshot in library.Screenshots)
        {
            string folder = Path.GetFileName(Path.GetDirectoryName(screenshot.Path) ?? string.Empty);
            if (folder.Equals(_game.GameId, StringComparison.OrdinalIgnoreCase))
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
    /// Scans the screenshots folder on a background thread and populates the grid.
    /// </summary>
    private async Task LoadAsync(TimeFormat timeFormat)
    {
        List<ScreenshotItemViewModel> loaded = await Task.Run(ScanScreenshots);
        IsLoading = false;

        foreach (ScreenshotItemViewModel screenshot in loaded)
        {
            screenshot.TimeFormat = timeFormat;
            Rows.Add(screenshot);
        }

        Logger.Info<GameScreenshotsPaneViewModel>($"Loaded {Rows.Count} screenshots for '{_game.Title}'");
    }

    /// <summary>
    /// Enumerates the game's screenshots folder (newest first), decoding each
    /// image; unreadable files are skipped with a warning.
    /// </summary>
    private List<ScreenshotItemViewModel> ScanScreenshots()
    {
        string folder = AppPathResolver.GetFullPath(
            XeniaVersionInfo.GetXeniaVersionInfo(_game.XeniaVersion).EmulatorDir,
            "screenshots",
            _game.GameId.ToUpperInvariant());

        List<ScreenshotItemViewModel> screenshots = [];
        if (!Directory.Exists(folder))
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
                screenshots.Add(new ScreenshotItemViewModel(
                    file,
                    fileName,
                    capturedAt,
                    _game.Title,
                    new Bitmap(file)));
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
    /// Selects the first screenshot when the pane becomes active.
    /// </summary>
    public void OnPaneEntered()
    {
        SelectionHelper.SelectOnlyAt(Rows, 0);
    }

    /// <summary>
    /// Clears the screenshot selection when the pane loses focus.
    /// </summary>
    public void OnPaneExited()
    {
        SelectionHelper.ClearSelection(Rows);
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
}
