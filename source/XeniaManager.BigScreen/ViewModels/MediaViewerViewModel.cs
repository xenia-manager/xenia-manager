using System;
using System.Collections.Generic;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using XeniaManager.BigScreen.Utilities;
using XeniaManager.BigScreen.ViewModels.Items;

namespace XeniaManager.BigScreen.ViewModels;

/// <summary>
/// Full-screen screenshot viewer state: the current screenshot, its caption,
/// and navigation through the media gallery.
/// </summary>
public partial class MediaViewerViewModel : ViewModelBase
{
    private readonly IList<ScreenshotItemViewModel> _screenshots;
    private ScreenshotItemViewModel _screenshot;

    public MediaViewerViewModel(ScreenshotItemViewModel screenshot, IList<ScreenshotItemViewModel> screenshots)
    {
        _screenshot = screenshot;
        _screenshots = screenshots;
    }

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
        OnPropertyChanged(nameof(Image));
        OnPropertyChanged(nameof(GameTitle));
        OnPropertyChanged(nameof(CapturedAtText));
        OnPropertyChanged(nameof(HasPrevious));
        OnPropertyChanged(nameof(HasNext));
    }
}
