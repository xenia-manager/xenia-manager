using System;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using XeniaManager.BigScreen.Utilities;

namespace XeniaManager.BigScreen.ViewModels.Items;

/// <summary>
/// ViewModel for a single screenshot tile in the media gallery.
/// </summary>
public partial class ScreenshotItemViewModel : ObservableObject, ISelectable
{
    /// <summary>
    /// Whether this tile currently has focus/selection in the gallery.
    /// </summary>
    [ObservableProperty] private bool _isSelected;

    /// <summary>
    /// The screenshot file name (shown in the modal as a subtitle).
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// When the screenshot was captured (file write time).
    /// </summary>
    public DateTime CapturedAt { get; }

    /// <summary>
    /// The capture date formatted for display (e.g. "5 Aug 2026, 14:30").
    /// </summary>
    public string CapturedAtText => CapturedAt.ToString("d MMM yyyy, HH:mm");

    /// <summary>
    /// The game title the screenshot belongs to, when it can be matched to the library.
    /// </summary>
    public string GameTitle { get; }

    /// <summary>
    /// The full path to the screenshot file on disk.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// The screenshot image, or null when unreadable.
    /// </summary>
    public Bitmap? Image { get; }

    /// <summary>
    /// Whether the image loaded successfully.
    /// </summary>
    public bool HasImage => Image != null;

    public ScreenshotItemViewModel(string path, string title, DateTime capturedAt, string gameTitle, Bitmap? image)
    {
        Path = path;
        Title = title;
        CapturedAt = capturedAt;
        GameTitle = gameTitle;
        Image = image;
    }
}
