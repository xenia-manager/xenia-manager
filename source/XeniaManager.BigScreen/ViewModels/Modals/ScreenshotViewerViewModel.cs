using System;
using System.Collections.Generic;
using Avalonia.Media.Imaging;
using XeniaManager.BigScreen.Models;
using XeniaManager.BigScreen.Utilities;
using XeniaManager.BigScreen.ViewModels.Items;

namespace XeniaManager.BigScreen.ViewModels.Modals;

/// <summary>
/// Full-screen screenshot viewer modal: the current screenshot, its caption,
/// and navigation through the surrounding list. Rendered on the modal stack
/// without the modal backdrop (its own opaque backdrop covers the window).
/// </summary>
public class ScreenshotViewerViewModel(
    ScreenshotItemViewModel screenshot,
    IList<ScreenshotItemViewModel> screenshots)
    : ModalViewModelBase
{
    private ScreenshotItemViewModel _screenshot = screenshot;

    /// <summary>
    /// The screenshot image.
    /// </summary>
    public Bitmap? Image => _screenshot.Image;

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
    public bool HasPrevious => screenshots.IndexOf(_screenshot) > 0;

    /// <summary>
    /// Whether the viewer can step to the next screenshot.
    /// </summary>
    public bool HasNext => screenshots.IndexOf(_screenshot) < screenshots.Count - 1;

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
    /// Moves the viewer to the neighbouring screenshot, clamped at both ends.
    /// </summary>
    public void Step(int delta)
    {
        int index = screenshots.IndexOf(_screenshot);
        if (index < 0)
        {
            return;
        }

        int target = Math.Clamp(index + delta, 0, screenshots.Count - 1);
        if (target == index)
        {
            return;
        }

        _screenshot = screenshots[target];
        SelectionHelper.SelectOnly(screenshots, _screenshot);
        OnPropertyChanged(nameof(Image));
        OnPropertyChanged(nameof(GameTitle));
        OnPropertyChanged(nameof(CapturedAtText));
        OnPropertyChanged(nameof(HasPrevious));
        OnPropertyChanged(nameof(HasNext));
    }
}