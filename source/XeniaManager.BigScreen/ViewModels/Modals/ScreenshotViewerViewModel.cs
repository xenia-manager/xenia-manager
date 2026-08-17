using System;
using System.Collections.Generic;
using Avalonia.Media.Imaging;
using XeniaManager.BigScreen.Models;
using XeniaManager.BigScreen.Utilities;
using XeniaManager.BigScreen.ViewModels.Items;
using XeniaManager.Core.Logging;

namespace XeniaManager.BigScreen.ViewModels.Modals;

/// <summary>
/// Full-screen screenshot viewer modal: the current screenshot, its caption,
/// and navigation through the surrounding list. Rendered on the modal stack
/// without the modal backdrop (its own opaque backdrop covers the window).
/// The full-resolution image is decoded on open and on every step and released
/// on swap, so only one full-res bitmap is alive at a time; the card-sized
/// thumbnail serves as the fallback when decoding fails.
/// </summary>
public class ScreenshotViewerViewModel : ModalViewModelBase
{
    private readonly IList<ScreenshotItemViewModel> _screenshots;
    private ScreenshotItemViewModel _screenshot;
    private Bitmap? _fullImage;

    public ScreenshotViewerViewModel(ScreenshotItemViewModel screenshot, IList<ScreenshotItemViewModel> screenshots)
    {
        _screenshot = screenshot;
        _screenshots = screenshots;
        LoadFullImage();
    }

    /// <summary>
    /// The full-resolution screenshot image, or the thumbnail when its decode failed.
    /// </summary>
    public Bitmap? Image => _fullImage ?? _screenshot.Thumbnail;

    /// <summary>
    /// Decodes the current screenshot's full-resolution image from disk,
    /// releasing the previous one. A failed decode falls back to the thumbnail.
    /// </summary>
    private void LoadFullImage()
    {
        _fullImage?.Dispose();
        _fullImage = null;
        try
        {
            _fullImage = new Bitmap(_screenshot.Path);
        }
        catch (Exception ex)
        {
            Logger.Warning<ScreenshotViewerViewModel>($"Failed to decode screenshot '{_screenshot.Path}'");
            Logger.LogExceptionDetails<ScreenshotViewerViewModel>(ex);
        }
    }

    /// <summary>
    /// The game title the screenshot belongs to.
    /// </summary>
    public string GameTitle => _screenshot.GameTitle;

    /// <summary>
    /// The capture date formatted for display.
    /// </summary>
    public string CapturedAtText => _screenshot.CapturedAtText;

    /// <summary>
    /// Whether the viewer can step to the previous screenshot.
    /// </summary>
    public bool HasPrevious => _screenshots.IndexOf(_screenshot) > 0;

    /// <summary>
    /// Whether the viewer can step to the next screenshot.
    /// </summary>
    public bool HasNext => _screenshots.IndexOf(_screenshot) < _screenshots.Count - 1;

    /// <summary>
    /// Moves the viewer to the neighbouring screenshot, clamped at both ends.
    /// </summary>
    public void Step(int delta)
    {
        int index = _screenshots.IndexOf(_screenshot);
        if (index < 0)
        {
            return;
        }

        int target = Math.Clamp(index + delta, 0, _screenshots.Count - 1);
        if (target == index)
        {
            return;
        }

        _screenshot = _screenshots[target];
        SelectionHelper.SelectOnly(_screenshots, _screenshot);
        LoadFullImage();
        OnPropertyChanged(nameof(Image));
        OnPropertyChanged(nameof(GameTitle));
        OnPropertyChanged(nameof(CapturedAtText));
        OnPropertyChanged(nameof(HasPrevious));
        OnPropertyChanged(nameof(HasNext));
    }

    /// <summary>
    /// Handles viewer input: Left/Right step through the screenshots, Back closes.
    /// </summary>
    public override bool HandleInput(NavigationCommand command)
    {
        switch (command)
        {
            case NavigationCommand.MoveLeft:
                Step(-1);
                return true;
            case NavigationCommand.MoveRight:
                Step(1);
                return true;
            case NavigationCommand.Back:
                Close();
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Releases the decoded full-resolution image (the thumbnails are owned
    /// by the gallery/pane collections, not the viewer).
    /// </summary>
    public override void Dispose()
    {
        _fullImage?.Dispose();
        _fullImage = null;
        base.Dispose();
    }
}